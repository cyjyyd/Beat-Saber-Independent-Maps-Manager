using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BeatSaberIndependentMapsManager;
using BeatSaberIndependentMapsManager.BeatSpiderSharp;
using BeatSaberIndependentMapsManager.ViewModels;
using BeatSaberIndependentMapsManager.Services;
using BSIMM.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BSIMM.Avalonia.Views;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext!;

    private DispatcherTimer _audioTimer;

    public MainWindow()
    {
        InitializeComponent();
        
        _audioTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _audioTimer.Tick += OnAudioTimerTick;
    }

    private void OnAudioTimerTick(object? sender, EventArgs e)
    {
        var vm = ViewModel;
        var progressBar = this.FindControl<ProgressBar>("AudioProgressBar");
        if (progressBar == null || vm == null || vm.SelectedSong == null) return;

        if (vm.AudioPreview.IsPlaying)
        {
            var totalPreview = vm.SelectedSong._previewDuration > 0 ? vm.SelectedSong._previewDuration : vm.AudioPreview.TotalTime.TotalSeconds;
            // The reader's CurrentTime includes the previewStartTime because we seeked it directly.
            var elapsed = vm.AudioPreview.CurrentTime.TotalSeconds - (vm.SelectedSong._previewDuration > 0 ? vm.SelectedSong._previewStartTime : 0);
            
            // Calculate percentage
            if (totalPreview > 0)
            {
                progressBar.Value = Math.Max(0, Math.Min(100, (elapsed / totalPreview) * 100));
            }
        }
        else if (vm.AudioPreview.IsStopped)
        {
            progressBar.Value = 0;
            _audioTimer.Stop();
        }
    }

    private void OnThemeSystemClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current != null)
            Application.Current.RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Default;
    }

    private void OnThemeLightClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current != null)
            Application.Current.RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Light;
    }

    private void OnThemeDarkClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current != null)
            Application.Current.RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Dark;
    }

    private async void OnAddFolderClick(object? sender, RoutedEventArgs e)
    {
        // 1. Direct and clean access to StorageProvider on the UI thread (MainWindow inherits from TopLevel)
        var folders = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "选择歌曲文件夹/曲包文件夹",
            AllowMultiple = false
        });

        if (folders == null || folders.Count == 0) return;

        // 2. Extract path on the UI thread (in case the platform storage item has thread-affinity)
        string? folderPath = null;
        if (Dispatcher.UIThread.CheckAccess())
        {
            folderPath = folders[0].Path.LocalPath;
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                folderPath = folders[0].Path.LocalPath;
            });
        }

        if (string.IsNullOrEmpty(folderPath)) return;

        // CRITICAL FIX: Capture ViewModel on the UI thread before jumping to Task.Run
        // Accessing this.DataContext (which ViewModel property does) on a background thread throws!
        var vm = ViewModel;

        // 3. Update status UI (guaranteed on UI thread)
        if (Dispatcher.UIThread.CheckAccess())
        {
            vm.ActionText = "解析：";
            vm.StatusText = $"正在解析：{folderPath}";
            vm.ProgressValue = 0;
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                vm.ActionText = "解析：";
                vm.StatusText = $"正在解析：{folderPath}";
                vm.ProgressValue = 0;
            });
        }

            try
            {
                // 4. Heavy parsing work on background thread (using the captured 'vm' variable)
                int lastProgress = -1;
                var result = await Task.Run(() => vm.SongScanner.ScanFolder(folderPath, progress => 
                {
                    if (progress != lastProgress)
                    {
                        lastProgress = progress;
                        Dispatcher.UIThread.Post(() => vm.ProgressValue = progress);
                    }
                }));

                // 5. Update ViewModel with results (guaranteed on UI thread)
                if (Dispatcher.UIThread.CheckAccess())
                {
                    vm.ApplyScanResult(result);
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() => vm.ApplyScanResult(result));
                }

                // 6. Automatically generate hash cache if enabled (this is where real-time progress happens)
                var config = new Config();
                if (config.HashCache && result.ResultType == ScanResultType.MusicPack && !string.IsNullOrEmpty(result.MusicPackName))
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        vm.ActionText = "缓存：";
                        vm.StatusText = $"正在缓存曲包哈希: {result.MusicPackName}";
                        vm.ProgressValue = 0;
                    });
                    
                    if (vm.MusicPackInfo.TryGetValue(result.MusicPackName, out var packSongs))
                    {
                        await vm.HashCache.EnsurePackHashesAsync(packSongs, pct => 
                        {
                            Dispatcher.UIThread.Post(() => vm.ProgressValue = pct);
                        });
                        vm.HashCache.SaveCache();
                    }
                }
                else if (config.HashCache && result.ResultType == ScanResultType.Multiple)
                {
                    // Cache all packs
                    foreach (var packName in vm.GetMusicPackNames())
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            vm.ActionText = "缓存：";
                            vm.StatusText = $"正在缓存曲包哈希: {packName}";
                            vm.ProgressValue = 0;
                        });
                        if (vm.MusicPackInfo.TryGetValue(packName, out var packSongs))
                        {
                            await vm.HashCache.EnsurePackHashesAsync(packSongs, pct => 
                            {
                                Dispatcher.UIThread.Post(() => vm.ProgressValue = pct);
                            });
                        }
                    }
                    vm.HashCache.SaveCache();
                }

                if (Dispatcher.UIThread.CheckAccess())
                {
                    vm.ActionText = "解析：";
                    vm.StatusText = $"已成功加载 {result.MapsCount} 首歌曲";
                    vm.ProgressValue = 100;
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        vm.ActionText = "解析：";
                        vm.StatusText = $"已成功加载 {result.MapsCount} 首歌曲";
                        vm.ProgressValue = 100;
                    });
                }
            }
        catch (Exception ex)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                vm.ActionText = "解析：";
                vm.StatusText = $"解析失败: {ex.Message}";
                vm.ProgressValue = 100;
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() => 
                {
                    vm.ActionText = "解析：";
                    vm.StatusText = $"解析失败: {ex.Message}";
                    vm.ProgressValue = 100;
                });
            }
        }
    }

    private async void OnChangePackCoverClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        var pack = vm.SelectedMusicPack;
        if (string.IsNullOrEmpty(pack)) return;

        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择曲包封面",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("图片文件") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp" } } }
        });

        if (files != null && files.Count > 0)
        {
            string sourcePath = files[0].Path.LocalPath;
            string packPath;
            lock (vm.SyncRoot)
            {
                if (!vm.MusicPackPath.TryGetValue(pack, out packPath)) return;
            }

            string targetPath = Path.Combine(packPath, "cover.jpg");
            try
            {
                // Convert or copy the image to cover.jpg
                // To support multiple formats and ensure it's saved as jpeg (since the system relies on cover.jpg)
                using (var bitmap = new System.Drawing.Bitmap(sourcePath))
                {
                    // If target exists, deleting it might fail if locked, but PathToBitmapConverter uses MemoryStream so it shouldn't be locked
                    bitmap.Save(targetPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                // Force UI update by triggering property changed
                // (Since the path is the same, just toggling it to null and back will refresh)
                vm.MusicPackCoverPath = null;
                vm.MusicPackCoverPath = targetPath;
                
                vm.ActionText = "封面：";
                vm.StatusText = "更换封面成功";
                vm.ProgressValue = 100;
            }
            catch (Exception ex)
            {
                vm.ActionText = "封面：";
                vm.StatusText = $"更换封面失败: {ex.Message}";
                vm.ProgressValue = 100;
            }
        }
    }
    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnSaveToSongCoreClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        int count = vm.SaveSongCoreFolders();
        vm.ActionText = "同步：";
        vm.StatusText = count > 0 ? $"已成功将目录信息同步至 {count} 个游戏实例！" : "没有检测到需要同步的游戏实例或无曲包";
        vm.ProgressValue = 100;
    }

    private async void OnExportSelectedBplistClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        var currentPack = vm.SelectedMusicPack;
        if (string.IsNullOrEmpty(currentPack))
        {
            vm.ActionText = "提示：";
            vm.StatusText = "请先在列表中选中要导出的曲包！";
            vm.ProgressValue = 100;
            return;
        }

        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;
        var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "选择要保存歌单的文件夹", AllowMultiple = false });
        if (folder == null || folder.Count == 0) return;
        string path = folder[0].Path.LocalPath;

        vm.ActionText = "导出：";
        vm.StatusText = $"正在导出 {currentPack}...";
        vm.ProgressValue = 0;

        try
        {
            var progress = new Progress<int>(pct => Dispatcher.UIThread.Post(() => vm.ProgressValue = pct));
            await vm.ExportMusicPackAsync(currentPack, path, progress);
            vm.StatusText = $"歌单 {currentPack} 导出成功！";
            vm.ProgressValue = 100;
        }
        catch (Exception ex)
        {
            vm.StatusText = $"导出失败：{ex.Message}";
            vm.ProgressValue = 100;
        }
    }

    private async void OnExportAllBplistClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (vm.MusicPacks.Count == 0) return;

        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;
        var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "选择要保存歌单的文件夹", AllowMultiple = false });
        if (folder == null || folder.Count == 0) return;
        string path = folder[0].Path.LocalPath;

        vm.ActionText = "导出：";
        vm.StatusText = "正在批量导出所有歌单...";
        vm.ProgressValue = 0;

        try
        {
            await vm.ExportAllMusicPacksAsync(path, (name, pct) => 
            {
                Dispatcher.UIThread.Post(() => 
                {
                    vm.StatusText = $"正在导出：{name}...";
                    vm.ProgressValue = pct;
                });
            });
            vm.StatusText = "所有歌单导出完成！";
            vm.ProgressValue = 100;
        }
        catch (Exception ex)
        {
            vm.StatusText = $"导出失败：{ex.Message}";
            vm.ProgressValue = 100;
        }
    }

    private async void OnDeduplicationClick(object? sender, RoutedEventArgs e)
    {
        // Capture ViewModel on UI thread
        var vm = ViewModel;
        
        var currentPack = vm.SelectedMusicPack;
        if (string.IsNullOrEmpty(currentPack))
        {
            vm.ActionText = "提示";
            vm.StatusText = "请先选择要去重的曲包！";
            vm.ProgressValue = 100;
            return;
        }

        vm.ActionText = "去重：";
        vm.StatusText = "正在进行一键去重...";
        vm.ProgressValue = 0;

        await Task.Run(() =>
        {
            int deletedCount = 0;

            // Using captured 'vm' safely on background thread (because it's not a UI element, just a plain C# object)
            lock (vm.SyncRoot)
            {
                if (!vm.MusicPackInfo.TryGetValue(currentPack, out var songDict)) return;

                var bsrList = songDict.Keys.ToList();

                foreach (var bsr in bsrList)
                {
                    if (IsDuplicate(bsr))
                    {
                        var song = songDict[bsr];
                        if (song != null && !string.IsNullOrEmpty(song.songFolder))
                        {
                            try
                            {
                                if (Directory.Exists(song.songFolder))
                                {
                                    Directory.Delete(song.songFolder, true);
                                    deletedCount++;
                                }
                                songDict.Remove(bsr);
                            }
                            catch (Exception) { }
                        }
                    }
                }
            }

            Dispatcher.UIThread.Post(() =>
            {
                // UI update MUST use the captured 'vm' to avoid triggering DataContext getter on background thread?
                // Actually Post runs on UI thread so it's safe to use ViewModel, but using 'vm' is consistent and perfectly safe.
                var selected = vm.SelectedMusicPack;
                vm.SelectedMusicPack = null!;
                vm.SelectedMusicPack = selected;

                vm.ActionText = "去重：";
                vm.StatusText = $"一键去重完成，删除了 {deletedCount} 个重复歌曲文件夹";
                vm.ProgressValue = 100;
            });
        });
    }

    private void OnPlayPreviewClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        var song = vm.SelectedSong;
        if (song != null && !string.IsNullOrEmpty(song.songFolder) && !string.IsNullOrEmpty(song._songFilename))
        {
            if (vm.AudioPreview.TryPlay(song.songFolder, song._songFilename, song._previewStartTime, song._previewDuration))
            {
                _audioTimer.Start();
            }
        }
    }

    private void OnPausePreviewClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (vm.AudioPreview.IsPlaying)
            vm.AudioPreview.Pause();
        else if (vm.AudioPreview.IsPaused)
            vm.AudioPreview.Resume();
    }

    private async void OnFullScanClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        
        vm.ActionText = "全盘扫描：";
        vm.StatusText = "正在搜索本地硬盘...";
        vm.ProgressValue = 0;
        vm.DelicatedSongs.Clear();

        try
        {
            var results = await vm.ScanFullDiskAsync(Enumerable.Empty<string>(), progress =>
            {
                // progress callback might be called from background thread, so dispatch safely
                Dispatcher.UIThread.Post(() => vm.ProgressValue = progress);
            });

            if (Dispatcher.UIThread.CheckAccess())
            {
                vm.ActionText = "全盘扫描：";
                vm.StatusText = $"全盘扫描完成，发现 {results.Count} 首歌曲";
                vm.ProgressValue = 100;
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    vm.ActionText = "全盘扫描：";
                    vm.StatusText = $"全盘扫描完成，发现 {results.Count} 首歌曲";
                    vm.ProgressValue = 100;
                });
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Everything"))
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                vm.ActionText = "全盘扫描：";
                vm.StatusText = "扫描已取消";
                vm.ProgressValue = 100;
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    vm.ActionText = "全盘扫描：";
                    vm.StatusText = "扫描已取消";
                    vm.ProgressValue = 100;
                });
            }
            await ShowAlertDialogAsync("全盘搜索需要 Everything 支持", ex.Message);
        }
        catch (Exception ex)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                vm.ActionText = "全盘扫描：";
                vm.StatusText = $"扫描失败: {ex.Message}";
                vm.ProgressValue = 100;
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    vm.ActionText = "全盘扫描：";
                    vm.StatusText = $"扫描失败: {ex.Message}";
                    vm.ProgressValue = 100;
                });
            }
        }
    }

    private FilterPreset? _currentFilterPreset;
    private List<BeatSaverMap> _currentSearchResults = new();
    private System.Threading.CancellationTokenSource? _searchCts;
    private int _currentPage = 0;
    private int _totalPages = 0;
    private int _totalResults = 0;
    private BeatSaverMap? _selectedBsMap;
    private static readonly CoverImageCacheService _coverCache = new();
    private CancellationTokenSource? _coverLoadCts;
    private DispatcherTimer? _bsAudioTimer;

    private async void OnOpenFilterBuilderClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        var window = new FilterBuilderWindow(_currentFilterPreset);
        window.SearchRequested += async (s, preset) =>
        {
            _currentFilterPreset = preset;
            _currentPage = 0;
            await ExecuteSearch(vm, preset, 0);
        };
        await window.ShowDialog(this);
    }

    private async void OnOpenPresetEditorClick(object? sender, RoutedEventArgs e)
    {
        var window = new PresetEditorWindow(_currentFilterPreset);
        window.SearchRequested += async (_, bssPreset) =>
        {
            var bsfPreset = BsfToPresetConverter.ConvertBack(bssPreset);
            _currentFilterPreset = bsfPreset;
            _currentPage = 0;
            await ExecuteSearch(ViewModel, bsfPreset, 0);
        };
        await window.ShowDialog(this);
    }

    private async Task PrefetchCoversAsync(List<BeatSaverMap> results, CancellationToken ct = default)
    {
        foreach (var map in results)
        {
            if (ct.IsCancellationRequested) break;
            var coverUrl = map.GetCoverUrl();
            if (string.IsNullOrEmpty(coverUrl)) continue;
            await _coverCache.GetCoverAsync(coverUrl, ct);
        }
    }

    private async Task ExecuteSearch(MainViewModel vm, FilterPreset preset, int page)
    {
        _searchCts?.Cancel();
        _searchCts = new System.Threading.CancellationTokenSource();
        var searchToken = _searchCts.Token;

        // Cancel any ongoing cover pre-fetch from previous search
        _coverLoadCts?.Cancel();
        _coverLoadCts = new CancellationTokenSource();

        vm.ActionText = "搜索：";
        vm.StatusText = page == 0 ? "正在搜索BeatSaver..." : $"正在加载第 {page + 1} 页...";
        vm.ProgressValue = 0;

        try
        {
            var searchResultsList = this.FindControl<ListBox>("SearchResultsList");
            if (searchResultsList == null) return;

            bool requiresCache = vm.BeatSaverSearch.RequiresLocalCache(preset);
            if (requiresCache)
            {
                // Use local cache filtering
                vm.StatusText = "正在使用本地缓存筛选...";
                
                if (!vm.LocalCache.IsCacheAvailable)
                {
                    vm.StatusText = "本地缓存不可用，请先在设置中下载缓存";
                    vm.ProgressValue = 100;
                    // Offer to download cache
                    bool download = await ShowConfirmDialogAsync("下载缓存", "本地缓存未下载。缓存文件约230MB，是否立即下载？\n\n下载后可使用更丰富的筛选条件（排行榜收录、统计数据等）");
                    if (download)
                    {
                        await DownloadCacheWithProgressAsync(vm);
                        if (vm.LocalCache.IsCacheAvailable)
                        {
                            vm.StatusText = "缓存下载完成，正在筛选...";
                            // Retry the filter now that cache is available
                            var retryResults = await Task.Run(() => vm.LocalCache.ParallelFilterMaps(preset, new Progress<int>(pct =>
                            {
                                Dispatcher.UIThread.Post(() => vm.ProgressValue = pct);
                            })));
                            _totalResults = retryResults.Count;
                            _totalPages = (_totalResults + 20 - 1) / 20;
                            _currentPage = page;
                            int startIdx = page * 20;
                            int endIdx = Math.Min(startIdx + 20, _totalResults);
                            _currentSearchResults = retryResults.Skip(startIdx).Take(20).ToList();
                            searchResultsList.ItemsSource = _currentSearchResults;
                            UpdatePaginationControls();
                            vm.StatusText = $"本地缓存筛选完成：共 {_totalResults} 条，当前第 {_currentPage + 1}/{_totalPages} 页";
                            _ = PrefetchCoversAsync(_currentSearchResults, _coverLoadCts.Token);
                        }
                    }
                    return;
                }

                var allResults = await Task.Run(() => vm.LocalCache.ParallelFilterMaps(preset, new Progress<int>(pct =>
                {
                    Dispatcher.UIThread.Post(() => vm.ProgressValue = pct);
                })));

                // Discard results if a newer search has started
                if (searchToken.IsCancellationRequested) return;

                // Apply client-side pagination (20 per page)
                int pageSize = 20;
                _totalResults = allResults.Count;
                _totalPages = (_totalResults + pageSize - 1) / pageSize;
                _currentPage = page;

                int startIndex = page * pageSize;
                int endIndex = Math.Min(startIndex + pageSize, _totalResults);
                _currentSearchResults = allResults.Skip(startIndex).Take(pageSize).ToList();

                searchResultsList.ItemsSource = _currentSearchResults;
                UpdatePaginationControls();

                vm.StatusText = $"本地缓存筛选完成：共 {_totalResults} 条，当前第 {_currentPage + 1}/{_totalPages} 页";
                vm.ProgressValue = 100;
                
                // Pre-fetch cover images in background (fire-and-forget, cancelled on next search)
                _ = PrefetchCoversAsync(_currentSearchResults, _coverLoadCts.Token);
                return;
            }

            // Online API search
            var filter = vm.BeatSaverSearch.BuildSearchFilterFromPreset(preset);
            var response = await vm.BeatSaverClient.SearchMapsAsync(filter, page);
            
            // Discard results if a newer search has started
            if (searchToken.IsCancellationRequested) return;
            
            _currentSearchResults = response?.Maps ?? new List<BeatSaverMap>();
            _currentPage = page;
            
            // Get pagination info
            if (response?.Info != null && response.Info.Pages > 0)
            {
                _totalPages = response.Info.Pages;
                _totalResults = response.Info.Total;
            }
            else if (response?.Metadata != null && response.Metadata.PageSize > 0)
            {
                _totalResults = response.Metadata.Total;
                _totalPages = (_totalResults + response.Metadata.PageSize - 1) / response.Metadata.PageSize;
            }
            else
            {
                _totalPages = 1;
                _totalResults = _currentSearchResults.Count;
            }

            searchResultsList.ItemsSource = _currentSearchResults;
            UpdatePaginationControls();

            vm.StatusText = $"找到 {_totalResults} 个结果，当前第 {_currentPage + 1}/{_totalPages} 页";
            vm.ProgressValue = 100;
            
            // Pre-fetch cover images in background (fire-and-forget, cancelled on next search)
            _ = PrefetchCoversAsync(_currentSearchResults, _coverLoadCts.Token);
        }
        catch (OperationCanceledException)
        {
            vm.StatusText = "搜索已取消";
            vm.ProgressValue = 100;
        }
        catch (Exception ex)
        {
            vm.StatusText = $"搜索失败: {ex.Message}";
            vm.ProgressValue = 100;
        }
    }

    private void UpdatePaginationControls()
    {
        var btnPrev = this.FindControl<Button>("BtnPrevPage");
        var btnNext = this.FindControl<Button>("BtnNextPage");
        var lblPage = this.FindControl<TextBlock>("LblPageInfo");
        if (btnPrev == null || btnNext == null || lblPage == null) return;

        btnPrev.IsEnabled = _currentPage > 0;
        btnNext.IsEnabled = _currentPage < _totalPages - 1;
        lblPage.Text = $"第 {_currentPage + 1}/{_totalPages} 页 (共 {_totalResults} 条)";
    }

    private async void OnPrevPageClick(object? sender, RoutedEventArgs e)
    {
        if (_currentPage > 0)
        {
            _currentPage--;
            await ExecuteSearch(ViewModel, _currentFilterPreset!, _currentPage);
        }
    }

    private async void OnNextPageClick(object? sender, RoutedEventArgs e)
    {
        if (_currentPage < _totalPages - 1)
        {
            _currentPage++;
            await ExecuteSearch(ViewModel, _currentFilterPreset!, _currentPage);
        }
    }

    private void OnSearchResultSelected(object? sender, SelectionChangedEventArgs e)
    {
        var listBox = sender as ListBox;
        if (listBox?.SelectedItem is BeatSaverMap map)
        {
            _selectedBsMap = map;
            UpdateBeatSaverDetailPanel(map);
        }
        else
        {
            var panel = this.FindControl<Grid>("BeatSaverDetailPanel");
            if (panel != null) panel.IsVisible = false;
        }
    }

    private async void UpdateBeatSaverDetailPanel(BeatSaverMap map)
    {
        var panel = this.FindControl<Grid>("BeatSaverDetailPanel");
        if (panel == null) return;
        panel.IsVisible = true;

        // Set text fields
        this.FindControl<TextBlock>("BsSongName")!.Text = map.Name ?? "未知";
        this.FindControl<TextBlock>("BsSongAuthor")!.Text = map.Metadata?.SongAuthorName ?? "";
        this.FindControl<TextBlock>("BsBsr")!.Text = map.Id ?? "";
        this.FindControl<TextBlock>("BsMapper")!.Text = map.Metadata?.LevelAuthorName ?? map.Uploader?.Name ?? "";
        this.FindControl<TextBlock>("BsBpm")!.Text = (map.Metadata?.Bpm ?? 0).ToString("0.##");
        this.FindControl<TextBlock>("BsDuration")!.Text = (map.Metadata?.Duration ?? 0).ToString("0") + "s";
        this.FindControl<TextBlock>("BsPlays")!.Text = (map.Stats?.Plays ?? 0).ToString();
        this.FindControl<TextBlock>("BsUpvotes")!.Text = (map.Stats?.Upvotes ?? 0).ToString();
        this.FindControl<TextBlock>("BsDownloads")!.Text = (map.Stats?.Downloads ?? 0).ToString();
        this.FindControl<TextBlock>("BsUploadedDate")!.Text = map.CreatedAt.ToString("yyyy-MM-dd");
        this.FindControl<TextBlock>("BsAi")!.Text = map.Automapper ? "是" : "否";

        // Difficulties
        var diffs = new System.Text.StringBuilder();
        if (map.Versions != null && map.Versions.Count > 0 && map.Versions[0].Diffs != null)
        {
            foreach (var d in map.Versions[0].Diffs)
            {
                diffs.Append($"{d.Characteristic}/{d.Difficulty} ");
            }
        }
        this.FindControl<TextBlock>("BsDifficulties")!.Text = diffs.ToString().Trim();

        // Load cover image from cache service (with retry, never throws)
        var coverImage = this.FindControl<Image>("BsCoverImage");
        if (coverImage != null)
        {
            coverImage.Source = null;
            var coverUrl = map.GetCoverUrl();
            if (!string.IsNullOrEmpty(coverUrl))
            {
                var bitmap = await _coverCache.GetCoverAsync(coverUrl);
                // Only apply cover if the selection hasn't changed
                if (bitmap != null && _selectedBsMap == map)
                {
                    coverImage.Source = bitmap;
                }
            }
        }
    }

    private async void OnBsPlayPreviewClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (_selectedBsMap == null) return;

        // Use the actual previewURL from BeatSaver API (format: https://cfcdn.beatsaver.com/{hash}.mp3)
        string? previewUrl = _selectedBsMap.GetPreviewUrl();
        if (string.IsNullOrEmpty(previewUrl))
        {
            vm.ActionText = "试听：";
            vm.StatusText = "该曲目不提供在线预览音频";
            vm.ProgressValue = 100;
            return;
        }
        
        vm.ActionText = "试听：";
        vm.StatusText = $"正在加载在线预览: {_selectedBsMap.Name}";
        vm.ProgressValue = 0;

        try
        {
            // Download to temp file with retry
            string? tempFile = await _coverCache.DownloadToTempFileAsync(previewUrl, ".mp3");
            if (tempFile == null)
            {
                vm.StatusText = "在线预览加载失败，网络连接异常";
                vm.ProgressValue = 100;
                return;
            }

            // Play on background thread
            string fileToPlay = tempFile;
            bool success = await Task.Run(() =>
            {
                try
                {
                    vm.AudioPreview.PlayLocalFile(fileToPlay);
                    return true;
                }
                catch
                {
                    try { if (File.Exists(fileToPlay)) File.Delete(fileToPlay); } catch { }
                    return false;
                }
            });

            if (success)
            {
                vm.StatusText = $"正在播放: {_selectedBsMap.Name}";
                
                _bsAudioTimer?.Stop();
                _bsAudioTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                _bsAudioTimer.Tick += (s, args) =>
                {
                    var progressBar = this.FindControl<ProgressBar>("BsAudioProgressBar");
                    if (progressBar == null) return;
                    if (vm.AudioPreview.IsPlaying)
                    {
                        var total = vm.AudioPreview.TotalTime.TotalSeconds;
                        var current = vm.AudioPreview.CurrentTime.TotalSeconds;
                        if (total > 0)
                            progressBar.Value = Math.Min(100, (current / total) * 100);
                    }
                    else if (vm.AudioPreview.IsStopped)
                    {
                        progressBar.Value = 0;
                        _bsAudioTimer?.Stop();
                    }
                };
                _bsAudioTimer.Start();
            }
            else
            {
                vm.StatusText = "在线预览播放失败，无法解析音频格式";
            }
        }
        catch (Exception ex)
        {
            vm.StatusText = $"在线预览加载失败: {ex.Message}";
        }
        vm.ProgressValue = 100;
    }

    private void OnBsPausePreviewClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (vm.AudioPreview.IsPlaying)
            vm.AudioPreview.Pause();
        else if (vm.AudioPreview.IsPaused)
            vm.AudioPreview.Resume();
    }

    private async void OnBsMapPreviewClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (_selectedBsMap == null) return;

        string? downloadUrl = _selectedBsMap.GetDownloadUrl();
        string? mapId = _selectedBsMap.Id;
        if (string.IsNullOrEmpty(downloadUrl) && string.IsNullOrEmpty(mapId))
        {
            vm.ActionText = "提示：";
            vm.StatusText = "该曲目不提供下载，无法预览谱面";
            return;
        }

        vm.ActionText = "预览：";
        vm.StatusText = $"正在加载谱面预览: {_selectedBsMap.Name}";
        vm.ProgressValue = 0;

        var previewWindow = new MapPreviewWindow();
        await previewWindow.LoadMapAsync(downloadUrl ?? "", _selectedBsMap.Name, mapId);
        await previewWindow.ShowDialog(this);

        vm.StatusText = $"谱面预览已关闭: {_selectedBsMap.Name}";
        vm.ProgressValue = 100;
    }

    private async void OnLocalMapPreviewClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (vm.SelectedSong == null || string.IsNullOrEmpty(vm.SelectedSong.songFolder))
        {
            vm.ActionText = "提示：";
            vm.StatusText = "请先选择一首歌曲";
            return;
        }

        if (!Directory.Exists(vm.SelectedSong.songFolder))
        {
            vm.ActionText = "提示：";
            vm.StatusText = "歌曲文件夹不存在";
            return;
        }

        var previewWindow = new MapPreviewWindow();
        await previewWindow.LoadLocalMapAsync(vm.SelectedSong.songFolder, vm.SelectedSong._songName);
        await previewWindow.ShowDialog(this);
    }

    private async void OnDelicatedMapPreviewClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (string.IsNullOrEmpty(vm.SelectedDelicatedSong))
        {
            vm.ActionText = "提示：";
            vm.StatusText = "请先选择一首歌曲";
            return;
        }

        if (!vm.DelicatedSongList.TryGetValue(vm.SelectedDelicatedSong, out var song) || song == null)
        {
            vm.ActionText = "提示：";
            vm.StatusText = "未找到歌曲信息";
            return;
        }

        if (!Directory.Exists(song.songFolder))
        {
            vm.ActionText = "提示：";
            vm.StatusText = "歌曲文件夹不存在";
            return;
        }

        var previewWindow = new MapPreviewWindow();
        await previewWindow.LoadLocalMapAsync(song.songFolder, song._songName);
        await previewWindow.ShowDialog(this);
    }

    private async void OnExportSearchResultsClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (_currentFilterPreset == null || _totalResults == 0)
        {
            vm.ActionText = "提示：";
            vm.StatusText = "没有搜索结果可导出！";
            vm.ProgressValue = 100;
            return;
        }

        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存歌单",
            DefaultExtension = "bplist",
            FileTypeChoices = new[] { new FilePickerFileType("Beat Saber Playlist") { Patterns = new[] { "*.bplist" } } }
        });

        if (file != null)
        {
            string path = file.Path.LocalPath;
            vm.ActionText = "导出：";
            vm.StatusText = $"正在获取全部搜索结果（共 {_totalResults} 条）...";
            vm.ProgressValue = 0;

            try
            {
                // Fetch ALL results (not just current page) using the current filter preset
                List<BeatSaverMap> allMaps;
                if (vm.BeatSaverSearch.RequiresLocalCache(_currentFilterPreset))
                {
                    // Local cache path: re-filter to get all results
                    if (!vm.LocalCache.IsCacheAvailable)
                    {
                        vm.StatusText = "本地缓存不可用，请先在设置中下载缓存";
                        vm.ProgressValue = 100;
                        return;
                    }
                    allMaps = await Task.Run(() => vm.LocalCache.ParallelFilterMaps(_currentFilterPreset, new Progress<int>(pct =>
                    {
                        Dispatcher.UIThread.Post(() => { vm.ProgressValue = pct / 2; vm.StatusText = $"正在筛选全部结果... {pct}%"; });
                    })));
                }
                else
                {
                    // Online API path: fetch all pages
                    allMaps = await vm.BeatSaverSearch.FetchAllMapsForPresetAsync(_currentFilterPreset, useSharedCache: false, pct =>
                    {
                        Dispatcher.UIThread.Post(() => { vm.ProgressValue = pct / 2; vm.StatusText = $"正在获取全部页面... {pct}%"; });
                    });
                }

                vm.StatusText = $"正在导出歌单（共 {allMaps.Count} 首）...";
                vm.ProgressValue = 60;

                string coverText = PlaylistExportService.ExtractCoverTextFromPresetName(_currentFilterPreset.Name);
                await Task.Run(() =>
                {
                    vm.PlaylistExporter.ExportMapsToPlaylist(allMaps, path, _currentFilterPreset.Name, coverText);
                });

                vm.StatusText = $"歌单已保存！共 {allMaps.Count} 首歌曲";
                vm.ProgressValue = 100;
            }
            catch (Exception ex)
            {
                vm.StatusText = $"导出失败: {ex.Message}";
                vm.ProgressValue = 100;
            }
        }
    }

    private static bool IsDuplicate(string str)
    {
        return Regex.IsMatch(str, @"\[([0-9]+)\]$");
    }

    private async Task<bool> ShowConfirmDialogAsync(string title, string message)
    {
        var bgBrush = this.TryFindResource("AppBackgroundBrush", out var bg) && bg is IBrush ib ? ib : Brush.Parse("#F4F7FC");
        var textBrush = this.TryFindResource("TextPrimaryBrush", out var fg) && fg is IBrush fb ? fb : Brush.Parse("#1A1E24");
        
        var bsBlueBg = this.TryFindResource("BsBlueBgBrush", out var bBg) && bBg is IBrush ibBg ? ibBg : Brush.Parse("#E0F2FE");
        var bsBlueBorder = this.TryFindResource("BsBlueBorderBrush", out var bBd) && bBd is IBrush ibBd ? ibBd : Brush.Parse("#0078D4");
        var bsBlueText = this.TryFindResource("BsBlueTextBrush", out var bTx) && bTx is IBrush ibTx ? ibTx : Brush.Parse("#005A9E");
        
        var btnBg = this.TryFindResource("ButtonBackgroundBrush", out var nBg) && nBg is IBrush inBg ? inBg : Brushes.White;
        var borderBrush = this.TryFindResource("BorderBrush", out var nBd) && nBd is IBrush inBd ? inBd : Brush.Parse("#D5DDEB");

        var dlg = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Background = bgBrush,
            Foreground = textBrush
        };
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 16 };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Foreground = textBrush });
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10, Margin = new Thickness(0, 10, 0, 0) };
        
        var btnNo = new Button { 
            Content = "否", 
            Width = 70, 
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Background = btnBg,
            Foreground = textBrush,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(0, 8)
        };
        var btnYes = new Button { 
            Content = "是", 
            Width = 70, 
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Background = bsBlueBg,
            Foreground = bsBlueText,
            BorderBrush = bsBlueBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(0, 8)
        };
        
        btnPanel.Children.Add(btnNo);
        btnPanel.Children.Add(btnYes);
        panel.Children.Add(btnPanel);
        dlg.Content = panel;

        btnYes.Click += (s, e) => Dispatcher.UIThread.Post(() => dlg.Close(true));
        btnNo.Click += (s, e) => Dispatcher.UIThread.Post(() => dlg.Close(false));

        return await dlg.ShowDialog<bool>(this);
    }

    private async Task ShowAlertDialogAsync(string title, string message)
    {
        var bgBrush = this.TryFindResource("AppBackgroundBrush", out var bg) && bg is IBrush ib ? ib : Brush.Parse("#F4F7FC");
        var textBrush = this.TryFindResource("TextPrimaryBrush", out var fg) && fg is IBrush fb ? fb : Brush.Parse("#1A1E24");
        
        var bsBlueBg = this.TryFindResource("BsBlueBgBrush", out var bBg) && bBg is IBrush ibBg ? ibBg : Brush.Parse("#E0F2FE");
        var bsBlueBorder = this.TryFindResource("BsBlueBorderBrush", out var bBd) && bBd is IBrush ibBd ? ibBd : Brush.Parse("#0078D4");
        var bsBlueText = this.TryFindResource("BsBlueTextBrush", out var bTx) && bTx is IBrush ibTx ? ibTx : Brush.Parse("#005A9E");

        var dlg = new Window
        {
            Title = title,
            Width = 450,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Background = bgBrush,
            Foreground = textBrush
        };
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 16 };
        var textBox = new TextBox 
        { 
            Text = message, 
            TextWrapping = TextWrapping.Wrap, 
            Foreground = textBrush,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            IsReadOnly = true,
            Padding = new Thickness(0)
        };
        panel.Children.Add(textBox);
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10, Margin = new Thickness(0, 10, 0, 0) };
        
        var btnOk = new Button { 
            Content = "确定", 
            Width = 70, 
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Background = bsBlueBg,
            Foreground = bsBlueText,
            BorderBrush = bsBlueBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(0, 8)
        };
        
        btnPanel.Children.Add(btnOk);
        panel.Children.Add(btnPanel);
        dlg.Content = panel;

        btnOk.Click += (s, e) => Dispatcher.UIThread.Post(() => dlg.Close());

        await dlg.ShowDialog(this);
    }

    private async Task DownloadCacheWithProgressAsync(MainViewModel vm)
    {
        vm.ActionText = "下载：";
        vm.StatusText = "正在下载本地缓存...";
        vm.ProgressValue = 0;

        EventHandler<CacheDownloadProgress> handler = (s, p) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                vm.ProgressValue = (int)p.Percentage;
                vm.StatusText = p.Status != null && p.Status.StartsWith("正在下载")
                    ? $"{p.Status} {p.Percentage:F1}%"
                    : (p.Status ?? "下载中...");
            });
        };

        vm.LocalCache.DownloadProgress += handler;
        try
        {
            await vm.LocalCache.DownloadCacheAsync();
        }
        finally
        {
            vm.LocalCache.DownloadProgress -= handler;
        }
        vm.ProgressValue = 100;
    }

    private async void OnBatchExportClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        var batchWindow = new BatchExportWindow();
        await batchWindow.ShowDialog(this);

        if (batchWindow.SelectedPresets.Count == 0) return;

        await BatchExportPresetsAsync(vm, batchWindow.SelectedPresets, batchWindow.OutputDirectory);
    }

    private async Task BatchExportPresetsAsync(MainViewModel vm, List<FilterPreset> presets, string outputDir)
    {
        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

        vm.ActionText = "批处理：";
        vm.StatusText = $"正在批处理导出 {presets.Count} 个预设...";
        vm.ProgressValue = 0;

        int successCount = 0, failCount = 0, zeroResultCount = 0;

        // Check if all presets require local cache (fast path)
        bool allRequireLocalCache = presets.All(p => vm.BeatSaverSearch.RequiresLocalCache(p));

        if (allRequireLocalCache)
        {
            vm.EnsureLocalCacheInitialized();
            if (!vm.LocalCache.IsCacheAvailable)
            {
                vm.StatusText = "需要本地缓存但未下载，请先在设置中下载本地缓存。";
                vm.ProgressValue = 100;
                return;
            }

            // Check if cache is outdated
            bool outdated = await vm.LocalCache.IsCacheOutdatedAsync();
            if (outdated)
            {
                bool update = await ShowConfirmDialogAsync("缓存已过期", "本地缓存已过期（超过7天），建议更新以获取最新谱面数据。\n\n是否立即更新缓存？\n选择「否」将继续使用旧缓存进行批处理。");
                if (update)
                {
                    await DownloadCacheWithProgressAsync(vm);
                }
            }

            // Parallel batch filter
            vm.StatusText = "正在并行筛选中...";
            List<List<BeatSaverMapSlim>> filterResults;
            try
            {
                filterResults = await Task.Run(() => vm.LocalCache.ParallelBatchFilterSlim(presets, new Progress<int>(pct =>
                {
                    Dispatcher.UIThread.Post(() => vm.ProgressValue = pct / 2);
                })));
            }
            catch (Exception ex)
            {
                vm.StatusText = $"批处理筛选失败: {ex.Message}";
                vm.ProgressValue = 100;
                return;
            }

            // Parallel export
            int total = presets.Count;
            int done = 0;
            var doneLock = new object();

            await Task.Run(() =>
            {
                var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, total) };
                Parallel.For(0, total, options, i =>
                {
                    var preset = presets[i];
                    var maps = filterResults[i];
                    if (maps == null || maps.Count == 0)
                    {
                        System.Threading.Interlocked.Increment(ref zeroResultCount);
                        return;
                    }

                    try
                    {
                        string coverText = PlaylistExportService.ExtractCoverTextFromPresetName(preset.Name);
                        string filePath = Path.Combine(outputDir, PlaylistExportService.SanitizeFileName(preset.Name) + ".bplist");
                        var fullMaps = maps.Select(m => m.ToFullMap()).ToList();
                        bool ok = vm.PlaylistExporter.ExportMapsToPlaylist(fullMaps, filePath, preset.Name, coverText, silent: true);
                        if (ok)
                            System.Threading.Interlocked.Increment(ref successCount);
                        else
                            System.Threading.Interlocked.Increment(ref failCount);
                    }
                    catch
                    {
                        System.Threading.Interlocked.Increment(ref failCount);
                    }

                    lock (doneLock)
                    {
                        done++;
                        int pct = 50 + done * 50 / total;
                        Dispatcher.UIThread.Post(() => vm.ProgressValue = pct);
                    }
                });
            });
        }
        else
        {
            // Online API path (serial)
            vm.StatusText = "正在通过API获取数据（较慢）...";
            int total = presets.Count;
            int done = 0;
            foreach (var preset in presets)
            {
                try
                {
                    var maps = await vm.BeatSaverSearch.FetchAllMapsForPresetAsync(preset, useSharedCache: false, pct =>
                    {
                        Dispatcher.UIThread.Post(() => vm.ProgressValue = done * 100 / total + pct / total);
                    });

                    if (maps == null || maps.Count == 0)
                    {
                        zeroResultCount++;
                    }
                    else
                    {
                        string coverText = PlaylistExportService.ExtractCoverTextFromPresetName(preset.Name);
                        string filePath = Path.Combine(outputDir, PlaylistExportService.SanitizeFileName(preset.Name) + ".bplist");
                        bool ok = vm.PlaylistExporter.ExportMapsToPlaylist(maps, filePath, preset.Name, coverText, silent: true);
                        if (ok) successCount++; else failCount++;
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    if (ex.Message.Contains("本地缓存"))
                    {
                        Dispatcher.UIThread.Post(() => vm.StatusText = $"预设「{preset.Name}」失败: 需要本地缓存但未下载，请在设置中下载");
                    }
                }
                done++;
                vm.ProgressValue = done * 100 / total;
            }
        }

        vm.StatusText = $"批处理完成: 成功 {successCount}, 失败 {failCount}, 无结果 {zeroResultCount}";
        vm.ProgressValue = 100;
    }

    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        var settingsWindow = new SettingsWindow(vm.Config, vm.LocalCache);
        await settingsWindow.ShowDialog(this);
    }
}
