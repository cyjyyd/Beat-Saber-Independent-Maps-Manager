using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BeatSaberIndependentMapsManager;
using BeatSaberIndependentMapsManager.ViewModels;
using BeatSaberIndependentMapsManager.Services;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
                var result = await Task.Run(() => vm.SongScanner.ScanFolder(folderPath, progress => 
                {
                    Dispatcher.UIThread.Post(() => vm.ProgressValue = progress);
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

    private static bool IsDuplicate(string str)
    {
        return Regex.IsMatch(str, @"\[([0-9]+)\]$");
    }
}
