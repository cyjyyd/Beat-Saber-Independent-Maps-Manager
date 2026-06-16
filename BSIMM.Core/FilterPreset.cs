using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// Custom JSON converter for FilterCondition that handles polymorphic Value serialization
    /// </summary>
    public class FilterConditionConverter : JsonConverter<FilterCondition>
    {
        public override FilterCondition ReadJson(JsonReader reader, Type objectType, FilterCondition existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            var condition = new FilterCondition();

            // Read simple properties
            if (obj.TryGetValue("Type", out var typeToken))
            {
                // Try to deserialize as integer first (legacy format), then as string name
                if (typeToken.Type == JTokenType.Integer)
                {
                    condition.Type = (FilterConditionType)typeToken.Value<int>();
                }
                else
                {
                    condition.Type = typeToken.ToObject<FilterConditionType>(serializer);
                }
            }

            if (obj.TryGetValue("CustomName", out var customNameToken))
            {
                condition.CustomName = customNameToken.ToObject<string>(serializer) ?? "";
            }

            if (obj.TryGetValue("Operator", out var operatorToken))
            {
                condition.Operator = operatorToken.ToObject<LogicOperator>(serializer);
            }

            if (obj.TryGetValue("IsEnabled", out var enabledToken))
            {
                condition.IsEnabled = enabledToken.ToObject<bool>(serializer);
            }

            // Handle Value based on the condition type
            if (obj.TryGetValue("Value", out var valueToken) && valueToken != null && valueToken.Type != JTokenType.Null)
            {
                condition.Value = DeserializeValue(valueToken, condition.Type, condition.ValueType, serializer);
            }

            return condition;
        }

        private object DeserializeValue(JToken valueToken, FilterConditionType type, FilterValueType valueType, JsonSerializer serializer)
        {
            try
            {
                switch (valueType)
                {
                    case FilterValueType.Number:
                        return valueToken.ToObject<double>(serializer);

                    case FilterValueType.Text:
                        return valueToken.ToObject<string>(serializer) ?? "";

                    case FilterValueType.Boolean:
                        // Handle tri-state boolean (null, true, false)
                        if (valueToken.Type == JTokenType.Null)
                            return null;
                        return valueToken.ToObject<bool>(serializer);

                    case FilterValueType.Selection:
                        return valueToken.ToObject<string>(serializer) ?? "";

                    case FilterValueType.Date:
                        return valueToken.ToObject<DateTime>(serializer);

                    case FilterValueType.NumberWithSort:
                        // Try to deserialize as ResultLimitValue
                        if (valueToken.Type == JTokenType.Object)
                        {
                            return valueToken.ToObject<ResultLimitValue>(serializer);
                        }
                        // Legacy format: might be string "count|sortOption"
                        if (valueToken.Type == JTokenType.String)
                        {
                            var strValue = valueToken.ToString();
                            var parts = strValue.Split('|');
                            if (parts.Length >= 1 && int.TryParse(parts[0], out int count))
                            {
                                var sortOption = ResultSortOption.Newest;
                                if (parts.Length >= 2 && Enum.TryParse<ResultSortOption>(parts[1], true, out var parsed))
                                    sortOption = parsed;
                                return new ResultLimitValue(count, sortOption);
                            }
                        }
                        // Fallback: just a number
                        if (valueToken.Type == JTokenType.Integer)
                        {
                            return new ResultLimitValue(valueToken.Value<int>(), ResultSortOption.Newest);
                        }
                        return new ResultLimitValue(100, ResultSortOption.Newest);

                    case FilterValueType.Range:
                        // Try to deserialize as RangeValue object
                        if (valueToken.Type == JTokenType.Object)
                        {
                            return valueToken.ToObject<RangeValue>(serializer);
                        }
                        // Handle null or empty
                        return new RangeValue();

                    case FilterValueType.SearchQuery:
                        // Try to deserialize as SearchQueryValue object
                        if (valueToken.Type == JTokenType.Object)
                        {
                            return valueToken.ToObject<SearchQueryValue>(serializer);
                        }
                        // Handle plain string (legacy format)
                        if (valueToken.Type == JTokenType.String)
                        {
                            return new SearchQueryValue(valueToken.ToString(), SearchFieldType.All);
                        }
                        return new SearchQueryValue();

                    case FilterValueType.ExcludeMod:
                        // Try to deserialize as ExcludeModValue object
                        if (valueToken.Type == JTokenType.Object)
                        {
                            return valueToken.ToObject<ExcludeModValue>(serializer);
                        }
                        // Handle plain string (legacy format)
                        if (valueToken.Type == JTokenType.String)
                        {
                            return new ExcludeModValue(valueToken.ToString(), false);
                        }
                        return new ExcludeModValue();

                    default:
                        return valueToken.ToObject<object>(serializer);
                }
            }
            catch
            {
                // If deserialization fails, return null to let SetDefaultValue handle it
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, FilterCondition value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Type");
            serializer.Serialize(writer, value.Type);

            writer.WritePropertyName("CustomName");
            serializer.Serialize(writer, value.CustomName);

            writer.WritePropertyName("Operator");
            serializer.Serialize(writer, value.Operator);

            writer.WritePropertyName("IsEnabled");
            serializer.Serialize(writer, value.IsEnabled);

            writer.WritePropertyName("Value");
            serializer.Serialize(writer, value.Value);

            writer.WriteEndObject();
        }
    }
    /// <summary>
    /// Represents a saved filter preset
    /// </summary>
    public class FilterPreset
    {
        /// <summary>
        /// Name of the preset
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// When the preset was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the preset was last modified
        /// </summary>
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// List of condition groups in this preset
        /// </summary>
        public List<FilterGroup> Groups { get; set; } = new List<FilterGroup>();

        /// <summary>
        /// Optional description for this preset
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Top-level result limit (overrides group-level limits)
        /// </summary>
        public ResultLimitValue TopLevelResultLimit { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public FilterPreset() { }

        /// <summary>
        /// Creates a new preset with the specified name
        /// </summary>
        public FilterPreset(string name)
        {
            Name = name;
            CreatedAt = DateTime.Now;
            ModifiedAt = DateTime.Now;
        }

        /// <summary>
        /// Adds a group to this preset
        /// </summary>
        public void AddGroup(FilterGroup group)
        {
            Groups.Add(group);
            ModifiedAt = DateTime.Now;
        }

        /// <summary>
        /// Removes a group from this preset
        /// </summary>
        public void RemoveGroup(FilterGroup group)
        {
            Groups.Remove(group);
            ModifiedAt = DateTime.Now;
        }

        /// <summary>
        /// Gets all active groups
        /// </summary>
        public List<FilterGroup> GetActiveGroups()
        {
            return Groups.Where(g => g.HasActiveConditions()).ToList();
        }

        /// <summary>
        /// Checks if the preset has a result limit (either top-level or group-level)
        /// </summary>
        public bool HasResultLimit()
        {
            // Check top-level limit
            if (TopLevelResultLimit != null && TopLevelResultLimit.Count > 0)
                return true;

            // Check group-level limits
            foreach (var group in Groups)
            {
                if (group.GetResultLimit() != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a deep copy of this preset
        /// </summary>
        public FilterPreset Clone()
        {
            var clone = new FilterPreset
            {
                Name = this.Name,
                CreatedAt = this.CreatedAt,
                ModifiedAt = this.ModifiedAt,
                Description = this.Description,
                TopLevelResultLimit = this.TopLevelResultLimit != null ? new ResultLimitValue
                {
                    Count = this.TopLevelResultLimit.Count,
                    SortOption = this.TopLevelResultLimit.SortOption
                } : null,
                Groups = this.Groups.Select(g => g.Clone()).ToList()
            };
            return clone;
        }

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter> { new FilterConditionConverter() }
        };

        /// <summary>
        /// Serializes this preset to JSON
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, SerializerSettings);
        }

        /// <summary>
        /// Deserializes a preset from JSON
        /// </summary>
        public static FilterPreset FromJson(string json)
        {
            try
            {
                var preset = JsonConvert.DeserializeObject<FilterPreset>(json, SerializerSettings);
                // Convert legacy Min/Max conditions to Range conditions
                preset?.ConvertLegacyConditions();
                return preset;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deserializing preset: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Converts legacy Min/Max conditions to Range conditions for backward compatibility
        /// Also converts Ne to Noodle (Ne was removed from UI)
        /// </summary>
        private void ConvertLegacyConditions()
        {
            foreach (var group in Groups)
            {
                var newConditions = new List<FilterCondition>();
                var processedTypes = new HashSet<FilterConditionType>();

                for (int i = 0; i < group.Conditions.Count; i++)
                {
                    var condition = group.Conditions[i];

                    // Convert Ne to Noodle
                    if (condition.Type == FilterConditionType.Ne)
                    {
                        condition.Type = FilterConditionType.Noodle;
                    }

                    // Skip if already processed (paired Min/Max)
                    if (processedTypes.Contains(condition.Type))
                        continue;

                    // Check if this is a legacy Min/Max condition that should be converted to Range
                    var rangeType = GetCorrespondingRangeType(condition.Type);
                    if (rangeType != null)
                    {
                        // Find the matching Max condition if exists
                        var minMaxPair = FindMatchingMinMaxCondition(group.Conditions, condition.Type, i);
                        if (minMaxPair != null)
                        {
                            // Create a Range condition from Min/Max pair
                            var rangeCondition = new FilterCondition(rangeType.Value);
                            double? min = condition.Value != null ? Convert.ToDouble(condition.Value) : null;
                            double? max = minMaxPair.Value != null ? Convert.ToDouble(minMaxPair.Value) : null;
                            rangeCondition.Value = new RangeValue(min, max);
                            rangeCondition.IsEnabled = condition.IsEnabled || minMaxPair.IsEnabled;
                            rangeCondition.Operator = minMaxPair.Operator; // Use the second condition's operator
                            newConditions.Add(rangeCondition);
                            processedTypes.Add(minMaxPair.Type);
                        }
                        else
                        {
                            // Single Min or Max condition, convert to Range with only one side
                            var rangeCondition = new FilterCondition(rangeType.Value);
                            if (IsMinCondition(condition.Type))
                            {
                                double? min = condition.Value != null ? Convert.ToDouble(condition.Value) : null;
                                rangeCondition.Value = new RangeValue(min, null);
                            }
                            else
                            {
                                double? max = condition.Value != null ? Convert.ToDouble(condition.Value) : null;
                                rangeCondition.Value = new RangeValue(null, max);
                            }
                            rangeCondition.IsEnabled = condition.IsEnabled;
                            rangeCondition.Operator = condition.Operator;
                            newConditions.Add(rangeCondition);
                        }
                        processedTypes.Add(condition.Type);
                    }
                    else
                    {
                        // Not a legacy condition, keep as is
                        newConditions.Add(condition);
                    }
                }

                group.Conditions = newConditions;
            }
        }

        /// <summary>
        /// Gets the corresponding Range type for a Min/Max condition type
        /// </summary>
        private static FilterConditionType? GetCorrespondingRangeType(FilterConditionType type)
        {
            return type switch
            {
                FilterConditionType.MinBpm or FilterConditionType.MaxBpm => FilterConditionType.BpmRange,
                FilterConditionType.MinNps or FilterConditionType.MaxNps => FilterConditionType.NpsRange,
                FilterConditionType.MinDuration or FilterConditionType.MaxDuration => FilterConditionType.DurationRange,
                FilterConditionType.MinSsStars or FilterConditionType.MaxSsStars => FilterConditionType.SsStarsRange,
                FilterConditionType.MinBlStars or FilterConditionType.MaxBlStars => FilterConditionType.BlStarsRange,
                FilterConditionType.MinScore or FilterConditionType.MaxScore => FilterConditionType.ScoreRange,
                FilterConditionType.MinPlays or FilterConditionType.MaxPlays => FilterConditionType.PlaysRange,
                FilterConditionType.MinDownloads or FilterConditionType.MaxDownloads => FilterConditionType.DownloadsRange,
                FilterConditionType.MinUpvotes or FilterConditionType.MaxUpvotes => FilterConditionType.UpvotesRange,
                FilterConditionType.MinDownvotes or FilterConditionType.MaxDownvotes => FilterConditionType.DownvotesRange,
                FilterConditionType.MinUpvoteRatio or FilterConditionType.MaxUpvoteRatio => FilterConditionType.UpvoteRatioRange,
                FilterConditionType.MinDownvoteRatio or FilterConditionType.MaxDownvoteRatio => FilterConditionType.DownvoteRatioRange,
                FilterConditionType.MinSageScore or FilterConditionType.MaxSageScore => FilterConditionType.SageScoreRange,
                FilterConditionType.MinNjs or FilterConditionType.MaxNjs => FilterConditionType.NjsRange,
                FilterConditionType.MinBombs or FilterConditionType.MaxBombs => FilterConditionType.BombsRange,
                FilterConditionType.MinOffset or FilterConditionType.MaxOffset => FilterConditionType.OffsetRange,
                FilterConditionType.MinEvents or FilterConditionType.MaxEvents => FilterConditionType.EventsRange,
                FilterConditionType.MinObstacles or FilterConditionType.MaxObstacles => FilterConditionType.ObstaclesRange,
                FilterConditionType.MinBombsMap or FilterConditionType.MaxBombsMap => FilterConditionType.BombsMapRange,
                FilterConditionType.MinNotes or FilterConditionType.MaxNotes => FilterConditionType.NotesRange,
                FilterConditionType.MinSeconds or FilterConditionType.MaxSeconds => FilterConditionType.SecondsRange,
                FilterConditionType.MinLength or FilterConditionType.MaxLength => FilterConditionType.LengthRange,
                FilterConditionType.MinParityErrors or FilterConditionType.MaxParityErrors => FilterConditionType.ParityErrorsRange,
                FilterConditionType.MinParityWarns or FilterConditionType.MaxParityWarns => FilterConditionType.ParityWarnsRange,
                FilterConditionType.MinParityResets or FilterConditionType.MaxParityResets => FilterConditionType.ParityResetsRange,
                FilterConditionType.MinMaxScore or FilterConditionType.MaxMaxScore => FilterConditionType.MaxScoreRange,
                _ => null
            };
        }

        /// <summary>
        /// Checks if a condition type is a Min condition
        /// </summary>
        private static bool IsMinCondition(FilterConditionType type)
        {
            return type == FilterConditionType.MinBpm ||
                   type == FilterConditionType.MinNps ||
                   type == FilterConditionType.MinDuration ||
                   type == FilterConditionType.MinSsStars ||
                   type == FilterConditionType.MinBlStars ||
                   type == FilterConditionType.MinScore ||
                   type == FilterConditionType.MinPlays ||
                   type == FilterConditionType.MinDownloads ||
                   type == FilterConditionType.MinUpvotes ||
                   type == FilterConditionType.MinDownvotes ||
                   type == FilterConditionType.MinUpvoteRatio ||
                   type == FilterConditionType.MinDownvoteRatio ||
                   type == FilterConditionType.MinSageScore ||
                   type == FilterConditionType.MinNjs ||
                   type == FilterConditionType.MinBombs ||
                   type == FilterConditionType.MinOffset ||
                   type == FilterConditionType.MinEvents ||
                   type == FilterConditionType.MinObstacles ||
                   type == FilterConditionType.MinBombsMap ||
                   type == FilterConditionType.MinNotes ||
                   type == FilterConditionType.MinSeconds ||
                   type == FilterConditionType.MinLength ||
                   type == FilterConditionType.MinParityErrors ||
                   type == FilterConditionType.MinParityWarns ||
                   type == FilterConditionType.MinParityResets ||
                   type == FilterConditionType.MinMaxScore;
        }

        /// <summary>
        /// Finds the matching Max/Min condition for a Min/Max pair
        /// </summary>
        private static FilterCondition FindMatchingMinMaxCondition(List<FilterCondition> conditions, FilterConditionType type, int startIndex)
        {
            var correspondingType = GetCorrespondingMinMaxType(type);
            if (correspondingType == null) return null;

            for (int i = startIndex + 1; i < conditions.Count; i++)
            {
                if (conditions[i].Type == correspondingType.Value)
                    return conditions[i];
            }
            return null;
        }

        /// <summary>
        /// Gets the corresponding Max type for a Min type, or vice versa
        /// </summary>
        private static FilterConditionType? GetCorrespondingMinMaxType(FilterConditionType type)
        {
            return type switch
            {
                FilterConditionType.MinBpm => FilterConditionType.MaxBpm,
                FilterConditionType.MaxBpm => FilterConditionType.MinBpm,
                FilterConditionType.MinNps => FilterConditionType.MaxNps,
                FilterConditionType.MaxNps => FilterConditionType.MinNps,
                FilterConditionType.MinDuration => FilterConditionType.MaxDuration,
                FilterConditionType.MaxDuration => FilterConditionType.MinDuration,
                FilterConditionType.MinSsStars => FilterConditionType.MaxSsStars,
                FilterConditionType.MaxSsStars => FilterConditionType.MinSsStars,
                FilterConditionType.MinBlStars => FilterConditionType.MaxBlStars,
                FilterConditionType.MaxBlStars => FilterConditionType.MinBlStars,
                FilterConditionType.MinScore => FilterConditionType.MaxScore,
                FilterConditionType.MaxScore => FilterConditionType.MinScore,
                FilterConditionType.MinPlays => FilterConditionType.MaxPlays,
                FilterConditionType.MaxPlays => FilterConditionType.MinPlays,
                FilterConditionType.MinDownloads => FilterConditionType.MaxDownloads,
                FilterConditionType.MaxDownloads => FilterConditionType.MinDownloads,
                FilterConditionType.MinUpvotes => FilterConditionType.MaxUpvotes,
                FilterConditionType.MaxUpvotes => FilterConditionType.MinUpvotes,
                FilterConditionType.MinDownvotes => FilterConditionType.MaxDownvotes,
                FilterConditionType.MaxDownvotes => FilterConditionType.MinDownvotes,
                FilterConditionType.MinUpvoteRatio => FilterConditionType.MaxUpvoteRatio,
                FilterConditionType.MaxUpvoteRatio => FilterConditionType.MinUpvoteRatio,
                FilterConditionType.MinDownvoteRatio => FilterConditionType.MaxDownvoteRatio,
                FilterConditionType.MaxDownvoteRatio => FilterConditionType.MinDownvoteRatio,
                FilterConditionType.MinSageScore => FilterConditionType.MaxSageScore,
                FilterConditionType.MaxSageScore => FilterConditionType.MinSageScore,
                FilterConditionType.MinNjs => FilterConditionType.MaxNjs,
                FilterConditionType.MaxNjs => FilterConditionType.MinNjs,
                FilterConditionType.MinBombs => FilterConditionType.MaxBombs,
                FilterConditionType.MaxBombs => FilterConditionType.MinBombs,
                FilterConditionType.MinOffset => FilterConditionType.MaxOffset,
                FilterConditionType.MaxOffset => FilterConditionType.MinOffset,
                FilterConditionType.MinEvents => FilterConditionType.MaxEvents,
                FilterConditionType.MaxEvents => FilterConditionType.MinEvents,
                FilterConditionType.MinObstacles => FilterConditionType.MaxObstacles,
                FilterConditionType.MaxObstacles => FilterConditionType.MinObstacles,
                FilterConditionType.MinBombsMap => FilterConditionType.MaxBombsMap,
                FilterConditionType.MaxBombsMap => FilterConditionType.MinBombsMap,
                FilterConditionType.MinNotes => FilterConditionType.MaxNotes,
                FilterConditionType.MaxNotes => FilterConditionType.MinNotes,
                FilterConditionType.MinSeconds => FilterConditionType.MaxSeconds,
                FilterConditionType.MaxSeconds => FilterConditionType.MinSeconds,
                FilterConditionType.MinLength => FilterConditionType.MaxLength,
                FilterConditionType.MaxLength => FilterConditionType.MinLength,
                FilterConditionType.MinParityErrors => FilterConditionType.MaxParityErrors,
                FilterConditionType.MaxParityErrors => FilterConditionType.MinParityErrors,
                FilterConditionType.MinParityWarns => FilterConditionType.MaxParityWarns,
                FilterConditionType.MaxParityWarns => FilterConditionType.MinParityWarns,
                FilterConditionType.MinParityResets => FilterConditionType.MaxParityResets,
                FilterConditionType.MaxParityResets => FilterConditionType.MinParityResets,
                FilterConditionType.MinMaxScore => FilterConditionType.MaxMaxScore,
                FilterConditionType.MaxMaxScore => FilterConditionType.MinMaxScore,
                _ => null
            };
        }

        /// <summary>
        /// Saves the preset to a file
        /// </summary>
        public void SaveToFile(string filePath)
        {
            ModifiedAt = DateTime.Now;
            File.WriteAllText(filePath, ToJson());
        }

        /// <summary>
        /// Loads a preset from a file
        /// </summary>
        public static FilterPreset LoadFromFile(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var preset = FromJson(json);
                if (preset == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to deserialize preset from {filePath}");
                }
                return preset;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading preset from {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Returns a string representation of this preset
        /// </summary>
        public override string ToString()
        {
            var activeGroups = GetActiveGroups().Count;
            var totalConditions = Groups.Sum(g => g.Conditions.Count);
            return $"{Name} ({activeGroups} 组, {totalConditions} 条件)";
        }
    }
}