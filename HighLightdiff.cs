using Newtonsoft.Json;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// bplist 中的难度条目，对应 PlaylistManager 的 Difficulty 结构
    /// JSON 格式: {"characteristic": "Standard", "name": "Expert"}
    /// </summary>
    internal class HighLightdiff
    {
        public HighLightdiff() { }

        public HighLightdiff(string characteristic, string name)
        {
            this.characteristic = characteristic;
            this.name = name;
        }

        /// <summary>
        /// 难度模式（如 Standard, OneSaber 等）
        /// </summary>
        [JsonProperty("characteristic")]
        public string characteristic { get; set; }

        /// <summary>
        /// 难度名称（如 Easy, Normal, Hard, Expert, ExpertPlus 等）
        /// </summary>
        [JsonProperty("name")]
        public string name { get; set; }
    }
}
