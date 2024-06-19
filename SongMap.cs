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
        private string version;
        private string songName;
        private string songSubName;
        private string songAuthorName;
        private string levelAuthorName;
        private double beatsPerMinute;
        private double previewStartTime;
        private double previewDuration;
        private string coverImagePath;
        private string environmentName;
        private JArray difficultyBeatmapSets;
        private JObject customData;
        private double shuffle;
        private double shufflePeriod;
        private string songFilename;
        private string coverImageFilename;
        private double songTimeOffset;
        private string songfolder;

        public SongMap() { }
        public string _version
        {
            get { return version; }
            set { version = value; }
        }
        public string _songName
        {
            get { return songName; }
            set { songName = value; }
        }
        public string _songSubName
        {
            get { return songSubName; }
            set { songSubName = value; }
        }
        public string _songAuthorName
        {
            get { return songAuthorName; }
            set { songAuthorName = value; }
        }
        public string _levelAuthorName
        {
            get { return levelAuthorName; }
            set { levelAuthorName = value; }
        }
        public double _beatsPerMinute
        {
            get { return beatsPerMinute; }
            set { beatsPerMinute = value; }
        }
        public double _previewStartTime
        {
            get { return previewStartTime; }
            set { previewStartTime = value; }
        }
        public double _previewDuration
        {
            get { return previewDuration; }
            set { previewDuration = value; }
        }
        public string _coverImagePath
        {
            get { return coverImagePath; }
            set { coverImagePath = value; }
        }
        public string _environmentName
        {
            get { return environmentName; }
            set { environmentName = value; }
        }
        public JArray _difficultyBeatmapSets
        {
            get { return difficultyBeatmapSets; }
            set { difficultyBeatmapSets = value; }
        }
        public JObject _customData
        {
            get { return customData; }
            set { customData = value; }
        }
        public double _shuffle
        {
            get { return shuffle; }
            set { shuffle = value; }
        }
        public double _shufflePeriod
        {
            get { return shufflePeriod; }
            set { shufflePeriod = value; }
        }
        public string _songFilename
        {
            get { return songFilename; }
            set { songFilename = value; }
        }
        public string _coverImageFilename
        {
            get { return coverImageFilename; }
            set { coverImageFilename = value; }
        }
        public double _songTimeOffset
        {
            get { return songTimeOffset; }
            set { songTimeOffset = value; }
        }
        public string songFolder
        {
            get { return songfolder; }
            set { songfolder = value; }
        }

        public string[] GetDifficulties()
        {
            List<string> difficulties = new List<string>();
            foreach (JObject difficultyBeatmapSet in difficultyBeatmapSets)
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
            foreach (JObject difficultyBeatmapSet in difficultyBeatmapSets)
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
