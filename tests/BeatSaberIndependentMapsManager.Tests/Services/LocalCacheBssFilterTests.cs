using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using BeatSaberIndependentMapsManager.Services;

namespace BeatSaberIndependentMapsManager.Tests.Services
{
    public class LocalCacheBssFilterTests : IDisposable
    {
        private readonly string _cacheDir;
        private readonly string _cachePath;

        public LocalCacheBssFilterTests()
        {
            _cacheDir = Path.Combine(Path.GetTempPath(), "bsimm-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_cacheDir);
            _cachePath = Path.Combine(_cacheDir, "cache.json");
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_cacheDir))
                    Directory.Delete(_cacheDir, true);
            }
            catch
            {
                // Best-effort cleanup; temp files are harmless if locked.
            }
        }

        [Fact]
        public async Task FilterWithBeatSpiderSharpAsync_FiltersByBpmRange_UsingBssEngine()
        {
            WriteCache(
                Song("fast", 150.0, curated: false),
                Song("slow", 90.0, curated: false));

            using var manager = new LocalCacheManager(_cachePath);
            var preset = new FilterPreset("BPM");
            var group = new FilterGroup("Group");
            group.AddCondition(new FilterCondition(FilterConditionType.BpmRange,
                new RangeValue(140, 160)));
            preset.AddGroup(group);

            var results = await manager.FilterWithBeatSpiderSharpAsync(preset, null);

            var result = Assert.Single(results);
            Assert.Equal("fast", result.Id);
        }

        [Fact]
        public async Task FilterWithBeatSpiderSharpAsync_FallsBackToBsimEngine_ForCuratedPreset()
        {
            WriteCache(
                Song("curated-song", 150.0, curated: true),
                Song("plain-song", 150.0, curated: false));

            using var manager = new LocalCacheManager(_cachePath);
            var preset = new FilterPreset("Curated");
            var group = new FilterGroup("Group");
            group.AddCondition(new FilterCondition(FilterConditionType.Curated, true));
            preset.AddGroup(group);

            var results = await manager.FilterWithBeatSpiderSharpAsync(preset, null);

            var result = Assert.Single(results);
            Assert.Equal("curated-song", result.Id);
        }

        private void WriteCache(params string[] songs)
        {
            File.WriteAllText(_cachePath,
                "{\"docs\":[" + string.Join(",", songs) + "],\"date\":0}");
        }

        private static string Song(string id, double bpm, bool curated)
        {
            string curatedJson = curated ? "true" : "false";
            return "{\"id\":\"" + id + "\",\"name\":\"" + id + "\",\"curated\":" + curatedJson +
                   ",\"metadata\":{\"bpm\":" + bpm.ToString("0.#") +
                   ",\"songName\":\"" + id + "\",\"songAuthorName\":\"author\",\"levelAuthorName\":\"mapper\"}," +
                   "\"stats\":{}," +
                   "\"versions\":[{\"hash\":\"1234567890abcdef1234567890abcdef12345678\"," +
                   "\"diffs\":[{\"characteristic\":\"Standard\",\"difficulty\":\"Expert\"," +
                   "\"njs\":10.0,\"notes\":200,\"seconds\":100.0,\"length\":200.0,\"nps\":5.0}]}]}";
        }
    }
}
