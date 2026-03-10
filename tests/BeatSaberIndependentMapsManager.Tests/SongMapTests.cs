using BeatSaberIndependentMapsManager;
using Newtonsoft.Json.Linq;

namespace BeatSaberIndependentMapsManager.Tests;

public class SongMapTests
{
    [Fact]
    public void GetDifficulties_ReturnsEmpty_WhenDifficultyBeatmapSetsIsNull()
    {
        SongMap songMap = new SongMap
        {
            _difficultyBeatmapSets = null
        };

        string[] result = songMap.GetDifficulties();

        Assert.Empty(result);
    }

    [Fact]
    public void GetDifficultiesFiles_IgnoresInvalidEntries_AndReturnsOnlyValidNames()
    {
        SongMap songMap = new SongMap
        {
            _difficultyBeatmapSets = new JArray
            {
                new JObject
                {
                    ["_difficultyBeatmaps"] = new JArray
                    {
                        new JObject { ["_beatmapFilename"] = "Expert.dat" },
                        new JObject { ["_beatmapFilename"] = "" },
                        new JObject()
                    }
                },
                new JObject()
            }
        };

        string[] result = songMap.GetDifficultiesFiles();

        Assert.Single(result);
        Assert.Equal("Expert.dat", result[0]);
    }
}
