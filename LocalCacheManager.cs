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

        // 轻量级读取器（用于批处理缓存复用）
        private LocalCacheReader sharedReader;

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
        /// Streams through the cache and filters maps based on preset (uses lightweight data structure)
        /// </summary>
        public IEnumerable<BeatSaverMapSlim> StreamFilterMapsSlim(FilterPreset preset, IProgress<int> progress, CancellationToken cancellationToken = default)
        {
            if (!IsCacheAvailable)
                yield break;

            using var reader = new LocalCacheReader(cachePath);
            foreach (var map in reader.StreamFilterMaps(preset, progress))
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;
                yield return map;
            }
        }

        /// <summary>
        /// Streams through the cache and filters maps based on preset
        /// Returns full BeatSaverMap objects (converts from lightweight structure)
        /// </summary>
        public IEnumerable<BeatSaverMap> StreamFilterMaps(FilterPreset preset, IProgress<int> progress, CancellationToken cancellationToken = default)
        {
            foreach (var slimMap in StreamFilterMapsSlim(preset, progress, cancellationToken))
            {
                yield return slimMap.ToFullMap();
            }
        }

        /// <summary>
        /// Initializes shared reader for batch processing (reuses cache data across multiple filter operations)
        /// Call ClearSharedReader() after batch processing to free memory
        /// </summary>
        public void InitializeSharedReader(bool preloadData = false, IProgress<int> progress = null)
        {
            if (sharedReader == null)
            {
                sharedReader = new LocalCacheReader(cachePath);
                if (preloadData)
                {
                    sharedReader.PreloadForBatchProcessing(progress);
                }
            }
        }

        /// <summary>
        /// Filters using shared reader (for batch processing)
        /// </summary>
        public IEnumerable<BeatSaverMapSlim> StreamFilterMapsShared(FilterPreset preset, IProgress<int> progress, CancellationToken cancellationToken = default)
        {
            if (!IsCacheAvailable)
                yield break;

            if (sharedReader == null)
                InitializeSharedReader();

            foreach (var map in sharedReader.StreamFilterMaps(preset, progress))
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;
                yield return map;
            }
        }

        /// <summary>
        /// Clears the shared reader to free memory
        /// </summary>
        public void ClearSharedReader()
        {
            sharedReader?.Dispose();
            sharedReader = null;
        }

        /// <summary>
        /// Performs parallel filtering on the cache (uses lightweight data structure for memory efficiency)
        /// </summary>
        public List<BeatSaverMap> ParallelFilterMaps(FilterPreset preset, IProgress<int> progress, CancellationToken cancellationToken = default)
        {
            if (!IsCacheAvailable)
                return new List<BeatSaverMap>();

            // 使用轻量级结构进行筛选
            var results = new List<BeatSaverMapSlim>();
            int processed = 0;
            long lastReportBytes = 0;
            int reportInterval = 1024 * 1024;

            // 第一遍：流式读取并筛选
            using (var reader = new LocalCacheReader(cachePath))
            {
                foreach (var map in reader.StreamFilterMaps(preset, null))
                {
                    if (cancellationToken.IsCancellationRequested)
                        return new List<BeatSaverMap>();

                    results.Add(map);
                    processed++;

                    // 进度报告
                    long pos = reader.CurrentPosition;
                    if (progress != null && pos - lastReportBytes > reportInterval)
                    {
                        progress.Report((int)(pos * 100 / reader.CacheSize));
                        lastReportBytes = pos;
                    }
                }
            }

            progress?.Report(100);

            // 应用结果限制
            var limitedResults = ApplyResultLimitSlim(results, preset);

            // 转换为完整对象
            return limitedResults.Select(m => m.ToFullMap()).ToList();
        }

        /// <summary>
        /// Batch filter multiple presets efficiently (reuses cache data)
        /// </summary>
        public List<List<BeatSaverMap>> BatchFilterMaps(List<FilterPreset> presets, IProgress<int> progress, CancellationToken cancellationToken = default)
        {
            if (!IsCacheAvailable || presets == null || presets.Count == 0)
                return new List<List<BeatSaverMap>>();

            var results = new List<List<BeatSaverMap>>();
            int processedPresets = 0;

            // 初始化共享读取器
            InitializeSharedReader(true, new Progress<int>(p =>
            {
                progress?.Report(p / 2); // 加载占前半进度
            }));

            try
            {
                foreach (var preset in presets)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var presetResults = new List<BeatSaverMapSlim>();
                    foreach (var map in StreamFilterMapsShared(preset, null, cancellationToken))
                    {
                        presetResults.Add(map);
                    }

                    var limitedResults = ApplyResultLimitSlim(presetResults, preset);
                    results.Add(limitedResults.Select(m => m.ToFullMap()).ToList());

                    processedPresets++;
                    progress?.Report(50 + processedPresets * 50 / presets.Count);
                }
            }
            finally
            {
                ClearSharedReader();
            }

            progress?.Report(100);
            return results;
        }

        /// <summary>
        /// Gets the result limit from a preset (checks top-level first, then group-level)
        /// </summary>
        private ResultLimitValue GetResultLimitFromPreset(FilterPreset preset)
        {
            var resultLimit = preset.TopLevelResultLimit;
            if (resultLimit == null)
            {
                foreach (var group in preset.GetActiveGroups())
                {
                    resultLimit = group.GetResultLimit();
                    if (resultLimit != null) break;
                }
            }
            return resultLimit;
        }

        /// <summary>
        /// Applies result limit to lightweight map list
        /// </summary>
        private List<BeatSaverMapSlim> ApplyResultLimitSlim(List<BeatSaverMapSlim> maps, FilterPreset preset)
        {
            var resultLimit = GetResultLimitFromPreset(preset);
            if (resultLimit == null || resultLimit.Count <= 0)
                return maps;

            return ApplySortAndLimit(maps, resultLimit, m => m.Uploaded);
        }

        /// <summary>
        /// Applies result limit (count + sort) to the filtered results
        /// </summary>
        private List<BeatSaverMap> ApplyResultLimit(List<BeatSaverMap> maps, FilterPreset preset)
        {
            var resultLimit = GetResultLimitFromPreset(preset);
            if (resultLimit == null || resultLimit.Count <= 0)
                return maps;

            return ApplySortAndLimit(maps, resultLimit, m => m.Uploaded);
        }

        /// <summary>
        /// Generic method to apply sorting and limit to a list
        /// </summary>
        private List<T> ApplySortAndLimit<T>(List<T> list, ResultLimitValue resultLimit, Func<T, DateTime> dateSelector)
        {
            switch (resultLimit.SortOption)
            {
                case ResultSortOption.Newest:
                    list = list.OrderByDescending(dateSelector).ToList();
                    break;
                case ResultSortOption.Oldest:
                    list = list.OrderBy(dateSelector).ToList();
                    break;
                case ResultSortOption.Random:
                    var random = new Random();
                    list = list.OrderBy(m => random.Next()).ToList();
                    break;
            }
            return list.Take(resultLimit.Count).ToList();
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
            LogicOperator? prevGroupOperator = null;

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
                    // Apply the PREVIOUS group's operator to combine with current group
                    if (prevGroupOperator == LogicOperator.Or)
                        groupResult = groupResult.Value || matchesGroup;
                    else
                        groupResult = groupResult.Value && matchesGroup;
                }

                // Store this group's operator for the next iteration
                prevGroupOperator = group.GroupOperator;
            }

            return groupResult ?? true;
        }

        /// <summary>
        /// Checks if a map matches all conditions in a group (with AND/OR between conditions)
        /// </summary>
        private bool MatchesGroupConditions(BeatSaverMap map, List<FilterCondition> conditions)
        {
            bool? result = null;
            LogicOperator? prevOperator = null;

            // Handle NPS conditions together (MinNps and MaxNps should check the same difficulty)
            var npsConditions = conditions.Where(c => c.Type == FilterConditionType.MinNps || c.Type == FilterConditionType.MaxNps).ToList();
            bool npsProcessed = false;

            // Handle SS Stars conditions together
            var ssStarsConditions = conditions.Where(c => c.Type == FilterConditionType.MinSsStars || c.Type == FilterConditionType.MaxSsStars).ToList();
            bool ssStarsProcessed = false;

            // Handle BL Stars conditions together
            var blStarsConditions = conditions.Where(c => c.Type == FilterConditionType.MinBlStars || c.Type == FilterConditionType.MaxBlStars).ToList();
            bool blStarsProcessed = false;

            for (int i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i];
                bool? conditionResult = null;

                // Handle NPS conditions together
                if (condition.Type == FilterConditionType.MinNps || condition.Type == FilterConditionType.MaxNps)
                {
                    if (!npsProcessed && npsConditions.Any())
                    {
                        conditionResult = CheckNpsRange(map, npsConditions);
                        npsProcessed = true;
                    }
                }
                // Handle SS Stars conditions together
                else if (condition.Type == FilterConditionType.MinSsStars || condition.Type == FilterConditionType.MaxSsStars)
                {
                    if (!ssStarsProcessed && ssStarsConditions.Any())
                    {
                        conditionResult = CheckSsStarsRange(map, ssStarsConditions);
                        ssStarsProcessed = true;
                    }
                }
                // Handle BL Stars conditions together
                else if (condition.Type == FilterConditionType.MinBlStars || condition.Type == FilterConditionType.MaxBlStars)
                {
                    if (!blStarsProcessed && blStarsConditions.Any())
                    {
                        conditionResult = CheckBlStarsRange(map, blStarsConditions);
                        blStarsProcessed = true;
                    }
                }
                else
                {
                    // Regular condition
                    conditionResult = MatchesCondition(map, condition);
                }

                // Combine result using the operator from the PREVIOUS condition
                if (conditionResult.HasValue)
                {
                    if (result == null)
                    {
                        result = conditionResult.Value;
                    }
                    else
                    {
                        if (prevOperator == LogicOperator.Or)
                            result = result.Value || conditionResult.Value;
                        else
                            result = result.Value && conditionResult.Value;
                    }
                }

                // Update prevOperator for the next condition
                prevOperator = condition.Operator;
            }

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
                // Handle range-type conditions
                if (FilterConditionMetadata.IsRangeType(condition.Type))
                {
                    return MatchesRangeCondition(map, condition);
                }

                switch (condition.Type)
                {
                    // API-supported filters (also applied locally for consistency)
                    case FilterConditionType.Query:
                        {
                            string query;
                            SearchFieldType fieldTypes;

                            // Handle SearchQueryValue with field types (multi-select)
                            if (condition.Value is SearchQueryValue queryValue)
                            {
                                query = queryValue.Query?.ToLower() ?? "";
                                fieldTypes = queryValue.FieldTypes;
                            }
                            else
                            {
                                // Backward compatibility: treat as string with All field type
                                query = condition.Value?.ToString()?.ToLower() ?? "";
                                fieldTypes = SearchFieldType.All;
                            }

                            if (string.IsNullOrWhiteSpace(query)) return true;

                            // If no field types selected, search all
                            if (fieldTypes == SearchFieldType.None)
                                fieldTypes = SearchFieldType.All;

                            // Check each selected field type
                            bool matches = false;

                            if (fieldTypes.HasFlag(SearchFieldType.SongName))
                                matches |= map.Metadata?.SongName?.ToLower().Contains(query) ?? false;

                            if (fieldTypes.HasFlag(SearchFieldType.Artist))
                                matches |= map.Metadata?.SongAuthorName?.ToLower().Contains(query) ?? false;

                            if (fieldTypes.HasFlag(SearchFieldType.Mapper))
                                matches |= map.Metadata?.LevelAuthorName?.ToLower().Contains(query) ?? false;

                            if (fieldTypes.HasFlag(SearchFieldType.MapName))
                                matches |= map.Name?.ToLower().Contains(query) ?? false;

                            if (fieldTypes.HasFlag(SearchFieldType.Description))
                                matches |= map.Description?.ToLower().Contains(query) ?? false;

                            if (fieldTypes.HasFlag(SearchFieldType.Uploader))
                                matches |= map.Uploader?.Name?.ToLower().Contains(query) ?? false;

                            return matches;
                        }

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
                        var minScore = Convert.ToDouble(condition.Value) / 100.0;  // 用户输入0-100，API返回0-1
                        return map.Stats?.Score >= minScore;

                    case FilterConditionType.MaxScore:
                        var maxScore = Convert.ToDouble(condition.Value) / 100.0;  // 用户输入0-100，API返回0-1
                        return map.Stats?.Score <= maxScore;

                    case FilterConditionType.Tags:
                        var tagQuery = condition.Value?.ToString()?.ToLower() ?? "";
                        if (string.IsNullOrWhiteSpace(tagQuery)) return true;
                        return map.Tags?.Any(t => t?.ToLower().Contains(tagQuery) ?? false) ?? false;

                    case FilterConditionType.ExcludeTags:
                        var excludeTagQuery = condition.Value?.ToString()?.ToLower() ?? "";
                        if (string.IsNullOrWhiteSpace(excludeTagQuery)) return true;
                        // 排除包含该标签的地图
                        return !(map.Tags?.Any(t => t?.ToLower().Contains(excludeTagQuery) ?? false) ?? false);

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

                    case FilterConditionType.MinSageScore:
                        {
                            var minSageScore = Convert.ToInt32(condition.Value);
                            return map.Versions != null && map.Versions.Count > 0 &&
                                   map.Versions[0].SageScore.HasValue &&
                                   map.Versions[0].SageScore.Value >= minSageScore;
                        }

                    case FilterConditionType.MaxSageScore:
                        {
                            var maxSageScore = Convert.ToInt32(condition.Value);
                            return map.Versions != null && map.Versions.Count > 0 &&
                                   map.Versions[0].SageScore.HasValue &&
                                   map.Versions[0].SageScore.Value <= maxSageScore;
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

                    case FilterConditionType.ExcludeCustomMod:
                        {
                            string excludeCustomMod;
                            bool strictMode = false;

                            // Handle ExcludeModValue with strict mode
                            if (condition.Value is ExcludeModValue excludeModValue)
                            {
                                excludeCustomMod = excludeModValue.ModName?.ToLower() ?? "";
                                strictMode = excludeModValue.Strict;
                            }
                            else
                            {
                                // Backward compatibility: old string value
                                excludeCustomMod = condition.Value?.ToString()?.ToLower() ?? "";
                            }

                            if (string.IsNullOrWhiteSpace(excludeCustomMod)) return true;

                            if (strictMode)
                            {
                                // 严格模式：任意难度都不得包含该Mod
                                return !HasCustomMod(map, excludeCustomMod);
                            }
                            else
                            {
                                // 非严格模式：只要有一个难度不包含该Mod即可
                                return HasDiffWithoutCustomMod(map, excludeCustomMod);
                            }
                        }

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

                    // Map objects filters (check across all difficulties in versions[0].diffs)
                    case FilterConditionType.MinObstacles:
                        var minObstacles = Convert.ToInt32(condition.Value);
                        return HasDiffWithValue(map, d => d.Obstacles >= minObstacles);

                    case FilterConditionType.MaxObstacles:
                        var maxObstacles = Convert.ToInt32(condition.Value);
                        return HasDiffWithValue(map, d => d.Obstacles <= maxObstacles);

                    case FilterConditionType.MinBombsMap:
                        var minBombsMap = Convert.ToInt32(condition.Value);
                        return HasDiffWithValue(map, d => d.Bombs >= minBombsMap);

                    case FilterConditionType.MaxBombsMap:
                        var maxBombsMap = Convert.ToInt32(condition.Value);
                        return HasDiffWithValue(map, d => d.Bombs <= maxBombsMap);

                    case FilterConditionType.MinNotes:
                        var minNotes = Convert.ToInt32(condition.Value);
                        return HasDiffWithValue(map, d => d.Notes >= minNotes);

                    case FilterConditionType.MaxNotes:
                        var maxNotes = Convert.ToInt32(condition.Value);
                        return HasDiffWithValue(map, d => d.Notes <= maxNotes);

                    case FilterConditionType.MinSeconds:
                        var minSeconds = Convert.ToDouble(condition.Value);
                        return HasDiffWithValue(map, d => d.Seconds >= minSeconds);

                    case FilterConditionType.MaxSeconds:
                        var maxSeconds = Convert.ToDouble(condition.Value);
                        return HasDiffWithValue(map, d => d.Seconds <= maxSeconds);

                    case FilterConditionType.MinLength:
                        var minLength = Convert.ToDouble(condition.Value);
                        return HasDiffWithValue(map, d => d.Length >= minLength);

                    case FilterConditionType.MaxLength:
                        var maxLength = Convert.ToDouble(condition.Value);
                        return HasDiffWithValue(map, d => d.Length <= maxLength);

                    case FilterConditionType.MinParityErrors:
                        var minErrors = Convert.ToInt32(condition.Value);
                        return HasDiffWithParity(map, p => p.Errors >= minErrors);

                    case FilterConditionType.MaxParityErrors:
                        var maxErrors = Convert.ToInt32(condition.Value);
                        return HasDiffWithParity(map, p => p.Errors <= maxErrors);

                    case FilterConditionType.MinParityWarns:
                        var minWarns = Convert.ToInt32(condition.Value);
                        return HasDiffWithParity(map, p => p.Warns >= minWarns);

                    case FilterConditionType.MaxParityWarns:
                        var maxWarns = Convert.ToInt32(condition.Value);
                        return HasDiffWithParity(map, p => p.Warns <= maxWarns);

                    case FilterConditionType.MinParityResets:
                        var minResets = Convert.ToInt32(condition.Value);
                        return HasDiffWithParity(map, p => p.Resets >= minResets);

                    case FilterConditionType.MaxParityResets:
                        var maxResets = Convert.ToInt32(condition.Value);
                        return HasDiffWithParity(map, p => p.Resets <= maxResets);

                    case FilterConditionType.MinMaxScore:
                        var minMaxScore = Convert.ToInt32(condition.Value);
                        return HasDiffWithMaxScore(map, minMaxScore, null);

                    case FilterConditionType.MaxMaxScore:
                        var maxMaxScore = Convert.ToInt32(condition.Value);
                        return HasDiffWithMaxScore(map, null, maxMaxScore);

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
        /// Checks if a map has at least one difficulty with parity summary matching the predicate
        /// </summary>
        private bool HasDiffWithParity(BeatSaverMap map, Func<BeatSaverParitySummary, bool> predicate)
        {
            if (map.Versions != null && map.Versions.Count > 0 && map.Versions[0].Diffs != null)
            {
                foreach (var diff in map.Versions[0].Diffs)
                {
                    if (diff?.ParitySummary != null && predicate(diff.ParitySummary))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a map has at least one difficulty with maxScore in the specified range
        /// </summary>
        private bool HasDiffWithMaxScore(BeatSaverMap map, int? minScore, int? maxScore)
        {
            if (map.Versions == null || map.Versions.Count == 0 || map.Versions[0].Diffs == null)
                return false;

            foreach (var diff in map.Versions[0].Diffs)
            {
                if (diff == null || !diff.MaxScore.HasValue) continue;

                bool matchesMin = minScore == null || diff.MaxScore.Value >= minScore.Value;
                bool matchesMax = maxScore == null || diff.MaxScore.Value <= maxScore.Value;

                if (matchesMin && matchesMax)
                    return true;
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
        /// Checks if a map has at least one difficulty WITHOUT a specific custom mod
        /// Used for non-strict exclude mod filtering
        /// </summary>
        private bool HasDiffWithoutCustomMod(BeatSaverMap map, string modName)
        {
            if (map.Versions == null || map.Versions.Count == 0 || map.Versions[0].Diffs == null)
                return true; // No difficulties = passes filter (no mod to exclude)

            foreach (var diff in map.Versions[0].Diffs)
            {
                if (diff == null) continue;

                // Use reflection to check for boolean properties matching the mod name
                var diffType = diff.GetType();
                var property = diffType.GetProperty(modName, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (property != null && property.PropertyType == typeof(bool))
                {
                    var value = (bool)property.GetValue(diff);
                    if (!value) return true; // Found a difficulty without this mod
                }
                else
                {
                    // Property doesn't exist on this diff = mod not present
                    return true;
                }
            }

            return false; // All difficulties have this mod
        }

        /// <summary>
        /// Handles range-type filter conditions
        /// </summary>
        private bool MatchesRangeCondition(BeatSaverMap map, FilterCondition condition)
        {
            if (!(condition.Value is RangeValue rangeVal) || !rangeVal.HasValue)
                return true;

            switch (condition.Type)
            {
                case FilterConditionType.BpmRange:
                    {
                        var bpm = map.Metadata?.Bpm ?? 0;
                        return (!rangeVal.Min.HasValue || bpm >= rangeVal.Min.Value) &&
                               (!rangeVal.Max.HasValue || bpm <= rangeVal.Max.Value);
                    }

                case FilterConditionType.NpsRange:
                    return CheckNpsRangeFromValue(map, rangeVal);

                case FilterConditionType.DurationRange:
                    {
                        var duration = map.Metadata?.Duration ?? 0;
                        return (!rangeVal.Min.HasValue || duration >= rangeVal.Min.Value) &&
                               (!rangeVal.Max.HasValue || duration <= rangeVal.Max.Value);
                    }

                case FilterConditionType.SsStarsRange:
                    return HasDiffWithStars(map, rangeVal.Min, rangeVal.Max);

                case FilterConditionType.BlStarsRange:
                    return HasDiffWithBlStars(map, rangeVal.Min, rangeVal.Max);

                case FilterConditionType.ScoreRange:
                    {
                        var score = map.Stats?.Score ?? 0;
                        // Score is stored as 0-1 in API, user inputs 0-100
                        var minScore = rangeVal.Min.HasValue ? rangeVal.Min.Value / 100.0 : (double?)null;
                        var maxScore = rangeVal.Max.HasValue ? rangeVal.Max.Value / 100.0 : (double?)null;
                        return (!minScore.HasValue || score >= minScore.Value) &&
                               (!maxScore.HasValue || score <= maxScore.Value);
                    }

                case FilterConditionType.PlaysRange:
                    {
                        var plays = map.Stats?.Plays ?? 0;
                        return (!rangeVal.Min.HasValue || plays >= rangeVal.Min.Value) &&
                               (!rangeVal.Max.HasValue || plays <= rangeVal.Max.Value);
                    }

                case FilterConditionType.DownloadsRange:
                    {
                        var downloads = map.Stats?.Downloads ?? 0;
                        return (!rangeVal.Min.HasValue || downloads >= rangeVal.Min.Value) &&
                               (!rangeVal.Max.HasValue || downloads <= rangeVal.Max.Value);
                    }

                case FilterConditionType.UpvotesRange:
                    {
                        var upvotes = map.Stats?.Upvotes ?? 0;
                        return (!rangeVal.Min.HasValue || upvotes >= rangeVal.Min.Value) &&
                               (!rangeVal.Max.HasValue || upvotes <= rangeVal.Max.Value);
                    }

                case FilterConditionType.DownvotesRange:
                    {
                        var downvotes = map.Stats?.Downvotes ?? 0;
                        return (!rangeVal.Min.HasValue || downvotes >= rangeVal.Min.Value) &&
                               (!rangeVal.Max.HasValue || downvotes <= rangeVal.Max.Value);
                    }

                case FilterConditionType.UpvoteRatioRange:
                    {
                        var ratio = CalculateUpvoteRatio(map);
                        return (!rangeVal.Min.HasValue || ratio >= rangeVal.Min.Value) &&
                               (!rangeVal.Max.HasValue || ratio <= rangeVal.Max.Value);
                    }

                case FilterConditionType.DownvoteRatioRange:
                    {
                        var ratio = CalculateDownvoteRatio(map);
                        return (!rangeVal.Min.HasValue || ratio >= rangeVal.Min.Value) &&
                               (!rangeVal.Max.HasValue || ratio <= rangeVal.Max.Value);
                    }

                case FilterConditionType.SageScoreRange:
                    {
                        if (map.Versions == null || map.Versions.Count == 0 || !map.Versions[0].SageScore.HasValue)
                            return false;
                        var sageScore = map.Versions[0].SageScore.Value;
                        return (!rangeVal.Min.HasValue || sageScore >= rangeVal.Min.Value) &&
                               (!rangeVal.Max.HasValue || sageScore <= rangeVal.Max.Value);
                    }

                case FilterConditionType.NjsRange:
                    return HasDiffWithValue(map, d =>
                        (!rangeVal.Min.HasValue || d.Njs >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Njs <= rangeVal.Max.Value));

                case FilterConditionType.BombsRange:
                    return HasDiffWithValue(map, d =>
                        (!rangeVal.Min.HasValue || d.Bombs >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Bombs <= rangeVal.Max.Value));

                case FilterConditionType.OffsetRange:
                    return HasDiffWithValue(map, d =>
                        (!rangeVal.Min.HasValue || d.Offset >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Offset <= rangeVal.Max.Value));

                case FilterConditionType.EventsRange:
                    return HasDiffWithValue(map, d =>
                        (!rangeVal.Min.HasValue || d.Events >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Events <= rangeVal.Max.Value));

                case FilterConditionType.ObstaclesRange:
                    return HasDiffWithValue(map, d =>
                        (!rangeVal.Min.HasValue || d.Obstacles >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Obstacles <= rangeVal.Max.Value));

                case FilterConditionType.BombsMapRange:
                    return HasDiffWithValue(map, d =>
                        (!rangeVal.Min.HasValue || d.Bombs >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Bombs <= rangeVal.Max.Value));

                case FilterConditionType.NotesRange:
                    return HasDiffWithValue(map, d =>
                        (!rangeVal.Min.HasValue || d.Notes >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Notes <= rangeVal.Max.Value));

                case FilterConditionType.SecondsRange:
                    return HasDiffWithValue(map, d =>
                        (!rangeVal.Min.HasValue || d.Seconds >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Seconds <= rangeVal.Max.Value));

                case FilterConditionType.LengthRange:
                    return HasDiffWithValue(map, d =>
                        (!rangeVal.Min.HasValue || d.Length >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Length <= rangeVal.Max.Value));

                case FilterConditionType.ParityErrorsRange:
                    return HasDiffWithParity(map, p =>
                        (!rangeVal.Min.HasValue || p.Errors >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || p.Errors <= rangeVal.Max.Value));

                case FilterConditionType.ParityWarnsRange:
                    return HasDiffWithParity(map, p =>
                        (!rangeVal.Min.HasValue || p.Warns >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || p.Warns <= rangeVal.Max.Value));

                case FilterConditionType.ParityResetsRange:
                    return HasDiffWithParity(map, p =>
                        (!rangeVal.Min.HasValue || p.Resets >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || p.Resets <= rangeVal.Max.Value));

                case FilterConditionType.MaxScoreRange:
                    return HasDiffWithMaxScore(map,
                        rangeVal.Min.HasValue ? (int)rangeVal.Min.Value : (int?)null,
                        rangeVal.Max.HasValue ? (int)rangeVal.Max.Value : (int?)null);

                default:
                    return true;
            }
        }

        /// <summary>
        /// Checks NPS range from RangeValue
        /// </summary>
        private bool CheckNpsRangeFromValue(BeatSaverMap map, RangeValue rangeVal)
        {
            if (!rangeVal.HasValue) return true;

            // Check from Versions[0].Diffs (local cache format)
            if (map.Versions != null && map.Versions.Count > 0 && map.Versions[0].Diffs != null)
            {
                foreach (var diff in map.Versions[0].Diffs)
                {
                    if (diff == null || diff.Nps <= 0) continue;

                    bool matchesMin = !rangeVal.Min.HasValue || diff.Nps >= rangeVal.Min.Value;
                    bool matchesMax = !rangeVal.Max.HasValue || diff.Nps <= rangeVal.Max.Value;

                    if (matchesMin && matchesMax)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Calculates vote ratio as a percentage (0-100)
        /// Returns 0 if there are no votes
        /// </summary>
        private double CalculateVoteRatio(BeatSaverMap map, bool isUpvote)
        {
            if (map.Stats == null) return 0;

            var total = map.Stats.Upvotes + map.Stats.Downvotes;
            if (total == 0) return 0;

            return (isUpvote ? map.Stats.Upvotes : map.Stats.Downvotes) * 100.0 / total;
        }

        /// <summary>
        /// Calculates the upvote ratio as a percentage (0-100)
        /// </summary>
        private double CalculateUpvoteRatio(BeatSaverMap map) => CalculateVoteRatio(map, true);

        /// <summary>
        /// Calculates the downvote ratio as a percentage (0-100)
        /// </summary>
        private double CalculateDownvoteRatio(BeatSaverMap map) => CalculateVoteRatio(map, false);

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
                sharedReader?.Dispose();
                sharedReader = null;
                disposed = true;
            }
        }
    }
}