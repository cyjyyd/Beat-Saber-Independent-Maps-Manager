using System;
using System.Collections.Generic;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// 轻量级谱面数据结构，用于内存高效的筛选操作
    /// 仅包含筛选所需的字段，内存占用约为完整 BeatSaverMap 的 1/3
    /// </summary>
    public class BeatSaverMapSlim
    {
        // 基础标识信息
        public string Id;
        public string Name;
        public string Description;

        // 上传信息
        public DateTime Uploaded;
        public int UploaderId;
        public string UploaderName;
        public bool UploaderVerified;

        // 元数据
        public double Bpm;
        public double Duration;
        public string SongName;
        public string SongAuthorName;
        public string LevelAuthorName;

        // 统计信息
        public int Plays;
        public int Downloads;
        public int Upvotes;
        public int Downvotes;
        public double Score;

        // 布尔标记
        public bool Automapper;
        public bool Curated;
        public bool Ranked;
        public bool Qualified;
        public bool BlRanked;
        public bool BlQualified;

        // 标签（可为 null 以节省内存）
        public List<string> Tags;

        // 难度信息（轻量级）
        public List<BeatSaverVersionDiffSlim> Diffs;

        // 版本信息
        public int? SageScore;
        public string Hash;
        public string CoverURL;
        public string DownloadURL;

        /// <summary>
        /// 转换为完整 BeatSaverMap（用于结果输出）
        /// </summary>
        public BeatSaverMap ToFullMap()
        {
            var map = new BeatSaverMap
            {
                Id = Id,
                Name = Name,
                Uploaded = Uploaded,
                Automapper = Automapper,
                Curated = Curated,
                Ranked = Ranked,
                Qualified = Qualified,
                BlRanked = BlRanked,
                BlQualified = BlQualified,
                Tags = Tags,
                Description = Description
            };

            map.Uploader = new BeatSaverUploader
            {
                Id = UploaderId,
                Name = UploaderName,
                Verified = UploaderVerified
            };

            map.Metadata = new BeatSaverMapMetadata
            {
                Bpm = Bpm,
                Duration = Duration,
                SongName = SongName,
                SongAuthorName = SongAuthorName,
                LevelAuthorName = LevelAuthorName
            };

            map.Stats = new BeatSaverStats
            {
                Plays = Plays,
                Downloads = Downloads,
                Upvotes = Upvotes,
                Downvotes = Downvotes,
                Score = Score
            };

            map.Versions = new List<BeatSaverVersion>
            {
                new BeatSaverVersion
                {
                    Hash = Hash,
                    SageScore = SageScore,
                    CoverURL = CoverURL,
                    DownloadURL = DownloadURL,
                    Diffs = Diffs != null ? new List<BeatSaverVersionDiff>(Diffs.Count) : new List<BeatSaverVersionDiff>()
                }
            };

            if (Diffs != null)
            {
                foreach (var diffSlim in Diffs)
                {
                    map.Versions[0].Diffs.Add(diffSlim.ToFullDiff());
                }
            }

            return map;
        }
    }

    /// <summary>
    /// 轻量级难度信息
    /// </summary>
    public class BeatSaverVersionDiffSlim
    {
        public double Njs;
        public double Offset;
        public int Notes;
        public int Bombs;
        public int Obstacles;
        public double Nps;
        public double Length;
        public double Seconds;
        public string Characteristic;
        public string Difficulty;
        public int Events;

        // Mod 标记
        public bool Chroma;
        public bool Me;
        public bool Ne;
        public bool Cinema;
        public bool Vivify;

        // 星级
        public double? Stars;
        public double? BlStars;

        // Parity 信息
        public int ParityErrors;
        public int ParityWarns;
        public int ParityResets;

        // 最大分数
        public int? MaxScore;

        /// <summary>
        /// 转换为完整 BeatSaverVersionDiff
        /// </summary>
        public BeatSaverVersionDiff ToFullDiff()
        {
            var diff = new BeatSaverVersionDiff
            {
                Njs = Njs,
                Offset = Offset,
                Notes = Notes,
                Bombs = Bombs,
                Obstacles = Obstacles,
                Nps = Nps,
                Length = Length,
                Seconds = Seconds,
                Characteristic = Characteristic,
                Difficulty = Difficulty,
                Events = Events,
                Chroma = Chroma,
                Me = Me,
                Ne = Ne,
                Cinema = Cinema,
                Vivify = Vivify,
                Stars = Stars,
                BlStars = BlStars,
                MaxScore = MaxScore
            };

            if (ParityErrors > 0 || ParityWarns > 0 || ParityResets > 0)
            {
                diff.ParitySummary = new BeatSaverParitySummary
                {
                    Errors = ParityErrors,
                    Warns = ParityWarns,
                    Resets = ParityResets
                };
            }

            return diff;
        }
    }
}