using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberIndependentMapsManager
{
    internal class PlayList
    {
        private string playlisttitle;
        private string Image;
        private string playlistauthor;
        private string playlistdescription;
        private List<MapHash> Songs;
        public PlayList() { }
        public PlayList(string title) { this.playlisttitle = title; }

        public PlayList(string title, string author)
        {
            this.playlisttitle = title;
            this.playlistauthor = author;
            Songs = new List<MapHash>();
        }
        public PlayList(string title, string author, string description)
        {
            this.playlisttitle = title;
            this.playlistauthor = author;
            this.playlistdescription = description;
            Songs = new List<MapHash>();
        }
        public PlayList(string title, string author,string description ,string imgbytes)
        {
            this.playlisttitle=title;
            this.playlistauthor = author;
            this.playlistdescription = description;
            this.image = imgbytes;
            Songs = new List<MapHash>();
        }
        public string playlistTitle 
        { 
            get { return playlisttitle; } set { playlisttitle = value; } 
        }
        public string image
        {
            get { return Image; }
            set { Image = value; }
        }
        public string playlistAuthor
        {
            get { return playlistauthor; }
            set { playlistauthor = value; }
        }
        public string PlaylistDescription
        {
            get { return playlistdescription; }
            set { playlistdescription = value; }
        }
        public List<MapHash> songs
        {
            get { return Songs; }
            set { Songs = value; }
        }
        public void AddSongHash(string hash)
        {
            Songs.Add(new MapHash(hash));
        }
    }
}
