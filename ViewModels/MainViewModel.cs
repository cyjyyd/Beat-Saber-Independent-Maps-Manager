using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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

        // Thread safety locks
        private readonly object _stateLock = new object();

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

        // BeatSaver client
        public BeatSaverClient BeatSaverClient { get; } = new();

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

        public void EnsureLocalCacheInitialized()
        {
            LocalCache.UseSystemProxy = _config.UseSystemProxy;
        }

        /// <summary>
        /// Initialize game instance detection.
        /// </summary>
        public void InitializeGameDetection()
        {
            string versionFilePath = Path.Combine(Application.StartupPath, "assets\\json\\bs-versions.json");
            GameDetector.LoadVersionFile(versionFilePath);

            bool found = GameDetector.Detect();
            if (!found)
            {
                _view.Log("未检测到Beat Saber实例");
            }

            if (_config.HashCache)
            {
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
                        lock (_stateLock)
                        {
                            if (!DelicatedSongList.ContainsKey(bsr))
                                DelicatedSongList[bsr] = result.DelicatedSong.song;
                        }
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

            lock (_stateLock)
            {
                string packName = result.MusicPackName;

                // Handle duplicate names
                if (MusicPackInfo.ContainsKey(packName))
                {
                    packName = GenerateUniqueName(packName, MusicPackInfo.Keys);
                    result.MusicPackName = packName; // IMPORTANT: Update the result so UI knows the unique name
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
                        // Handle duplicate BSR within pack
                        string uniqueKey = GenerateUniqueName(kvp.Key, MusicPackInfo[packName].Keys);
                        MusicPackInfo[packName][uniqueKey] = kvp.Value;
                    }
                }

                MusicPackPath[packName] = result.MusicPackPath;
            }
        }

        /// <summary>
        /// Perform full disk scan using Everything and update presenter state.
        /// </summary>
        public async Task<Dictionary<string, SongMap>> ScanFullDiskAsync(IEnumerable<string> excludedPaths, Action<int> onProgress = null)
        {
            var results = await SongScanner.ScanFullDiskWithEverythingAsync(excludedPaths, onProgress);
            lock (_stateLock)
            {
                foreach (var kvp in results)
                {
                    if (!DelicatedSongList.ContainsKey(kvp.Key))
                    {
                        DelicatedSongList[kvp.Key] = kvp.Value;
                    }
                }
            }
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
