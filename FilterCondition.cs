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
                    Value = false;
                    break;
                case FilterValueType.Selection:
                    Value = Options != null && Options.Count > 0 ? Options[0] : "";
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
                    return Value != null && Convert.ToBoolean(Value);
                case FilterValueType.Selection:
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
            return new FilterCondition
            {
                Type = this.Type,
                CustomName = this.CustomName,
                Value = this.Value,
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