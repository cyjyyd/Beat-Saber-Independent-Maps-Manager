using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace BeatSaberIndependentMapsManager
{
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
                Groups = this.Groups.Select(g => g.Clone()).ToList()
            };
            return clone;
        }

        /// <summary>
        /// Serializes this preset to JSON
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Deserializes a preset from JSON
        /// </summary>
        public static FilterPreset FromJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<FilterPreset>(json);
            }
            catch
            {
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
                return FromJson(json);
            }
            catch
            {
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