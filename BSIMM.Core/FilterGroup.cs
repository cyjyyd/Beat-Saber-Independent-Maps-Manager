using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// Represents a group of filter conditions
    /// </summary>
    public class FilterGroup
    {
        /// <summary>
        /// Name of this condition group
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// List of conditions in this group
        /// </summary>
        public List<FilterCondition> Conditions { get; set; } = new List<FilterCondition>();

        /// <summary>
        /// Logical operator to use when combining this group with other groups
        /// </summary>
        public LogicOperator GroupOperator { get; set; } = LogicOperator.And;

        /// <summary>
        /// Whether this group is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Whether to use local cache for this group (enables local cache-specific filters)
        /// </summary>
        public bool UseLocalCache { get; set; } = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public FilterGroup() { }

        /// <summary>
        /// Creates a new filter group with the specified name
        /// </summary>
        public FilterGroup(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Adds a condition to this group
        /// </summary>
        public void AddCondition(FilterCondition condition)
        {
            Conditions.Add(condition);
        }

        /// <summary>
        /// Removes a condition from this group
        /// </summary>
        public void RemoveCondition(FilterCondition condition)
        {
            Conditions.Remove(condition);
        }

        /// <summary>
        /// Gets enabled conditions that have values
        /// </summary>
        public List<FilterCondition> GetActiveConditions()
        {
            return Conditions.Where(c => c.IsEnabled && c.HasValue()).ToList();
        }

        /// <summary>
        /// Gets a value indicating whether this group has any active conditions
        /// </summary>
        public bool HasActiveConditions()
        {
            return IsEnabled && Conditions.Any(c => c.IsEnabled && c.HasValue());
        }

        /// <summary>
        /// Gets the ResultLimitValue if this group has a ResultLimit condition
        /// </summary>
        public ResultLimitValue GetResultLimit()
        {
            var limitCondition = Conditions.FirstOrDefault(c =>
                c.Type == FilterConditionType.ResultLimit && c.IsEnabled && c.Value != null);

            if (limitCondition?.Value is ResultLimitValue resultLimit)
                return resultLimit;

            // Try to parse from string (for serialization compatibility)
            if (limitCondition?.Value is string strValue)
            {
                // Format: "count|sortOption" e.g. "100|Newest"
                var parts = strValue.Split('|');
                if (parts.Length >= 1 && int.TryParse(parts[0], out int count))
                {
                    var sortOption = ResultSortOption.Newest;
                    if (parts.Length >= 2 && Enum.TryParse<ResultSortOption>(parts[1], true, out var parsed))
                        sortOption = parsed;
                    return new ResultLimitValue(count, sortOption);
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a deep copy of this group
        /// </summary>
        public FilterGroup Clone()
        {
            var clone = new FilterGroup
            {
                Name = this.Name,
                GroupOperator = this.GroupOperator,
                IsEnabled = this.IsEnabled,
                UseLocalCache = this.UseLocalCache,
                Conditions = this.Conditions.Select(c => c.Clone()).ToList()
            };
            return clone;
        }

        /// <summary>
        /// Returns a string representation of this group
        /// </summary>
        public override string ToString()
        {
            var activeCount = GetActiveConditions().Count;
            return $"{Name} ({activeCount} 条件, {GroupOperator})";
        }
    }
}