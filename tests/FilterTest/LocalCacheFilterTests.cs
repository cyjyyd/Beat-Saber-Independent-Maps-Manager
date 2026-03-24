using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeatSaberIndependentMapsManager.Tests
{
    /// <summary>
    /// Comprehensive test for all local cache filter conditions
    /// </summary>
    public class LocalCacheFilterTests
    {
        private readonly string cachePath;
        private List<BeatSaverMap> allMaps;
        private LocalCacheManager manager;
        private int passCount = 0;
        private int failCount = 0;

        public LocalCacheFilterTests(string cachePath)
        {
            this.cachePath = cachePath;
            this.manager = new LocalCacheManager();
            this.manager.SetCachePath(cachePath);
            LoadAllMaps();
        }

        private void LoadAllMaps()
        {
            allMaps = new List<BeatSaverMap>();
            using (var fileStream = new FileStream(cachePath, FileMode.Open, FileAccess.Read))
            using (var streamReader = new StreamReader(fileStream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer();

                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonToken.StartArray && jsonReader.Path == "docs")
                        break;
                }

                while (jsonReader.Read() && jsonReader.TokenType != JsonToken.EndArray)
                {
                    if (jsonReader.TokenType == JsonToken.StartObject)
                    {
                        var map = serializer.Deserialize<BeatSaverMap>(jsonReader);
                        if (map != null)
                            allMaps.Add(map);
                    }
                }
            }
            Console.WriteLine($"Loaded {allMaps.Count} maps from cache\n");
        }

        public void RunAllTests()
        {
            Console.WriteLine("========== LOCAL CACHE FILTER COMPREHENSIVE TESTS ==========\n");

            // === API & Local Cache Common Filters ===
            Console.WriteLine("=== API & Local Cache Common Filters ===\n");

            TestFilter("BPM >= 120", FilterConditionType.MinBpm, 120.0,
                m => m.Metadata?.Bpm >= 120);

            TestFilter("BPM <= 150", FilterConditionType.MaxBpm, 150.0,
                m => m.Metadata?.Bpm <= 150);

            TestRangeFilter("NPS 5-10", FilterConditionType.MinNps, 5.0, FilterConditionType.MaxNps, 10.0,
                m => HasNpsInRange(m, 5, 10));

            TestFilter("Duration >= 60s", FilterConditionType.MinDuration, 60.0,
                m => m.Metadata?.Duration >= 60);

            TestFilter("Duration <= 300s", FilterConditionType.MaxDuration, 300.0,
                m => m.Metadata?.Duration <= 300);

            // SS Stars - Now supported!
            TestRangeFilter("SS Stars 5-10", FilterConditionType.MinSsStars, 5.0, FilterConditionType.MaxSsStars, 10.0,
                m => HasStarsInRange(m, 5, 10, "ss"));

            // BL Stars - Now supported!
            TestRangeFilter("BL Stars 5-10", FilterConditionType.MinBlStars, 5.0, FilterConditionType.MaxBlStars, 10.0,
                m => HasStarsInRange(m, 5, 10, "bl"));

            // Mod filters
            TestFilter("Chroma=true", FilterConditionType.Chroma, true,
                m => HasModInDiffs(m, "chroma"));

            TestFilter("ME=true", FilterConditionType.Me, true,
                m => HasModInDiffs(m, "me"));

            TestFilter("NE=true", FilterConditionType.Ne, true,
                m => HasModInDiffs(m, "ne"));

            TestFilter("Cinema=true", FilterConditionType.Cinema, true,
                m => HasModInDiffs(m, "cinema"));

            // Other filters
            TestSelectionFilter("AI Only", FilterConditionType.Automapper, "仅AI谱",
                m => m.Automapper);

            TestSelectionFilter("Exclude AI", FilterConditionType.Automapper, "排除AI谱",
                m => !m.Automapper);

            TestFilter("Curated=true", FilterConditionType.Curated, true,
                m => m.Curated);

            TestFilter("Verified=true", FilterConditionType.Verified, true,
                m => m.Uploader?.Verified ?? false);

            // === Local Cache Specific Filters ===
            Console.WriteLine("\n=== Local Cache Specific Filters ===\n");

            TestFilter("SS Ranked=true", FilterConditionType.Ranked, true,
                m => m.Ranked);

            TestFilter("BL Ranked=true", FilterConditionType.BlRanked, true,
                m => m.BlRanked);

            TestFilter("SS Qualified=true", FilterConditionType.Qualified, true,
                m => m.Qualified);

            TestFilter("BL Qualified=true", FilterConditionType.BlQualified, true,
                m => m.BlQualified);

            TestFilter("MinPlays >= 100", FilterConditionType.MinPlays, 100.0,
                m => m.Stats?.Plays >= 100);

            TestFilter("MinDownloads >= 1000", FilterConditionType.MinDownloads, 1000.0,
                m => m.Stats?.Downloads >= 1000);

            TestFilter("MinUpvotes >= 50", FilterConditionType.MinUpvotes, 50.0,
                m => m.Stats?.Upvotes >= 50);

            // Vote ratio filters
            TestFilter("MinUpvoteRatio >= 80%", FilterConditionType.MinUpvoteRatio, 80.0,
                m => CalculateUpvoteRatio(m) >= 80);

            TestFilter("MaxDownvoteRatio <= 10%", FilterConditionType.MaxDownvoteRatio, 10.0,
                m => CalculateDownvoteRatio(m) <= 10);

            // === Diff-specific Filters ===
            Console.WriteLine("\n=== Diff-specific Filters ===\n");

            TestFilter("MinNJS >= 16", FilterConditionType.MinNjs, 16.0,
                m => HasDiffWithValue(m, d => d.Njs >= 16));

            TestFilter("MinBombs >= 1", FilterConditionType.MinBombs, 1.0,
                m => HasDiffWithValue(m, d => d.Bombs >= 1));

            TestSelectionFilter("Characteristic=Standard", FilterConditionType.Characteristic, "Standard",
                m => HasCharacteristicValue(m, "Standard"));

            TestSelectionFilter("Difficulty=ExpertPlus", FilterConditionType.Difficulty, "ExpertPlus",
                m => HasDifficultyValue(m, "ExpertPlus"));

            // === Upload Time Filters ===
            Console.WriteLine("\n=== Upload Time Filters ===\n");

            // Test upload time range (last 30 days)
            var minDate = DateTime.UtcNow.AddDays(-30);
            TestFilter("Uploaded >= 30 days ago", FilterConditionType.MinUploadedDate, minDate,
                m => m.Uploaded >= minDate);

            // Test upload time range (before a specific date)
            var maxDate = new DateTime(2025, 1, 1);
            TestFilter("Uploaded <= 2025-01-01", FilterConditionType.MaxUploadedDate, maxDate,
                m => m.Uploaded <= maxDate);

            // === Result Limit Filter ===
            Console.WriteLine("\n=== Result Limit Filter ===\n");

            TestResultLimit("ResultLimit=10 Newest", 10, ResultSortOption.Newest);
            TestResultLimit("ResultLimit=10 Oldest", 10, ResultSortOption.Oldest);
            TestResultLimit("ResultLimit=10 Random", 10, ResultSortOption.Random);

            // Test group-level result limit
            Console.WriteLine("\n=== Group-Level Result Limit ===\n");
            TestGroupLevelResultLimit("Group ResultLimit=5 Newest", 5, ResultSortOption.Newest);
            TestGroupLevelResultLimit("Group ResultLimit=5 Oldest", 5, ResultSortOption.Oldest);

            // === Range-type Filters (Simplified UI) ===
            Console.WriteLine("\n=== Range-type Filters (Simplified UI) ===\n");

            TestRangeTypeFilter("BPM Range 120-150", FilterConditionType.BpmRange, 120, 150,
                m => m.Metadata?.Bpm >= 120 && m.Metadata?.Bpm <= 150);

            TestRangeTypeFilter("NPS Range 5-10", FilterConditionType.NpsRange, 5, 10,
                m => HasNpsInRange(m, 5, 10));

            TestRangeTypeFilter("Duration Range 60-300s", FilterConditionType.DurationRange, 60, 300,
                m => m.Metadata?.Duration >= 60 && m.Metadata?.Duration <= 300);

            TestRangeTypeFilter("SS Stars Range 5-10", FilterConditionType.SsStarsRange, 5, 10,
                m => HasStarsInRange(m, 5, 10, "ss"));

            TestRangeTypeFilter("BL Stars Range 5-10", FilterConditionType.BlStarsRange, 5, 10,
                m => HasStarsInRange(m, 5, 10, "bl"));

            TestRangeTypeFilter("Score Range 80-100", FilterConditionType.ScoreRange, 80, 100,
                m => m.Stats?.Score >= 0.8 && m.Stats?.Score <= 1.0);

            TestRangeTypeFilter("Upvote Ratio 80-100%", FilterConditionType.UpvoteRatioRange, 80, 100,
                m => CalculateUpvoteRatio(m) >= 80 && CalculateUpvoteRatio(m) <= 100);

            // === AND/OR Logic Tests ===
            Console.WriteLine("\n=== AND/OR Logic Tests ===\n");

            // Test AND logic (default)
            TestAndLogic("AND: BPM>=120 AND Duration>=60",
                (FilterConditionType.MinBpm, 120.0), (FilterConditionType.MinDuration, 60.0),
                m => m.Metadata?.Bpm >= 120 && m.Metadata?.Duration >= 60);

            TestAndLogic("AND: Chroma=true AND NE=true",
                (FilterConditionType.Chroma, true), (FilterConditionType.Ne, true),
                m => HasModInDiffs(m, "chroma") && HasModInDiffs(m, "ne"));

            TestAndLogic("AND: SS Ranked AND BL Ranked",
                (FilterConditionType.Ranked, true), (FilterConditionType.BlRanked, true),
                m => m.Ranked && m.BlRanked);

            // Test OR logic
            TestOrLogic("OR: Chroma=true OR NE=true",
                (FilterConditionType.Chroma, true), (FilterConditionType.Ne, true),
                m => HasModInDiffs(m, "chroma") || HasModInDiffs(m, "ne"));

            TestOrLogic("OR: SS Ranked OR BL Ranked",
                (FilterConditionType.Ranked, true), (FilterConditionType.BlRanked, true),
                m => m.Ranked || m.BlRanked);

            TestOrLogic("OR: BPM>=180 OR NPS>=10",
                (FilterConditionType.MinBpm, 180.0), (FilterConditionType.MinNps, 10.0),
                m => m.Metadata?.Bpm >= 180 || HasNpsAtLeast(m, 10));

            // Test complex combinations
            TestComplexLogic("Complex: (BPM>=120 OR NPS>=8) AND Duration>=60",
                new (FilterConditionType, object, LogicOperator)[] { (FilterConditionType.MinBpm, (object)120.0, LogicOperator.Or), (FilterConditionType.MinNps, (object)8.0, LogicOperator.And) },
                new (FilterConditionType, object)[] { (FilterConditionType.MinDuration, (object)60.0) },
                m => (m.Metadata?.Bpm >= 120 || HasNpsAtLeast(m, 8)) && m.Metadata?.Duration >= 60);

            // Test group-level AND/OR
            TestGroupOrLogic("Group OR: (Chroma+NE) OR (ME+Cinema)",
                new (FilterConditionType, object)[] { (FilterConditionType.Chroma, (object)true), (FilterConditionType.Ne, (object)true) },
                new (FilterConditionType, object)[] { (FilterConditionType.Me, (object)true), (FilterConditionType.Cinema, (object)true) },
                m => (HasModInDiffs(m, "chroma") && HasModInDiffs(m, "ne")) ||
                     (HasModInDiffs(m, "me") && HasModInDiffs(m, "cinema")));

            // === Summary ===
            Console.WriteLine("\n========== TEST SUMMARY ==========");
            Console.WriteLine($"Total: {passCount + failCount}, Passed: {passCount}, Failed: {failCount}");

            if (failCount == 0)
                Console.WriteLine("\nAll tests PASSED!");
            else
                Console.WriteLine($"\n{failCount} tests FAILED!");
        }

        private void TestFilter(string name, FilterConditionType type, object value, Func<BeatSaverMap, bool> expectedPredicate)
        {
            int expected = allMaps.Count(expectedPredicate);

            var preset = CreatePreset(type, value);
            int actual = allMaps.Count(m => manager.TestMatchesFilter(m, preset));

            bool passed = expected == actual;
            if (passed) passCount++; else failCount++;

            Console.WriteLine($"  {name}: Expected={expected}, Actual={actual} {(passed ? "[PASS]" : "[FAIL]")}");
        }

        private void TestRangeFilter(string name, FilterConditionType minType, object minValue,
            FilterConditionType maxType, object maxValue, Func<BeatSaverMap, bool> expectedPredicate)
        {
            int expected = allMaps.Count(expectedPredicate);

            var preset = CreateRangePreset(minType, minValue, maxType, maxValue);
            int actual = allMaps.Count(m => manager.TestMatchesFilter(m, preset));

            bool passed = expected == actual;
            if (passed) passCount++; else failCount++;

            Console.WriteLine($"  {name}: Expected={expected}, Actual={actual} {(passed ? "[PASS]" : "[FAIL]")}");
        }

        private void TestSelectionFilter(string name, FilterConditionType type, string value, Func<BeatSaverMap, bool> expectedPredicate)
        {
            int expected = allMaps.Count(expectedPredicate);

            var preset = CreatePreset(type, value);
            int actual = allMaps.Count(m => manager.TestMatchesFilter(m, preset));

            bool passed = expected == actual;
            if (passed) passCount++; else failCount++;

            Console.WriteLine($"  {name}: Expected={expected}, Actual={actual} {(passed ? "[PASS]" : "[FAIL]")}");
        }

        private void TestResultLimit(string name, int count, ResultSortOption sortOption)
        {
            var preset = new FilterPreset("Test");
            preset.TopLevelResultLimit = new ResultLimitValue(count, sortOption);

            // Verify HasResultLimit() returns true
            if (!preset.HasResultLimit())
            {
                Console.WriteLine($"  {name}: HasResultLimit() returned false [FAIL]");
                failCount++;
                return;
            }

            // Use ParallelFilterMaps which applies result limit
            var results = manager.ParallelFilterMaps(preset, null);

            bool countMatch = results.Count == count;
            bool sortCorrect = true;

            if (countMatch && results.Count > 1)
            {
                // Verify sorting
                switch (sortOption)
                {
                    case ResultSortOption.Newest:
                        // Results should be in descending upload order
                        for (int i = 1; i < results.Count; i++)
                        {
                            if (results[i].Uploaded > results[i - 1].Uploaded)
                            {
                                sortCorrect = false;
                                break;
                            }
                        }
                        break;
                    case ResultSortOption.Oldest:
                        // Results should be in ascending upload order
                        for (int i = 1; i < results.Count; i++)
                        {
                            if (results[i].Uploaded < results[i - 1].Uploaded)
                            {
                                sortCorrect = false;
                                break;
                            }
                        }
                        break;
                    case ResultSortOption.Random:
                        // Can't verify randomness, just check count
                        break;
                }
            }

            bool passed = countMatch && sortCorrect;
            if (passed) passCount++; else failCount++;

            var sortName = sortOption switch
            {
                ResultSortOption.Newest => "最新上传",
                ResultSortOption.Oldest => "最早上传",
                ResultSortOption.Random => "随机",
                _ => sortOption.ToString()
            };

            Console.WriteLine($"  {name}: Count={results.Count}/{count}, Sort={sortName} {(passed ? "[PASS]" : "[FAIL]")}");
        }

        private void TestGroupLevelResultLimit(string name, int count, ResultSortOption sortOption)
        {
            var preset = new FilterPreset("Test");
            var group = new FilterGroup("TestGroup");
            group.UseLocalCache = true;

            // Add ResultLimit condition to group
            var condition = new FilterCondition(FilterConditionType.ResultLimit);
            condition.Value = new ResultLimitValue(count, sortOption);
            condition.IsEnabled = true;
            group.AddCondition(condition);
            preset.AddGroup(group);

            // Verify HasResultLimit() returns true for group-level limit
            if (!preset.HasResultLimit())
            {
                Console.WriteLine($"  {name}: HasResultLimit() returned false [FAIL]");
                failCount++;
                return;
            }

            // Use ParallelFilterMaps which applies result limit
            var results = manager.ParallelFilterMaps(preset, null);

            bool passed = results.Count == count;
            if (passed) passCount++; else failCount++;

            Console.WriteLine($"  {name}: Count={results.Count}/{count} {(passed ? "[PASS]" : "[FAIL]")}");
        }

        private FilterPreset CreatePreset(FilterConditionType type, object value)
        {
            var preset = new FilterPreset("Test");
            var group = new FilterGroup("TestGroup");
            group.UseLocalCache = true;

            var condition = new FilterCondition(type);
            condition.Value = value;
            condition.IsEnabled = true;
            group.AddCondition(condition);

            preset.AddGroup(group);
            return preset;
        }

        private FilterPreset CreateRangePreset(FilterConditionType minType, object minValue,
            FilterConditionType maxType, object maxValue)
        {
            var preset = new FilterPreset("Test");
            var group = new FilterGroup("TestGroup");
            group.UseLocalCache = true;

            var minCond = new FilterCondition(minType) { Value = minValue, IsEnabled = true };
            var maxCond = new FilterCondition(maxType) { Value = maxValue, IsEnabled = true };
            group.AddCondition(minCond);
            group.AddCondition(maxCond);

            preset.AddGroup(group);
            return preset;
        }

        // Helper predicates
        private bool HasNpsInRange(BeatSaverMap map, double? min, double? max)
        {
            if (map.Versions == null || map.Versions.Count == 0 || map.Versions[0].Diffs == null)
                return false;

            foreach (var diff in map.Versions[0].Diffs)
            {
                if (diff == null || diff.Nps <= 0) continue;
                bool matchesMin = min == null || diff.Nps >= min.Value;
                bool matchesMax = max == null || diff.Nps <= max.Value;
                if (matchesMin && matchesMax) return true;
            }
            return false;
        }

        private bool HasStarsInRange(BeatSaverMap map, double? min, double? max, string type)
        {
            if (map.Versions == null || map.Versions.Count == 0 || map.Versions[0].Diffs == null)
                return false;

            foreach (var diff in map.Versions[0].Diffs)
            {
                if (diff == null) continue;

                double? stars = type == "ss" ? diff.Stars : diff.BlStars;
                if (!stars.HasValue) continue;

                bool matchesMin = min == null || stars.Value >= min.Value;
                bool matchesMax = max == null || stars.Value <= max.Value;
                if (matchesMin && matchesMax) return true;
            }
            return false;
        }

        private bool HasModInDiffs(BeatSaverMap map, string modName)
        {
            if (map.Versions == null || map.Versions.Count == 0 || map.Versions[0].Diffs == null)
                return false;

            foreach (var diff in map.Versions[0].Diffs)
            {
                if (diff == null) continue;
                switch (modName.ToLower())
                {
                    case "chroma": if (diff.Chroma) return true; break;
                    case "me": if (diff.Me) return true; break;
                    case "ne": if (diff.Ne) return true; break;
                    case "cinema": if (diff.Cinema) return true; break;
                }
            }
            return false;
        }

        private bool HasDiffWithValue(BeatSaverMap map, Func<BeatSaverVersionDiff, bool> predicate)
        {
            if (map.Versions == null || map.Versions.Count == 0 || map.Versions[0].Diffs == null)
                return false;

            foreach (var diff in map.Versions[0].Diffs)
            {
                if (diff != null && predicate(diff))
                    return true;
            }
            return false;
        }

        private bool HasCharacteristicValue(BeatSaverMap map, string value)
        {
            if (map.Versions == null || map.Versions.Count == 0 || map.Versions[0].Diffs == null)
                return false;

            return map.Versions[0].Diffs.Any(d =>
                d?.Characteristic?.Equals(value, StringComparison.OrdinalIgnoreCase) == true);
        }

        private bool HasDifficultyValue(BeatSaverMap map, string value)
        {
            if (map.Versions == null || map.Versions.Count == 0 || map.Versions[0].Diffs == null)
                return false;

            return map.Versions[0].Diffs.Any(d =>
                d?.Difficulty?.Equals(value, StringComparison.OrdinalIgnoreCase) == true);
        }

        private double CalculateUpvoteRatio(BeatSaverMap map)
        {
            if (map.Stats == null) return 0;
            var total = map.Stats.Upvotes + map.Stats.Downvotes;
            if (total == 0) return 0;
            return (double)map.Stats.Upvotes / total * 100;
        }

        private double CalculateDownvoteRatio(BeatSaverMap map)
        {
            if (map.Stats == null) return 0;
            var total = map.Stats.Upvotes + map.Stats.Downvotes;
            if (total == 0) return 0;
            return (double)map.Stats.Downvotes / total * 100;
        }

        // === Range-type Filter Tests ===
        private void TestRangeTypeFilter(string name, FilterConditionType rangeType, double? min, double? max, Func<BeatSaverMap, bool> expectedPredicate)
        {
            int expected = allMaps.Count(expectedPredicate);

            var preset = new FilterPreset("Test");
            var group = new FilterGroup("TestGroup");
            group.UseLocalCache = true;

            var condition = new FilterCondition(rangeType);
            condition.Value = new RangeValue(min, max);
            condition.IsEnabled = true;
            group.AddCondition(condition);

            preset.AddGroup(group);

            int actual = allMaps.Count(m => manager.TestMatchesFilter(m, preset));

            bool passed = expected == actual;
            if (passed) passCount++; else failCount++;

            Console.WriteLine($"  {name}: Expected={expected}, Actual={actual} {(passed ? "[PASS]" : "[FAIL]")}");
        }

        // === AND Logic Tests ===
        private void TestAndLogic(string name, (FilterConditionType type, object value) cond1, (FilterConditionType type, object value) cond2, Func<BeatSaverMap, bool> expectedPredicate)
        {
            int expected = allMaps.Count(expectedPredicate);

            var preset = new FilterPreset("Test");
            var group = new FilterGroup("TestGroup");
            group.UseLocalCache = true;

            var condition1 = new FilterCondition(cond1.type) { Value = cond1.value, IsEnabled = true, Operator = LogicOperator.And };
            var condition2 = new FilterCondition(cond2.type) { Value = cond2.value, IsEnabled = true, Operator = LogicOperator.And };
            group.AddCondition(condition1);
            group.AddCondition(condition2);

            preset.AddGroup(group);

            int actual = allMaps.Count(m => manager.TestMatchesFilter(m, preset));

            bool passed = expected == actual;
            if (passed) passCount++; else failCount++;

            Console.WriteLine($"  {name}: Expected={expected}, Actual={actual} {(passed ? "[PASS]" : "[FAIL]")}");
        }

        // === OR Logic Tests ===
        private void TestOrLogic(string name, (FilterConditionType type, object value) cond1, (FilterConditionType type, object value) cond2, Func<BeatSaverMap, bool> expectedPredicate)
        {
            int expected = allMaps.Count(expectedPredicate);

            var preset = new FilterPreset("Test");
            var group = new FilterGroup("TestGroup");
            group.UseLocalCache = true;

            // First condition with OR operator, second with AND (default, but won't apply since it's last)
            var condition1 = new FilterCondition(cond1.type) { Value = cond1.value, IsEnabled = true, Operator = LogicOperator.Or };
            var condition2 = new FilterCondition(cond2.type) { Value = cond2.value, IsEnabled = true, Operator = LogicOperator.And };
            group.AddCondition(condition1);
            group.AddCondition(condition2);

            preset.AddGroup(group);

            int actual = allMaps.Count(m => manager.TestMatchesFilter(m, preset));

            bool passed = expected == actual;
            if (passed) passCount++; else failCount++;

            Console.WriteLine($"  {name}: Expected={expected}, Actual={actual} {(passed ? "[PASS]" : "[FAIL]")}");
        }

        // === Complex Logic Tests ===
        private void TestComplexLogic(string name, (FilterConditionType, object, LogicOperator)[] conds1, (FilterConditionType, object)[] conds2, Func<BeatSaverMap, bool> expectedPredicate)
        {
            int expected = allMaps.Count(expectedPredicate);

            var preset = new FilterPreset("Test");
            var group = new FilterGroup("TestGroup");
            group.UseLocalCache = true;

            // Add first set of conditions
            foreach (var cond in conds1)
            {
                var condition = new FilterCondition(cond.Item1) { Value = cond.Item2, IsEnabled = true, Operator = cond.Item3 };
                group.AddCondition(condition);
            }

            // Add second set of conditions
            foreach (var cond in conds2)
            {
                var condition = new FilterCondition(cond.Item1) { Value = cond.Item2, IsEnabled = true, Operator = LogicOperator.And };
                group.AddCondition(condition);
            }

            preset.AddGroup(group);

            int actual = allMaps.Count(m => manager.TestMatchesFilter(m, preset));

            bool passed = expected == actual;
            if (passed) passCount++; else failCount++;

            Console.WriteLine($"  {name}: Expected={expected}, Actual={actual} {(passed ? "[PASS]" : "[FAIL]")}");
        }

        // === Group OR Logic Tests ===
        private void TestGroupOrLogic(string name, (FilterConditionType, object)[] group1Conds, (FilterConditionType, object)[] group2Conds, Func<BeatSaverMap, bool> expectedPredicate)
        {
            int expected = allMaps.Count(expectedPredicate);

            var preset = new FilterPreset("Test");

            // First group
            var group1 = new FilterGroup("Group1") { UseLocalCache = true, GroupOperator = LogicOperator.Or };
            foreach (var cond in group1Conds)
            {
                var condition = new FilterCondition(cond.Item1) { Value = cond.Item2, IsEnabled = true, Operator = LogicOperator.And };
                group1.AddCondition(condition);
            }
            preset.AddGroup(group1);

            // Second group
            var group2 = new FilterGroup("Group2") { UseLocalCache = true, GroupOperator = LogicOperator.And };
            foreach (var cond in group2Conds)
            {
                var condition = new FilterCondition(cond.Item1) { Value = cond.Item2, IsEnabled = true, Operator = LogicOperator.And };
                group2.AddCondition(condition);
            }
            preset.AddGroup(group2);

            int actual = allMaps.Count(m => manager.TestMatchesFilter(m, preset));

            bool passed = expected == actual;
            if (passed) passCount++; else failCount++;

            Console.WriteLine($"  {name}: Expected={expected}, Actual={actual} {(passed ? "[PASS]" : "[FAIL]")}");
        }

        // Helper to check if map has NPS >= threshold
        private bool HasNpsAtLeast(BeatSaverMap map, double minNps)
        {
            if (map.Versions == null || map.Versions.Count == 0 || map.Versions[0].Diffs == null)
                return false;

            foreach (var diff in map.Versions[0].Diffs)
            {
                if (diff != null && diff.Nps >= minNps)
                    return true;
            }
            return false;
        }

        public static void Main(string[] args)
        {
            string cachePath = args.Length > 0 ? args[0] : @"bin/Release/net9.0-windows10.0.17763.0/cache.json";

            if (!File.Exists(cachePath))
            {
                // Try Debug path
                cachePath = @"bin/Debug/net9.0-windows10.0.17763.0/cache.json";
            }

            if (!File.Exists(cachePath))
            {
                Console.WriteLine($"Cache file not found. Tried:");
                Console.WriteLine("  bin/Release/net9.0-windows10.0.17763.0/cache.json");
                Console.WriteLine("  bin/Debug/net9.0-windows10.0.17763.0/cache.json");
                return;
            }

            var tests = new LocalCacheFilterTests(cachePath);
            tests.RunAllTests();
        }
    }
}