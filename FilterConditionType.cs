using System;
using System.Collections.Generic;

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

        // Diff-specific filters (from 120)
        MinNjs = 120,       // Note Jump Speed
        MaxNjs,             // 121
        MinBombs,           // 122
        MaxBombs,           // 123
        MinOffset,          // 124
        MaxOffset,          // 125
        MinEvents,          // 126
        MaxEvents,          // 127
        Characteristic,     // 128 - Standard, Lawless, Lightshow, etc.
        Difficulty,         // 129 - Easy, Normal, Hard, Expert, ExpertPlus

        // Additional mods
        Ne,                 // 130 - Noodle Extensions
        CustomMod,          // 131 - User-defined mod name

        // Upload time filters (from 140)
        MinUploadedDate = 140,  // 最小上传时间（在此时间之后上传）
        MaxUploadedDate,        // 141 - 最大上传时间（在此时间之前上传）

        // Result limit (from 145)
        ResultLimit = 145       // 数量限制（带排序选项）
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
        NumberWithSort  // Number input with sort selection (数量+排序)
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
                { FilterConditionType.Query, ("搜索关键词", FilterValueType.Text, null) },
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
                { FilterConditionType.Tags, ("标签", FilterValueType.Text, null) },
                { FilterConditionType.UploaderName, ("上传者", FilterValueType.Text, null) },
                { FilterConditionType.MinUpvoteRatio, ("最小点赞比例(%)", FilterValueType.Number, null) },
                { FilterConditionType.MaxUpvoteRatio, ("最大点赞比例(%)", FilterValueType.Number, null) },
                { FilterConditionType.MinDownvoteRatio, ("最小点踩比例(%)", FilterValueType.Number, null) },
                { FilterConditionType.MaxDownvoteRatio, ("最大点踩比例(%)", FilterValueType.Number, null) },
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
                { FilterConditionType.CustomMod, ("自定义Mod", FilterValueType.Text, null) },
                // Upload time filters
                { FilterConditionType.MinUploadedDate, ("上传时间起始", FilterValueType.Date, null) },
                { FilterConditionType.MaxUploadedDate, ("上传时间截止", FilterValueType.Date, null) },
                // Result limit
                { FilterConditionType.ResultLimit, ("数量限制", FilterValueType.NumberWithSort, new List<string> { "最新上传", "最早上传", "随机" }) }
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
                { "BPM", new List<FilterConditionType> { FilterConditionType.MinBpm, FilterConditionType.MaxBpm } },
                { "NPS", new List<FilterConditionType> { FilterConditionType.MinNps, FilterConditionType.MaxNps } },
                { "时长", new List<FilterConditionType> { FilterConditionType.MinDuration, FilterConditionType.MaxDuration } },
                { "SS星级", new List<FilterConditionType> { FilterConditionType.MinSsStars, FilterConditionType.MaxSsStars } },
                { "BL星级", new List<FilterConditionType> { FilterConditionType.MinBlStars, FilterConditionType.MaxBlStars } },
                { "Mod支持", new List<FilterConditionType> { FilterConditionType.Chroma, FilterConditionType.Ne, FilterConditionType.Noodle, FilterConditionType.Me, FilterConditionType.Cinema, FilterConditionType.Vivify, FilterConditionType.CustomMod } },
                { "其他", new List<FilterConditionType> { FilterConditionType.Automapper, FilterConditionType.Leaderboard, FilterConditionType.Curated, FilterConditionType.Verified } },
                { "难度参数", new List<FilterConditionType> {
                    FilterConditionType.Characteristic,
                    FilterConditionType.Difficulty,
                    FilterConditionType.MinNjs,
                    FilterConditionType.MaxNjs,
                    FilterConditionType.MinBombs,
                    FilterConditionType.MaxBombs,
                    FilterConditionType.MinOffset,
                    FilterConditionType.MaxOffset,
                    FilterConditionType.MinEvents,
                    FilterConditionType.MaxEvents
                }},
                { "本地缓存专属", new List<FilterConditionType> {
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
                    FilterConditionType.MinScore,
                    FilterConditionType.MaxScore,
                    FilterConditionType.MinUpvoteRatio,
                    FilterConditionType.MaxUpvoteRatio,
                    FilterConditionType.MinDownvoteRatio,
                    FilterConditionType.MaxDownvoteRatio,
                    FilterConditionType.Tags,
                    FilterConditionType.UploaderName,
                    FilterConditionType.MinUploadedDate,
                    FilterConditionType.MaxUploadedDate,
                    FilterConditionType.ResultLimit
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
                FilterConditionType.Chroma,
                FilterConditionType.Ne,
                FilterConditionType.Noodle,
                FilterConditionType.Me,
                FilterConditionType.Cinema,
                FilterConditionType.Vivify,
                FilterConditionType.CustomMod,
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
                FilterConditionType.MinScore,
                FilterConditionType.MaxScore,
                FilterConditionType.MinUpvoteRatio,
                FilterConditionType.MaxUpvoteRatio,
                FilterConditionType.MinDownvoteRatio,
                FilterConditionType.MaxDownvoteRatio,
                FilterConditionType.Tags,
                FilterConditionType.UploaderName,
                // Upload time
                FilterConditionType.MinUploadedDate,
                FilterConditionType.MaxUploadedDate,
                // Result limit
                FilterConditionType.ResultLimit
            };
        }

        /// <summary>
        /// Checks if a filter condition type requires local cache
        /// </summary>
        public static bool RequiresLocalCache(FilterConditionType type)
        {
            // Types from 100 onwards require local cache
            return (int)type >= 100;
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
                FilterConditionType.MinScore,
                FilterConditionType.MaxScore,
                FilterConditionType.MinUpvoteRatio,
                FilterConditionType.MaxUpvoteRatio,
                FilterConditionType.MinDownvoteRatio,
                FilterConditionType.MaxDownvoteRatio,
                FilterConditionType.Tags,
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
                // Upload time
                FilterConditionType.MinUploadedDate,
                FilterConditionType.MaxUploadedDate,
                // Result limit
                FilterConditionType.ResultLimit
            };
        }
    }
}