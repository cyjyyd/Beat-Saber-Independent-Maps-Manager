using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Xml.Linq;
using BeatSaberIndependentMapsManager.Abstractions;
using BeatSaberIndependentMapsManager.Services;

namespace BeatSaberIndependentMapsManager.ViewModels
{
    internal partial class MainViewModel : ObservableObject
    {
        private readonly IMainView _view;
        private readonly Config _config;

        public GameInstanceDetector GameDetector { get; }
        public AudioPreviewService AudioPreview { get; }
        public HashCacheService HashCache { get; }
        public PlaylistExportService PlaylistExporter { get; }
        public SongScanService SongScanner { get; }
        public BeatSaverSearchService BeatSaverSearch { get; }
        public LocalCacheManager LocalCache { get; private set; }

        // MVVM Observable Properties
        [ObservableProperty]
        private string _statusText = "就绪";

        [ObservableProperty]
        private string _actionText = "信息";

        [ObservableProperty]
        private int _progressValue = 0;

        // MVVM Observable Collections for Binding (e.g. Avalonia)
        public ObservableCollection<string> MusicPacks { get; } = new();
        public ObservableCollection<SongMap> Songs { get; } = new();
        public ObservableCollection<SongMap> DelicatedSongs { get; } = new();

        [ObservableProperty]
        private string _selectedMusicPack;

        [ObservableProperty]
        private SongMap _selectedSong;

        [ObservableProperty]
        private string _selectedDelicatedSong;

        [ObservableProperty]
        private string _musicPackCoverPath;

        [ObservableProperty]
        private int _musicPackSongCount;

        [ObservableProperty]
        private bool _isMusicPackHashCached;

        [ObservableProperty]
        private bool _isMusicPackSynced;

        partial void OnSelectedMusicPackChanged(string value)
        {
            RunOnUIThread(() =>
            {
                Songs.Clear();
                MusicPackSongCount = 0;
                IsMusicPackHashCached = false;
                IsMusicPackSynced = false;
                MusicPackCoverPath = null;

                if (value != null)
                {
                    lock (_stateLock)
                    {
                        if (MusicPackInfo.TryGetValue(value, out var songDict))
                        {
                            foreach (var song in songDict.Values)
                            {
                                Songs.Add(song);
                            }
                            MusicPackSongCount = songDict.Count;

                            // Check Hash Cache
                            bool cached = true;
                            if (HashCache.SongsHash == null) cached = false;
                            else
                            {
                                foreach(var key in songDict.Keys)
                                {
                                    if (!HashCache.SongsHash.ContainsKey(key))
                                    {
                                        cached = false;
                                        break;
                                    }
                                }
                            }
                            IsMusicPackHashCached = cached;
                        }

                        if (MusicPackPath.TryGetValue(value, out var path))
                        {
                            MusicPackCoverPath = Path.Combine(path, "cover.jpg");

                            // Check Sync status
                            bool synced = false;
                            foreach (var instance in GameDetector.BSInstancePath.Keys)
                            {
                                if (GameDetector.InstanceSongCoreReady.TryGetValue(instance, out var ready) && ready[3])
                                {
                                    string targetFolder = Path.Combine(GameDetector.BSInstancePath[instance], "UserData", "SongCore", "folders.xml");
                                    if (File.Exists(targetFolder))
                                    {
                                        try
                                        {
                                            string content = File.ReadAllText(targetFolder);
                                            if (content.Contains($"<Path>{path}</Path>"))
                                            {
                                                synced = true;
                                                break;
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                            IsMusicPackSynced = synced;
                        }
                    }
                }
            });
        }

        // Thread safety locks
        private readonly object _stateLock = new object();
        public object SyncRoot => _stateLock;

        // State - music packs and songs
        public Dictionary<string, Dictionary<string, SongMap>> MusicPackInfo { get; } = new();
        public Dictionary<string, string> MusicPackPath { get; } = new();
        public Dictionary<string, Image> MusicPackCoverImages { get; } = new();
        public Dictionary<string, SongMap> DelicatedSongList { get; } = new();

        public List<string> GetMusicPackNames()
        {
            lock (_stateLock)
            {
                return MusicPackInfo.Keys.ToList();
            }
        }
        public Dictionary<string, string> SongsHash
        {
            get => HashCache.SongsHash;
            set => HashCache.SongsHash = value;
        }

        public Dictionary<string, Dictionary<string, SongMap>> GetMusicPackInfoSnapshot()
        {
            lock (_stateLock)
            {
                var snapshot = new Dictionary<string, Dictionary<string, SongMap>>();
                foreach (var kvp in MusicPackInfo)
                {
                    snapshot[kvp.Key] = new Dictionary<string, SongMap>(kvp.Value);
                }
                return snapshot;
            }
        }

        public Dictionary<string, Image> GetMusicPackCoverImagesSnapshot()
        {
            lock (_stateLock)
            {
                return new Dictionary<string, Image>(MusicPackCoverImages);
            }
        }

        public async Task ExportMusicPackAsync(string packName, string outputPath, IProgress<int> progress = null)
        {
            Dictionary<string, SongMap> packSongs;
            Image cover = null;
            string packPath = null;
            lock (_stateLock)
            {
                if (!MusicPackInfo.TryGetValue(packName, out packSongs)) return;
                MusicPackCoverImages.TryGetValue(packName, out cover);
                MusicPackPath.TryGetValue(packName, out packPath);
            }

            if (cover == null && !string.IsNullOrEmpty(packPath))
            {
                string coverFile = Path.Combine(packPath, "cover.jpg");
                if (File.Exists(coverFile))
                {
                    try
                    {
                        cover = Image.FromFile(coverFile);
                    }
                    catch { }
                }
            }

            await PlaylistExporter.ExportMusicPackAsync(packName, packSongs, cover, outputPath, progress);
            
            if (cover != null && !MusicPackCoverImages.ContainsKey(packName))
            {
                // We loaded it dynamically, so dispose it to prevent memory leaks if we don't cache it
                cover.Dispose();
            }
        }

        public async Task ExportAllMusicPacksAsync(string outputPath, Action<string, int> progress = null)
        {
            Dictionary<string, Dictionary<string, SongMap>> packs;
            Dictionary<string, Image> covers;
            Dictionary<string, string> paths;
            lock (_stateLock)
            {
                packs = new Dictionary<string, Dictionary<string, SongMap>>(MusicPackInfo);
                covers = new Dictionary<string, Image>(MusicPackCoverImages);
                paths = new Dictionary<string, string>(MusicPackPath);
            }
            
            // Fill in missing covers dynamically
            foreach (var kvp in packs)
            {
                if (!covers.ContainsKey(kvp.Key) && paths.TryGetValue(kvp.Key, out var packPath))
                {
                    string coverFile = Path.Combine(packPath, "cover.jpg");
                    if (File.Exists(coverFile))
                    {
                        try
                        {
                            covers[kvp.Key] = Image.FromFile(coverFile);
                        }
                        catch { }
                    }
                }
            }

            await PlaylistExporter.ExportAllPacksAsync(packs, covers, outputPath, progress);
            
            // Dispose dynamically loaded covers
            lock (_stateLock)
            {
                foreach (var kvp in covers)
                {
                    if (!MusicPackCoverImages.ContainsKey(kvp.Key))
                    {
                        kvp.Value.Dispose();
                    }
                }
            }
        }

        // BeatSaver client
        public BeatSaverClient BeatSaverClient { get; } = new();

        private void RunOnUIThread(Action action)
        {
            _view.RunOnUIThread(action);
        }

        public MainViewModel(IMainView view, Config config)
        {
            _view = view;
            _config = config;

            GameDetector = new GameInstanceDetector();
            AudioPreview = new AudioPreviewService();
            HashCache = new HashCacheService();
            SongScanner = new SongScanService();
            PlaylistExporter = new PlaylistExportService(HashCache, config);
            LocalCache = new LocalCacheManager() { UseSystemProxy = config.UseSystemProxy };
            BeatSaverSearch = new BeatSaverSearchService(BeatSaverClient, LocalCache);
        }

        public int SaveSongCoreFolders()
        {
            int successCount = 0;
            if (MusicPackInfo.Count == 0 && DelicatedSongList.Count == 0) return 0;

            XElement rootElement = new XElement("folders");
            lock (_stateLock)
            {
                // Add music packs
                foreach (var kvp in MusicPackPath)
                {
                    string name = kvp.Key;
                    string path = kvp.Value;
                    string coverPath = Path.Combine(path, "cover.jpg");
                    // We don't save covers automatically here for Avalonia yet, we assume user adds them or it's empty
                    XElement folder = new XElement("folder",
                        new XElement("Name", name),
                        new XElement("Path", path),
                        new XElement("Pack", "2"),
                        new XElement("ImagePath", coverPath)
                    );
                    rootElement.Add(folder);
                }
            }

            // Write to each Beat Saber instance
            foreach (var instance in GameDetector.BSInstancePath.Keys)
            {
                if (GameDetector.InstanceSongCoreReady.ContainsKey(instance) && GameDetector.InstanceSongCoreReady[instance][3])
                {
                    string targetFolder = Path.Combine(GameDetector.BSInstancePath[instance], "UserData", "SongCore");
                    if (Directory.Exists(targetFolder))
                    {
                        try
                        {
                            rootElement.Save(Path.Combine(targetFolder, "folders.xml"));
                            successCount++;
                        }
                        catch { }
                    }
                }
            }
            return successCount;
        }

        public void EnsureLocalCacheInitialized()
        {
            LocalCache.UseSystemProxy = _config.UseSystemProxy;
        }

        /// <summary>
        /// Initialize game instance detection.
        /// </summary>
        [ObservableProperty]
        private string _gameInstancesText = "未检测到 Beat Saber 实例";

        public void InitializeGameDetection()
        {
            _view.Log("开始检测 Beat Saber 实例...");
            string versionFilePath = Path.Combine(AppContext.BaseDirectory, "assets\\json\\bs-versions.json");
            GameDetector.LoadVersionFile(versionFilePath);

            bool found = GameDetector.Detect();
            if (!found)
            {
                _view.Log("未检测到 Beat Saber 实例");
                GameInstancesText = "未检测到 Beat Saber 实例";
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                foreach(var kvp in GameDetector.BSInstancePath)
                {
                    sb.Append($"v{kvp.Key} ");
                    _view.Log($"成功检测到游戏实例: 版本 {kvp.Key}, 路径: {kvp.Value}");
                }
                GameInstancesText = $"游戏版本: {sb.ToString().Trim()}";
            }

            if (_config.HashCache)
            {
                _view.Log("正在加载本地哈希缓存...");
                HashCache.LoadCache();
            }
        }

        /// <summary>
        /// Process song scanning results to update MainForm's state.
        /// This is called after SongScanner.ScanFolder() in the UI thread context.
        /// </summary>
        public void ApplyScanResult(SongScanResult result)
        {
            switch (result.ResultType)
            {
                case ScanResultType.DelicatedSong:
                    if (result.DelicatedSong?.song != null)
                    {
                        string bsr = result.DelicatedSong.bsr;
                        SongMap song;
                        bool isNew = false;
                        lock (_stateLock)
                        {
                            if (!DelicatedSongList.ContainsKey(bsr))
                            {
                                DelicatedSongList[bsr] = result.DelicatedSong.song;
                                song = result.DelicatedSong.song;
                                isNew = true;
                            }
                            else
                            {
                                song = null;
                            }
                        }
                        if (isNew && song != null)
                            RunOnUIThread(() => DelicatedSongs.Add(song));
                    }
                    break;

                case ScanResultType.MusicPack:
                    ApplyMusicPackResult(result);
                    break;

                case ScanResultType.Multiple:
                    foreach (var sub in result.SubResults)
                        ApplyScanResult(sub);
                    break;
            }
        }

        private void ApplyMusicPackResult(SongScanResult result)
        {
            if (result.PackSongs == null || result.PackSongs.Count == 0)
                return;

            string packName;
            lock (_stateLock)
            {
                packName = result.MusicPackName;

                // Handle duplicate names
                if (MusicPackInfo.ContainsKey(packName))
                {
                    packName = GenerateUniqueName(packName, MusicPackInfo.Keys);
                    result.MusicPackName = packName;
                }

                MusicPackInfo[packName] = new Dictionary<string, SongMap>();
                foreach (var kvp in result.PackSongs)
                {
                    if (!MusicPackInfo[packName].ContainsKey(kvp.Key))
                    {
                        MusicPackInfo[packName][kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        string uniqueKey = GenerateUniqueName(kvp.Key, MusicPackInfo[packName].Keys);
                        MusicPackInfo[packName][uniqueKey] = kvp.Value;
                    }
                }

                MusicPackPath[packName] = result.MusicPackPath;
            }

            // Lock released before UI dispatch to prevent deadlock with synchronous RunOnUIThread
            RunOnUIThread(() =>
            {
                if (!MusicPacks.Contains(packName))
                {
                    MusicPacks.Add(packName);
                }
            });
        }

        /// <summary>
        /// Perform full disk scan using Everything and update presenter state.
        /// </summary>
        public async Task<Dictionary<string, SongMap>> ScanFullDiskAsync(IEnumerable<string> excludedPaths, Action<int> onProgress = null)
        {
            var results = await SongScanner.ScanFullDiskWithEverythingAsync(excludedPaths, onProgress);
            var newSongs = new List<SongMap>();
            lock (_stateLock)
            {
                foreach (var kvp in results)
                {
                    if (!DelicatedSongList.ContainsKey(kvp.Key))
                    {
                        DelicatedSongList[kvp.Key] = kvp.Value;
                        newSongs.Add(kvp.Value);
                    }
                }
            }
            foreach (var song in newSongs)
                RunOnUIThread(() => DelicatedSongs.Add(song));
            return results;
        }

        private static string GenerateUniqueName(string baseName, IEnumerable<string> existingKeys)
        {
            string escapedPrefix = System.Text.RegularExpressions.Regex.Escape(baseName);
            string pattern = @"^" + escapedPrefix + @"(\[[0-9]+\])?$";
            var matches = existingKeys
                .Where(k => System.Text.RegularExpressions.Regex.Match(k, pattern).Success)
                .ToList();

            if (matches.Count == 0)
                return baseName;

            var last = matches[^1];
            var renamePattern = @"\[([0-9]+)\]$";
            var match = System.Text.RegularExpressions.Regex.Match(last, renamePattern);
            if (match.Success)
            {
                int count = Convert.ToInt32(match.Groups[1].Value) + 1;
                return System.Text.RegularExpressions.Regex.Replace(last, renamePattern, $"[{count}]");
            }
            return baseName + "[1]";
        }
    }
}
