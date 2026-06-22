using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BeatSaberIndependentMapsManager.Services
{
    internal class SongScanService
    {
        #region P/Invoke
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern uint Everything_SetSearch(string lpSearchString);
        [DllImport("Everything64.dll")]
        private static extern bool Everything_Query(bool bWait);
        [DllImport("Everything64.dll")]
        private static extern bool Everything_SetMatchWholeWord(bool bEnable);
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern uint Everything_GetNumResults();
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern void Everything_GetResultFullPathName(uint nIndex, StringBuilder lpString, uint nMaxCount);
        [DllImport("Everything64.dll")]
        private static extern void Everything_SetRequestFlags(uint dwRequestFlags);
        #endregion

        private const int EVERYTHING_REQUEST_FILE_NAME = 0x00000001;
        private const int EVERYTHING_REQUEST_PATH = 0x00000002;

        /// <summary>
        /// Perform a full disk scan using Everything search to find scattered maps.
        /// </summary>
        public async Task<Dictionary<string, SongMap>> ScanFullDiskWithEverythingAsync(IEnumerable<string> excludedPaths, Action<int> onProgress = null)
        {
            var results = new Dictionary<string, SongMap>();
            if (!File.Exists("Everything64.dll"))
                return results;

            Everything_SetSearch("info.dat");
            Everything_SetRequestFlags(EVERYTHING_REQUEST_PATH | EVERYTHING_REQUEST_FILE_NAME);
            Everything_SetMatchWholeWord(true);
            Everything_Query(true);

            var buf = new StringBuilder(300);
            var paths = new List<string>();
            uint count = Everything_GetNumResults();

            for (uint i = 0; i < count; i++)
            {
                buf.Clear();
                Everything_GetResultFullPathName(i, buf, 300);
                var path = Path.GetDirectoryName(buf.ToString())!;
                if (path.Contains("Prefetch") || path.Contains("$RECYCLE.BIN") || path.Contains("OneDrive") || Directory.Exists(buf.ToString())) 
                    continue;
                paths.Add(path);
            }

            var tasks = new List<Task>();
            var semaphore = new System.Threading.SemaphoreSlim(10);
            var listExcludedPaths = excludedPaths?.ToList() ?? new List<string>();
            int processedCount = 0;

            foreach (string path in paths)
            {
                bool excluded = false;
                foreach (string excludedPath in listExcludedPaths)
                {
                    if (path.Contains(excludedPath))
                    {
                        excluded = true;
                        break;
                    }
                }

                if (!excluded)
                {
                    await semaphore.WaitAsync();
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            var parsed = ParseSongDirectory(path, null);
                            if (parsed.song != null && !string.IsNullOrEmpty(parsed.bsr))
                            {
                                lock (results)
                                {
                                    results[parsed.bsr] = parsed.song;
                                }
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                            var current = System.Threading.Interlocked.Increment(ref processedCount);
                            onProgress?.Invoke(current * 100 / paths.Count);
                        }
                    }));
                }
                else
                {
                    var current = System.Threading.Interlocked.Increment(ref processedCount);
                    onProgress?.Invoke(current * 100 / paths.Count);
                }
            }

            await Task.WhenAll(tasks);
            return results;
        }

        /// <summary>
        /// Scan a folder and return parsed results.
        /// Returns: (musicPackName, musicPackSongs, resultCode)
        /// resultCode: 0=no songs, 1=music pack added, 2=delicated song added
        /// </summary>
        public SongScanResult ScanFolder(string path, Action<int> onProgress = null)
        {
            int depth = CalculateFolderDepth(path);
            switch (depth)
            {
                case 0:
                    var singleResult = ParseSongDirectory(path, null);
                    if (singleResult.song != null)
                    {
                        return new SongScanResult
                        {
                            ResultType = ScanResultType.DelicatedSong,
                            DelicatedSong = singleResult
                        };
                    }
                    return new SongScanResult { ResultType = ScanResultType.None };

                case 1:
                    if (Directory.GetDirectories(path).Length >= 2)
                    {
                        return ScanMusicPack(path, onProgress);
                    }
                    else
                    {
                        var songs = new List<ParsedSongResult>();
                        var subdirs = Directory.GetDirectories(path);
                        for (int i = 0; i < subdirs.Length; i++)
                        {
                            var result = ParseSongDirectory(subdirs[i], null);
                            if (result.song != null)
                                songs.Add(result);
                            onProgress?.Invoke((i + 1) * 100 / subdirs.Length);
                        }
                        return new SongScanResult
                        {
                            ResultType = ScanResultType.DelicatedSongs,
                            DelicatedSongs = songs
                        };
                    }

                default:
                    string[] folders = Directory.GetDirectories(path);
                    int[] depths = new int[folders.Length];
                    for (int i = 0; i < folders.Length; i++)
                        depths[i] = CalculateFolderDepth(folders[i]);
                    var populardepth = depths.GroupBy(x => x)
                        .OrderByDescending(g => g.Count()).FirstOrDefault();
                    if (populardepth?.Key == 0)
                    {
                        return ScanMusicPack(path, onProgress);
                    }
                    else
                    {
                        var results = new List<SongScanResult>();
                        for (int i = 0; i < folders.Length; i++)
                        {
                            results.Add(ScanFolder(folders[i])); // Sub-folders might not accurately report global progress, keep simple
                            onProgress?.Invoke((i + 1) * 100 / folders.Length);
                        }
                        return new SongScanResult
                        {
                            ResultType = ScanResultType.Multiple,
                            SubResults = results
                        };
                    }
            }
        }

        /// <summary>
        /// Scan a music pack directory (folder containing multiple song subdirectories).
        /// </summary>
        public SongScanResult ScanMusicPack(string path, Action<int> onProgress = null)
        {
            string musicPackName = ExtractMusicPackName(path);
            if (string.IsNullOrEmpty(musicPackName))
                musicPackName = new DirectoryInfo(path).Name;

            var songs = new Dictionary<string, SongMap>();
            var results = new List<ParsedSongResult>();
            int mapsCount = 0, duplicateCount = 0, integrityCount = 0, otherCount = 0;

            string[] mapsDir = Directory.GetDirectories(path);
            for (int i = 0; i < mapsDir.Length; i++)
            {
                var result = ParseSongDirectory(mapsDir[i], musicPackName);
                results.Add(result);
                switch (result.resultCode)
                {
                    case 1: integrityCount++; break;
                    case 2: mapsCount++; break;
                    case 3: duplicateCount++; break;
                    default: otherCount++; break;
                }
                onProgress?.Invoke((i + 1) * 100 / mapsDir.Length);
            }

            var packSongs = new Dictionary<string, SongMap>();
            foreach (var r in results.Where(r => r.song != null))
            {
                if (!packSongs.ContainsKey(r.bsr))
                {
                    packSongs[r.bsr] = r.song;
                }
                else
                {
                    string baseBsr = r.bsr;
                    int duplicateIndex = 1;
                    string newBsr = $"{baseBsr}[{duplicateIndex}]";
                    while (packSongs.ContainsKey(newBsr))
                    {
                        duplicateIndex++;
                        newBsr = $"{baseBsr}[{duplicateIndex}]";
                    }
                    packSongs[newBsr] = r.song;
                    r.resultCode = 3; // Mark as duplicate in result code
                    r.bsr = newBsr;   // Update the bsr to match the actual key used
                    duplicateCount++;
                    mapsCount--; // Adjust counts since it originally incremented mapsCount
                }
            }

            return new SongScanResult
            {
                ResultType = (mapsCount + duplicateCount) > 0 ? ScanResultType.MusicPack : ScanResultType.None,
                MusicPackName = musicPackName,
                MusicPackPath = path,
                PackSongs = packSongs,
                MapsCount = mapsCount + duplicateCount, // MapsCount in the original code included the first one, but let's keep total
                DuplicateCount = duplicateCount,
                IntegrityCount = integrityCount,
                OtherCount = otherCount,
                ScanResults = results
            };
        }

        /// <summary>
        /// Parse a single song directory (Info.dat/json).
        /// </summary>
        public ParsedSongResult ParseSongDirectory(string mapDir, string musicPackName = null)
        {
            var dir = new DirectoryInfo(mapDir);
            string dirName = dir.Name;
            string bsr = ExtractBsr(dirName);
            if (bsr == null)
            {
                return new ParsedSongResult { resultCode = 0 };
            }

            string infoPath = Path.Combine(mapDir, "Info.dat");
            string infoJsonPath = Path.Combine(mapDir, "Info.json");

            string infoFilePath = File.Exists(infoPath) ? infoPath :
                                  File.Exists(infoJsonPath) ? infoJsonPath : null;

            if (infoFilePath == null)
                return new ParsedSongResult { resultCode = 0 };

            try
            {
                byte[] mapInfo = File.ReadAllBytes(infoFilePath);
                var mapStruct = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    Encoding.UTF8.GetString(mapInfo));
                var songMap = ToEntity<SongMap>(mapStruct);
                songMap.songFolder = mapDir;

                if (!CheckIntegrity(mapDir, songMap))
                    return new ParsedSongResult { resultCode = 1, bsr = bsr };

                return new ParsedSongResult
                {
                    resultCode = 2,
                    bsr = bsr,
                    song = songMap
                };
            }
            catch (JsonException)
            {
                return new ParsedSongResult { resultCode = 0, bsr = bsr };
            }
            catch
            {
                return new ParsedSongResult { resultCode = 0, bsr = bsr };
            }
        }

        /// <summary>
        /// Check that all required song files exist.
        /// </summary>
        public bool CheckIntegrity(string mapDir, SongMap map)
        {
            string[] mapStruct = map.GetDifficultiesFiles();
            string coverImg = map._coverImageFilename;
            string musicFile = map._songFilename;

            if (!File.Exists(Path.Combine(mapDir, coverImg)))
                return false;
            if (!File.Exists(Path.Combine(mapDir, musicFile)))
                return false;
            foreach (string mapFile in mapStruct)
            {
                if (!File.Exists(Path.Combine(mapDir, mapFile)))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Copy or move delicated songs to a target directory.
        /// </summary>
        public void CopyOrMoveSongs(Dictionary<string, SongMap> delicatedSongList, string path, bool copyFile)
        {
            foreach (SongMap item in delicatedSongList.Values)
            {
                var song = new DirectoryInfo(item.songFolder);
                string newPath = Path.Combine(path, song.Name);
                if (Directory.Exists(newPath) && newPath != item.songFolder)
                {
                    Directory.Delete(newPath, true);
                }
                else if (Directory.Exists(newPath) && newPath == item.songFolder)
                {
                    continue;
                }
                if (copyFile)
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.CopyDirectory(item.songFolder, newPath);
                }
                else
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.MoveDirectory(item.songFolder, newPath);
                }
            }
        }

        /// <summary>
        /// Extract music pack name from folder path (text between 【】 brackets).
        /// </summary>
        public static string ExtractMusicPackName(string path)
        {
            string pattern = @"【(.+?)】";
            Match match = Regex.Match(path, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// Extract BSR/BEATSAVER key from folder name.
        /// </summary>
        public static string ExtractBsr(string dirName)
        {
            int spaceIndex = dirName.IndexOf(' ');
            string bsr;
            if (spaceIndex == -1)
            {
                bsr = dirName;
                if (bsr.Length > 5)
                    return null;
            }
            else
            {
                bsr = dirName.Substring(0, spaceIndex);
            }

            if (bsr.Length > 5 && spaceIndex != -1)
            {
                bsr = dirName.Substring(0, 5);
                if (!Regex.IsMatch(bsr, @"^[a-fA-F0-9]+$"))
                {
                    bsr = dirName.Substring(0, 4);
                    if (!Regex.IsMatch(bsr, @"^[a-fA-F0-9]+$"))
                        return null;
                }
            }
            return bsr;
        }

        private static int CalculateFolderDepth(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException();
            return CalculateFolderDepthRecursive(folderPath, 0);
        }

        private static int CalculateFolderDepthRecursive(string folderPath, int currentDepth)
        {
            int maxDepth = currentDepth;
            var di = new DirectoryInfo(folderPath);
            foreach (var subDir in di.GetDirectories())
            {
                if (subDir.GetDirectories().Length == 0 && subDir.GetFiles().Length == 0)
                {
                    if (di.GetDirectories().Length > 0)
                        maxDepth++;
                    else return maxDepth;
                }
                else
                {
                    int subDepth = CalculateFolderDepthRecursive(subDir.FullName, currentDepth + 1);
                    maxDepth = Math.Max(maxDepth, subDepth);
                }
            }
            return maxDepth;
        }

        private static T ToEntity<T>(IDictionary<string, object> dictionary) where T : new()
        {
            T entity = new T();
            var type = typeof(T);
            foreach (var pair in dictionary)
            {
                var property = type.GetProperty(pair.Key);
                if (property != null && property.CanWrite)
                    property.SetValue(entity, pair.Value, null);
            }
            return entity;
        }
    }

    public enum ScanResultType
    {
        None,
        DelicatedSong,
        DelicatedSongs,
        MusicPack,
        Multiple
    }

    public class SongScanResult
    {
        public ScanResultType ResultType { get; set; }
        public string MusicPackName { get; set; }
        public string MusicPackPath { get; set; }
        public Dictionary<string, SongMap> PackSongs { get; set; } = new();
        public ParsedSongResult DelicatedSong { get; set; }
        public List<ParsedSongResult> DelicatedSongs { get; set; } = new();
        public List<ParsedSongResult> ScanResults { get; set; } = new();
        public List<SongScanResult> SubResults { get; set; } = new();
        public int MapsCount { get; set; }
        public int DuplicateCount { get; set; }
        public int IntegrityCount { get; set; }
        public int OtherCount { get; set; }
    }

    public class ParsedSongResult
    {
        public int resultCode { get; set; } // 0=none, 1=integrity fail, 2=success, 3=duplicate
        public string bsr { get; set; }
        public SongMap song { get; set; }
    }
}
