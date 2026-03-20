using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// Progress information for cache download
    /// </summary>
    public class CacheDownloadProgress
    {
        public long BytesReceived { get; set; }
        public long TotalBytes { get; set; }
        public double Percentage => TotalBytes > 0 ? (BytesReceived * 100.0 / TotalBytes) : 0;
        public string Status { get; set; }
    }

    /// <summary>
    /// Manages local BeatSaver cache for advanced filtering
    /// </summary>
    public class LocalCacheManager : IDisposable
    {
        private const string CacheUrl = "https://raw.githubusercontent.com/qe201020335/BSC-ScrapeData/refs/heads/data/cache.json.gz";
        private const string LocalCacheFileName = "cache.json";
        private string cachePath;  // Removed readonly for testing support
        private readonly HttpClient httpClient;
        private bool disposed = false;
        private long cacheDate = 0;
        private bool cacheAvailable = false;

        /// <summary>
        /// Event raised when download progress changes
        /// </summary>
        public event EventHandler<CacheDownloadProgress> DownloadProgress;

        /// <summary>
        /// Gets whether the cache is available
        /// </summary>
        public bool IsCacheAvailable => cacheAvailable && File.Exists(cachePath);

        /// <summary>
        /// Gets the cache date (Unix timestamp)
        /// </summary>
        public long CacheDate => cacheDate;

        /// <summary>
        /// Gets the path to the cache file
        /// </summary>
        public string CachePath => cachePath;

        public LocalCacheManager()
        {
            cachePath = Path.Combine(Application.StartupPath, LocalCacheFileName);
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);
            httpClient.DefaultRequestHeaders.Add("User-Agent", "BSIMM/1.0");

            // Check if cache exists
            if (File.Exists(cachePath))
            {
                cacheAvailable = true;
                // Try to get file date as cache date
                try
                {
                    cacheDate = new DateTimeOffset(File.GetLastWriteTimeUtc(cachePath)).ToUnixTimeSeconds();
                }
                catch { }
            }
        }

        /// <summary>
        /// Downloads the cache file from remote source
        /// </summary>
        public async Task<bool> DownloadCacheAsync()
        {
            try
            {
                DownloadProgress?.Invoke(this, new CacheDownloadProgress { Status = "正在下载缓存文件..." });

                // Download gzip file to temp location
                var tempFile = Path.GetTempFileName();

                using (var response = await httpClient.GetAsync(CacheUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = File.Create(tempFile))
                    {
                        var buffer = new byte[8192];
                        var bytesRead = 0;
                        var totalRead = 0L;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;

                            if (totalBytes > 0)
                            {
                                DownloadProgress?.Invoke(this, new CacheDownloadProgress
                                {
                                    BytesReceived = totalRead,
                                    TotalBytes = totalBytes,
                                    Status = "正在下载..."
                                });
                            }
                        }
                    }
                }

                DownloadProgress?.Invoke(this, new CacheDownloadProgress { Status = "正在解压..." });

                // Decompress gzip
                using (var gzipStream = new GZipStream(File.OpenRead(tempFile), CompressionMode.Decompress))
                using (var outputFile = File.Create(cachePath))
                {
                    await gzipStream.CopyToAsync(outputFile);
                }

                // Clean up temp file
                File.Delete(tempFile);

                cacheAvailable = true;
                cacheDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                DownloadProgress?.Invoke(this, new CacheDownloadProgress { Status = "下载完成" });

                return true;
            }
            catch (Exception ex)
            {
                DownloadProgress?.Invoke(this, new CacheDownloadProgress { Status = $"下载失败: {ex.Message}" });
                return false;
            }
        }

        /// <summary>
        /// Ensures cache is available, downloading if necessary
        /// </summary>
        public async Task<bool> EnsureCacheAvailableAsync()
        {
            if (IsCacheAvailable)
                return true;

            return await DownloadCacheAsync();
        }

        /// <summary>
        /// Reads the cache date from the JSON file
        /// </summary>
        public long ReadCacheDate()
        {
            if (!IsCacheAvailable) return 0;

            try
            {
                using (var fileStream = new FileStream(cachePath, FileMode.Open, FileAccess.Read))
                using (var streamReader = new StreamReader(fileStream))
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    while (jsonReader.Read())
                    {
                        if (jsonReader.TokenType == JsonToken.PropertyName && jsonReader.Value?.ToString() == "date")
                        {
                            jsonReader.Read();
                            return jsonReader.Value != null ? Convert.ToInt64(jsonReader.Value) : 0;
                        }
                    }
                }
            }
            catch { }

            return 0;
        }

        /// <summary>
        /// Streams through the cache and filters maps based on preset
        /// </summary>
        public IEnumerable<BeatSaverMap> StreamFilterMaps(FilterPreset preset, IProgress<int> progress, CancellationToken cancellationToken = default)
        {
            if (!IsCacheAvailable)
                yield break;

            using var fileStream = new FileStream(cachePath, FileMode.Open, FileAccess.Read);
            using var streamReader = new StreamReader(fileStream);
            using var jsonReader = new JsonTextReader(streamReader);

            var serializer = new JsonSerializer();
            long totalBytes = fileStream.Length;
            long lastReportBytes = 0;
            int reportInterval = 1024 * 1024; // Report every 1MB

            // Navigate to docs array
            while (jsonReader.Read())
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                if (jsonReader.TokenType == JsonToken.StartArray && jsonReader.Path == "docs")
                {
                    break;
                }
            }

            // Stream read each map object
            while (jsonReader.Read() && jsonReader.TokenType != JsonToken.EndArray)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                if (jsonReader.TokenType == JsonToken.StartObject)
                {
                    var map = serializer.Deserialize<BeatSaverMap>(jsonReader);

                    if (map != null && MatchesFilter(map, preset))
                    {
                        yield return map;
                    }

                    // Report progress
                    if (fileStream.Position - lastReportBytes > reportInterval)
                    {
                        var percent = (int)(fileStream.Position * 100 / totalBytes);
                        progress?.Report(percent);
                        lastReportBytes = fileStream.Position;
                    }
                }
            }

            progress?.Report(100);
        }

        /// <summary>
        /// Performs parallel filtering on the cache (loads all maps into memory first)
        /// </summary>
        public List<BeatSaverMap> ParallelFilterMaps(FilterPreset preset, IProgress<int> progress, CancellationToken cancellationToken = default)
        {
            if (!IsCacheAvailable)
                return new List<BeatSaverMap>();

            // First pass: load all maps into a list
            var allMaps = new List<BeatSaverMap>();
            using (var fileStream = new FileStream(cachePath, FileMode.Open, FileAccess.Read))
            using (var streamReader = new StreamReader(fileStream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer();

                // Navigate to docs array
                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonToken.StartArray && jsonReader.Path == "docs")
                        break;
                }

                // Load all maps
                while (jsonReader.Read() && jsonReader.TokenType != JsonToken.EndArray)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return new List<BeatSaverMap>();

                    if (jsonReader.TokenType == JsonToken.StartObject)
                    {
                        var map = serializer.Deserialize<BeatSaverMap>(jsonReader);
                        if (map != null)
                            allMaps.Add(map);
                    }
                }
            }

            progress?.Report(50);

            // Second pass: parallel filter
            var results = new ConcurrentBag<BeatSaverMap>();
            int processed = 0;
            int total = allMaps.Count;

            Parallel.ForEach(Partitioner.Create(allMaps), new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            }, map =>
            {
                if (MatchesFilter(map, preset))
                {
                    results.Add(map);
                }

                var current = Interlocked.Increment(ref processed);
                if (current % 1000 == 0)
                {
                    var percent = 50 + (current * 50 / total);
                    progress?.Report(percent);
                }
            });

            progress?.Report(100);

            // Apply result limit
            var resultList = results.ToList();
            return ApplyResultLimit(resultList, preset);
        }

        /// <summary>
        /// Applies result limit (count + sort) to the filtered results
        /// </summary>
        private List<BeatSaverMap> ApplyResultLimit(List<BeatSaverMap> maps, FilterPreset preset)
        {
            // First check for top-level result limit (overrides group-level)
            ResultLimitValue resultLimit = preset.TopLevelResultLimit;

            // If no top-level limit, check for group-level limit (first group that has one)
            if (resultLimit == null)
            {
                foreach (var group in preset.GetActiveGroups())
                {
                    resultLimit = group.GetResultLimit();
                    if (resultLimit != null)
                        break;
                }
            }

            if (resultLimit == null || resultLimit.Count <= 0)
                return maps;

            // Sort based on sort option
            switch (resultLimit.SortOption)
            {
                case ResultSortOption.Newest:
                    maps = maps.OrderByDescending(m => m.Uploaded).ToList();
                    break;
                case ResultSortOption.Oldest:
                    maps = maps.OrderBy(m => m.Uploaded).ToList();
                    break;
                case ResultSortOption.Random:
                    var random = new Random();
                    maps = maps.OrderBy(m => random.Next()).ToList();
                    break;
            }

            // Take the specified count
            return maps.Take(resultLimit.Count).ToList();
        }

        /// <summary>
        /// Checks if a map matches the filter preset
        /// </summary>
        public bool MatchesFilter(BeatSaverMap map, FilterPreset preset)
        {
            if (preset == null || map == null)
                return true;

            var activeGroups = preset.GetActiveGroups();
            if (!activeGroups.Any())
                return true;

            // Process groups with group-level AND/OR logic
            bool? groupResult = null;

            foreach (var group in activeGroups)
            {
                var conditions = group.GetActiveConditions();
                if (!conditions.Any()) continue;

                bool matchesGroup = MatchesGroupConditions(map, conditions);

                if (groupResult == null)
                {
                    groupResult = matchesGroup;
                }
                else
                {
                    // Apply group operator
                    if (group.GroupOperator == LogicOperator.Or)
                        groupResult = groupResult.Value || matchesGroup;
                    else
                        groupResult = groupResult.Value && matchesGroup;
                }
            }

            return groupResult ?? true;
        }

        /// <summary>
        /// Checks if a map matches all conditions in a group (with AND/OR between conditions)
        /// </summary>
        private bool MatchesGroupConditions(BeatSaverMap map, List<FilterCondition> conditions)
        {
            bool? result = null;
            LogicOperator? lastOperator = null;

            // Handle NPS conditions together (MinNps and MaxNps should check the same difficulty)
            var npsConditions = conditions.Where(c => c.Type == FilterConditionType.MinNps || c.Type == FilterConditionType.MaxNps).ToList();
            bool? npsResult = null;
            bool npsProcessed = false;

            // Handle SS Stars conditions together
            var ssStarsConditions = conditions.Where(c => c.Type == FilterConditionType.MinSsStars || c.Type == FilterConditionType.MaxSsStars).ToList();
            bool? ssStarsResult = null;
            bool ssStarsProcessed = false;

            // Handle BL Stars conditions together
            var blStarsConditions = conditions.Where(c => c.Type == FilterConditionType.MinBlStars || c.Type == FilterConditionType.MaxBlStars).ToList();
            bool? blStarsResult = null;
            bool blStarsProcessed = false;

            for (int i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i];

                // Skip NPS conditions - they are handled separately
                if (condition.Type == FilterConditionType.MinNps || condition.Type == FilterConditionType.MaxNps)
                {
                    if (!npsProcessed && npsConditions.Any())
                    {
                        npsResult = CheckNpsRange(map, npsConditions);
                        npsProcessed = true;
                        var lastNpsCondition = npsConditions.LastOrDefault();
                        if (lastNpsCondition != null)
                            lastOperator = lastNpsCondition.Operator;
                    }
                    continue;
                }

                // Skip SS Stars conditions - they are handled separately
                if (condition.Type == FilterConditionType.MinSsStars || condition.Type == FilterConditionType.MaxSsStars)
                {
                    if (!ssStarsProcessed && ssStarsConditions.Any())
                    {
                        ssStarsResult = CheckSsStarsRange(map, ssStarsConditions);
                        ssStarsProcessed = true;
                        var lastSsCondition = ssStarsConditions.LastOrDefault();
                        if (lastSsCondition != null)
                            lastOperator = lastSsCondition.Operator;
                    }
                    continue;
                }

                // Skip BL Stars conditions - they are handled separately
                if (condition.Type == FilterConditionType.MinBlStars || condition.Type == FilterConditionType.MaxBlStars)
                {
                    if (!blStarsProcessed && blStarsConditions.Any())
                    {
                        blStarsResult = CheckBlStarsRange(map, blStarsConditions);
                        blStarsProcessed = true;
                        var lastBlCondition = blStarsConditions.LastOrDefault();
                        if (lastBlCondition != null)
                            lastOperator = lastBlCondition.Operator;
                    }
                    continue;
                }

                bool matches = MatchesCondition(map, condition);

                // Apply pending results first if they were computed
                if ((npsResult.HasValue || ssStarsResult.HasValue || blStarsResult.HasValue) && !result.HasValue)
                {
                    bool pendingResult = true;
                    if (npsResult.HasValue) pendingResult = pendingResult && npsResult.Value;
                    if (ssStarsResult.HasValue) pendingResult = pendingResult && ssStarsResult.Value;
                    if (blStarsResult.HasValue) pendingResult = pendingResult && blStarsResult.Value;

                    if (lastOperator == LogicOperator.Or)
                        result = pendingResult || matches;
                    else
                        result = pendingResult && matches;
                    lastOperator = null;
                }
                else if (result == null)
                {
                    result = matches;
                }
                else
                {
                    var prevCondition = conditions[i - 1];
                    if (prevCondition.Operator == LogicOperator.Or)
                        result = result.Value || matches;
                    else
                        result = result.Value && matches;
                }
            }

            // Combine remaining pending results
            if (result == null)
            {
                bool combined = true;
                if (npsResult.HasValue) combined = combined && npsResult.Value;
                if (ssStarsResult.HasValue) combined = combined && ssStarsResult.Value;
                if (blStarsResult.HasValue) combined = combined && blStarsResult.Value;
                return combined;
            }

            // Apply any remaining pending results
            if (npsResult.HasValue) result = result.Value && npsResult.Value;
            if (ssStarsResult.HasValue) result = result.Value && ssStarsResult.Value;
            if (blStarsResult.HasValue) result = result.Value && blStarsResult.Value;

            return result ?? true;
        }

        /// <summary>
        /// Checks if a map matches a single condition
        /// </summary>
        private bool MatchesCondition(BeatSaverMap map, FilterCondition condition)
        {
            if (condition?.Value == null || !condition.IsEnabled)
                return true;

            try
            {
                switch (condition.Type)
                {
                    // API-supported filters (also applied locally for consistency)
                    case FilterConditionType.Query:
                        var query = condition.Value?.ToString()?.ToLower() ?? "";
                        if (string.IsNullOrWhiteSpace(query)) return true;
                        return (map.Name?.ToLower().Contains(query) ?? false) ||
                               (map.Description?.ToLower().Contains(query) ?? false) ||
                               (map.Metadata?.SongName?.ToLower().Contains(query) ?? false) ||
                               (map.Metadata?.SongAuthorName?.ToLower().Contains(query) ?? false) ||
                               (map.Metadata?.LevelAuthorName?.ToLower().Contains(query) ?? false);

                    case FilterConditionType.MinBpm:
                        var minBpm = Convert.ToDouble(condition.Value);
                        return map.Metadata?.Bpm >= minBpm;

                    case FilterConditionType.MaxBpm:
                        var maxBpm = Convert.ToDouble(condition.Value);
                        return map.Metadata?.Bpm <= maxBpm;

                    case FilterConditionType.MinNps:
                    case FilterConditionType.MaxNps:
                        // NPS conditions are handled together in MatchesGroupConditions
                        // Return true here to avoid double-checking
                        return true;

                    case FilterConditionType.MinDuration:
                        var minDur = Convert.ToDouble(condition.Value);
                        return map.Metadata?.Duration >= minDur;

                    case FilterConditionType.MaxDuration:
                        var maxDur = Convert.ToDouble(condition.Value);
                        return map.Metadata?.Duration <= maxDur;

                    case FilterConditionType.MinSsStars:
                        {
                            var minStars = Convert.ToDouble(condition.Value);
                            return HasDiffWithStars(map, minStars, null);
                        }

                    case FilterConditionType.MaxSsStars:
                        {
                            var maxStars = Convert.ToDouble(condition.Value);
                            return HasDiffWithStars(map, null, maxStars);
                        }

                    case FilterConditionType.MinBlStars:
                        {
                            var minBlStars = Convert.ToDouble(condition.Value);
                            return HasDiffWithBlStars(map, minBlStars, null);
                        }

                    case FilterConditionType.MaxBlStars:
                        {
                            var maxBlStars = Convert.ToDouble(condition.Value);
                            return HasDiffWithBlStars(map, null, maxBlStars);
                        }

                    case FilterConditionType.Chroma:
                        if (condition.Value == null) return true;
                        return Convert.ToBoolean(condition.Value) == HasMod(map, "Chroma");

                    case FilterConditionType.Noodle:
                        if (condition.Value == null) return true;
                        return Convert.ToBoolean(condition.Value) == HasMod(map, "Noodle");

                    case FilterConditionType.Me:
                        if (condition.Value == null) return true;
                        return Convert.ToBoolean(condition.Value) == HasMod(map, "Me");

                    case FilterConditionType.Cinema:
                        if (condition.Value == null) return true;
                        return Convert.ToBoolean(condition.Value) == HasMod(map, "Cinema");

                    case FilterConditionType.Vivify:
                        if (condition.Value == null) return true;
                        return Convert.ToBoolean(condition.Value) == HasMod(map, "Vivify");

                    case FilterConditionType.Automapper:
                        var autoVal = condition.Value?.ToString();
                        if (autoVal == "仅AI谱")
                            return map.Automapper;
                        else if (autoVal == "排除AI谱")
                            return !map.Automapper;
                        return true;

                    case FilterConditionType.Curated:
                        if (condition.Value == null) return true;
                        return Convert.ToBoolean(condition.Value) == map.Curated;

                    case FilterConditionType.Verified:
                        if (condition.Value == null) return true;
                        return Convert.ToBoolean(condition.Value) == (map.Uploader?.Verified ?? false);

                    // Local cache-specific filters
                    case FilterConditionType.Ranked:
                        if (condition.Value == null) return true;
                        return Convert.ToBoolean(condition.Value) == map.Ranked;

                    case FilterConditionType.BlRanked:
                        if (condition.Value == null) return true;
                        return Convert.ToBoolean(condition.Value) == map.BlRanked;

                    case FilterConditionType.Qualified:
                        if (condition.Value == null) return true;
                        return Convert.ToBoolean(condition.Value) == map.Qualified;

                    case FilterConditionType.BlQualified:
                        if (condition.Value == null) return true;
                        return Convert.ToBoolean(condition.Value) == map.BlQualified;

                    case FilterConditionType.MinPlays:
                        var minPlays = Convert.ToInt32(condition.Value);
                        return map.Stats?.Plays >= minPlays;

                    case FilterConditionType.MaxPlays:
                        var maxPlays = Convert.ToInt32(condition.Value);
                        return map.Stats?.Plays <= maxPlays;

                    case FilterConditionType.MinDownloads:
                        var minDownloads = Convert.ToInt32(condition.Value);
                        return map.Stats?.Downloads >= minDownloads;

                    case FilterConditionType.MaxDownloads:
                        var maxDownloads = Convert.ToInt32(condition.Value);
                        return map.Stats?.Downloads <= maxDownloads;

                    case FilterConditionType.MinUpvotes:
                        var minUpvotes = Convert.ToInt32(condition.Value);
                        return map.Stats?.Upvotes >= minUpvotes;

                    case FilterConditionType.MaxUpvotes:
                        var maxUpvotes = Convert.ToInt32(condition.Value);
                        return map.Stats?.Upvotes <= maxUpvotes;

                    case FilterConditionType.MinDownvotes:
                        var minDownvotes = Convert.ToInt32(condition.Value);
                        return map.Stats?.Downvotes >= minDownvotes;

                    case FilterConditionType.MaxDownvotes:
                        var maxDownvotes = Convert.ToInt32(condition.Value);
                        return map.Stats?.Downvotes <= maxDownvotes;

                    case FilterConditionType.MinScore:
                        var minScore = Convert.ToDouble(condition.Value);
                        return map.Stats?.Score >= minScore;

                    case FilterConditionType.MaxScore:
                        var maxScore = Convert.ToDouble(condition.Value);
                        return map.Stats?.Score <= maxScore;

                    case FilterConditionType.Tags:
                        var tagQuery = condition.Value?.ToString()?.ToLower() ?? "";
                        if (string.IsNullOrWhiteSpace(tagQuery)) return true;
                        return map.Tags?.Any(t => t?.ToLower().Contains(tagQuery) ?? false) ?? false;

                    case FilterConditionType.UploaderName:
                        var uploaderQuery = condition.Value?.ToString()?.ToLower() ?? "";
                        if (string.IsNullOrWhiteSpace(uploaderQuery)) return true;
                        // Check both uploader name and ID
                        return (map.Uploader?.Name?.ToLower().Contains(uploaderQuery) ?? false) ||
                               (map.Uploader?.Id.ToString().Contains(uploaderQuery) ?? false);

                    // Vote ratio filters (0-100 percentage)
                    case FilterConditionType.MinUpvoteRatio:
                        {
                            var minRatio = Convert.ToDouble(condition.Value);
                            var ratio = CalculateUpvoteRatio(map);
                            return ratio >= minRatio;
                        }

                    case FilterConditionType.MaxUpvoteRatio:
                        {
                            var maxRatio = Convert.ToDouble(condition.Value);
                            var ratio = CalculateUpvoteRatio(map);
                            return ratio <= maxRatio;
                        }

                    case FilterConditionType.MinDownvoteRatio:
                        {
                            var minRatio = Convert.ToDouble(condition.Value);
                            var ratio = CalculateDownvoteRatio(map);
                            return ratio >= minRatio;
                        }

                    case FilterConditionType.MaxDownvoteRatio:
                        {
                            var maxRatio = Convert.ToDouble(condition.Value);
                            var ratio = CalculateDownvoteRatio(map);
                            return ratio <= maxRatio;
                        }

                    // Diff-specific filters (require checking versions[0].diffs)
                    case FilterConditionType.MinNjs:
                        var minNjs = Convert.ToDouble(condition.Value);
                        return HasDiffWithValue(map, d => d.Njs >= minNjs);

                    case FilterConditionType.MaxNjs:
                        var maxNjs = Convert.ToDouble(condition.Value);
                        return HasDiffWithValue(map, d => d.Njs > 0 && d.Njs <= maxNjs);

                    case FilterConditionType.MinBombs:
                        var minBombs = Convert.ToInt32(condition.Value);
                        return HasDiffWithValue(map, d => d.Bombs >= minBombs);

                    case FilterConditionType.MaxBombs:
                        var maxBombs = Convert.ToInt32(condition.Value);
                        return HasDiffWithValue(map, d => d.Bombs <= maxBombs);

                    case FilterConditionType.MinOffset:
                        var minOffset = Convert.ToDouble(condition.Value);
                        return HasDiffWithValue(map, d => d.Offset >= minOffset);

                    case FilterConditionType.MaxOffset:
                        var maxOffset = Convert.ToDouble(condition.Value);
                        return HasDiffWithValue(map, d => d.Offset <= maxOffset);

                    case FilterConditionType.MinEvents:
                        var minEvents = Convert.ToInt32(condition.Value);
                        return HasDiffWithValue(map, d => d.Events >= minEvents);

                    case FilterConditionType.MaxEvents:
                        var maxEvents = Convert.ToInt32(condition.Value);
                        return HasDiffWithValue(map, d => d.Events <= maxEvents);

                    case FilterConditionType.Characteristic:
                        var charVal = condition.Value?.ToString();
                        if (string.IsNullOrWhiteSpace(charVal)) return true;
                        return HasCharacteristic(map, charVal);

                    case FilterConditionType.Difficulty:
                        var diffVal = condition.Value?.ToString();
                        if (string.IsNullOrWhiteSpace(diffVal)) return true;
                        return HasDifficulty(map, diffVal);

                    // Additional mods
                    case FilterConditionType.Ne:
                        if (condition.Value == null) return true;
                        return Convert.ToBoolean(condition.Value) == HasMod(map, "Ne");

                    case FilterConditionType.CustomMod:
                        var customMod = condition.Value?.ToString()?.ToLower() ?? "";
                        if (string.IsNullOrWhiteSpace(customMod)) return true;
                        return HasCustomMod(map, customMod);

                    // Upload time filters
                    case FilterConditionType.MinUploadedDate:
                        {
                            if (condition.Value == null) return true;
                            var minDate = Convert.ToDateTime(condition.Value);
                            return map.Uploaded >= minDate;
                        }

                    case FilterConditionType.MaxUploadedDate:
                        {
                            if (condition.Value == null) return true;
                            var maxDate = Convert.ToDateTime(condition.Value);
                            return map.Uploaded <= maxDate;
                        }

                    // ResultLimit is handled separately in ApplyResultLimit method
                    case FilterConditionType.ResultLimit:
                        return true;

                    default:
                        return true;
                }
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the maximum NPS across all difficulties
        /// </summary>
        private double GetMaxNps(BeatSaverMap map)
        {
            if (map?.Metadata?.Characteristics == null)
                return 0;

            double maxNps = 0;
            foreach (var characteristic in map.Metadata.Characteristics)
            {
                if (characteristic.Difficulties == null) continue;

                var difficulties = new[]
                {
                    characteristic.Difficulties.Easy,
                    characteristic.Difficulties.Normal,
                    characteristic.Difficulties.Hard,
                    characteristic.Difficulties.Expert,
                    characteristic.Difficulties.ExpertPlus
                };

                foreach (var diff in difficulties)
                {
                    if (diff != null && diff.Nps > maxNps)
                        maxNps = diff.Nps;
                }
            }

            return maxNps;
        }

        /// <summary>
        /// Checks if a map has at least one difficulty with NPS >= minNps
        /// </summary>
        private bool HasNpsAtLeast(BeatSaverMap map, double minNps)
        {
            if (map?.Metadata?.Characteristics == null)
                return false;

            foreach (var characteristic in map.Metadata.Characteristics)
            {
                if (characteristic.Difficulties == null) continue;

                var difficulties = new[]
                {
                    characteristic.Difficulties.Easy,
                    characteristic.Difficulties.Normal,
                    characteristic.Difficulties.Hard,
                    characteristic.Difficulties.Expert,
                    characteristic.Difficulties.ExpertPlus
                };

                foreach (var diff in difficulties)
                {
                    if (diff != null && diff.Nps >= minNps)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a map has at least one difficulty with NPS <= maxNps
        /// </summary>
        private bool HasNpsAtMost(BeatSaverMap map, double maxNps)
        {
            if (map?.Metadata?.Characteristics == null)
                return false;

            foreach (var characteristic in map.Metadata.Characteristics)
            {
                if (characteristic.Difficulties == null) continue;

                var difficulties = new[]
                {
                    characteristic.Difficulties.Easy,
                    characteristic.Difficulties.Normal,
                    characteristic.Difficulties.Hard,
                    characteristic.Difficulties.Expert,
                    characteristic.Difficulties.ExpertPlus
                };

                foreach (var diff in difficulties)
                {
                    if (diff != null && diff.Nps > 0 && diff.Nps <= maxNps)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a map has at least one difficulty with NPS in the specified range.
        /// This method handles both MinNps and MaxNps conditions together to ensure
        /// at least one difficulty satisfies BOTH conditions.
        /// </summary>
        private bool CheckNpsRange(BeatSaverMap map, List<FilterCondition> conditions)
        {
            if (map == null) return false;

            // Extract MinNps and MaxNps from conditions
            double? minNps = null;
            double? maxNps = null;

            foreach (var condition in conditions)
            {
                if (condition.Type == FilterConditionType.MinNps && condition.Value != null)
                    minNps = Convert.ToDouble(condition.Value);
                else if (condition.Type == FilterConditionType.MaxNps && condition.Value != null)
                    maxNps = Convert.ToDouble(condition.Value);
            }

            // If no NPS conditions, return true
            if (minNps == null && maxNps == null)
                return true;

            // First try to check from Versions[0].Diffs (local cache format)
            if (map.Versions != null && map.Versions.Count > 0 && map.Versions[0].Diffs != null)
            {
                foreach (var diff in map.Versions[0].Diffs)
                {
                    if (diff == null || diff.Nps <= 0)
                        continue;

                    bool matchesMin = minNps == null || diff.Nps >= minNps.Value;
                    bool matchesMax = maxNps == null || diff.Nps <= maxNps.Value;

                    if (matchesMin && matchesMax)
                        return true;
                }
            }

            // Also check from Metadata.Characteristics (API format)
            if (map.Metadata?.Characteristics != null)
            {
                // Check each difficulty to see if any satisfies BOTH conditions
                foreach (var characteristic in map.Metadata.Characteristics)
                {
                    if (characteristic.Difficulties == null) continue;

                    var difficulties = new[]
                    {
                        characteristic.Difficulties.Easy,
                        characteristic.Difficulties.Normal,
                        characteristic.Difficulties.Hard,
                        characteristic.Difficulties.Expert,
                        characteristic.Difficulties.ExpertPlus
                    };

                    foreach (var diff in difficulties)
                    {
                        if (diff == null || diff.Nps <= 0)
                            continue;

                        bool matchesMin = minNps == null || diff.Nps >= minNps.Value;
                        bool matchesMax = maxNps == null || diff.Nps <= maxNps.Value;

                        if (matchesMin && matchesMax)
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a map has at least one difficulty with SS Stars in the specified range
        /// </summary>
        private bool CheckSsStarsRange(BeatSaverMap map, List<FilterCondition> conditions)
        {
            if (map == null) return false;

            double? minStars = null;
            double? maxStars = null;

            foreach (var condition in conditions)
            {
                if (condition.Type == FilterConditionType.MinSsStars && condition.Value != null)
                    minStars = Convert.ToDouble(condition.Value);
                else if (condition.Type == FilterConditionType.MaxSsStars && condition.Value != null)
                    maxStars = Convert.ToDouble(condition.Value);
            }

            if (minStars == null && maxStars == null)
                return true;

            return HasDiffWithStars(map, minStars, maxStars);
        }

        /// <summary>
        /// Checks if a map has at least one difficulty with BL Stars in the specified range
        /// </summary>
        private bool CheckBlStarsRange(BeatSaverMap map, List<FilterCondition> conditions)
        {
            if (map == null) return false;

            double? minStars = null;
            double? maxStars = null;

            foreach (var condition in conditions)
            {
                if (condition.Type == FilterConditionType.MinBlStars && condition.Value != null)
                    minStars = Convert.ToDouble(condition.Value);
                else if (condition.Type == FilterConditionType.MaxBlStars && condition.Value != null)
                    maxStars = Convert.ToDouble(condition.Value);
            }

            if (minStars == null && maxStars == null)
                return true;

            return HasDiffWithBlStars(map, minStars, maxStars);
        }

        /// <summary>
        /// Checks if a map has a specific mod requirement
        /// </summary>
        private bool HasMod(BeatSaverMap map, string modName)
        {
            // First check from Versions[0].Diffs (local cache format)
            if (map.Versions != null && map.Versions.Count > 0 && map.Versions[0].Diffs != null)
            {
                foreach (var diff in map.Versions[0].Diffs)
                {
                    if (diff == null) continue;

                    switch (modName)
                    {
                        case "Chroma":
                            if (diff.Chroma) return true;
                            break;
                        case "Noodle":
                        case "Ne":
                            if (diff.Ne) return true; // NE = Noodle Extensions
                            break;
                        case "Me":
                            if (diff.Me) return true;
                            break;
                        case "Cinema":
                            if (diff.Cinema) return true;
                            break;
                        case "Vivify":
                            if (diff.Vivify) return true;
                            break;
                    }
                }
            }

            // Also check from Metadata.Characteristics (API format)
            if (map.Metadata?.Characteristics == null)
                return false;

            foreach (var characteristic in map.Metadata.Characteristics)
            {
                if (characteristic.Difficulties == null) continue;

                var difficulties = new[]
                {
                    characteristic.Difficulties.Easy,
                    characteristic.Difficulties.Normal,
                    characteristic.Difficulties.Hard,
                    characteristic.Difficulties.Expert,
                    characteristic.Difficulties.ExpertPlus
                };

                foreach (var diff in difficulties)
                {
                    if (diff == null) continue;

                    switch (modName)
                    {
                        case "Chroma":
                            if (diff.Chroma) return true;
                            break;
                        case "Noodle":
                        case "Ne":
                            if (diff.Ne) return true; // NE = Noodle Extensions
                            break;
                        case "Me":
                            if (diff.Me) return true;
                            break;
                        case "Cinema":
                            if (diff.Cinema) return true;
                            break;
                        case "Vivify":
                            if (diff.Vivify) return true;
                            break;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a map has at least one difficulty with stars in the specified range
        /// </summary>
        private bool HasDiffWithStars(BeatSaverMap map, double? minStars, double? maxStars)
        {
            if (map.Versions == null || map.Versions.Count == 0 || map.Versions[0].Diffs == null)
                return false;

            foreach (var diff in map.Versions[0].Diffs)
            {
                if (diff == null || !diff.Stars.HasValue) continue;

                bool matchesMin = minStars == null || diff.Stars.Value >= minStars.Value;
                bool matchesMax = maxStars == null || diff.Stars.Value <= maxStars.Value;

                if (matchesMin && matchesMax)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a map has at least one difficulty with BL stars in the specified range
        /// </summary>
        private bool HasDiffWithBlStars(BeatSaverMap map, double? minStars, double? maxStars)
        {
            if (map.Versions == null || map.Versions.Count == 0 || map.Versions[0].Diffs == null)
                return false;

            foreach (var diff in map.Versions[0].Diffs)
            {
                if (diff == null || !diff.BlStars.HasValue) continue;

                bool matchesMin = minStars == null || diff.BlStars.Value >= minStars.Value;
                bool matchesMax = maxStars == null || diff.BlStars.Value <= maxStars.Value;

                if (matchesMin && matchesMax)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a map has at least one difficulty matching the predicate
        /// </summary>
        private bool HasDiffWithValue(BeatSaverMap map, Func<BeatSaverVersionDiff, bool> predicate)
        {
            // Check from Versions[0].Diffs (local cache format)
            if (map.Versions != null && map.Versions.Count > 0 && map.Versions[0].Diffs != null)
            {
                foreach (var diff in map.Versions[0].Diffs)
                {
                    if (diff != null && predicate(diff))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a map has a specific characteristic
        /// </summary>
        private bool HasCharacteristic(BeatSaverMap map, string characteristic)
        {
            // Check from Versions[0].Diffs (local cache format)
            if (map.Versions != null && map.Versions.Count > 0 && map.Versions[0].Diffs != null)
            {
                foreach (var diff in map.Versions[0].Diffs)
                {
                    if (diff?.Characteristic?.Equals(characteristic, StringComparison.OrdinalIgnoreCase) == true)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a map has a specific difficulty
        /// </summary>
        private bool HasDifficulty(BeatSaverMap map, string difficulty)
        {
            // Check from Versions[0].Diffs (local cache format)
            if (map.Versions != null && map.Versions.Count > 0 && map.Versions[0].Diffs != null)
            {
                foreach (var diff in map.Versions[0].Diffs)
                {
                    if (diff?.Difficulty?.Equals(difficulty, StringComparison.OrdinalIgnoreCase) == true)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a map has a custom mod by checking the diff properties dynamically
        /// </summary>
        private bool HasCustomMod(BeatSaverMap map, string modName)
        {
            if (map.Versions == null || map.Versions.Count == 0 || map.Versions[0].Diffs == null)
                return false;

            foreach (var diff in map.Versions[0].Diffs)
            {
                if (diff == null) continue;

                // Use reflection to check for boolean properties matching the mod name
                var diffType = diff.GetType();
                var property = diffType.GetProperty(modName, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (property != null && property.PropertyType == typeof(bool))
                {
                    var value = (bool)property.GetValue(diff);
                    if (value) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Calculates the upvote ratio as a percentage (0-100)
        /// Returns 0 if there are no votes
        /// </summary>
        private double CalculateUpvoteRatio(BeatSaverMap map)
        {
            if (map.Stats == null) return 0;

            var upvotes = map.Stats.Upvotes;
            var downvotes = map.Stats.Downvotes;
            var total = upvotes + downvotes;

            if (total == 0) return 0;

            return (double)upvotes / total * 100;
        }

        /// <summary>
        /// Calculates the downvote ratio as a percentage (0-100)
        /// Returns 0 if there are no votes
        /// </summary>
        private double CalculateDownvoteRatio(BeatSaverMap map)
        {
            if (map.Stats == null) return 0;

            var upvotes = map.Stats.Upvotes;
            var downvotes = map.Stats.Downvotes;
            var total = upvotes + downvotes;

            if (total == 0) return 0;

            return (double)downvotes / total * 100;
        }

        /// <summary>
        /// Gets the cache file size in bytes
        /// </summary>
        public long GetCacheFileSize()
        {
            if (File.Exists(cachePath))
            {
                return new FileInfo(cachePath).Length;
            }
            return 0;
        }

        /// <summary>
        /// Sets the cache path for testing purposes
        /// </summary>
        public void SetCachePath(string path)
        {
            cachePath = path;
            cacheAvailable = File.Exists(path);
        }

        /// <summary>
        /// Debug test method for filtering
        /// </summary>
        public static void RunFilterTest(string cachePath)
        {
            Console.WriteLine("=== LocalCacheManager Filter Test ===\n");

            // Test 1: Direct JSON parsing to count ranked maps
            Console.WriteLine("Test 1: Direct JSON parsing for Ranked=true...");
            int directRankedCount = 0;
            int directBlRankedCount = 0;
            int totalCount = 0;
            string firstRankedMap = null;

            using (var fileStream = new FileStream(cachePath, FileMode.Open, FileAccess.Read))
            using (var streamReader = new StreamReader(fileStream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer();

                // Navigate to docs array
                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonToken.StartArray && jsonReader.Path == "docs")
                        break;
                }

                while (jsonReader.Read() && jsonReader.TokenType != JsonToken.EndArray)
                {
                    if (jsonReader.TokenType == JsonToken.StartObject)
                    {
                        try
                        {
                            var map = serializer.Deserialize<BeatSaverMap>(jsonReader);
                            totalCount++;

                            if (map != null)
                            {
                                if (map.Ranked)
                                {
                                    directRankedCount++;
                                    if (firstRankedMap == null)
                                        firstRankedMap = $"{map.Name} (ID: {map.Id}, Ranked={map.Ranked})";
                                }
                                if (map.BlRanked)
                                    directBlRankedCount++;
                            }

                            if (totalCount % 20000 == 0)
                                Console.WriteLine($"  Processed {totalCount} maps...");
                        }
                        catch { }
                    }
                }
            }

            Console.WriteLine($"  Total maps: {totalCount}");
            Console.WriteLine($"  Ranked=true: {directRankedCount}");
            Console.WriteLine($"  BlRanked=true: {directBlRankedCount}");
            if (firstRankedMap != null)
                Console.WriteLine($"  First ranked map: {firstRankedMap}");
            Console.WriteLine();

            // Test 2: Using LocalCacheManager filter
            Console.WriteLine("Test 2: Using LocalCacheManager.StreamFilterMaps...");

            var manager = new LocalCacheManager();
            manager.SetCachePath(cachePath);

            var preset = new FilterPreset("Test Ranked");
            var group = new FilterGroup("Group1");
            group.UseLocalCache = true;

            var condition = new FilterCondition(FilterConditionType.Ranked);
            condition.Value = true;
            condition.IsEnabled = true;
            group.AddCondition(condition);
            preset.AddGroup(group);

            Console.WriteLine($"  Condition Type: {condition.Type} (int: {(int)condition.Type})");
            Console.WriteLine($"  Condition Value: {condition.Value}");
            Console.WriteLine($"  Condition HasValue: {condition.HasValue()}");
            Console.WriteLine($"  RequiresLocalCache: {FilterConditionMetadata.RequiresLocalCache(condition.Type)}");
            Console.WriteLine($"  Group UseLocalCache: {group.UseLocalCache}");

            int filteredCount = 0;
            string firstFiltered = null;
            foreach (var map in manager.StreamFilterMaps(preset, null))
            {
                filteredCount++;
                if (firstFiltered == null)
                    firstFiltered = $"{map.Name} (Ranked={map.Ranked})";

                if (filteredCount >= 10)
                    break; // Only get first 10 for quick test
            }

            Console.WriteLine($"  Filtered count (first pass): {filteredCount}");
            if (firstFiltered != null)
                Console.WriteLine($"  First filtered: {firstFiltered}");
            Console.WriteLine();

            // Test 3: Debug individual map matching
            Console.WriteLine("Test 3: Debug individual map matching...");
            if (directRankedCount > 0)
            {
                // Get a ranked map
                BeatSaverMap testMap = null;
                using (var fs = new FileStream(cachePath, FileMode.Open, FileAccess.Read))
                using (var sr = new StreamReader(fs))
                using (var jr = new JsonTextReader(sr))
                {
                    var ser = new JsonSerializer();
                    while (jr.Read())
                    {
                        if (jr.TokenType == JsonToken.StartArray && jr.Path == "docs")
                            break;
                    }
                    while (jr.Read() && jr.TokenType != JsonToken.EndArray)
                    {
                        if (jr.TokenType == JsonToken.StartObject)
                        {
                            var m = ser.Deserialize<BeatSaverMap>(jr);
                            if (m != null && m.Ranked)
                            {
                                testMap = m;
                                break;
                            }
                        }
                    }
                }

                if (testMap != null)
                {
                    Console.WriteLine($"  Test map: {testMap.Name}");
                    Console.WriteLine($"  testMap.Ranked = {testMap.Ranked}");
                    Console.WriteLine($"  testMap.BlRanked = {testMap.BlRanked}");

                    var activeGroups = preset.GetActiveGroups();
                    Console.WriteLine($"  Active groups count: {activeGroups.Count}");

                    foreach (var g in activeGroups)
                    {
                        Console.WriteLine($"  Group: {g.Name}, UseLocalCache: {g.UseLocalCache}");
                        var activeConditions = g.GetActiveConditions();
                        Console.WriteLine($"  Active conditions: {activeConditions.Count}");

                        foreach (var c in activeConditions)
                        {
                            Console.WriteLine($"    Condition: {c.Type}, Value: {c.Value}, IsEnabled: {c.IsEnabled}");
                        }
                    }

                    bool matches = manager.MatchesFilter(testMap, preset);
                    Console.WriteLine($"  MatchesFilter result: {matches}");
                }
            }

            Console.WriteLine("\n=== Test Complete ===");
        }

        /// <summary>
        /// Deletes the cache file
        /// </summary>
        public void DeleteCache()
        {
            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
                cacheAvailable = false;
                cacheDate = 0;
            }
        }

        /// <summary>
        /// Public test method for NPS filter debugging
        /// </summary>
        public bool TestMatchesFilter(BeatSaverMap map, FilterPreset preset)
        {
            return MatchesFilter(map, preset);
        }

        /// <summary>
        /// Gets debug info about NPS conditions
        /// </summary>
        public string DebugNpsConditions(BeatSaverMap map, List<FilterCondition> conditions)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Map: {map?.Name ?? "null"}");
            sb.AppendLine($"Characteristics: {map?.Metadata?.Characteristics?.Count ?? 0}");

            if (map?.Metadata?.Characteristics != null)
            {
                foreach (var charac in map.Metadata.Characteristics)
                {
                    sb.AppendLine($"  Characteristic: {charac.Name}");
                    if (charac.Difficulties != null)
                    {
                        var diffs = new[] { charac.Difficulties.Easy, charac.Difficulties.Normal, charac.Difficulties.Hard, charac.Difficulties.Expert, charac.Difficulties.ExpertPlus };
                        var names = new[] { "Easy", "Normal", "Hard", "Expert", "ExpertPlus" };
                        for (int i = 0; i < diffs.Length; i++)
                        {
                            if (diffs[i] != null)
                                sb.AppendLine($"    {names[i]}: NPS={diffs[i].Nps}");
                        }
                    }
                }
            }

            sb.AppendLine("Conditions:");
            foreach (var cond in conditions)
            {
                sb.AppendLine($"  {cond.Type}: {cond.Value}");
            }

            sb.AppendLine($"CheckNpsRange result: {CheckNpsRange(map, conditions)}");

            return sb.ToString();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                httpClient?.Dispose();
                disposed = true;
            }
        }
    }
}