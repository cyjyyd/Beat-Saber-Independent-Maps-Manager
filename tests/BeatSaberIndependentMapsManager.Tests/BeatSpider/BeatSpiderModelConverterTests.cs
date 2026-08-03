using Xunit;
using Newtonsoft.Json;
using BeatSaberIndependentMapsManager.BeatSpiderSharp;

namespace BeatSaberIndependentMapsManager.Tests.BeatSpider
{
    public class BeatSpiderModelConverterTests
    {
        private const string MapJson = @"{
            ""id"": ""abc"",
            ""name"": ""Test Song"",
            ""description"": ""A test map"",
            ""metadata"": { ""bpm"": 150.0, ""duration"": 120.0, ""songName"": ""Test Song"" },
            ""stats"": {},
            ""versions"": [{
                ""hash"": ""1234567890abcdef1234567890abcdef12345678"",
                ""diffs"": []
            }]
        }";

        [Fact]
        public void ToBeatSpiderSong_RoundTrips_BackToBeatSaverMap()
        {
            var map = JsonConvert.DeserializeObject<BeatSaverMap>(MapJson);
            Assert.NotNull(map);

            var song = map.ToBeatSpiderSong();
            var back = song.ToBeatSaverMap();

            Assert.NotNull(back);
            Assert.Equal("abc", back.Id);
            Assert.Equal("Test Song", back.Name);
            Assert.Equal(150.0, back.Metadata.Bpm);
            Assert.Equal(120.0, back.Metadata.Duration);
            Assert.Equal("1234567890abcdef1234567890abcdef12345678", back.Versions[0].Hash);
        }
    }
}
