using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using Newtonsoft.Json;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// 高效的本地缓存流式读取器
    /// 支持流式读取、内存映射文件、以及批处理时的缓存复用
    /// </summary>
    public class LocalCacheReader : IDisposable
    {
        private readonly string cachePath;
        private MemoryMappedFile mappedFile;
        private MemoryMappedViewStream mappedStream;
        private StreamReader streamReader;
        private JsonTextReader jsonReader;
        private JsonSerializer serializer;
        private bool disposed;

        // 用于批处理的预加载模式
        private List<BeatSaverMapSlim> preloadedMaps;
        private bool isPreloaded;

        /// <summary>
        /// 获取缓存文件大小
        /// </summary>
        public long CacheSize { get; private set; }

        /// <summary>
        /// 获取当前读取位置（用于进度报告）
        /// </summary>
        public long CurrentPosition
        {
            get
            {
                if (mappedStream != null)
                    return mappedStream.Position;
                if (streamReader?.BaseStream != null)
                    return streamReader.BaseStream.Position;
                return 0;
            }
        }

        public LocalCacheReader(string path)
        {
            cachePath = path;
            CacheSize = File.Exists(path) ? new FileInfo(path).Length : 0;
            serializer = new JsonSerializer();
        }

        /// <summary>
        /// 预加载所有数据到内存（用于批处理多次筛选）
        /// 使用轻量级数据结构，内存占用约为原始 JSON 的 2-3 倍（而非 5-10 倍）
        /// </summary>
        public void PreloadForBatchProcessing(IProgress<int> progress = null)
        {
            if (isPreloaded) return;

            preloadedMaps = new List<BeatSaverMapSlim>();
            long lastReportBytes = 0;
            int reportInterval = 1024 * 1024; // 每MB报告一次

            using (var fs = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
            using (var sr = new StreamReader(fs))
            using (var jr = new JsonTextReader(sr))
            {
                NavigateToDocsArray(jr);

                while (jr.Read() && jr.TokenType != JsonToken.EndArray)
                {
                    if (jr.TokenType == JsonToken.StartObject)
                    {
                        var map = ReadMapSlim(jr);
                        if (map != null)
                            preloadedMaps.Add(map);
                    }

                    // 进度报告
                    if (progress != null && fs.Position - lastReportBytes > reportInterval)
                    {
                        progress.Report((int)(fs.Position * 100 / CacheSize));
                        lastReportBytes = fs.Position;
                    }
                }
            }

            isPreloaded = true;
            progress?.Report(100);
        }

        /// <summary>
        /// 清除预加载数据，释放内存
        /// </summary>
        public void ClearPreloadedData()
        {
            preloadedMaps?.Clear();
            preloadedMaps = null;
            isPreloaded = false;
        }

        /// <summary>
        /// 流式筛选地图（轻量级）
        /// </summary>
        public IEnumerable<BeatSaverMapSlim> StreamFilterMaps(
            FilterPreset preset,
            IProgress<int> progress,
            Func<BeatSaverMapSlim, bool> customFilter = null)
        {
            // 如果已预加载，从内存筛选
            if (isPreloaded && preloadedMaps != null)
            {
                int processed = 0;
                int total = preloadedMaps.Count;

                foreach (var map in preloadedMaps)
                {
                    if (MatchesFilterSlim(map, preset) && (customFilter == null || customFilter(map)))
                    {
                        yield return map;
                    }

                    processed++;
                    if (processed % 5000 == 0)
                    {
                        progress?.Report(processed * 100 / total);
                    }
                }
                progress?.Report(100);
                yield break;
            }

            // 否则从文件流式读取
            long lastReportBytes = 0;
            int reportInterval = 1024 * 1024;

            using (var fs = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
            using (var sr = new StreamReader(fs))
            using (var jr = new JsonTextReader(sr))
            {
                NavigateToDocsArray(jr);

                while (jr.Read() && jr.TokenType != JsonToken.EndArray)
                {
                    if (jr.TokenType == JsonToken.StartObject)
                    {
                        var map = ReadMapSlim(jr);
                        if (map != null && MatchesFilterSlim(map, preset) && (customFilter == null || customFilter(map)))
                        {
                            yield return map;
                        }
                    }

                    // 进度报告
                    if (progress != null && fs.Position - lastReportBytes > reportInterval)
                    {
                        progress.Report((int)(fs.Position * 100 / CacheSize));
                        lastReportBytes = fs.Position;
                    }
                }
            }

            progress?.Report(100);
        }

        /// <summary>
        /// 批量筛选（使用预加载数据或流式读取）
        /// </summary>
        public List<BeatSaverMapSlim> BatchFilter(
            List<FilterPreset> presets,
            IProgress<int> progress,
            Action<int, int> onPresetComplete = null)
        {
            var results = new List<BeatSaverMapSlim>();
            int processed = 0;

            foreach (var preset in presets)
            {
                var presetResults = new List<BeatSaverMapSlim>();

                foreach (var map in StreamFilterMaps(preset, null))
                {
                    presetResults.Add(map);
                }

                // 应用结果限制
                var limitedResults = ApplyResultLimitSlim(presetResults, preset);
                results.AddRange(limitedResults);

                processed++;
                progress?.Report(processed * 100 / presets.Count);
                onPresetComplete?.Invoke(processed, limitedResults.Count);
            }

            return results;
        }

        /// <summary>
        /// 初始化流式读取器（使用内存映射文件以提高性能）
        /// </summary>
        public void InitializeStreamReader()
        {
            if (mappedFile != null) return;

            try
            {
                mappedFile = MemoryMappedFile.CreateFromFile(cachePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
                mappedStream = mappedFile.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
                streamReader = new StreamReader(mappedStream);
                jsonReader = new JsonTextReader(streamReader);
            }
            catch
            {
                // 回退到普通文件流
                var fs = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
                streamReader = new StreamReader(fs);
                jsonReader = new JsonTextReader(streamReader);
            }

            NavigateToDocsArray(jsonReader);
        }

        /// <summary>
        /// 读取下一个地图（用于迭代式读取）
        /// </summary>
        public BeatSaverMapSlim ReadNextMap()
        {
            if (jsonReader == null)
                InitializeStreamReader();

            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == JsonToken.EndArray)
                    return null;

                if (jsonReader.TokenType == JsonToken.StartObject)
                    return ReadMapSlim(jsonReader);
            }

            return null;
        }

        /// <summary>
        /// 重置读取器到数组开始位置
        /// </summary>
        public void ResetReader()
        {
            if (jsonReader != null)
            {
                jsonReader.Close();
                streamReader?.Dispose();
                mappedStream?.Dispose();
                mappedFile?.Dispose();

                mappedFile = null;
                mappedStream = null;
                streamReader = null;
                jsonReader = null;
            }
            InitializeStreamReader();
        }

        #region 私有方法

        private void NavigateToDocsArray(JsonReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartArray && reader.Path == "docs")
                    break;
            }
        }

        /// <summary>
        /// 读取轻量级地图数据，跳过不需要的字段
        /// </summary>
        private BeatSaverMapSlim ReadMapSlim(JsonReader reader)
        {
            var map = new BeatSaverMapSlim();
            map.Diffs = null; // 延迟初始化

            int depth = reader.Depth;
            string currentProperty = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject && reader.Depth == depth)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    currentProperty = reader.Value?.ToString();
                    continue;
                }

                if (currentProperty == null) continue;

                // 根据属性名读取需要的字段
                ReadProperty(map, reader, currentProperty);
                currentProperty = null;
            }

            return map;
        }

        private void ReadProperty(BeatSaverMapSlim map, JsonReader reader, string property)
        {
            switch (property)
            {
                // 基础信息
                case "id":
                    map.Id = reader.Value?.ToString();
                    break;
                case "name":
                    map.Name = reader.Value?.ToString();
                    break;
                case "description":
                    map.Description = reader.Value?.ToString();
                    break;

                // 上传信息
                case "uploaded":
                    if (reader.Value != null)
                        map.Uploaded = Convert.ToDateTime(reader.Value);
                    break;
                case "uploader":
                    ReadUploader(map, reader);
                    break;

                // 元数据
                case "metadata":
                    ReadMetadata(map, reader);
                    break;

                // 统计
                case "stats":
                    ReadStats(map, reader);
                    break;

                // 布尔标记
                case "automapper":
                    if (reader.Value != null)
                        map.Automapper = Convert.ToBoolean(reader.Value);
                    break;
                case "curated":
                    if (reader.Value != null)
                        map.Curated = Convert.ToBoolean(reader.Value);
                    break;
                case "ranked":
                    if (reader.Value != null)
                        map.Ranked = Convert.ToBoolean(reader.Value);
                    break;
                case "qualified":
                    if (reader.Value != null)
                        map.Qualified = Convert.ToBoolean(reader.Value);
                    break;
                case "blRanked":
                    if (reader.Value != null)
                        map.BlRanked = Convert.ToBoolean(reader.Value);
                    break;
                case "blQualified":
                    if (reader.Value != null)
                        map.BlQualified = Convert.ToBoolean(reader.Value);
                    break;

                // 标签
                case "tags":
                    map.Tags = ReadStringArray(reader);
                    break;

                // 版本信息
                case "versions":
                    ReadVersions(map, reader);
                    break;

                // 跳过其他字段（不读取以节省内存）
                default:
                    reader.Skip();
                    break;
            }
        }

        private void ReadUploader(BeatSaverMapSlim map, JsonReader reader)
        {
            int depth = reader.Depth;
            string currentProperty = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject && reader.Depth == depth)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    currentProperty = reader.Value?.ToString();
                    continue;
                }

                if (currentProperty == null) continue;

                switch (currentProperty)
                {
                    case "id":
                        if (reader.Value != null)
                            map.UploaderId = Convert.ToInt32(reader.Value);
                        break;
                    case "name":
                        map.UploaderName = reader.Value?.ToString();
                        break;
                    case "verified":
                        if (reader.Value != null)
                            map.UploaderVerified = Convert.ToBoolean(reader.Value);
                        break;
                }
                currentProperty = null;
            }
        }

        private void ReadMetadata(BeatSaverMapSlim map, JsonReader reader)
        {
            int depth = reader.Depth;
            string currentProperty = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject && reader.Depth == depth)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    currentProperty = reader.Value?.ToString();
                    continue;
                }

                if (currentProperty == null) continue;

                switch (currentProperty)
                {
                    case "bpm":
                        if (reader.Value != null)
                            map.Bpm = Convert.ToDouble(reader.Value);
                        break;
                    case "duration":
                        if (reader.Value != null)
                            map.Duration = Convert.ToDouble(reader.Value);
                        break;
                    case "songName":
                        map.SongName = reader.Value?.ToString();
                        break;
                    case "songAuthorName":
                        map.SongAuthorName = reader.Value?.ToString();
                        break;
                    case "levelAuthorName":
                        map.LevelAuthorName = reader.Value?.ToString();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
                currentProperty = null;
            }
        }

        private void ReadStats(BeatSaverMapSlim map, JsonReader reader)
        {
            int depth = reader.Depth;
            string currentProperty = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject && reader.Depth == depth)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    currentProperty = reader.Value?.ToString();
                    continue;
                }

                if (currentProperty == null) continue;

                switch (currentProperty)
                {
                    case "plays":
                        if (reader.Value != null)
                            map.Plays = Convert.ToInt32(reader.Value);
                        break;
                    case "downloads":
                        if (reader.Value != null)
                            map.Downloads = Convert.ToInt32(reader.Value);
                        break;
                    case "upvotes":
                        if (reader.Value != null)
                            map.Upvotes = Convert.ToInt32(reader.Value);
                        break;
                    case "downvotes":
                        if (reader.Value != null)
                            map.Downvotes = Convert.ToInt32(reader.Value);
                        break;
                    case "score":
                        if (reader.Value != null)
                            map.Score = Convert.ToDouble(reader.Value);
                        break;
                }
                currentProperty = null;
            }
        }

        private void ReadVersions(BeatSaverMapSlim map, JsonReader reader)
        {
            if (reader.TokenType != JsonToken.StartArray)
            {
                reader.Skip();
                return;
            }

            int depth = reader.Depth;
            bool isFirstVersion = true;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray && reader.Depth == depth)
                    break;

                if (reader.TokenType == JsonToken.StartObject)
                {
                    if (isFirstVersion)
                    {
                        ReadFirstVersion(map, reader);
                        isFirstVersion = false;
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
            }
        }

        private void ReadFirstVersion(BeatSaverMapSlim map, JsonReader reader)
        {
            int depth = reader.Depth;
            string currentProperty = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject && reader.Depth == depth)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    currentProperty = reader.Value?.ToString();
                    continue;
                }

                if (currentProperty == null) continue;

                switch (currentProperty)
                {
                    case "hash":
                        map.Hash = reader.Value?.ToString();
                        break;
                    case "sageScore":
                        if (reader.Value != null)
                            map.SageScore = Convert.ToInt32(reader.Value);
                        break;
                    case "coverURL":
                        map.CoverURL = reader.Value?.ToString();
                        break;
                    case "downloadURL":
                        map.DownloadURL = reader.Value?.ToString();
                        break;
                    case "diffs":
                        ReadDiffs(map, reader);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
                currentProperty = null;
            }
        }

        private void ReadDiffs(BeatSaverMapSlim map, JsonReader reader)
        {
            if (reader.TokenType != JsonToken.StartArray)
            {
                reader.Skip();
                return;
            }

            map.Diffs = new List<BeatSaverVersionDiffSlim>();
            int depth = reader.Depth;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray && reader.Depth == depth)
                    break;

                if (reader.TokenType == JsonToken.StartObject)
                {
                    var diff = ReadDiff(reader);
                    if (diff != null)
                        map.Diffs.Add(diff);
                }
            }
        }

        private BeatSaverVersionDiffSlim ReadDiff(JsonReader reader)
        {
            var diff = new BeatSaverVersionDiffSlim();
            int depth = reader.Depth;
            string currentProperty = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject && reader.Depth == depth)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    currentProperty = reader.Value?.ToString();
                    continue;
                }

                if (currentProperty == null) continue;

                switch (currentProperty)
                {
                    case "njs":
                        if (reader.Value != null)
                            diff.Njs = Convert.ToDouble(reader.Value);
                        break;
                    case "offset":
                        if (reader.Value != null)
                            diff.Offset = Convert.ToDouble(reader.Value);
                        break;
                    case "notes":
                        if (reader.Value != null)
                            diff.Notes = Convert.ToInt32(reader.Value);
                        break;
                    case "bombs":
                        if (reader.Value != null)
                            diff.Bombs = Convert.ToInt32(reader.Value);
                        break;
                    case "obstacles":
                        if (reader.Value != null)
                            diff.Obstacles = Convert.ToInt32(reader.Value);
                        break;
                    case "nps":
                        if (reader.Value != null)
                            diff.Nps = Convert.ToDouble(reader.Value);
                        break;
                    case "length":
                        if (reader.Value != null)
                            diff.Length = Convert.ToDouble(reader.Value);
                        break;
                    case "seconds":
                        if (reader.Value != null)
                            diff.Seconds = Convert.ToDouble(reader.Value);
                        break;
                    case "characteristic":
                        diff.Characteristic = reader.Value?.ToString();
                        break;
                    case "difficulty":
                        diff.Difficulty = reader.Value?.ToString();
                        break;
                    case "events":
                        if (reader.Value != null)
                            diff.Events = Convert.ToInt32(reader.Value);
                        break;
                    case "chroma":
                        if (reader.Value != null)
                            diff.Chroma = Convert.ToBoolean(reader.Value);
                        break;
                    case "me":
                        if (reader.Value != null)
                            diff.Me = Convert.ToBoolean(reader.Value);
                        break;
                    case "ne":
                        if (reader.Value != null)
                            diff.Ne = Convert.ToBoolean(reader.Value);
                        break;
                    case "cinema":
                        if (reader.Value != null)
                            diff.Cinema = Convert.ToBoolean(reader.Value);
                        break;
                    case "vivify":
                        if (reader.Value != null)
                            diff.Vivify = Convert.ToBoolean(reader.Value);
                        break;
                    case "stars":
                        if (reader.Value != null)
                            diff.Stars = Convert.ToDouble(reader.Value);
                        break;
                    case "blStars":
                        if (reader.Value != null)
                            diff.BlStars = Convert.ToDouble(reader.Value);
                        break;
                    case "maxScore":
                        if (reader.Value != null)
                            diff.MaxScore = Convert.ToInt32(reader.Value);
                        break;
                    case "paritySummary":
                        ReadParitySummary(diff, reader);
                        break;
                    default:
                        // 动态属性（自定义 Mod）- 暂时跳过
                        if (reader.Value is bool boolVal)
                        {
                            // 尝试设置动态属性（通过反射）
                            var prop = diff.GetType().GetProperty(currentProperty,
                                System.Reflection.BindingFlags.IgnoreCase |
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.Instance);
                            if (prop != null && prop.PropertyType == typeof(bool))
                            {
                                prop.SetValue(diff, boolVal);
                            }
                        }
                        break;
                }
                currentProperty = null;
            }

            return diff;
        }

        private void ReadParitySummary(BeatSaverVersionDiffSlim diff, JsonReader reader)
        {
            int depth = reader.Depth;
            string currentProperty = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject && reader.Depth == depth)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    currentProperty = reader.Value?.ToString();
                    continue;
                }

                if (currentProperty == null) continue;

                switch (currentProperty)
                {
                    case "errors":
                        if (reader.Value != null)
                            diff.ParityErrors = Convert.ToInt32(reader.Value);
                        break;
                    case "warns":
                        if (reader.Value != null)
                            diff.ParityWarns = Convert.ToInt32(reader.Value);
                        break;
                    case "resets":
                        if (reader.Value != null)
                            diff.ParityResets = Convert.ToInt32(reader.Value);
                        break;
                }
                currentProperty = null;
            }
        }

        private List<string> ReadStringArray(JsonReader reader)
        {
            if (reader.TokenType != JsonToken.StartArray)
                return null;

            var list = new List<string>();
            int depth = reader.Depth;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray && reader.Depth == depth)
                    break;

                if (reader.TokenType == JsonToken.String)
                {
                    list.Add(reader.Value?.ToString());
                }
            }

            return list;
        }

        #endregion

        #region 筛选逻辑

        /// <summary>
        /// 检查地图是否匹配筛选条件（轻量级版本）
        /// </summary>
        public bool MatchesFilterSlim(BeatSaverMapSlim map, FilterPreset preset)
        {
            if (preset == null || map == null)
                return true;

            var activeGroups = preset.GetActiveGroups();
            if (!activeGroups.Any())
                return true;

            bool? groupResult = null;

            foreach (var group in activeGroups)
            {
                var conditions = group.GetActiveConditions();
                if (!conditions.Any()) continue;

                bool matchesGroup = MatchesGroupConditionsSlim(map, conditions);

                if (groupResult == null)
                {
                    groupResult = matchesGroup;
                }
                else
                {
                    if (group.GroupOperator == LogicOperator.Or)
                        groupResult = groupResult.Value || matchesGroup;
                    else
                        groupResult = groupResult.Value && matchesGroup;
                }
            }

            return groupResult ?? true;
        }

        private bool MatchesGroupConditionsSlim(BeatSaverMapSlim map, List<FilterCondition> conditions)
        {
            bool? result = null;
            LogicOperator? lastOperator = null;

            // NPS 条件组合处理
            var npsConditions = conditions.Where(c => c.Type == FilterConditionType.MinNps || c.Type == FilterConditionType.MaxNps).ToList();
            bool? npsResult = null;
            bool npsProcessed = false;

            // SS Stars 条件组合处理
            var ssStarsConditions = conditions.Where(c => c.Type == FilterConditionType.MinSsStars || c.Type == FilterConditionType.MaxSsStars).ToList();
            bool? ssStarsResult = null;
            bool ssStarsProcessed = false;

            // BL Stars 条件组合处理
            var blStarsConditions = conditions.Where(c => c.Type == FilterConditionType.MinBlStars || c.Type == FilterConditionType.MaxBlStars).ToList();
            bool? blStarsResult = null;
            bool blStarsProcessed = false;

            for (int i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i];

                // NPS 条件
                if (condition.Type == FilterConditionType.MinNps || condition.Type == FilterConditionType.MaxNps)
                {
                    if (!npsProcessed && npsConditions.Any())
                    {
                        npsResult = CheckNpsRangeSlim(map, npsConditions);
                        npsProcessed = true;
                        var lastNpsCondition = npsConditions.LastOrDefault();
                        if (lastNpsCondition != null)
                            lastOperator = lastNpsCondition.Operator;
                    }
                    continue;
                }

                // SS Stars 条件
                if (condition.Type == FilterConditionType.MinSsStars || condition.Type == FilterConditionType.MaxSsStars)
                {
                    if (!ssStarsProcessed && ssStarsConditions.Any())
                    {
                        ssStarsResult = CheckStarsRangeSlim(map, ssStarsConditions, false);
                        ssStarsProcessed = true;
                        var lastSsCondition = ssStarsConditions.LastOrDefault();
                        if (lastSsCondition != null)
                            lastOperator = lastSsCondition.Operator;
                    }
                    continue;
                }

                // BL Stars 条件
                if (condition.Type == FilterConditionType.MinBlStars || condition.Type == FilterConditionType.MaxBlStars)
                {
                    if (!blStarsProcessed && blStarsConditions.Any())
                    {
                        blStarsResult = CheckStarsRangeSlim(map, blStarsConditions, true);
                        blStarsProcessed = true;
                        var lastBlCondition = blStarsConditions.LastOrDefault();
                        if (lastBlCondition != null)
                            lastOperator = lastBlCondition.Operator;
                    }
                    continue;
                }

                bool matches = MatchesConditionSlim(map, condition);

                // 处理组合结果
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

            // 组合剩余结果
            if (result == null)
            {
                bool combined = true;
                if (npsResult.HasValue) combined = combined && npsResult.Value;
                if (ssStarsResult.HasValue) combined = combined && ssStarsResult.Value;
                if (blStarsResult.HasValue) combined = combined && blStarsResult.Value;
                return combined;
            }

            if (npsResult.HasValue) result = result.Value && npsResult.Value;
            if (ssStarsResult.HasValue) result = result.Value && ssStarsResult.Value;
            if (blStarsResult.HasValue) result = result.Value && blStarsResult.Value;

            return result ?? true;
        }

        private bool MatchesConditionSlim(BeatSaverMapSlim map, FilterCondition condition)
        {
            if (condition?.Value == null || !condition.IsEnabled)
                return true;

            try
            {
                // 范围类型条件
                if (FilterConditionMetadata.IsRangeType(condition.Type))
                {
                    return MatchesRangeConditionSlim(map, condition);
                }

                switch (condition.Type)
                {
                    case FilterConditionType.Query:
                        return MatchesQuerySlim(map, condition);

                    case FilterConditionType.MinBpm:
                        return map.Bpm >= Convert.ToDouble(condition.Value);

                    case FilterConditionType.MaxBpm:
                        return map.Bpm <= Convert.ToDouble(condition.Value);

                    case FilterConditionType.MinDuration:
                        return map.Duration >= Convert.ToDouble(condition.Value);

                    case FilterConditionType.MaxDuration:
                        return map.Duration <= Convert.ToDouble(condition.Value);

                    case FilterConditionType.MinPlays:
                        return map.Plays >= Convert.ToInt32(condition.Value);

                    case FilterConditionType.MaxPlays:
                        return map.Plays <= Convert.ToInt32(condition.Value);

                    case FilterConditionType.MinDownloads:
                        return map.Downloads >= Convert.ToInt32(condition.Value);

                    case FilterConditionType.MaxDownloads:
                        return map.Downloads <= Convert.ToInt32(condition.Value);

                    case FilterConditionType.MinUpvotes:
                        return map.Upvotes >= Convert.ToInt32(condition.Value);

                    case FilterConditionType.MaxUpvotes:
                        return map.Upvotes <= Convert.ToInt32(condition.Value);

                    case FilterConditionType.MinDownvotes:
                        return map.Downvotes >= Convert.ToInt32(condition.Value);

                    case FilterConditionType.MaxDownvotes:
                        return map.Downvotes <= Convert.ToInt32(condition.Value);

                    case FilterConditionType.MinScore:
                        return map.Score >= Convert.ToDouble(condition.Value) / 100.0;

                    case FilterConditionType.MaxScore:
                        return map.Score <= Convert.ToDouble(condition.Value) / 100.0;

                    case FilterConditionType.Chroma:
                        return Convert.ToBoolean(condition.Value) == map.HasMod("Chroma");

                    case FilterConditionType.Noodle:
                        return Convert.ToBoolean(condition.Value) == map.HasMod("Noodle");

                    case FilterConditionType.Me:
                        return Convert.ToBoolean(condition.Value) == map.HasMod("Me");

                    case FilterConditionType.Cinema:
                        return Convert.ToBoolean(condition.Value) == map.HasMod("Cinema");

                    case FilterConditionType.Vivify:
                        return Convert.ToBoolean(condition.Value) == map.HasMod("Vivify");

                    case FilterConditionType.Automapper:
                        var autoVal = condition.Value?.ToString();
                        if (autoVal == "仅AI谱")
                            return map.Automapper;
                        else if (autoVal == "排除AI谱")
                            return !map.Automapper;
                        return true;

                    case FilterConditionType.Curated:
                        return Convert.ToBoolean(condition.Value) == map.Curated;

                    case FilterConditionType.Verified:
                        return Convert.ToBoolean(condition.Value) == map.UploaderVerified;

                    case FilterConditionType.Ranked:
                        return Convert.ToBoolean(condition.Value) == map.Ranked;

                    case FilterConditionType.BlRanked:
                        return Convert.ToBoolean(condition.Value) == map.BlRanked;

                    case FilterConditionType.Qualified:
                        return Convert.ToBoolean(condition.Value) == map.Qualified;

                    case FilterConditionType.BlQualified:
                        return Convert.ToBoolean(condition.Value) == map.BlQualified;

                    case FilterConditionType.Tags:
                        var tagQuery = condition.Value?.ToString()?.ToLower() ?? "";
                        if (string.IsNullOrWhiteSpace(tagQuery)) return true;
                        return map.Tags?.Any(t => t?.ToLower().Contains(tagQuery) ?? false) ?? false;

                    case FilterConditionType.ExcludeTags:
                        var excludeTagQuery = condition.Value?.ToString()?.ToLower() ?? "";
                        if (string.IsNullOrWhiteSpace(excludeTagQuery)) return true;
                        return !(map.Tags?.Any(t => t?.ToLower().Contains(excludeTagQuery) ?? false) ?? false);

                    case FilterConditionType.UploaderName:
                        var uploaderQuery = condition.Value?.ToString()?.ToLower() ?? "";
                        if (string.IsNullOrWhiteSpace(uploaderQuery)) return true;
                        return (map.UploaderName?.ToLower().Contains(uploaderQuery) ?? false) ||
                               (map.UploaderId.ToString().Contains(uploaderQuery));

                    case FilterConditionType.MinUpvoteRatio:
                        return CalculateUpvoteRatio(map) >= Convert.ToDouble(condition.Value);

                    case FilterConditionType.MaxUpvoteRatio:
                        return CalculateUpvoteRatio(map) <= Convert.ToDouble(condition.Value);

                    case FilterConditionType.MinDownvoteRatio:
                        return CalculateDownvoteRatio(map) >= Convert.ToDouble(condition.Value);

                    case FilterConditionType.MaxDownvoteRatio:
                        return CalculateDownvoteRatio(map) <= Convert.ToDouble(condition.Value);

                    case FilterConditionType.MinSageScore:
                        return map.SageScore.HasValue && map.SageScore.Value >= Convert.ToInt32(condition.Value);

                    case FilterConditionType.MaxSageScore:
                        return map.SageScore.HasValue && map.SageScore.Value <= Convert.ToInt32(condition.Value);

                    case FilterConditionType.MinNjs:
                        return HasDiffWithValueSlim(map, d => d.Njs >= Convert.ToDouble(condition.Value));

                    case FilterConditionType.MaxNjs:
                        return HasDiffWithValueSlim(map, d => d.Njs > 0 && d.Njs <= Convert.ToDouble(condition.Value));

                    case FilterConditionType.MinBombs:
                    case FilterConditionType.MinBombsMap:
                        return HasDiffWithValueSlim(map, d => d.Bombs >= Convert.ToInt32(condition.Value));

                    case FilterConditionType.MaxBombs:
                    case FilterConditionType.MaxBombsMap:
                        return HasDiffWithValueSlim(map, d => d.Bombs <= Convert.ToInt32(condition.Value));

                    case FilterConditionType.MinOffset:
                        return HasDiffWithValueSlim(map, d => d.Offset >= Convert.ToDouble(condition.Value));

                    case FilterConditionType.MaxOffset:
                        return HasDiffWithValueSlim(map, d => d.Offset <= Convert.ToDouble(condition.Value));

                    case FilterConditionType.MinEvents:
                        return HasDiffWithValueSlim(map, d => d.Events >= Convert.ToInt32(condition.Value));

                    case FilterConditionType.MaxEvents:
                        return HasDiffWithValueSlim(map, d => d.Events <= Convert.ToInt32(condition.Value));

                    case FilterConditionType.Characteristic:
                        var charVal = condition.Value?.ToString();
                        if (string.IsNullOrWhiteSpace(charVal)) return true;
                        return HasCharacteristicSlim(map, charVal);

                    case FilterConditionType.Difficulty:
                        var diffVal = condition.Value?.ToString();
                        if (string.IsNullOrWhiteSpace(diffVal)) return true;
                        return HasDifficultySlim(map, diffVal);

                    case FilterConditionType.Ne:
                        return Convert.ToBoolean(condition.Value) == map.HasMod("Ne");

                    case FilterConditionType.CustomMod:
                        var customMod = condition.Value?.ToString()?.ToLower() ?? "";
                        if (string.IsNullOrWhiteSpace(customMod)) return true;
                        return map.HasCustomMod(customMod);

                    case FilterConditionType.ExcludeCustomMod:
                        string excludeCustomMod;
                        bool strictMode = false;
                        if (condition.Value is ExcludeModValue excludeModValue)
                        {
                            excludeCustomMod = excludeModValue.ModName?.ToLower() ?? "";
                            strictMode = excludeModValue.Strict;
                        }
                        else
                        {
                            excludeCustomMod = condition.Value?.ToString()?.ToLower() ?? "";
                        }
                        if (string.IsNullOrWhiteSpace(excludeCustomMod)) return true;
                        if (strictMode)
                            return !map.HasCustomMod(excludeCustomMod);
                        else
                            return map.HasDiffWithoutMod(excludeCustomMod);

                    case FilterConditionType.MinUploadedDate:
                        if (condition.Value == null) return true;
                        return map.Uploaded >= Convert.ToDateTime(condition.Value);

                    case FilterConditionType.MaxUploadedDate:
                        if (condition.Value == null) return true;
                        return map.Uploaded <= Convert.ToDateTime(condition.Value);

                    case FilterConditionType.MinObstacles:
                        return HasDiffWithValueSlim(map, d => d.Obstacles >= Convert.ToInt32(condition.Value));

                    case FilterConditionType.MaxObstacles:
                        return HasDiffWithValueSlim(map, d => d.Obstacles <= Convert.ToInt32(condition.Value));

                    case FilterConditionType.MinNotes:
                        return HasDiffWithValueSlim(map, d => d.Notes >= Convert.ToInt32(condition.Value));

                    case FilterConditionType.MaxNotes:
                        return HasDiffWithValueSlim(map, d => d.Notes <= Convert.ToInt32(condition.Value));

                    case FilterConditionType.MinSeconds:
                        return HasDiffWithValueSlim(map, d => d.Seconds >= Convert.ToDouble(condition.Value));

                    case FilterConditionType.MaxSeconds:
                        return HasDiffWithValueSlim(map, d => d.Seconds <= Convert.ToDouble(condition.Value));

                    case FilterConditionType.MinLength:
                        return HasDiffWithValueSlim(map, d => d.Length >= Convert.ToDouble(condition.Value));

                    case FilterConditionType.MaxLength:
                        return HasDiffWithValueSlim(map, d => d.Length <= Convert.ToDouble(condition.Value));

                    case FilterConditionType.MinParityErrors:
                        return HasDiffWithParitySlim(map, p => p.ParityErrors >= Convert.ToInt32(condition.Value));

                    case FilterConditionType.MaxParityErrors:
                        return HasDiffWithParitySlim(map, p => p.ParityErrors <= Convert.ToInt32(condition.Value));

                    case FilterConditionType.MinParityWarns:
                        return HasDiffWithParitySlim(map, p => p.ParityWarns >= Convert.ToInt32(condition.Value));

                    case FilterConditionType.MaxParityWarns:
                        return HasDiffWithParitySlim(map, p => p.ParityWarns <= Convert.ToInt32(condition.Value));

                    case FilterConditionType.MinParityResets:
                        return HasDiffWithParitySlim(map, p => p.ParityResets >= Convert.ToInt32(condition.Value));

                    case FilterConditionType.MaxParityResets:
                        return HasDiffWithParitySlim(map, p => p.ParityResets <= Convert.ToInt32(condition.Value));

                    case FilterConditionType.MinMaxScore:
                        return HasDiffWithMaxScoreSlim(map, Convert.ToInt32(condition.Value), null);

                    case FilterConditionType.MaxMaxScore:
                        return HasDiffWithMaxScoreSlim(map, null, Convert.ToInt32(condition.Value));

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

        private bool MatchesRangeConditionSlim(BeatSaverMapSlim map, FilterCondition condition)
        {
            if (!(condition.Value is RangeValue rangeVal) || !rangeVal.HasValue)
                return true;

            switch (condition.Type)
            {
                case FilterConditionType.BpmRange:
                    return (!rangeVal.Min.HasValue || map.Bpm >= rangeVal.Min.Value) &&
                           (!rangeVal.Max.HasValue || map.Bpm <= rangeVal.Max.Value);

                case FilterConditionType.NpsRange:
                    return CheckNpsRangeFromValueSlim(map, rangeVal);

                case FilterConditionType.DurationRange:
                    return (!rangeVal.Min.HasValue || map.Duration >= rangeVal.Min.Value) &&
                           (!rangeVal.Max.HasValue || map.Duration <= rangeVal.Max.Value);

                case FilterConditionType.SsStarsRange:
                    return HasDiffWithStarsSlim(map, rangeVal.Min, rangeVal.Max, false);

                case FilterConditionType.BlStarsRange:
                    return HasDiffWithStarsSlim(map, rangeVal.Min, rangeVal.Max, true);

                case FilterConditionType.ScoreRange:
                    var score = map.Score;
                    var minScore = rangeVal.Min.HasValue ? rangeVal.Min.Value / 100.0 : (double?)null;
                    var maxScore = rangeVal.Max.HasValue ? rangeVal.Max.Value / 100.0 : (double?)null;
                    return (!minScore.HasValue || score >= minScore.Value) &&
                           (!maxScore.HasValue || score <= maxScore.Value);

                case FilterConditionType.PlaysRange:
                    return (!rangeVal.Min.HasValue || map.Plays >= rangeVal.Min.Value) &&
                           (!rangeVal.Max.HasValue || map.Plays <= rangeVal.Max.Value);

                case FilterConditionType.DownloadsRange:
                    return (!rangeVal.Min.HasValue || map.Downloads >= rangeVal.Min.Value) &&
                           (!rangeVal.Max.HasValue || map.Downloads <= rangeVal.Max.Value);

                case FilterConditionType.UpvotesRange:
                    return (!rangeVal.Min.HasValue || map.Upvotes >= rangeVal.Min.Value) &&
                           (!rangeVal.Max.HasValue || map.Upvotes <= rangeVal.Max.Value);

                case FilterConditionType.DownvotesRange:
                    return (!rangeVal.Min.HasValue || map.Downvotes >= rangeVal.Min.Value) &&
                           (!rangeVal.Max.HasValue || map.Downvotes <= rangeVal.Max.Value);

                case FilterConditionType.UpvoteRatioRange:
                    var ratio = CalculateUpvoteRatio(map);
                    return (!rangeVal.Min.HasValue || ratio >= rangeVal.Min.Value) &&
                           (!rangeVal.Max.HasValue || ratio <= rangeVal.Max.Value);

                case FilterConditionType.DownvoteRatioRange:
                    var downRatio = CalculateDownvoteRatio(map);
                    return (!rangeVal.Min.HasValue || downRatio >= rangeVal.Min.Value) &&
                           (!rangeVal.Max.HasValue || downRatio <= rangeVal.Max.Value);

                case FilterConditionType.SageScoreRange:
                    if (!map.SageScore.HasValue) return false;
                    return (!rangeVal.Min.HasValue || map.SageScore.Value >= rangeVal.Min.Value) &&
                           (!rangeVal.Max.HasValue || map.SageScore.Value <= rangeVal.Max.Value);

                case FilterConditionType.NjsRange:
                    return HasDiffWithValueSlim(map, d =>
                        (!rangeVal.Min.HasValue || d.Njs >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Njs <= rangeVal.Max.Value));

                case FilterConditionType.BombsRange:
                    return HasDiffWithValueSlim(map, d =>
                        (!rangeVal.Min.HasValue || d.Bombs >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Bombs <= rangeVal.Max.Value));

                case FilterConditionType.OffsetRange:
                    return HasDiffWithValueSlim(map, d =>
                        (!rangeVal.Min.HasValue || d.Offset >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Offset <= rangeVal.Max.Value));

                case FilterConditionType.EventsRange:
                    return HasDiffWithValueSlim(map, d =>
                        (!rangeVal.Min.HasValue || d.Events >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Events <= rangeVal.Max.Value));

                case FilterConditionType.ObstaclesRange:
                    return HasDiffWithValueSlim(map, d =>
                        (!rangeVal.Min.HasValue || d.Obstacles >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Obstacles <= rangeVal.Max.Value));

                case FilterConditionType.BombsMapRange:
                    return HasDiffWithValueSlim(map, d =>
                        (!rangeVal.Min.HasValue || d.Bombs >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Bombs <= rangeVal.Max.Value));

                case FilterConditionType.NotesRange:
                    return HasDiffWithValueSlim(map, d =>
                        (!rangeVal.Min.HasValue || d.Notes >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Notes <= rangeVal.Max.Value));

                case FilterConditionType.SecondsRange:
                    return HasDiffWithValueSlim(map, d =>
                        (!rangeVal.Min.HasValue || d.Seconds >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Seconds <= rangeVal.Max.Value));

                case FilterConditionType.LengthRange:
                    return HasDiffWithValueSlim(map, d =>
                        (!rangeVal.Min.HasValue || d.Length >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || d.Length <= rangeVal.Max.Value));

                case FilterConditionType.ParityErrorsRange:
                    return HasDiffWithParitySlim(map, p =>
                        (!rangeVal.Min.HasValue || p.ParityErrors >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || p.ParityErrors <= rangeVal.Max.Value));

                case FilterConditionType.ParityWarnsRange:
                    return HasDiffWithParitySlim(map, p =>
                        (!rangeVal.Min.HasValue || p.ParityWarns >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || p.ParityWarns <= rangeVal.Max.Value));

                case FilterConditionType.ParityResetsRange:
                    return HasDiffWithParitySlim(map, p =>
                        (!rangeVal.Min.HasValue || p.ParityResets >= rangeVal.Min.Value) &&
                        (!rangeVal.Max.HasValue || p.ParityResets <= rangeVal.Max.Value));

                case FilterConditionType.MaxScoreRange:
                    return HasDiffWithMaxScoreSlim(map,
                        rangeVal.Min.HasValue ? (int)rangeVal.Min.Value : (int?)null,
                        rangeVal.Max.HasValue ? (int)rangeVal.Max.Value : (int?)null);

                default:
                    return true;
            }
        }

        private bool MatchesQuerySlim(BeatSaverMapSlim map, FilterCondition condition)
        {
            string query;
            SearchFieldType fieldTypes;

            if (condition.Value is SearchQueryValue queryValue)
            {
                query = queryValue.Query?.ToLower() ?? "";
                fieldTypes = queryValue.FieldTypes;
            }
            else
            {
                query = condition.Value?.ToString()?.ToLower() ?? "";
                fieldTypes = SearchFieldType.All;
            }

            if (string.IsNullOrWhiteSpace(query)) return true;
            if (fieldTypes == SearchFieldType.None) fieldTypes = SearchFieldType.All;

            bool matches = false;

            if (fieldTypes.HasFlag(SearchFieldType.SongName))
                matches |= map.SongName?.ToLower().Contains(query) ?? false;

            if (fieldTypes.HasFlag(SearchFieldType.Artist))
                matches |= map.SongAuthorName?.ToLower().Contains(query) ?? false;

            if (fieldTypes.HasFlag(SearchFieldType.Mapper))
                matches |= map.LevelAuthorName?.ToLower().Contains(query) ?? false;

            if (fieldTypes.HasFlag(SearchFieldType.MapName))
                matches |= map.Name?.ToLower().Contains(query) ?? false;

            if (fieldTypes.HasFlag(SearchFieldType.Description))
                matches |= map.Description?.ToLower().Contains(query) ?? false;

            if (fieldTypes.HasFlag(SearchFieldType.Uploader))
                matches |= map.UploaderName?.ToLower().Contains(query) ?? false;

            return matches;
        }

        private bool CheckNpsRangeSlim(BeatSaverMapSlim map, List<FilterCondition> conditions)
        {
            if (map.Diffs == null) return false;

            double? minNps = null;
            double? maxNps = null;

            foreach (var condition in conditions)
            {
                if (condition.Type == FilterConditionType.MinNps && condition.Value != null)
                    minNps = Convert.ToDouble(condition.Value);
                else if (condition.Type == FilterConditionType.MaxNps && condition.Value != null)
                    maxNps = Convert.ToDouble(condition.Value);
            }

            if (minNps == null && maxNps == null) return true;

            foreach (var diff in map.Diffs)
            {
                if (diff.Nps <= 0) continue;
                bool matchesMin = minNps == null || diff.Nps >= minNps.Value;
                bool matchesMax = maxNps == null || diff.Nps <= maxNps.Value;
                if (matchesMin && matchesMax) return true;
            }

            return false;
        }

        private bool CheckNpsRangeFromValueSlim(BeatSaverMapSlim map, RangeValue rangeVal)
        {
            if (!rangeVal.HasValue || map.Diffs == null) return true;

            foreach (var diff in map.Diffs)
            {
                if (diff.Nps <= 0) continue;
                bool matchesMin = !rangeVal.Min.HasValue || diff.Nps >= rangeVal.Min.Value;
                bool matchesMax = !rangeVal.Max.HasValue || diff.Nps <= rangeVal.Max.Value;
                if (matchesMin && matchesMax) return true;
            }

            return false;
        }

        private bool CheckStarsRangeSlim(BeatSaverMapSlim map, List<FilterCondition> conditions, bool isBlStars)
        {
            double? minStars = null;
            double? maxStars = null;

            foreach (var condition in conditions)
            {
                if (condition.Type == (isBlStars ? FilterConditionType.MinBlStars : FilterConditionType.MinSsStars) && condition.Value != null)
                    minStars = Convert.ToDouble(condition.Value);
                else if (condition.Type == (isBlStars ? FilterConditionType.MaxBlStars : FilterConditionType.MaxSsStars) && condition.Value != null)
                    maxStars = Convert.ToDouble(condition.Value);
            }

            if (minStars == null && maxStars == null) return true;

            return HasDiffWithStarsSlim(map, minStars, maxStars, isBlStars);
        }

        private bool HasDiffWithStarsSlim(BeatSaverMapSlim map, double? minStars, double? maxStars, bool isBlStars)
        {
            if (map.Diffs == null) return false;

            foreach (var diff in map.Diffs)
            {
                double? stars = isBlStars ? diff.BlStars : diff.Stars;
                if (!stars.HasValue) continue;

                bool matchesMin = minStars == null || stars.Value >= minStars.Value;
                bool matchesMax = maxStars == null || stars.Value <= maxStars.Value;

                if (matchesMin && matchesMax) return true;
            }

            return false;
        }

        private bool HasDiffWithValueSlim(BeatSaverMapSlim map, Func<BeatSaverVersionDiffSlim, bool> predicate)
        {
            if (map.Diffs == null) return false;

            foreach (var diff in map.Diffs)
            {
                if (predicate(diff)) return true;
            }

            return false;
        }

        private bool HasDiffWithParitySlim(BeatSaverMapSlim map, Func<BeatSaverVersionDiffSlim, bool> predicate)
        {
            if (map.Diffs == null) return false;

            foreach (var diff in map.Diffs)
            {
                if (predicate(diff)) return true;
            }

            return false;
        }

        private bool HasDiffWithMaxScoreSlim(BeatSaverMapSlim map, int? minScore, int? maxScore)
        {
            if (map.Diffs == null) return false;

            foreach (var diff in map.Diffs)
            {
                if (!diff.MaxScore.HasValue) continue;

                bool matchesMin = minScore == null || diff.MaxScore.Value >= minScore.Value;
                bool matchesMax = maxScore == null || diff.MaxScore.Value <= maxScore.Value;

                if (matchesMin && matchesMax) return true;
            }

            return false;
        }

        private bool HasCharacteristicSlim(BeatSaverMapSlim map, string characteristic)
        {
            if (map.Diffs == null) return false;

            foreach (var diff in map.Diffs)
            {
                if (diff.Characteristic?.Equals(characteristic, StringComparison.OrdinalIgnoreCase) == true)
                    return true;
            }

            return false;
        }

        private bool HasDifficultySlim(BeatSaverMapSlim map, string difficulty)
        {
            if (map.Diffs == null) return false;

            foreach (var diff in map.Diffs)
            {
                if (diff.Difficulty?.Equals(difficulty, StringComparison.OrdinalIgnoreCase) == true)
                    return true;
            }

            return false;
        }

        private double CalculateUpvoteRatio(BeatSaverMapSlim map)
        {
            var total = map.Upvotes + map.Downvotes;
            return total == 0 ? 0 : (double)map.Upvotes / total * 100;
        }

        private double CalculateDownvoteRatio(BeatSaverMapSlim map)
        {
            var total = map.Upvotes + map.Downvotes;
            return total == 0 ? 0 : (double)map.Downvotes / total * 100;
        }

        private List<BeatSaverMapSlim> ApplyResultLimitSlim(List<BeatSaverMapSlim> maps, FilterPreset preset)
        {
            ResultLimitValue resultLimit = preset.TopLevelResultLimit;

            if (resultLimit == null)
            {
                foreach (var group in preset.GetActiveGroups())
                {
                    resultLimit = group.GetResultLimit();
                    if (resultLimit != null) break;
                }
            }

            if (resultLimit == null || resultLimit.Count <= 0)
                return maps;

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

            return maps.Take(resultLimit.Count).ToList();
        }

        #endregion

        public void Dispose()
        {
            if (!disposed)
            {
                jsonReader?.Close();
                streamReader?.Dispose();
                mappedStream?.Dispose();
                mappedFile?.Dispose();
                preloadedMaps?.Clear();
                preloadedMaps = null;
                disposed = true;
            }
        }
    }

    // BeatSaverMapSlim 的扩展方法
    public static class BeatSaverMapSlimExtensions
    {
        public static bool HasMod(this BeatSaverMapSlim map, string modName)
        {
            if (map.Diffs == null) return false;

            foreach (var diff in map.Diffs)
            {
                switch (modName.ToLower())
                {
                    case "chroma":
                        if (diff.Chroma) return true;
                        break;
                    case "noodle":
                    case "ne":
                        if (diff.Ne) return true;
                        break;
                    case "me":
                        if (diff.Me) return true;
                        break;
                    case "cinema":
                        if (diff.Cinema) return true;
                        break;
                    case "vivify":
                        if (diff.Vivify) return true;
                        break;
                }
            }
            return false;
        }

        public static bool HasCustomMod(this BeatSaverMapSlim map, string modName)
        {
            if (map.Diffs == null) return false;

            foreach (var diff in map.Diffs)
            {
                var prop = diff.GetType().GetProperty(modName,
                    System.Reflection.BindingFlags.IgnoreCase |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);
                if (prop != null && prop.PropertyType == typeof(bool))
                {
                    if ((bool)prop.GetValue(diff)) return true;
                }
            }
            return false;
        }

        public static bool HasDiffWithoutMod(this BeatSaverMapSlim map, string modName)
        {
            if (map.Diffs == null || map.Diffs.Count == 0) return true;

            foreach (var diff in map.Diffs)
            {
                switch (modName.ToLower())
                {
                    case "chroma":
                        if (!diff.Chroma) return true;
                        break;
                    case "noodle":
                    case "ne":
                        if (!diff.Ne) return true;
                        break;
                    case "me":
                        if (!diff.Me) return true;
                        break;
                    case "cinema":
                        if (!diff.Cinema) return true;
                        break;
                    case "vivify":
                        if (!diff.Vivify) return true;
                        break;
                    default:
                        var prop = diff.GetType().GetProperty(modName,
                            System.Reflection.BindingFlags.IgnoreCase |
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.Instance);
                        if (prop != null && prop.PropertyType == typeof(bool))
                        {
                            if (!((bool)prop.GetValue(diff))) return true;
                        }
                        else
                        {
                            return true;
                        }
                        break;
                }
            }
            return false;
        }
    }
}