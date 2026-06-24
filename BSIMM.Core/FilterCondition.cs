using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// Logical operator for combining conditions
    /// </summary>
    public enum LogicOperator
    {
        And,
        Or
    }

    /// <summary>
    /// Represents a single filter condition
    /// </summary>
    public class FilterCondition
    {
        /// <summary>
        /// The type of this filter condition
        /// </summary>
        public FilterConditionType Type { get; set; }

        /// <summary>
        /// Custom name for Custom type conditions
        /// </summary>
        public string CustomName { get; set; } = "";

        /// <summary>
        /// Display name for the condition
        /// </summary>
        [JsonIgnore]
        public string DisplayName => Type == FilterConditionType.Custom ?
            (string.IsNullOrWhiteSpace(CustomName) ? "自定义" : CustomName) :
            FilterConditionMetadata.GetDisplayName(Type);

        /// <summary>
        /// The value type of this condition
        /// </summary>
        [JsonIgnore]
        public FilterValueType ValueType => FilterConditionMetadata.GetValueType(Type);

        /// <summary>
        /// The current value of this condition
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Logical operator to use when combining with the next condition
        /// </summary>
        public LogicOperator Operator { get; set; } = LogicOperator.And;

        /// <summary>
        /// Whether this condition is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Available options for selection-type conditions
        /// </summary>
        [JsonIgnore]
        public List<string> Options => FilterConditionMetadata.GetOptions(Type);

        /// <summary>
        /// Default constructor
        /// </summary>
        public FilterCondition() { }

        /// <summary>
        /// Creates a new filter condition with the specified type
        /// </summary>
        public FilterCondition(FilterConditionType type)
        {
            Type = type;
            SetDefaultValue();
        }

        /// <summary>
        /// Creates a new filter condition with the specified type and value
        /// </summary>
        public FilterCondition(FilterConditionType type, object value)
        {
            Type = type;
            Value = value;
            IsEnabled = true;
        }

        /// <summary>
        /// Sets the default value based on the value type
        /// </summary>
        public void SetDefaultValue()
        {
            switch (ValueType)
            {
                case FilterValueType.Text:
                    Value = "";
                    break;
                case FilterValueType.Number:
                    Value = 0.0;
                    break;
                case FilterValueType.Boolean:
                    // Default to null (不限) - tri-state: null/true/false
                    Value = null;
                    break;
                case FilterValueType.Selection:
                    Value = Options != null && Options.Count > 0 ? Options[0] : "";
                    break;
                case FilterValueType.Date:
                    Value = DateTime.Now.AddDays(-30); // Default to 30 days ago
                    break;
                case FilterValueType.NumberWithSort:
                    Value = new ResultLimitValue(100, ResultSortOption.Newest);
                    break;
                case FilterValueType.Range:
                    Value = new RangeValue();
                    break;
                case FilterValueType.SearchQuery:
                    Value = new SearchQueryValue();
                    break;
                case FilterValueType.ExcludeMod:
                    Value = new ExcludeModValue();
                    break;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this condition has a meaningful value
        /// </summary>
        public bool HasValue()
        {
            if (!IsEnabled) return false;

            switch (ValueType)
            {
                case FilterValueType.Text:
                    return !string.IsNullOrWhiteSpace(Value?.ToString());
                case FilterValueType.Number:
                    return Value != null && Convert.ToDouble(Value) > 0;
                case FilterValueType.Boolean:
                    // Boolean uses tri-state: null = 不限, true = 是, false = 否
                    // Only filter when value is explicitly set (not null)
                    return Value != null;
                case FilterValueType.Selection:
                    return !string.IsNullOrWhiteSpace(Value?.ToString());
                case FilterValueType.Date:
                    return Value != null;
                case FilterValueType.NumberWithSort:
                    if (Value == null) return false;
                    if (Value is ResultLimitValue resultLimit)
                        return resultLimit.Count > 0;
                    return true;
                case FilterValueType.Range:
                    if (Value is RangeValue rangeVal)
                        return rangeVal.HasValue;
                    return false;
                case FilterValueType.SearchQuery:
                    if (Value is SearchQueryValue queryVal)
                        return queryVal.HasValue;
                    return false;
                case FilterValueType.ExcludeMod:
                    if (Value is ExcludeModValue excludeModVal)
                        return excludeModVal.HasValue;
                    // Backward compatibility: old string value
                    return !string.IsNullOrWhiteSpace(Value?.ToString());
                default:
                    return false;
            }
        }

        /// <summary>
        /// Creates a deep copy of this condition
        /// </summary>
        public FilterCondition Clone()
        {
            object clonedValue = this.Value;
            if (clonedValue != null)
            {
                // Deep-copy mutable value objects to avoid shared state between clones
                var type = clonedValue.GetType();
                if (type == typeof(RangeValue))
                    clonedValue = new RangeValue(((RangeValue)clonedValue).Min, ((RangeValue)clonedValue).Max);
                else if (type == typeof(SearchQueryValue))
                {
                    var src = (SearchQueryValue)clonedValue;
                    clonedValue = new SearchQueryValue { Query = src.Query, FieldTypes = src.FieldTypes };
                }
                else if (type == typeof(ResultLimitValue))
                    clonedValue = new ResultLimitValue(((ResultLimitValue)clonedValue).Count) { SortOption = ((ResultLimitValue)clonedValue).SortOption };
                else if (type == typeof(ExcludeModValue))
                    clonedValue = new ExcludeModValue { ModName = ((ExcludeModValue)clonedValue).ModName, Strict = ((ExcludeModValue)clonedValue).Strict };
            }
            return new FilterCondition
            {
                Type = this.Type,
                CustomName = this.CustomName,
                Value = clonedValue,
                Operator = this.Operator,
                IsEnabled = this.IsEnabled
            };
        }

        /// <summary>
        /// Returns a string representation of this condition
        /// </summary>
        public override string ToString()
        {
            return $"{DisplayName}: {Value} ({Operator})";
        }
    }
}