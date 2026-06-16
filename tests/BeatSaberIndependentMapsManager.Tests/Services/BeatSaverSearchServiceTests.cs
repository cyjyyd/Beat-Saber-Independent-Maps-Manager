using System;
using System.Collections.Generic;
using Xunit;
using BeatSaberIndependentMapsManager.Services;

namespace BeatSaberIndependentMapsManager.Tests.Services
{
    public class BeatSaverSearchServiceTests
    {
        [Fact]
        public void HasOrLogic_ReturnsTrue_WhenGroupHasOrOperator()
        {
            var service = new BeatSaverSearchService(null, null);
            var preset = new FilterPreset("Test");
            var group1 = new FilterGroup("Group 1") { GroupOperator = LogicOperator.Or };
            group1.AddCondition(new FilterCondition { Type = FilterConditionType.MinBpm, Value = 120.0 });
            preset.AddGroup(group1);

            bool result = service.HasOrLogic(preset);

            Assert.True(result);
        }

        [Fact]
        public void HasOrLogic_ReturnsFalse_ForModOnlyGroup()
        {
            var service = new BeatSaverSearchService(null, null);
            var preset = new FilterPreset("Test");
            var group1 = new FilterGroup("Group 1");
            group1.AddCondition(new FilterCondition { Type = FilterConditionType.Chroma, Value = true, Operator = LogicOperator.Or });
            group1.AddCondition(new FilterCondition { Type = FilterConditionType.Me, Value = true, Operator = LogicOperator.Or });
            preset.AddGroup(group1);

            // Even though conditions have OR, it's a mod-only group which API handles naturally
            bool result = service.HasOrLogic(preset);

            Assert.False(result);
        }

        [Fact]
        public void BuildSearchFilterFromPreset_ExtractsCorrectProperties()
        {
            var service = new BeatSaverSearchService(null, null);
            var preset = new FilterPreset("Test");
            var group1 = new FilterGroup("Group 1");
            
            group1.AddCondition(new FilterCondition { Type = FilterConditionType.MinBpm, Value = 120.5 });
            group1.AddCondition(new FilterCondition { Type = FilterConditionType.MaxNps, Value = 8.0 });
            
            preset.AddGroup(group1);

            var filter = service.BuildSearchFilterFromPreset(preset);

            Assert.Equal(120.5, filter.MinBpm);
            Assert.Equal(8.0, filter.MaxNps);
            Assert.Null(filter.MaxBpm);
            Assert.Null(filter.MinNps);
        }
    }
}
