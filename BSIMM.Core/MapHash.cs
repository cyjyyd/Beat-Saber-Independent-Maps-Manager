using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberIndependentMapsManager
{
    internal class MapHash
    {
        public string hash
        { get; set; }
        public string songName
        { get; set; }
        public string levelAuthorName 
        { get; set; }
        public string levelid
        { get; set; }
        public List<HighLightdiff> difficulties
        { get; set; }
        public MapHash (string Hash)
        {
            this.hash = Hash;
        }
        public MapHash (string Hash,string SongName,string LevelAuthorName,string LevelID,List<HighLightdiff> Difficulties)
        {
            this.hash = Hash;
            this.songName = SongName;
            this.levelAuthorName = LevelAuthorName;
            this.levelid = LevelID;
            this.difficulties = Difficulties;
        }
    }
}
