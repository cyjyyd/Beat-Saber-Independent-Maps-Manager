using System;
using System.Collections.Generic;
using System.Linq;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// Defines all available filter condition types
    /// </summary>
    public enum FilterConditionType
    {
        // Placeholder for "no selection" (must be distinct from valid types)
        None = int.MinValue,

        // Custom condition (user-defined)
        Custom = -2,

        // Text search
        Query = 0,

        // Sorting
        Order,

        // BPM range
        MinBpm,
        MaxBpm,

        // NPS (Notes Per Second) range
        MinNps,
        MaxNps,

        // Duration range (seconds)
        MinDuration,
        MaxDuration,

        // ScoreSaber stars range
        MinSsStars,
        MaxSsStars,

        // BeatLeader stars range
        MinBlStars,
        MaxBlStars,

        // Mod support
        Chroma,
        Noodle,
        Me,
        Cinema,
        Vivify,

        // AI mapped
        Automapper,

        // Leaderboard
        Leaderboard,

        // Curation status
        Curated,
        Verified,

        // Local cache-specific filters (explicitly from 100)
        Ranked = 100,       // ScoreSaber ranked
        BlRanked,           // BeatLeader ranked (101)
        Qualified,          // ScoreSaber qualified (102)
        BlQualified,        // BeatLeader qualified (103)
        MinPlays,           // 104
        MaxPlays,           // 105
        MinDownloads,       // 106
        MaxDownloads,       // 107
        MinUpvotes,         // 108
        MaxUpvotes,         // 109
        MinDownvotes,       // 110
        MaxDownvotes,       // 111
        MinScore,           // 112
        MaxScore,           // 113
        Tags,               // 114
        UploaderName,       // 115
        MinUpvoteRatio,     // 116 - 点赞比例 (0-100)
        MaxUpvoteRatio,     // 117
        MinDownvoteRatio,   // 118 - 点踩比例 (0-100)
        MaxDownvoteRatio,   // 119
        MinSageScore,       // 120 - Sage分数
        MaxSageScore,       // 121

        // Diff-specific filters (from 130)
        MinNjs = 130,       // Note Jump Speed
        MaxNjs,             // 131
        MinBombs,           // 132
        MaxBombs,           // 133
        MinOffset,          // 134
        MaxOffset,          // 135
        MinEvents,          // 136
        MaxEvents,          // 137
        Characteristic,     // 138 - Standard, Lawless, Lightshow, etc.
        Difficulty,         // 139 - Easy, Normal, Hard, Expert, ExpertPlus

        // Additional mods
        Ne = 150,           // Noodle Extensions
        CustomMod,          // 151 - User-defined mod name (包含)
        ExcludeCustomMod,   // 152 - User-defined mod name (排除)

        // Upload time filters (from 140)
        MinUploadedDate = 140,  // 最小上传时间（在此时间之后上传）
        MaxUploadedDate,        // 141 - 最大上传时间（在此时间之前上传）

        // Result limit (from 145)
        ResultLimit = 145,      // 数量限制（带排序选项）

        // Map objects filters (from 160)
        MinObstacles = 160,     // 最小墙壁数量
        MaxObstacles,           // 161 - 最大墙壁数量
        MinBombsMap,            // 162 - 最小地图炸弹数量
        MaxBombsMap,            // 163 - 最大地图炸弹数量
        MinNotes,               // 164 - 最小方块数量
        MaxNotes,               // 165 - 最大方块数量
        MinSeconds,             // 166 - 最小谱面时长(秒)
        MaxSeconds,             // 167 - 最大谱面时长(秒)
        MinLength,              // 168 - 最小节拍数量
        MaxLength,              // 169 - 最大节拍数量
        MinParityErrors = 170,  // 最小校验错误数
        MaxParityErrors,        // 171 - 最大校验错误数
        MinParityWarns,         // 172 - 最小校验警告数
        MaxParityWarns,         // 173 - 最大校验警告数
        MinParityResets,        // 174 - 最小校验重置数
        MaxParityResets,         // 175 - 最大校验重置数
        ExcludeTags = 180,       // 排除标签
        MinMaxScore = 181,       // 最小最高分数
        MaxMaxScore,             // 182 - 最大最高分数

        // Range-type filters (from 200) - simplified UI with min-max in one condition
        BpmRange = 200,          // BPM范围
        NpsRange,                // 201 - NPS范围
        DurationRange,           // 202 - 时长范围
        SsStarsRange,            // 203 - SS星级范围
        BlStarsRange,            // 204 - BL星级范围
        ScoreRange,              // 205 - 评分范围
        PlaysRange,              // 206 - 游玩次数范围
        DownloadsRange,          // 207 - 下载次数范围
        UpvotesRange,            // 208 - 点赞数范围
        DownvotesRange,          // 209 - 踩数范围
        UpvoteRatioRange,        // 210 - 点赞比例范围
        DownvoteRatioRange,      // 211 - 点踩比例范围
        SageScoreRange,          // 212 - Sage分数范围
        NjsRange,                // 213 - NJS范围
        BombsRange,              // 214 - 炸弹数范围(难度)
        OffsetRange,             // 215 - 偏移范围
        EventsRange,             // 216 - 事件数范围
        ObstaclesRange,          // 217 - 墙壁数范围
        BombsMapRange,           // 218 - 地图炸弹数范围
        NotesRange,              // 219 - 方块数范围
        SecondsRange,            // 220 - 谱面时长范围
        LengthRange,             // 221 - 节拍数范围
        ParityErrorsRange,       // 222 - 校验错误范围
        ParityWarnsRange,        // 223 - 校验警告范围
        ParityResetsRange,       // 224 - 校验重置范围
        MaxScoreRange,           // 225 - 最高分范围
    }

    /// <summary>
    /// Defines the value type for filter conditions
    /// </summary>
    public enum FilterValueType
    {
        Text,           // Text input
        Number,         // Numeric input
        Boolean,        // Boolean selection (是/否/不限 dropdown)
        Selection,      // Dropdown selection
        Date,           // Date picker
        NumberWithSort, // Number input with sort selection (数量+排序)
        Range,          // Range input (min-max)
        SearchQuery,    // Search query with field type selection (搜索关键词+类型)
        ExcludeMod      // Exclude mod with strict mode option (排除mod+严格模式)
    }

    /// <summary>
    /// Defines the search field type for query conditions (flags enum for multi-select)
    /// </summary>
    [Flags]
    public enum SearchFieldType
    {
        None = 0,
        SongName = 1,       // 歌曲名 (songname:)
        Artist = 2,         // 艺术家 (artist:)
        Mapper = 4,         // 谱师 (mapper:)
        MapName = 8,        // 谱面标题 (name:)
        Description = 16,   // 简介 (description:)
        Uploader = 32,      // 上传者 (uploader:)

        // Preset combinations
        All = SongName | Artist | Mapper | MapName | Description | Uploader
    }

    /// <summary>
    /// Represents a search query with field types (supports multi-select)
    /// </summary>
    public class SearchQueryValue
    {
        public string Query { get; set; } = "";
        public SearchFieldType FieldTypes { get; set; } = SearchFieldType.All;

        public SearchQueryValue() { }

        public SearchQueryValue(string query, SearchFieldType fieldTypes = SearchFieldType.All)
        {
            Query = query ?? "";
            FieldTypes = fieldTypes;
        }

        /// <summary>
        /// Gets the API query string with field prefix for single field type
        /// For multiple field types, returns the plain query (API will search all fields)
        /// </summary>
        public string ToApiQuery()
        {
            if (string.IsNullOrWhiteSpace(Query))
                return "";

            // If only one field type is selected, use field prefix
            var singleField = GetSingleFieldType();
            if (singleField != null)
            {
                var prefix = singleField switch
                {
                    SearchFieldType.SongName => "songname:",
                    SearchFieldType.Artist => "artist:",
                    SearchFieldType.Mapper => "mapper:",
                    SearchFieldType.MapName => "name:",
                    SearchFieldType.Description => "description:",
                    SearchFieldType.Uploader => "uploader:",
                    _ => ""
                };
                return $"{prefix}{Query}";
            }

            // Multiple or all field types - return plain query
            return Query;
        }

        /// <summary>
        /// Gets the single field type if only one is selected, otherwise null
        /// </summary>
        private SearchFieldType? GetSingleFieldType()
        {
            if (FieldTypes == SearchFieldType.None || FieldTypes == SearchFieldType.All)
                return null;

            // Check if exactly one bit is set
            var values = Enum.GetValues(typeof(SearchFieldType)).Cast<SearchFieldType>()
                .Where(v => v != SearchFieldType.None && v != SearchFieldType.All && FieldTypes.HasFlag(v))
                .ToList();

            return values.Count == 1 ? values[0] : null;
        }

        public bool HasValue => !string.IsNullOrWhiteSpace(Query);

        /// <summary>
        /// Checks if a specific field type is selected
        /// </summary>
        public bool HasFieldType(SearchFieldType fieldType)
        {
            return FieldTypes.HasFlag(fieldType);
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(Query))
                return "";

            var fieldNames = new List<string>();
            if (HasFieldType(SearchFieldType.SongName)) fieldNames.Add("歌名");
            if (HasFieldType(SearchFieldType.Artist)) fieldNames.Add("艺术家");
            if (HasFieldType(SearchFieldType.Mapper)) fieldNames.Add("谱师");
            if (HasFieldType(SearchFieldType.MapName)) fieldNames.Add("标题");
            if (HasFieldType(SearchFieldType.Description)) fieldNames.Add("简介");
            if (HasFieldType(SearchFieldType.Uploader)) fieldNames.Add("上传者");

            var fieldStr = fieldNames.Count == 0 || fieldNames.Count == 6 ? "" : $"[{string.Join("/", fieldNames)}]";
            return $"{fieldStr}{Query}";
        }
    }

    /// <summary>
    /// Represents a range value with min and max
    /// </summary>
    public class RangeValue
    {
        // Use double.NaN as sentinel for "not set" internally
        // This allows 0 to be a valid filter value
        private double _min = double.NaN;
        private double _max = double.NaN;

        public double? Min
        {
            get => double.IsNaN(_min) ? null : _min;
            set => _min = value ?? double.NaN;
        }

        public double? Max
        {
            get => double.IsNaN(_max) ? null : _max;
            set => _max = value ?? double.NaN;
        }

        // Internal properties for serialization (store NaN directly)
        public double MinRaw
        {
            get => _min;
            set => _min = value;
        }

        public double MaxRaw
        {
            get => _max;
            set => _max = value;
        }

        public RangeValue() { }

        public RangeValue(double? min, double? max)
        {
            Min = min;
            Max = max;
        }

        public bool HasValue => !double.IsNaN(_min) || !double.IsNaN(_max);

        public bool HasMin => !double.IsNaN(_min);
        public bool HasMax => !double.IsNaN(_max);

        public override string ToString()
        {
            if (HasMin && HasMax)
                return $"{_min}-{_max}";
            else if (HasMin)
                return $"≥{_min}";
            else if (HasMax)
                return $"≤{_max}";
            return "";
        }
    }

    /// <summary>
    /// Represents an exclude mod value with mod name and strict mode option
    /// </summary>
    public class ExcludeModValue
    {
        public string ModName { get; set; } = "";
        public bool Strict { get; set; } = false;

        public ExcludeModValue() { }

        public ExcludeModValue(string modName, bool strict = false)
        {
            ModName = modName ?? "";
            Strict = strict;
        }

        public bool HasValue => !string.IsNullOrWhiteSpace(ModName);

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(ModName))
                return "";
            return Strict ? $"{ModName} (严格)" : ModName;
        }
    }

    /// <summary>
    /// Sort options for result limit
    /// </summary>
    public enum ResultSortOption
    {
        Newest,     // 最新上传
        Oldest,     // 最早上传
        Random      // 随机
    }

    /// <summary>
    /// Represents a result limit with count and sort option
    /// </summary>
    public class ResultLimitValue
    {
        public int Count { get; set; }
        public ResultSortOption SortOption { get; set; } = ResultSortOption.Newest;

        public ResultLimitValue() { }

        public ResultLimitValue(int count, ResultSortOption sortOption = ResultSortOption.Newest)
        {
            Count = count;
            SortOption = sortOption;
        }

        public override string ToString()
        {
            var sortName = SortOption switch
            {
                ResultSortOption.Newest => "最新上传",
                ResultSortOption.Oldest => "最早上传",
                ResultSortOption.Random => "随机",
                _ => SortOption.ToString()
            };
            return $"{Count}首 ({sortName})";
        }
    }

    /// <summary>
    /// Provides metadata for filter condition types
    /// </summary>
    public static class FilterConditionMetadata
    {
        private static readonly Dictionary<FilterConditionType, (string DisplayName, FilterValueType ValueType, List<string> Options)> _metadata =
            new Dictionary<FilterConditionType, (string, FilterValueType, List<string>)>
            {
                { FilterConditionType.Custom, ("自定义条件", FilterValueType.Text, null) },
                { FilterConditionType.Query, ("搜索关键词", FilterValueType.SearchQuery, null) },
                { FilterConditionType.Order, ("排序方式", FilterValueType.Selection, new List<string> { "Latest", "Relevance", "Rating", "Curated", "Random", "Duration" }) },
                { FilterConditionType.MinBpm, ("最小BPM", FilterValueType.Number, null) },
                { FilterConditionType.MaxBpm, ("最大BPM", FilterValueType.Number, null) },
                { FilterConditionType.MinNps, ("最小NPS", FilterValueType.Number, null) },
                { FilterConditionType.MaxNps, ("最大NPS", FilterValueType.Number, null) },
                { FilterConditionType.MinDuration, ("最小时长(秒)", FilterValueType.Number, null) },
                { FilterConditionType.MaxDuration, ("最大时长(秒)", FilterValueType.Number, null) },
                { FilterConditionType.MinSsStars, ("最小SS星级", FilterValueType.Number, null) },
                { FilterConditionType.MaxSsStars, ("最大SS星级", FilterValueType.Number, null) },
                { FilterConditionType.MinBlStars, ("最小BL星级", FilterValueType.Number, null) },
                { FilterConditionType.MaxBlStars, ("最大BL星级", FilterValueType.Number, null) },
                { FilterConditionType.Chroma, ("Chroma", FilterValueType.Boolean, null) },
                { FilterConditionType.Noodle, ("Noodle", FilterValueType.Boolean, null) },
                { FilterConditionType.Me, ("Mapping Extensions", FilterValueType.Boolean, null) },
                { FilterConditionType.Cinema, ("Cinema", FilterValueType.Boolean, null) },
                { FilterConditionType.Vivify, ("Vivify", FilterValueType.Boolean, null) },
                { FilterConditionType.Automapper, ("AI谱面", FilterValueType.Selection, new List<string> { "全部", "仅AI谱", "排除AI谱" }) },
                { FilterConditionType.Leaderboard, ("排行榜", FilterValueType.Selection, new List<string> { "All", "Ranked", "BeatLeader", "ScoreSaber" }) },
                { FilterConditionType.Curated, ("精选", FilterValueType.Boolean, null) },
                { FilterConditionType.Verified, ("认证谱师", FilterValueType.Boolean, null) },
                // Local cache-specific filters (explicitly from 100)
                { FilterConditionType.Ranked, ("SS排位", FilterValueType.Boolean, null) },
                { FilterConditionType.BlRanked, ("BL排位", FilterValueType.Boolean, null) },
                { FilterConditionType.Qualified, ("SS待评级", FilterValueType.Boolean, null) },
                { FilterConditionType.BlQualified, ("BL待评级", FilterValueType.Boolean, null) },
                { FilterConditionType.MinPlays, ("最小游玩次数", FilterValueType.Number, null) },
                { FilterConditionType.MaxPlays, ("最大游玩次数", FilterValueType.Number, null) },
                { FilterConditionType.MinDownloads, ("最小下载次数", FilterValueType.Number, null) },
                { FilterConditionType.MaxDownloads, ("最大下载次数", FilterValueType.Number, null) },
                { FilterConditionType.MinUpvotes, ("最小点赞数", FilterValueType.Number, null) },
                { FilterConditionType.MaxUpvotes, ("最大点赞数", FilterValueType.Number, null) },
                { FilterConditionType.MinDownvotes, ("最小踩数", FilterValueType.Number, null) },
                { FilterConditionType.MaxDownvotes, ("最大踩数", FilterValueType.Number, null) },
                { FilterConditionType.MinScore, ("最小评分", FilterValueType.Number, null) },
                { FilterConditionType.MaxScore, ("最大评分", FilterValueType.Number, null) },
                { FilterConditionType.Tags, ("包含标签", FilterValueType.Text, null) },
                { FilterConditionType.ExcludeTags, ("排除标签", FilterValueType.Text, null) },
                { FilterConditionType.MinMaxScore, ("最小最高分", FilterValueType.Number, null) },
                { FilterConditionType.MaxMaxScore, ("最大最高分", FilterValueType.Number, null) },
                { FilterConditionType.UploaderName, ("上传者", FilterValueType.Text, null) },
                { FilterConditionType.MinUpvoteRatio, ("最小点赞比例(%)", FilterValueType.Number, null) },
                { FilterConditionType.MaxUpvoteRatio, ("最大点赞比例(%)", FilterValueType.Number, null) },
                { FilterConditionType.MinDownvoteRatio, ("最小点踩比例(%)", FilterValueType.Number, null) },
                { FilterConditionType.MaxDownvoteRatio, ("最大点踩比例(%)", FilterValueType.Number, null) },
                { FilterConditionType.MinSageScore, ("最小Sage分数", FilterValueType.Number, null) },
                { FilterConditionType.MaxSageScore, ("最大Sage分数", FilterValueType.Number, null) },
                // Diff-specific filters
                { FilterConditionType.MinNjs, ("最小NJS", FilterValueType.Number, null) },
                { FilterConditionType.MaxNjs, ("最大NJS", FilterValueType.Number, null) },
                { FilterConditionType.MinBombs, ("最小炸弹数", FilterValueType.Number, null) },
                { FilterConditionType.MaxBombs, ("最大炸弹数", FilterValueType.Number, null) },
                { FilterConditionType.MinOffset, ("最小偏移", FilterValueType.Number, null) },
                { FilterConditionType.MaxOffset, ("最大偏移", FilterValueType.Number, null) },
                { FilterConditionType.MinEvents, ("最小事件数", FilterValueType.Number, null) },
                { FilterConditionType.MaxEvents, ("最大事件数", FilterValueType.Number, null) },
                { FilterConditionType.Characteristic, ("特征", FilterValueType.Selection, new List<string> { "Standard", "OneSaber", "NoArrows", "360Degree", "90Degree", "Lightshow", "Lawless", "Legacy" }) },
                { FilterConditionType.Difficulty, ("难度", FilterValueType.Selection, new List<string> { "Easy", "Normal", "Hard", "Expert", "ExpertPlus" }) },
                // Additional mods
                { FilterConditionType.Ne, ("Noodle Extensions", FilterValueType.Boolean, null) },
                { FilterConditionType.CustomMod, ("包含Mod", FilterValueType.Text, null) },
                { FilterConditionType.ExcludeCustomMod, ("排除Mod", FilterValueType.ExcludeMod, null) },
                // Upload time filters
                { FilterConditionType.MinUploadedDate, ("上传时间起始", FilterValueType.Date, null) },
                { FilterConditionType.MaxUploadedDate, ("上传时间截止", FilterValueType.Date, null) },
                // Result limit
                { FilterConditionType.ResultLimit, ("数量限制", FilterValueType.NumberWithSort, new List<string> { "最新上传", "最早上传", "随机" }) },
                // Map objects filters
                { FilterConditionType.MinObstacles, ("最小墙壁数", FilterValueType.Number, null) },
                { FilterConditionType.MaxObstacles, ("最大墙壁数", FilterValueType.Number, null) },
                { FilterConditionType.MinBombsMap, ("最小炸弹数(全图)", FilterValueType.Number, null) },
                { FilterConditionType.MaxBombsMap, ("最大炸弹数(全图)", FilterValueType.Number, null) },
                { FilterConditionType.MinNotes, ("最小方块数", FilterValueType.Number, null) },
                { FilterConditionType.MaxNotes, ("最大方块数", FilterValueType.Number, null) },
                { FilterConditionType.MinSeconds, ("最小谱面时长(秒)", FilterValueType.Number, null) },
                { FilterConditionType.MaxSeconds, ("最大谱面时长(秒)", FilterValueType.Number, null) },
                { FilterConditionType.MinLength, ("最小节拍数", FilterValueType.Number, null) },
                { FilterConditionType.MaxLength, ("最大节拍数", FilterValueType.Number, null) },
                { FilterConditionType.MinParityErrors, ("最小校验错误", FilterValueType.Number, null) },
                { FilterConditionType.MaxParityErrors, ("最大校验错误", FilterValueType.Number, null) },
                { FilterConditionType.MinParityWarns, ("最小校验警告", FilterValueType.Number, null) },
                { FilterConditionType.MaxParityWarns, ("最大校验警告", FilterValueType.Number, null) },
                { FilterConditionType.MinParityResets, ("最小校验重置", FilterValueType.Number, null) },
                { FilterConditionType.MaxParityResets, ("最大校验重置", FilterValueType.Number, null) },
                // Range-type filters (simplified UI)
                { FilterConditionType.BpmRange, ("BPM", FilterValueType.Range, null) },
                { FilterConditionType.NpsRange, ("NPS", FilterValueType.Range, null) },
                { FilterConditionType.DurationRange, ("时长(秒)", FilterValueType.Range, null) },
                { FilterConditionType.SsStarsRange, ("SS星级", FilterValueType.Range, null) },
                { FilterConditionType.BlStarsRange, ("BL星级", FilterValueType.Range, null) },
                { FilterConditionType.ScoreRange, ("评分", FilterValueType.Range, null) },
                { FilterConditionType.PlaysRange, ("游玩次数", FilterValueType.Range, null) },
                { FilterConditionType.DownloadsRange, ("下载次数", FilterValueType.Range, null) },
                { FilterConditionType.UpvotesRange, ("点赞数", FilterValueType.Range, null) },
                { FilterConditionType.DownvotesRange, ("踩数", FilterValueType.Range, null) },
                { FilterConditionType.UpvoteRatioRange, ("点赞比例(%)", FilterValueType.Range, null) },
                { FilterConditionType.DownvoteRatioRange, ("点踩比例(%)", FilterValueType.Range, null) },
                { FilterConditionType.SageScoreRange, ("Sage分数", FilterValueType.Range, null) },
                { FilterConditionType.NjsRange, ("NJS", FilterValueType.Range, null) },
                { FilterConditionType.BombsRange, ("炸弹数(难度)", FilterValueType.Range, null) },
                { FilterConditionType.OffsetRange, ("偏移", FilterValueType.Range, null) },
                { FilterConditionType.EventsRange, ("事件数", FilterValueType.Range, null) },
                { FilterConditionType.ObstaclesRange, ("墙壁数", FilterValueType.Range, null) },
                { FilterConditionType.BombsMapRange, ("炸弹数(地图)", FilterValueType.Range, null) },
                { FilterConditionType.NotesRange, ("方块数", FilterValueType.Range, null) },
                { FilterConditionType.SecondsRange, ("谱面时长(秒)", FilterValueType.Range, null) },
                { FilterConditionType.LengthRange, ("节拍数", FilterValueType.Range, null) },
                { FilterConditionType.ParityErrorsRange, ("校验错误", FilterValueType.Range, null) },
                { FilterConditionType.ParityWarnsRange, ("校验警告", FilterValueType.Range, null) },
                { FilterConditionType.ParityResetsRange, ("校验重置", FilterValueType.Range, null) },
                { FilterConditionType.MaxScoreRange, ("最高分", FilterValueType.Range, null) }
            };

        /// <summary>
        /// Gets the display name for a filter condition type
        /// </summary>
        public static string GetDisplayName(FilterConditionType type)
        {
            return _metadata.TryGetValue(type, out var meta) ? meta.DisplayName : type.ToString();
        }

        /// <summary>
        /// Gets the value type for a filter condition type
        /// </summary>
        public static FilterValueType GetValueType(FilterConditionType type)
        {
            return _metadata.TryGetValue(type, out var meta) ? meta.ValueType : FilterValueType.Text;
        }

        /// <summary>
        /// Gets the available options for selection-type conditions
        /// </summary>
        public static List<string> GetOptions(FilterConditionType type)
        {
            return _metadata.TryGetValue(type, out var meta) ? meta.Options : null;
        }

        /// <summary>
        /// Gets all available filter condition types grouped by category
        /// </summary>
        public static Dictionary<string, List<FilterConditionType>> GetGroupedConditions()
        {
            return new Dictionary<string, List<FilterConditionType>>
            {
                { "基本", new List<FilterConditionType> { FilterConditionType.Query, FilterConditionType.Order } },
                { "BPM", new List<FilterConditionType> { FilterConditionType.BpmRange } },
                { "NPS", new List<FilterConditionType> { FilterConditionType.NpsRange } },
                { "时长", new List<FilterConditionType> { FilterConditionType.DurationRange } },
                { "SS星级", new List<FilterConditionType> { FilterConditionType.SsStarsRange } },
                { "BL星级", new List<FilterConditionType> { FilterConditionType.BlStarsRange } },
                { "总评分", new List<FilterConditionType> { FilterConditionType.ScoreRange } },
                { "Mod支持", new List<FilterConditionType> { FilterConditionType.Chroma, FilterConditionType.Ne, FilterConditionType.Noodle, FilterConditionType.Me, FilterConditionType.Cinema, FilterConditionType.Vivify, FilterConditionType.CustomMod, FilterConditionType.ExcludeCustomMod } },
                { "其他", new List<FilterConditionType> { FilterConditionType.Automapper, FilterConditionType.Leaderboard, FilterConditionType.Curated, FilterConditionType.Verified } },
                { "难度参数", new List<FilterConditionType> {
                    FilterConditionType.Characteristic,
                    FilterConditionType.Difficulty,
                    FilterConditionType.NjsRange,
                    FilterConditionType.BombsRange,
                    FilterConditionType.OffsetRange,
                    FilterConditionType.EventsRange
                }},
                { "本地缓存专属", new List<FilterConditionType> {
                    FilterConditionType.Ranked,
                    FilterConditionType.BlRanked,
                    FilterConditionType.Qualified,
                    FilterConditionType.BlQualified,
                    FilterConditionType.PlaysRange,
                    FilterConditionType.DownloadsRange,
                    FilterConditionType.UpvotesRange,
                    FilterConditionType.DownvotesRange,
                    FilterConditionType.UpvoteRatioRange,
                    FilterConditionType.DownvoteRatioRange,
                    FilterConditionType.SageScoreRange,
                    FilterConditionType.Tags,
                    FilterConditionType.ExcludeTags,
                    FilterConditionType.UploaderName,
                    FilterConditionType.MinUploadedDate,
                    FilterConditionType.MaxUploadedDate,
                    FilterConditionType.ResultLimit,
                    FilterConditionType.ObstaclesRange,
                    FilterConditionType.BombsMapRange,
                    FilterConditionType.NotesRange,
                    FilterConditionType.SecondsRange,
                    FilterConditionType.LengthRange,
                    FilterConditionType.ParityErrorsRange,
                    FilterConditionType.ParityWarnsRange,
                    FilterConditionType.ParityResetsRange,
                    FilterConditionType.MaxScoreRange
                }}
            };
        }

        /// <summary>
        /// Gets all filter condition types as a flat list
        /// </summary>
        public static List<FilterConditionType> GetAllConditions()
        {
            return new List<FilterConditionType>
            {
                FilterConditionType.Query,
                FilterConditionType.Order,
                FilterConditionType.MinBpm,
                FilterConditionType.MaxBpm,
                FilterConditionType.MinNps,
                FilterConditionType.MaxNps,
                FilterConditionType.MinDuration,
                FilterConditionType.MaxDuration,
                FilterConditionType.MinSsStars,
                FilterConditionType.MaxSsStars,
                FilterConditionType.MinBlStars,
                FilterConditionType.MaxBlStars,
                FilterConditionType.MinScore,
                FilterConditionType.MaxScore,
                FilterConditionType.Chroma,
                FilterConditionType.Ne,
                FilterConditionType.Noodle,
                FilterConditionType.Me,
                FilterConditionType.Cinema,
                FilterConditionType.Vivify,
                FilterConditionType.CustomMod,
                FilterConditionType.ExcludeCustomMod,
                FilterConditionType.Automapper,
                FilterConditionType.Leaderboard,
                FilterConditionType.Curated,
                FilterConditionType.Verified,
                // Diff-specific
                FilterConditionType.Characteristic,
                FilterConditionType.Difficulty,
                FilterConditionType.MinNjs,
                FilterConditionType.MaxNjs,
                FilterConditionType.MinBombs,
                FilterConditionType.MaxBombs,
                FilterConditionType.MinOffset,
                FilterConditionType.MaxOffset,
                FilterConditionType.MinEvents,
                FilterConditionType.MaxEvents,
                // Local cache-specific
                FilterConditionType.Ranked,
                FilterConditionType.BlRanked,
                FilterConditionType.Qualified,
                FilterConditionType.BlQualified,
                FilterConditionType.MinPlays,
                FilterConditionType.MaxPlays,
                FilterConditionType.MinDownloads,
                FilterConditionType.MaxDownloads,
                FilterConditionType.MinUpvotes,
                FilterConditionType.MaxUpvotes,
                FilterConditionType.MinDownvotes,
                FilterConditionType.MaxDownvotes,
                FilterConditionType.MinUpvoteRatio,
                FilterConditionType.MaxUpvoteRatio,
                FilterConditionType.MinDownvoteRatio,
                FilterConditionType.MaxDownvoteRatio,
                FilterConditionType.MinSageScore,
                FilterConditionType.MaxSageScore,
                FilterConditionType.Tags,
                FilterConditionType.ExcludeTags,
                FilterConditionType.UploaderName,
                // Upload time
                FilterConditionType.MinUploadedDate,
                FilterConditionType.MaxUploadedDate,
                // Result limit
                FilterConditionType.ResultLimit,
                // Map objects
                FilterConditionType.MinObstacles,
                FilterConditionType.MaxObstacles,
                FilterConditionType.MinBombsMap,
                FilterConditionType.MaxBombsMap,
                FilterConditionType.MinNotes,
                FilterConditionType.MaxNotes,
                FilterConditionType.MinSeconds,
                FilterConditionType.MaxSeconds,
                FilterConditionType.MinLength,
                FilterConditionType.MaxLength,
                FilterConditionType.MinParityErrors,
                FilterConditionType.MaxParityErrors,
                FilterConditionType.MinParityWarns,
                FilterConditionType.MaxParityWarns,
                FilterConditionType.MinParityResets,
                FilterConditionType.MaxParityResets,
                FilterConditionType.MinMaxScore,
                FilterConditionType.MaxMaxScore,
                // Range-type conditions
                FilterConditionType.BpmRange,
                FilterConditionType.NpsRange,
                FilterConditionType.DurationRange,
                FilterConditionType.SsStarsRange,
                FilterConditionType.BlStarsRange,
                FilterConditionType.ScoreRange,
                FilterConditionType.PlaysRange,
                FilterConditionType.DownloadsRange,
                FilterConditionType.UpvotesRange,
                FilterConditionType.DownvotesRange,
                FilterConditionType.UpvoteRatioRange,
                FilterConditionType.DownvoteRatioRange,
                FilterConditionType.SageScoreRange,
                FilterConditionType.NjsRange,
                FilterConditionType.BombsRange,
                FilterConditionType.OffsetRange,
                FilterConditionType.EventsRange,
                FilterConditionType.ObstaclesRange,
                FilterConditionType.BombsMapRange,
                FilterConditionType.NotesRange,
                FilterConditionType.SecondsRange,
                FilterConditionType.LengthRange,
                FilterConditionType.ParityErrorsRange,
                FilterConditionType.ParityWarnsRange,
                FilterConditionType.ParityResetsRange,
                FilterConditionType.MaxScoreRange
            };
        }

        /// <summary>
        /// Checks if a filter condition type requires local cache
        /// </summary>
        public static bool RequiresLocalCache(FilterConditionType type)
        {
            // Types from 100 onwards require local cache
            // Range conditions from 200 onwards also require local cache (except BpmRange, NpsRange, DurationRange, SsStarsRange, BlStarsRange, ScoreRange)
            return (int)type >= 100;
        }

        /// <summary>
        /// Checks if a filter condition type is a range type
        /// </summary>
        public static bool IsRangeType(FilterConditionType type)
        {
            return (int)type >= 200;
        }

        /// <summary>
        /// Gets the Min/Max condition types for a range condition
        /// </summary>
        public static (FilterConditionType? MinType, FilterConditionType? MaxType) GetRangeMapping(FilterConditionType rangeType)
        {
            return rangeType switch
            {
                FilterConditionType.BpmRange => (FilterConditionType.MinBpm, FilterConditionType.MaxBpm),
                FilterConditionType.NpsRange => (FilterConditionType.MinNps, FilterConditionType.MaxNps),
                FilterConditionType.DurationRange => (FilterConditionType.MinDuration, FilterConditionType.MaxDuration),
                FilterConditionType.SsStarsRange => (FilterConditionType.MinSsStars, FilterConditionType.MaxSsStars),
                FilterConditionType.BlStarsRange => (FilterConditionType.MinBlStars, FilterConditionType.MaxBlStars),
                FilterConditionType.ScoreRange => (FilterConditionType.MinScore, FilterConditionType.MaxScore),
                FilterConditionType.PlaysRange => (FilterConditionType.MinPlays, FilterConditionType.MaxPlays),
                FilterConditionType.DownloadsRange => (FilterConditionType.MinDownloads, FilterConditionType.MaxDownloads),
                FilterConditionType.UpvotesRange => (FilterConditionType.MinUpvotes, FilterConditionType.MaxUpvotes),
                FilterConditionType.DownvotesRange => (FilterConditionType.MinDownvotes, FilterConditionType.MaxDownvotes),
                FilterConditionType.UpvoteRatioRange => (FilterConditionType.MinUpvoteRatio, FilterConditionType.MaxUpvoteRatio),
                FilterConditionType.DownvoteRatioRange => (FilterConditionType.MinDownvoteRatio, FilterConditionType.MaxDownvoteRatio),
                FilterConditionType.SageScoreRange => (FilterConditionType.MinSageScore, FilterConditionType.MaxSageScore),
                FilterConditionType.NjsRange => (FilterConditionType.MinNjs, FilterConditionType.MaxNjs),
                FilterConditionType.BombsRange => (FilterConditionType.MinBombs, FilterConditionType.MaxBombs),
                FilterConditionType.OffsetRange => (FilterConditionType.MinOffset, FilterConditionType.MaxOffset),
                FilterConditionType.EventsRange => (FilterConditionType.MinEvents, FilterConditionType.MaxEvents),
                FilterConditionType.ObstaclesRange => (FilterConditionType.MinObstacles, FilterConditionType.MaxObstacles),
                FilterConditionType.BombsMapRange => (FilterConditionType.MinBombsMap, FilterConditionType.MaxBombsMap),
                FilterConditionType.NotesRange => (FilterConditionType.MinNotes, FilterConditionType.MaxNotes),
                FilterConditionType.SecondsRange => (FilterConditionType.MinSeconds, FilterConditionType.MaxSeconds),
                FilterConditionType.LengthRange => (FilterConditionType.MinLength, FilterConditionType.MaxLength),
                FilterConditionType.ParityErrorsRange => (FilterConditionType.MinParityErrors, FilterConditionType.MaxParityErrors),
                FilterConditionType.ParityWarnsRange => (FilterConditionType.MinParityWarns, FilterConditionType.MaxParityWarns),
                FilterConditionType.ParityResetsRange => (FilterConditionType.MinParityResets, FilterConditionType.MaxParityResets),
                FilterConditionType.MaxScoreRange => (FilterConditionType.MinMaxScore, FilterConditionType.MaxMaxScore),
                _ => (null, null)
            };
        }

        /// <summary>
        /// Gets all local cache-specific condition types
        /// </summary>
        public static List<FilterConditionType> GetLocalCacheConditions()
        {
            return new List<FilterConditionType>
            {
                FilterConditionType.Ranked,
                FilterConditionType.BlRanked,
                FilterConditionType.Qualified,
                FilterConditionType.BlQualified,
                FilterConditionType.MinPlays,
                FilterConditionType.MaxPlays,
                FilterConditionType.MinDownloads,
                FilterConditionType.MaxDownloads,
                FilterConditionType.MinUpvotes,
                FilterConditionType.MaxUpvotes,
                FilterConditionType.MinDownvotes,
                FilterConditionType.MaxDownvotes,
                FilterConditionType.MinUpvoteRatio,
                FilterConditionType.MaxUpvoteRatio,
                FilterConditionType.MinDownvoteRatio,
                FilterConditionType.MaxDownvoteRatio,
                FilterConditionType.MinSageScore,
                FilterConditionType.MaxSageScore,
                FilterConditionType.Tags,
                FilterConditionType.ExcludeTags,
                FilterConditionType.UploaderName,
                // Diff-specific
                FilterConditionType.Characteristic,
                FilterConditionType.Difficulty,
                FilterConditionType.MinNjs,
                FilterConditionType.MaxNjs,
                FilterConditionType.MinBombs,
                FilterConditionType.MaxBombs,
                FilterConditionType.MinOffset,
                FilterConditionType.MaxOffset,
                FilterConditionType.MinEvents,
                FilterConditionType.MaxEvents,
                // Additional mods
                FilterConditionType.Ne,
                FilterConditionType.CustomMod,
                FilterConditionType.ExcludeCustomMod,
                // Upload time
                FilterConditionType.MinUploadedDate,
                FilterConditionType.MaxUploadedDate,
                // Result limit
                FilterConditionType.ResultLimit,
                // Map objects
                FilterConditionType.MinObstacles,
                FilterConditionType.MaxObstacles,
                FilterConditionType.MinBombsMap,
                FilterConditionType.MaxBombsMap,
                FilterConditionType.MinNotes,
                FilterConditionType.MaxNotes,
                FilterConditionType.MinSeconds,
                FilterConditionType.MaxSeconds,
                FilterConditionType.MinLength,
                FilterConditionType.MaxLength,
                FilterConditionType.MinParityErrors,
                FilterConditionType.MaxParityErrors,
                FilterConditionType.MinParityWarns,
                FilterConditionType.MaxParityWarns,
                FilterConditionType.MinParityResets,
                FilterConditionType.MaxParityResets,
                FilterConditionType.MinMaxScore,
                FilterConditionType.MaxMaxScore,
                // Range-type conditions for local cache
                FilterConditionType.PlaysRange,
                FilterConditionType.DownloadsRange,
                FilterConditionType.UpvotesRange,
                FilterConditionType.DownvotesRange,
                FilterConditionType.UpvoteRatioRange,
                FilterConditionType.DownvoteRatioRange,
                FilterConditionType.SageScoreRange,
                FilterConditionType.NjsRange,
                FilterConditionType.BombsRange,
                FilterConditionType.OffsetRange,
                FilterConditionType.EventsRange,
                FilterConditionType.ObstaclesRange,
                FilterConditionType.BombsMapRange,
                FilterConditionType.NotesRange,
                FilterConditionType.SecondsRange,
                FilterConditionType.LengthRange,
                FilterConditionType.ParityErrorsRange,
                FilterConditionType.ParityWarnsRange,
                FilterConditionType.ParityResetsRange,
                FilterConditionType.MaxScoreRange
            };
        }
    }
}