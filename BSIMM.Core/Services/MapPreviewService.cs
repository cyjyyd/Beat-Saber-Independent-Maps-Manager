using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BeatSaberIndependentMapsManager.Services
{
    public class PreviewNote
    {
        public double Beat { get; set; }
        public int X { get; set; }       // 0-3 (lineIndex)
        public int Y { get; set; }       // 0-2 (lineLayer)
        public int Color { get; set; }   // 0=red/left, 1=blue/right
        public int CutDirection { get; set; }  // 0-8
        public bool IsBomb { get; set; }
    }

    public class PreviewObstacle
    {
        public double Beat { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public double Duration { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class PreviewDifficultyInfo
    {
        public string Characteristic { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string Filename { get; set; } = "";
        public int Rank { get; set; }
        public double Njs { get; set; } = 16;
        public double NoteJumpStartBeatOffset { get; set; }
    }

    public class MapPreviewData
    {
        public double Bpm { get; set; }
        public List<PreviewNote> Notes { get; set; } = new();
        public List<PreviewObstacle> Obstacles { get; set; } = new();
        public List<PreviewDifficultyInfo> Difficulties { get; set; } = new();
        public string SelectedDifficulty { get; set; } = "";
        public string SelectedCharacteristic { get; set; } = "";
    }

    public class MapPreviewService
    {
        private static readonly HttpClient _httpClient;
        private static readonly HttpClientHandler _handler;

        static MapPreviewService()
        {
            _handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(_handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BSIMM/1.1");
            _httpClient.Timeout = TimeSpan.FromMinutes(2);
        }

        public async Task<string?> DownloadMapZipAsync(string downloadUrl, string tempDir)
        {
            try
            {
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                string zipPath = Path.Combine(tempDir, $"map_{Guid.NewGuid():N}.zip");
                var bytes = await _httpClient.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(zipPath, bytes);
                return zipPath;
            }
            catch
            {
                return null;
            }
        }

        public MapPreviewData ParseMapFromZip(string zipPath, string preferredDifficulty = "Expert", string preferredCharacteristic = "Standard")
        {
            var data = new MapPreviewData();
            string extractDir = Path.Combine(Path.GetTempPath(), $"bsim_preview_{Guid.NewGuid():N}");
            Directory.CreateDirectory(extractDir);

            try
            {
                ZipFile.ExtractToDirectory(zipPath, extractDir, true);

                string infoPath = Path.Combine(extractDir, "info.dat");
                if (!File.Exists(infoPath)) return data;

                string infoJson = File.ReadAllText(infoPath);
                var info = JObject.Parse(infoJson);

                double bpm = info["_beatsPerMinute"]?.Value<double>() ?? 120;
                data.Bpm = bpm;

                var beatmapSets = info["_difficultyBeatmapSets"] as JArray;
                if (beatmapSets == null) return data;

                var difficulties = new List<PreviewDifficultyInfo>();
                string? bestFilename = null;
                string? bestCharacteristic = null;

                foreach (JObject set in beatmapSets)
                {
                    string charName = set["_beatmapCharacteristicName"]?.Value<string>() ?? "Standard";
                    var beatmaps = set["_difficultyBeatmaps"] as JArray;
                    if (beatmaps == null) continue;

                    foreach (JObject beatmap in beatmaps)
                    {
                        var diffInfo = new PreviewDifficultyInfo
                        {
                            Characteristic = charName,
                            Difficulty = beatmap["_difficulty"]?.Value<string>() ?? "",
                            Filename = beatmap["_beatmapFilename"]?.Value<string>() ?? "",
                            Rank = beatmap["_difficultyRank"]?.Value<int>() ?? 0,
                            Njs = beatmap["_noteJumpMovementSpeed"]?.Value<double>() ?? 16,
                            NoteJumpStartBeatOffset = beatmap["_noteJumpStartBeatOffset"]?.Value<double>() ?? 0
                        };
                        difficulties.Add(diffInfo);

                        if (charName == preferredCharacteristic && diffInfo.Difficulty == preferredDifficulty)
                        {
                            bestFilename = diffInfo.Filename;
                            bestCharacteristic = charName;
                        }
                    }
                }

                data.Difficulties = difficulties;

                if (bestFilename == null && difficulties.Count > 0)
                {
                    var fallback = difficulties.Find(d => d.Characteristic == preferredCharacteristic && d.Difficulty == "Hard")
                                ?? difficulties.Find(d => d.Characteristic == preferredCharacteristic)
                                ?? difficulties[0];
                    bestFilename = fallback.Filename;
                    bestCharacteristic = fallback.Characteristic;
                }

                if (bestFilename != null)
                {
                    data.SelectedDifficulty = difficulties.Find(d => d.Filename == bestFilename)!.Difficulty;
                    data.SelectedCharacteristic = bestCharacteristic!;
                    string diffPath = Path.Combine(extractDir, bestFilename);
                    if (File.Exists(diffPath))
                    {
                        ParseDifficultyFileInto(data, diffPath);
                    }
                }
            }
            catch
            {
            }
            finally
            {
                try { Directory.Delete(extractDir, true); } catch { }
            }

            return data;
        }

        public MapPreviewData ParseDifficultyFromZip(string zipPath, MapPreviewData baseData, string characteristic, string difficulty)
        {
            var diffInfo = baseData.Difficulties.Find(d => d.Characteristic == characteristic && d.Difficulty == difficulty);
            if (diffInfo == null) return baseData;

            var data = new MapPreviewData
            {
                Bpm = baseData.Bpm,
                Difficulties = baseData.Difficulties,
                SelectedDifficulty = difficulty,
                SelectedCharacteristic = characteristic
            };

            string extractDir = Path.Combine(Path.GetTempPath(), $"bsim_preview_reload_{Guid.NewGuid():N}");
            try
            {
                ZipFile.ExtractToDirectory(zipPath, extractDir, true);
                string diffPath = Path.Combine(extractDir, diffInfo.Filename);
                if (File.Exists(diffPath))
                {
                    ParseDifficultyFileInto(data, diffPath);
                }
            }
            catch { }
            finally
            {
                try { Directory.Delete(extractDir, true); } catch { }
            }

            return data;
        }

        public void ParseDifficultyFileInto(MapPreviewData data, string diffPath)
        {
            string json = File.ReadAllText(diffPath);
            var doc = JObject.Parse(json);

            string? version = doc["_version"]?.Value<string>();
            bool isV2 = version != null && version.StartsWith("2.");

            if (isV2)
            {
                ParseV2Notes(data, doc);
            }
            else
            {
                ParseV3Notes(data, doc);
            }
        }

        private void ParseV2Notes(MapPreviewData data, JObject doc)
        {
            var notes = doc["_notes"] as JArray;
            if (notes != null)
            {
                foreach (JObject note in notes)
                {
                    int type = note["_type"]?.Value<int>() ?? 0;
                    data.Notes.Add(new PreviewNote
                    {
                        Beat = note["_time"]?.Value<double>() ?? 0,
                        X = note["_lineIndex"]?.Value<int>() ?? 0,
                        Y = note["_lineLayer"]?.Value<int>() ?? 0,
                        Color = type == 3 ? 0 : type,
                        CutDirection = note["_cutDirection"]?.Value<int>() ?? 8,
                        IsBomb = type == 3
                    });
                }
            }

            var obstacles = doc["_obstacles"] as JArray;
            if (obstacles != null)
            {
                foreach (JObject obs in obstacles)
                {
                    data.Obstacles.Add(new PreviewObstacle
                    {
                        Beat = obs["_time"]?.Value<double>() ?? 0,
                        X = obs["_lineIndex"]?.Value<int>() ?? 0,
                        Y = obs["_lineLayer"]?.Value<int>() ?? 0,
                        Duration = obs["_duration"]?.Value<double>() ?? 0,
                        Width = obs["_width"]?.Value<int>() ?? 1,
                        Height = obs["_height"]?.Value<int>() ?? 1
                    });
                }
            }
        }

        private void ParseV3Notes(MapPreviewData data, JObject doc)
        {
            var colorNotes = doc["colorNotes"] as JArray;
            if (colorNotes != null)
            {
                foreach (JObject note in colorNotes)
                {
                    data.Notes.Add(new PreviewNote
                    {
                        Beat = note["b"]?.Value<double>() ?? 0,
                        X = note["x"]?.Value<int>() ?? 0,
                        Y = note["y"]?.Value<int>() ?? 0,
                        Color = note["c"]?.Value<int>() ?? 0,
                        CutDirection = note["d"]?.Value<int>() ?? 8,
                        IsBomb = false
                    });
                }
            }

            var bombNotes = doc["bombNotes"] as JArray;
            if (bombNotes != null)
            {
                foreach (JObject bomb in bombNotes)
                {
                    data.Notes.Add(new PreviewNote
                    {
                        Beat = bomb["b"]?.Value<double>() ?? 0,
                        X = bomb["x"]?.Value<int>() ?? 0,
                        Y = bomb["y"]?.Value<int>() ?? 0,
                        Color = 0,
                        CutDirection = 8,
                        IsBomb = true
                    });
                }
            }

            var obstacles = doc["obstacles"] as JArray;
            if (obstacles != null)
            {
                foreach (JObject obs in obstacles)
                {
                    data.Obstacles.Add(new PreviewObstacle
                    {
                        Beat = obs["b"]?.Value<double>() ?? 0,
                        X = obs["x"]?.Value<int>() ?? 0,
                        Y = obs["y"]?.Value<int>() ?? 0,
                        Duration = obs["d"]?.Value<double>() ?? 0,
                        Width = obs["w"]?.Value<int>() ?? 1,
                        Height = obs["h"]?.Value<int>() ?? 1
                    });
                }
            }
        }
    }
}
