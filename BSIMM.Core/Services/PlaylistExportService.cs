using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BeatSaberIndependentMapsManager.Services
{
    internal class PlaylistExportService
    {
        private readonly HashCacheService _hashCache;
        private readonly Config _config;

        public PlaylistExportService(HashCacheService hashCache, Config config)
        {
            _hashCache = hashCache;
            _config = config;
        }

        /// <summary>
        /// Export a single music pack to a .bplist file.
        /// </summary>
        public async Task ExportMusicPackAsync(
            string musicPackName,
            Dictionary<string, SongMap> packSongs,
            Image coverImage,
            string outputPath,
            IProgress<int> progress = null)
        {
            string imgBytes = "data:image/jpg;base64," + ImageToBase64(coverImage, ImageFormat.Jpeg);
            string author = Environment.UserName + "使用BSIMM@万毒不侵 生成";
            string description = "本歌单由" + Environment.UserName + "使用BSIMM生成\r\n" +
                "BSIMM由万毒不侵开发，开源且免费，如果你是购买的请要求商家退款\r\n" +
                "项目地址：https://github.com/cyjyyd/Beat-Saber-Independent-Maps-Manager";

            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            PlayList playList = new PlayList(musicPackName, author, description, imgBytes);

            int processed = 0;
            foreach (var kvp in packSongs)
            {
                string hash = await _hashCache.GetOrComputeHashAsync(kvp.Key, kvp.Value);
                playList.AddSongHash(hash);
                processed++;
                progress?.Report(processed * 100 / packSongs.Count);
            }

            string filePath = Path.Combine(outputPath, SanitizeFileName(musicPackName) + ".bplist");
            string json = JsonConvert.SerializeObject(playList, Formatting.None, settings);
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Export multiple music packs in parallel.
        /// </summary>
        public async Task ExportAllPacksAsync(
            Dictionary<string, Dictionary<string, SongMap>> musicPackInfo,
            Dictionary<string, Image> musicPackCoverimgs,
            string outputPath,
            Action<string, int> onProgress = null)
        {
            var tasks = new List<Task>();
            int total = musicPackInfo.Count;
            int completed = 0;

            foreach (var pack in musicPackInfo)
            {
                Image cover = musicPackCoverimgs.TryGetValue(pack.Key, out var img) ? img : null;
                if (cover == null) continue;

                tasks.Add(ExportMusicPackAsync(pack.Key, pack.Value, cover, outputPath,
                    new Progress<int>(pct => onProgress?.Invoke(pack.Key, pct))));
                completed++;
            }

            await Task.WhenAll(tasks);

            if (_config.HashCache)
                _hashCache.SaveCache();
        }

        /// <summary>
        /// Export a list of BeatSaverMap results to a .bplist file.
        /// </summary>
        public bool ExportMapsToPlaylist(
            List<BeatSaverMap> maps,
            string filePath,
            string playlistName,
            string coverText = null,
            Dictionary<int, HashSet<(string characteristic, string difficulty)>> perSongDifficulties = null,
            bool silent = false,
            Image defaultBackgroundImage = null)
        {
            try
            {
                if (string.IsNullOrEmpty(coverText))
                    coverText = playlistName;

                Image coverImage = GeneratePlaylistCover(coverText, defaultBackgroundImage);
                string imgBytes = "data:image/jpg;base64," + ImageToBase64(coverImage, ImageFormat.Jpeg);
                string author = Environment.UserName + "使用BSIMM@万毒不侵 生成";
                string description = "本歌单由" + Environment.UserName + "使用BSIMM生成\r\n" +
                    "BSIMM由万毒不侵开发，开源且免费，如果你是购买的请要求商家退款\r\n" +
                    "项目地址：https://github.com/cyjyyd/Beat-Saber-Independent-Maps-Manager";

                var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                PlayList playlist = new PlayList(playlistName, author, description, imgBytes);

                bool hasDiffs = perSongDifficulties != null && perSongDifficulties.Count > 0;

                for (int i = 0; i < maps.Count; i++)
                {
                    var map = maps[i];
                    string hash = map.GetHash();
                    if (string.IsNullOrEmpty(hash)) continue;

                    if (hasDiffs && perSongDifficulties.TryGetValue(i, out var songDiffs) && songDiffs.Count > 0)
                    {
                        var version = map.Versions?.FirstOrDefault();
                        var difficulties = new List<HighLightdiff>();
                        if (version?.Diffs != null)
                        {
                            foreach (var diff in version.Diffs)
                            {
                                var pair = (diff.Characteristic, diff.Difficulty);
                                if (songDiffs.Contains(pair))
                                    difficulties.Add(new HighLightdiff(diff.Characteristic, diff.Difficulty));
                            }
                        }
                        if (difficulties.Count > 0)
                            playlist.AddSongHashWithDifficulties(hash, difficulties);
                        else
                            playlist.AddSongHash(hash);
                    }
                    else
                    {
                        playlist.AddSongHash(hash);
                    }
                }

                string json = JsonConvert.SerializeObject(playlist, Formatting.None, settings);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generate playlist cover image with text.
        /// </summary>
        public static Image GeneratePlaylistCover(string playlistName, Image defaultBackgroundImage = null)
        {
            int width = 256;
            int height = 256;
            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                if (defaultBackgroundImage != null)
                {
                    try
                    {
                        g.DrawImage(defaultBackgroundImage, new Rectangle(0, 0, width, height));
                    }
                    catch
                    {
                        DrawGradientBackground(g, width, height);
                    }
                }
                else
                {
                    DrawGradientBackground(g, width, height);
                }

                using (Font font = new Font("Microsoft YaHei UI", 14f, FontStyle.Bold))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    RectangleF textRect = new RectangleF(10, 10, width - 20, height - 20);
                    g.DrawString(playlistName, font, Brushes.White, textRect, format);
                }

                using (Font smallFont = new Font("Microsoft YaHei UI", 8f))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Far
                    };
                    g.DrawString("BSIMM自动生成@万毒不侵", smallFont, Brushes.Gray,
                        new RectangleF(0, height - 30, width, 25), format);
                }
            }

            return bitmap;
        }

        private static void DrawGradientBackground(Graphics g, int width, int height)
        {
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, 0, width, height),
                Color.FromArgb(64, 64, 128),
                Color.FromArgb(32, 32, 64),
                System.Drawing.Drawing2D.LinearGradientMode.Vertical))
            {
                g.FillRectangle(brush, 0, 0, width, height);
            }
        }

        /// <summary>
        /// Extract text between 【】 brackets from preset name.
        /// </summary>
        public static string ExtractCoverTextFromPresetName(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
                return null;

            int start = presetName.IndexOf('【');
            int end = presetName.IndexOf('】');
            if (start >= 0 && end > start)
                return presetName.Substring(start + 1, end - start - 1);

            return null;
        }

        /// <summary>
        /// Clean invalid filename characters.
        /// </summary>
        public static string SanitizeFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }

        /// <summary>
        /// Convert Image to Base64 string.
        /// </summary>
        public static string ImageToBase64(Image image, ImageFormat format)
        {
            using (var memoryStream = new MemoryStream())
            {
                image.Save(memoryStream, format);
                byte[] imageBytes = memoryStream.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
        }
    }
}

