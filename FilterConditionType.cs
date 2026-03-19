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
        Verified
    }

    /// <summary>
    /// Defines the value type for filter conditions
    /// </summary>
    public enum FilterValueType
    {
        Text,       // Text input
        Number,     // Numeric input
        Boolean,    // Boolean selection (checkbox)
        Selection   // Dropdown selection
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
                { FilterConditionType.Verified, ("认证谱师", FilterValueType.Boolean, null) }
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
                { "Mod支持", new List<FilterConditionType> { FilterConditionType.Chroma, FilterConditionType.Noodle, FilterConditionType.Me, FilterConditionType.Cinema, FilterConditionType.Vivify } },
                { "其他", new List<FilterConditionType> { FilterConditionType.Automapper, FilterConditionType.Leaderboard, FilterConditionType.Curated, FilterConditionType.Verified } }
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
                FilterConditionType.Noodle,
                FilterConditionType.Me,
                FilterConditionType.Cinema,
                FilterConditionType.Vivify,
                FilterConditionType.Automapper,
                FilterConditionType.Leaderboard,
                FilterConditionType.Curated,
                FilterConditionType.Verified
            };
        }
    }
}