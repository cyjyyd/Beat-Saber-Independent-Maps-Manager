using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeatSaberIndependentMapsManager
{
    internal class Config
    {
        [DllImport("kernel32")]//返回0表示失败，非0为成功
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]//返回取得字符串缓冲区的长度
        private static extern long GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        private string iniFilePath;
        #region 读Ini文件
        public string ReadIniData(string Section, string Key, string NoText, string iniFilePath)
        {
            if (File.Exists(iniFilePath))
            {
                StringBuilder temp = new StringBuilder(1024);
                GetPrivateProfileString(Section, Key, NoText, temp, 1024, iniFilePath);
                return temp.ToString();
            }
            else
            {
                return String.Empty;
            }
        }

        #endregion
        #region 写Ini文件
        public bool WriteIniData(string Section, string Key, string Value, string iniFilePath)
        {
            if (File.Exists(iniFilePath))
            {
                long OpStation = WritePrivateProfileString(Section, Key, Value, iniFilePath);
                if (OpStation == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        #endregion

        public Config() 
        {
            iniFilePath = Application.StartupPath+"\\BSIMM.ini";
            if (File.Exists(iniFilePath))
            {
                if(ReadIniData("Settings","HashCache","",iniFilePath)=="")
                {
                    File.Delete(iniFilePath);
                    FileStream fs = new FileStream("BSIMM.ini", FileMode.Create, FileAccess.ReadWrite);
                    fs.Close();
                    HashCache = LastFolder = DownProxy = LocalCaChe = false;
                    writeset();
                    readset();
                }
                else
                {
                    readset();
                }
            }
            else
            {
                FileStream fs = new FileStream("BSIMM.ini", FileMode.Create, FileAccess.ReadWrite);
                fs.Close();
                HashCache = LastFolder = DownProxy = LocalCaChe = false;
                writeset();
                readset();
            }
        }
        public bool HashCache
            { get; set; }
        public bool LastFolder
            { get; set; }
        public bool DownProxy
            { get; set; }
        public bool LocalCaChe 
            { get; set; }
        public void readset()
        {
            HashCache = Convert.ToBoolean(ReadIniData("Settings", "HashCache", "false", iniFilePath));
            LastFolder = Convert.ToBoolean(ReadIniData("Settings", "LastFolder", "false", iniFilePath));
            DownProxy = Convert.ToBoolean(ReadIniData("Settings", "DownProxy", "false", iniFilePath));
            LocalCaChe = Convert.ToBoolean(ReadIniData("Settings", "LocalCache", "false", iniFilePath));
        }
        private void writeset()
        {
            WriteIniData("Settings", "HashCache", Convert.ToString(HashCache), iniFilePath);
            WriteIniData("Settings", "LastFolder", Convert.ToString(LastFolder), iniFilePath);
            WriteIniData("Settings", "DownProxy", Convert.ToString(DownProxy), iniFilePath);
            WriteIniData("Settings", "LocalCache", Convert.ToString(LocalCaChe), iniFilePath);
        }
        public void configUpdate()
        {
            writeset();
        }
    }
}
