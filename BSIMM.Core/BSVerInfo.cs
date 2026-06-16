using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberIndependentMapsManager
{
    internal class BSVerInfo
    {
        private string bsVersion;
        private string bsManifest;
        private string oculusBinaryid;
        private string releaseURL;
        private string releaseDate;
        private string releaseIMG;
        private string Year;
        public string BSVersion
        {
            get { return bsVersion; }
            set { bsVersion = value; }
        }
        public string BSManifest
        {
            get { return bsManifest; }
            set { bsManifest = value; }
        }
        public string OculusBinaryId
        {
            get { return oculusBinaryid; }
            set { oculusBinaryid = value; }
        }
        public string ReleaseURL
        {
            get { return releaseURL; }
            set { releaseURL = value; }
        }
        public string ReleaseDate
        {
            get { return releaseDate; }
            set { releaseDate = value; }
        }
        public string year
        {
            get { return Year; }
            set { Year = value; }
        }
        public string ReleaseImg
        {
            get { return releaseIMG; }
            set { releaseIMG = value; }
        }
    }
}
