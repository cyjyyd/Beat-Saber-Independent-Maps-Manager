using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace BeatSaberIndependentMapsManager
{
    internal class SongMap
    {

        public SongMap() { }
        public string _version
        { 
            get; set; 
        }
        public string _songName
        {
            get; set;
        }
        public string _songSubName
        {
            get; set;
        }
        public string _songAuthorName
        {
            get; set;
        }
        public string _levelAuthorName
        {
            get; set;
        }
        public double _beatsPerMinute
        {
            get; set;
        }
        public double _previewStartTime
        {
            get; set;
        }
        public double _previewDuration
        {
            get; set;
        }
        public string _coverImagePath
        {
            get; set;
        }
        public string _environmentName
        {
            get; set;
        }
        public JArray _difficultyBeatmapSets
        {
            get; set;
        }
        public JObject _customData
        {
            get; set;
        }
        public double _shuffle
        {
            get; set;
        }
        public double _shufflePeriod
        {
            get; set;
        }
        public string _songFilename
        {
            get; set;
        }
        public string _coverImageFilename
        {
            get; set;
        }
        public double _songTimeOffset
        {
            get; set;
        }
        public string songFolder
        {
            get; set;
        }

        public string[] GetDifficulties()
        {
            List<string> difficulties = new List<string>();
            if (_difficultyBeatmapSets == null)
            {
                return difficulties.ToArray();
            }

            foreach (JObject difficultyBeatmapSet in _difficultyBeatmapSets)
            {
                JArray difficultyBeatmaps = difficultyBeatmapSet["_difficultyBeatmaps"] as JArray;
                if (difficultyBeatmaps == null)
                {
                    continue;
                }

                foreach (JObject difficultyBeatmap in difficultyBeatmaps)
                {
                    string difficulty = difficultyBeatmap["_difficulty"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(difficulty))
                    {
                        difficulties.Add(difficulty);
                    }
                }
            }
            return difficulties.ToArray();
        }
        public string[] GetDifficultiesFiles()
        {
            List<string> difficulties = new List<string>();
            if (_difficultyBeatmapSets == null)
            {
                return difficulties.ToArray();
            }

            foreach (JObject difficultyBeatmapSet in _difficultyBeatmapSets)
            {
                JArray difficultyBeatmaps = difficultyBeatmapSet["_difficultyBeatmaps"] as JArray;
                if (difficultyBeatmaps == null)
                {
                    continue;
                }

                foreach (JObject difficultyBeatmap in difficultyBeatmaps)
                {
                    string difficultyFile = difficultyBeatmap["_beatmapFilename"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(difficultyFile))
                    {
                        difficulties.Add(difficultyFile);
                    }
                }
            }
            return difficulties.ToArray();
        }
    }
}
