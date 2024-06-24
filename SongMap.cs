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
            foreach (JObject difficultyBeatmapSet in _difficultyBeatmapSets)
            {
                foreach (JObject difficultyBeatmap in (JArray)difficultyBeatmapSet["_difficultyBeatmaps"])
                {
                    difficulties.Add(difficultyBeatmap["_difficulty"].ToString());
                }
            }
            return difficulties.ToArray();
        }
        public string[] GetDifficultiesFiles()
        {
            List<string> difficulties = new List<string>();
            foreach (JObject difficultyBeatmapSet in _difficultyBeatmapSets)
            {
                foreach (JObject difficultyBeatmap in (JArray)difficultyBeatmapSet["_difficultyBeatmaps"])
                {
                    difficulties.Add(difficultyBeatmap["_beatmapFilename"].ToString());
                }
            }
            return difficulties.ToArray();
        }
    }
}
