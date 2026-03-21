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
                return preset;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deserializing preset: {ex.Message}");
                return null;
            }
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