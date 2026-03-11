using BeatSaberIndependentMapsManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace BeatSaberIndependentMapsManager.Tests;

public class SongMapTests
{
    public void GetDifficulties_ReturnsEmpty_WhenDifficultyBeatmapSetsIsNull()
    {
        SongMap songMap = new SongMap
        {
            _difficultyBeatmapSets = null
        };

        string[] result = songMap.GetDifficulties();

        Assert.IsEmpty(result);
    }
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

        Assert.ContainsSingle(result);
        Assert.AreEqual("Expert.dat", result[0]);
    }
}
