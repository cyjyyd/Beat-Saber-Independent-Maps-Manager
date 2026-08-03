using System.Linq;
using Xunit;
using BeatSaberIndependentMapsManager.BeatSpiderSharp;
using BeatSpiderSharp.Models.Enums;

namespace BeatSaberIndependentMapsManager.Tests.BeatSpider
{
    public class BsfToPresetConverterTests
    {
        [Fact]
        public void Convert_MinMaxBpm_MapsToSongDetailBpmRange()
        {
            var preset = NewPreset(
                (FilterConditionType.MinBpm, (object)120.0, LogicOperator.And),
                (FilterConditionType.MaxBpm, 150.0, LogicOperator.And));

            var converted = BsfToPresetConverter.Convert(preset);

            var config = Assert.Single(converted.FilterOptions);
            var bpm = config.SongDetailFilter.Bpm;
            Assert.True(bpm.Enable);
            Assert.Equal(120f, bpm.Min);
            Assert.Equal(150f, bpm.Max);
        }

        [Fact]
        public void Convert_QueryCondition_SetsSearchOptions()
        {
            var preset = NewPreset(
                (FilterConditionType.Query, (object)new SearchQueryValue { Query = "hello world" }, LogicOperator.And));

            var converted = BsfToPresetConverter.Convert(preset);

            var config = Assert.Single(converted.FilterOptions);
            var search = config.SearchOptions;
            Assert.True(search.Enable);
            Assert.True(search.SearchTitle);
            Assert.True(search.SearchSongName);
            Assert.True(search.SearchAuthor);
            Assert.True(search.SearchMapper);
            Assert.Contains("hello world", search.AdvanceTerms.Select(t => t.Content));
        }

        [Fact]
        public void Convert_ChromaAndNoodleConditions_SetRequireMods()
        {
            var preset = NewPreset(
                (FilterConditionType.Chroma, (object)true, LogicOperator.And),
                (FilterConditionType.Noodle, true, LogicOperator.And));

            var converted = BsfToPresetConverter.Convert(preset);

            var config = Assert.Single(converted.FilterOptions);
            var mods = config.LevelDetailOptions.RequireMods;
            Assert.True(mods.Enable);
            Assert.Contains(MMod.Chroma, mods.Filter);
            Assert.Contains(MMod.NoodleExtensions, mods.Filter);
        }

        [Fact]
        public void Convert_DisablesFullSpreadAndAutoMapperByDefault()
        {
            var preset = NewPreset((FilterConditionType.MinBpm, (object)100.0, LogicOperator.And));

            var converted = BsfToPresetConverter.Convert(preset);

            var config = Assert.Single(converted.FilterOptions);
            Assert.False(config.SongDetailFilter.FullSpread.Enable);
            Assert.False(config.SongDetailFilter.AutoMapper.Enable);
        }

        [Theory]
        [InlineData(FilterConditionType.Curated)]
        [InlineData(FilterConditionType.Verified)]
        [InlineData(FilterConditionType.Leaderboard)]
        [InlineData(FilterConditionType.CustomMod)]
        [InlineData(FilterConditionType.ExcludeCustomMod)]
        public void HasUnmappableConditions_ReturnsTrue_ForUnsupportedTypes(FilterConditionType type)
        {
            var preset = NewPreset((type, (object)true, LogicOperator.And));

            Assert.True(BsfToPresetConverter.HasUnmappableConditions(preset));
        }

        [Fact]
        public void HasUnmappableConditions_ReturnsFalse_ForSupportedTypes()
        {
            var preset = NewPreset(
                (FilterConditionType.MinBpm, (object)120.0, LogicOperator.And),
                (FilterConditionType.Chroma, true, LogicOperator.And),
                (FilterConditionType.Tags, "dance,tech", LogicOperator.And));

            Assert.False(BsfToPresetConverter.HasUnmappableConditions(preset));
        }

        [Fact]
        public void Convert_ResultLimit_SetsOutputLimit()
        {
            var preset = NewPreset((FilterConditionType.MinBpm, (object)120.0, LogicOperator.And));
            preset.TopLevelResultLimit = new ResultLimitValue(50);

            var converted = BsfToPresetConverter.Convert(preset);

            Assert.True(converted.Output.LimitSongs);
            Assert.Equal(50, converted.Output.MaxSongs);
        }

        [Fact]
        public void Convert_OrOperator_SplitsIntoMultipleFilterConfigs()
        {
            var preset = NewPreset(
                (FilterConditionType.MinBpm, (object)100.0, LogicOperator.Or),
                (FilterConditionType.MinNps, 5.0, LogicOperator.And));

            var converted = BsfToPresetConverter.Convert(preset);

            Assert.Equal(2, converted.FilterOptions.Count);
            Assert.True(converted.FilterOptions[0].SongDetailFilter.Bpm.Enable);
            Assert.True(converted.FilterOptions[1].LevelDetailOptions.Nps.Enable);
        }

        [Fact]
        public void ConvertBack_RoundTrip_PreservesCoreConditions()
        {
            var preset = new FilterPreset("RoundTrip") { Description = "desc" };
            var group = new FilterGroup("Group");
            group.AddCondition(new FilterCondition(FilterConditionType.MinBpm, 120.0));
            group.AddCondition(new FilterCondition(FilterConditionType.MaxBpm, 150.0));
            group.AddCondition(new FilterCondition(FilterConditionType.Chroma, true));
            group.AddCondition(new FilterCondition(FilterConditionType.Tags, "dance,tech"));
            group.AddCondition(new FilterCondition(FilterConditionType.Ranked, true));
            preset.AddGroup(group);

            var converted = BsfToPresetConverter.Convert(preset);
            var back = BsfToPresetConverter.ConvertBack(converted);

            Assert.Equal("RoundTrip", back.Name);
            Assert.Equal("desc", back.Description);
            var backGroup = Assert.Single(back.Groups);
            Assert.Contains(backGroup.Conditions, c => c.Type == FilterConditionType.BpmRange);
            Assert.Contains(backGroup.Conditions, c => c.Type == FilterConditionType.Chroma);
            Assert.Contains(backGroup.Conditions, c => c.Type == FilterConditionType.Ranked);
            Assert.Contains(backGroup.Conditions, c => c.Type == FilterConditionType.Tags);
        }

        private static FilterPreset NewPreset(params (FilterConditionType Type, object Value, LogicOperator Op)[] conditions)
        {
            var preset = new FilterPreset("Test");
            var group = new FilterGroup("Group");
            foreach (var (type, value, op) in conditions)
                group.AddCondition(new FilterCondition(type, value) { Operator = op });
            preset.AddGroup(group);
            return preset;
        }
    }
}
