using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using BeatSaberIndependentMapsManager.Services;

namespace BeatSaberIndependentMapsManager.Tests.Services
{
    public class HashCacheServiceTests : IDisposable
    {
        private readonly string _testDirectory;

        public HashCacheServiceTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "BSIMM_Tests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        private SongMap CreateTestSongMap(string bsr)
        {
            string songDir = Path.Combine(_testDirectory, bsr);
            Directory.CreateDirectory(songDir);
            
            // Create dummy Info.dat
            File.WriteAllText(Path.Combine(songDir, "Info.dat"), "test info");
            
            return new SongMap
            {
                songFolder = songDir,
                _songFilename = "song.ogg",
                _coverImageFilename = "cover.jpg",
                _difficultyBeatmapSets = new Newtonsoft.Json.Linq.JArray()
            };
        }

        [Fact]
        public async Task EnsurePackHashesAsync_ConcurrentAccess_DoesNotThrowArgumentException()
        {
            var hashCache = new HashCacheService();
            var dict1 = new Dictionary<string, SongMap>();
            var dict2 = new Dictionary<string, SongMap>();

            // Generate 100 test items
            for (int i = 0; i < 100; i++)
            {
                var bsr1 = "A" + i.ToString("D3");
                var bsr2 = "B" + i.ToString("D3");
                dict1[bsr1] = CreateTestSongMap(bsr1);
                dict2[bsr2] = CreateTestSongMap(bsr2);
            }

            // Run concurrent ensure tasks
            var task1 = Task.Run(() => hashCache.EnsurePackHashesAsync(dict1));
            var task2 = Task.Run(() => hashCache.EnsurePackHashesAsync(dict2));

            // Wait for completion, any ArgumentException will be wrapped in AggregateException
            var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(task1, task2));

            Assert.Null(exception); // Should not throw ArgumentException or any other exception
            Assert.Equal(200, hashCache.SongsHash.Count); // Should have successfully cached all 200 items
        }
    }
}
