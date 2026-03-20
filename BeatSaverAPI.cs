using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// 处理可能是布尔值或字符串的 JSON 字段的转换器
    /// </summary>
    public class FlexibleBooleanConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(bool?) || objectType == typeof(bool);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType == JsonToken.Boolean)
                return reader.Value;

            if (reader.TokenType == JsonToken.String)
            {
                var str = reader.Value?.ToString()?.ToLower();
                if (str == "true") return true;
                if (str == "false") return false;
                // "None" 或其他字符串返回 null
                return null;
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
                writer.WriteNull();
            else
                writer.WriteValue((bool)value);
        }
    }
    /// <summary>
    /// BeatSaver API 客户端和数据模型
    /// </summary>
    public class BeatSaverClient
    {
        private static readonly HttpClient client = new HttpClient();
        private const string BaseUrl = "https://api.beatsaver.com";

        public BeatSaverClient()
        {
            client.DefaultRequestHeaders.Add("User-Agent", "BSIMM/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// 搜索谱面
        /// </summary>
        public async Task<BeatSaverSearchResponse> SearchMapsAsync(BeatSaverSearchFilter filter, int page = 0)
        {
            var url = BuildSearchUrl(filter, page);
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            // 尝试解析响应
            var jObj = JObject.Parse(json);

            // 检查是否是直接的数组响应（旧版API格式）
            if (jObj["docs"] == null && jObj.First?.First is JArray)
            {
                // 直接是数组
                var maps = JsonConvert.DeserializeObject<List<BeatSaverMap>>(json);
                return new BeatSaverSearchResponse { Maps = maps, Metadata = null };
            }

            return JsonConvert.DeserializeObject<BeatSaverSearchResponse>(json);
        }

        private string BuildSearchUrl(BeatSaverSearchFilter filter, int page)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"{BaseUrl}/search/text/{page}?");

            if (!string.IsNullOrEmpty(filter.Query))
                sb.Append($"q={Uri.EscapeDataString(filter.Query)}&");

            if (!string.IsNullOrEmpty(filter.Order))
                sb.Append($"order={filter.Order}&");

            if (filter.MinBpm.HasValue)
                sb.Append($"minBpm={filter.MinBpm.Value}&");
            if (filter.MaxBpm.HasValue)
                sb.Append($"maxBpm={filter.MaxBpm.Value}&");

            if (filter.MinNps.HasValue)
                sb.Append($"minNps={filter.MinNps.Value}&");
            if (filter.MaxNps.HasValue)
                sb.Append($"maxNps={filter.MaxNps.Value}&");

            if (filter.MinDuration.HasValue)
                sb.Append($"minDuration={filter.MinDuration.Value}&");
            if (filter.MaxDuration.HasValue)
                sb.Append($"maxDuration={filter.MaxDuration.Value}&");

            if (filter.MinSsStars.HasValue)
                sb.Append($"minSsStars={filter.MinSsStars.Value}&");
            if (filter.MaxSsStars.HasValue)
                sb.Append($"maxSsStars={filter.MaxSsStars.Value}&");

            if (filter.MinBlStars.HasValue)
                sb.Append($"minBlStars={filter.MinBlStars.Value}&");
            if (filter.MaxBlStars.HasValue)
                sb.Append($"maxBlStars={filter.MaxBlStars.Value}&");

            // Mod 支持
            if (filter.Chroma == true) sb.Append("chroma=true&");
            if (filter.Noodle == true) sb.Append("noodle=true&");
            if (filter.Me == true) sb.Append("me=true&");
            if (filter.Cinema == true) sb.Append("cinema=true&");
            if (filter.Vivify == true) sb.Append("vivify=true&");

            // AI 谱面
            if (filter.Automapper.HasValue)
            {
                if (filter.Automapper.Value) sb.Append("automapper=true&");
                else sb.Append("automapper=false&");
            }

            // 排行榜
            if (!string.IsNullOrEmpty(filter.Leaderboard))
                sb.Append($"leaderboard={filter.Leaderboard}&");

            // 精选/认证
            if (filter.Curated == true) sb.Append("curated=true&");
            if (filter.Verified == true) sb.Append("verified=true&");

            return sb.ToString().TrimEnd('&');
        }
    }

    /// <summary>
    /// 搜索筛选条件
    /// </summary>
    public class BeatSaverSearchFilter
    {
        public string Query { get; set; }
        public string Order { get; set; }
        public double? MinBpm { get; set; }
        public double? MaxBpm { get; set; }
        public double? MinNps { get; set; }
        public double? MaxNps { get; set; }
        public int? MinDuration { get; set; }
        public int? MaxDuration { get; set; }
        public double? MinSsStars { get; set; }
        public double? MaxSsStars { get; set; }
        public double? MinBlStars { get; set; }
        public double? MaxBlStars { get; set; }
        public bool? Chroma { get; set; }
        public bool? Noodle { get; set; }
        public bool? Me { get; set; }
        public bool? Cinema { get; set; }
        public bool? Vivify { get; set; }
        public bool? Automapper { get; set; }
        public string Leaderboard { get; set; }
        public bool? Curated { get; set; }
        public bool? Verified { get; set; }
    }

    /// <summary>
    /// 搜索响应
    /// </summary>
    public class BeatSaverSearchResponse
    {
        [JsonProperty("docs")]
        public List<BeatSaverMap> Maps { get; set; }

        [JsonProperty("metadata")]
        public BeatSaverMetadata Metadata { get; set; }

        [JsonProperty("info")]
        public BeatSaverInfo Info { get; set; }
    }

    /// <summary>
    /// 分页元数据（旧版API）
    /// </summary>
    public class BeatSaverMetadata
    {
        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }
    }

    /// <summary>
    /// 分页信息（新版API）
    /// </summary>
    public class BeatSaverInfo
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("pages")]
        public int Pages { get; set; }
    }

    /// <summary>
    /// 谱面信息
    /// </summary>
    public class BeatSaverMap
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("uploader")]
        public BeatSaverUploader Uploader { get; set; }

        [JsonProperty("metadata")]
        public BeatSaverMapMetadata Metadata { get; set; }

        [JsonProperty("stats")]
        public BeatSaverStats Stats { get; set; }

        [JsonProperty("versions")]
        public List<BeatSaverVersion> Versions { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("lastPublishedAt")]
        public DateTime? LastPublishedAt { get; set; }

        [JsonProperty("automapper")]
        public bool Automapper { get; set; }

        [JsonProperty("published")]
        public bool Published { get; set; }

        [JsonProperty("curated")]
        public bool Curated { get; set; }

        [JsonProperty("declaredAi")]
        [JsonConverter(typeof(FlexibleBooleanConverter))]
        public bool? DeclaredAi { get; set; }

        // Local cache-specific fields
        [JsonProperty("ranked")]
        public bool Ranked { get; set; }

        [JsonProperty("qualified")]
        public bool Qualified { get; set; }

        [JsonProperty("blRanked")]
        public bool BlRanked { get; set; }

        [JsonProperty("blQualified")]
        public bool BlQualified { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("uploaded")]
        public DateTime Uploaded { get; set; }

        /// <summary>
        /// 获取最新版本的封面图片URL
        /// </summary>
        public string GetCoverUrl()
        {
            if (Versions != null && Versions.Count > 0)
            {
                return Versions[0].CoverURL;
            }
            return null;
        }

        /// <summary>
        /// 获取最新版本的下载URL
        /// </summary>
        public string GetDownloadUrl()
        {
            if (Versions != null && Versions.Count > 0)
            {
                return Versions[0].DownloadURL;
            }
            return null;
        }

        /// <summary>
        /// 获取最新版本的 hash
        /// </summary>
        public string GetHash()
        {
            if (Versions != null && Versions.Count > 0)
            {
                return Versions[0].Hash;
            }
            return null;
        }
    }

    /// <summary>
    /// 上传者信息
    /// </summary>
    public class BeatSaverUploader
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("avatar")]
        public string Avatar { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("uniqueSet")]
        public bool UniqueSet { get; set; }

        [JsonProperty("admin")]
        public bool Admin { get; set; }

        [JsonProperty("curator")]
        public bool Curator { get; set; }

        [JsonProperty("seniorCurator")]
        public bool SeniorCurator { get; set; }

        [JsonProperty("verified")]
        public bool Verified { get; set; }
    }

    /// <summary>
    /// 谱面元数据
    /// </summary>
    public class BeatSaverMapMetadata
    {
        [JsonProperty("bpm")]
        public double Bpm { get; set; }

        [JsonProperty("duration")]
        public double Duration { get; set; }

        [JsonProperty("songName")]
        public string SongName { get; set; }

        [JsonProperty("songSubName")]
        public string SongSubName { get; set; }

        [JsonProperty("songAuthorName")]
        public string SongAuthorName { get; set; }

        [JsonProperty("levelAuthorName")]
        public string LevelAuthorName { get; set; }

        [JsonProperty("characteristics")]
        public List<BeatSaverCharacteristic> Characteristics { get; set; }
    }

    /// <summary>
    /// 特征信息（包含难度）
    /// </summary>
    public class BeatSaverCharacteristic
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("difficulties")]
        public BeatSaverDifficulties Difficulties { get; set; }
    }

    /// <summary>
    /// 难度信息
    /// </summary>
    public class BeatSaverDifficulties
    {
        [JsonProperty("easy")]
        public BeatSaverDifficulty Easy { get; set; }

        [JsonProperty("normal")]
        public BeatSaverDifficulty Normal { get; set; }

        [JsonProperty("hard")]
        public BeatSaverDifficulty Hard { get; set; }

        [JsonProperty("expert")]
        public BeatSaverDifficulty Expert { get; set; }

        [JsonProperty("expertPlus")]
        public BeatSaverDifficulty ExpertPlus { get; set; }
    }

    /// <summary>
    /// 单个难度详情
    /// </summary>
    public class BeatSaverDifficulty
    {
        [JsonProperty("njs")]
        public double Njs { get; set; }

        [JsonProperty("offset")]
        public double Offset { get; set; }

        [JsonProperty("notes")]
        public int Notes { get; set; }

        [JsonProperty("bombs")]
        public int Bombs { get; set; }

        [JsonProperty("obstacles")]
        public int Obstacles { get; set; }

        [JsonProperty("nps")]
        public double Nps { get; set; }

        [JsonProperty("length")]
        public double Length { get; set; }

        [JsonProperty("characteristic")]
        public string Characteristic { get; set; }

        [JsonProperty("difficulty")]
        public string Difficulty { get; set; }

        [JsonProperty("events")]
        public int Events { get; set; }

        [JsonProperty("chroma")]
        public bool Chroma { get; set; }

        [JsonProperty("me")]
        public bool Me { get; set; }

        [JsonProperty("ne")]
        public bool Ne { get; set; }

        [JsonProperty("cinema")]
        public bool Cinema { get; set; }

        [JsonProperty("vivify")]
        public bool Vivify { get; set; }
    }

    /// <summary>
    /// 统计信息
    /// </summary>
    public class BeatSaverStats
    {
        [JsonProperty("plays")]
        public int Plays { get; set; }

        [JsonProperty("downloads")]
        public int Downloads { get; set; }

        [JsonProperty("upvotes")]
        public int Upvotes { get; set; }

        [JsonProperty("downvotes")]
        public int Downvotes { get; set; }

        [JsonProperty("score")]
        public double Score { get; set; }

        [JsonProperty("scoreOneDP")]
        public double ScoreOneDP { get; set; }
    }

    /// <summary>
    /// 版本信息
    /// </summary>
    public class BeatSaverVersion
    {
        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("hash64")]
        public string Hash64 { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("sageScore")]
        public int? SageScore { get; set; }

        [JsonProperty("testplayAt")]
        public DateTime? TestplayAt { get; set; }

        [JsonProperty("downloadURL")]
        public string DownloadURL { get; set; }

        [JsonProperty("coverURL")]
        public string CoverURL { get; set; }

        [JsonProperty("feedback")]
        public string Feedback { get; set; }

        [JsonProperty("states")]
        public List<string> States { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("diffs")]
        public List<BeatSaverVersionDiff> Diffs { get; set; }
    }

    /// <summary>
    /// 难度信息（在versions[].diffs中）
    /// </summary>
    public class BeatSaverVersionDiff
    {
        [JsonProperty("njs")]
        public double Njs { get; set; }

        [JsonProperty("offset")]
        public double Offset { get; set; }

        [JsonProperty("notes")]
        public int Notes { get; set; }

        [JsonProperty("bombs")]
        public int Bombs { get; set; }

        [JsonProperty("obstacles")]
        public int Obstacles { get; set; }

        [JsonProperty("nps")]
        public double Nps { get; set; }

        [JsonProperty("length")]
        public double Length { get; set; }

        [JsonProperty("characteristic")]
        public string Characteristic { get; set; }

        [JsonProperty("difficulty")]
        public string Difficulty { get; set; }

        [JsonProperty("events")]
        public int Events { get; set; }

        [JsonProperty("chroma")]
        public bool Chroma { get; set; }

        [JsonProperty("me")]
        public bool Me { get; set; }

        [JsonProperty("ne")]
        public bool Ne { get; set; }

        [JsonProperty("cinema")]
        public bool Cinema { get; set; }

        [JsonProperty("vivify")]
        public bool Vivify { get; set; }

        [JsonProperty("stars")]
        public double? Stars { get; set; }

        [JsonProperty("blStars")]
        public double? BlStars { get; set; }
    }
}