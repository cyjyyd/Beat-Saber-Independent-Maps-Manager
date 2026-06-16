using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace BeatSaberIndependentMapsManager.Services
{
    internal class GameInstanceDetector
    {
        #region P/Invoke
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern uint Everything_SetSearch(string lpSearchString);
        [DllImport("Everything64.dll")]
        public static extern bool Everything_Query(bool bWait);
        [DllImport("Everything64.dll")]
        public static extern bool Everything_SetMatchWholeWord(bool bEnable);
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern uint Everything_GetNumResults();
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern void Everything_GetResultFullPathName(uint nIndex, StringBuilder lpString, uint nMaxCount);
        [DllImport("Everything64.dll")]
        private static extern void Everything_SetRequestFlags(uint dwRequestFlags);
        #endregion

        private const int EVERYTHING_REQUEST_FILE_NAME = 0x00000001;
        private const int EVERYTHING_REQUEST_PATH = 0x00000002;

        private byte[] _bsVersions;
        private List<BSVerInfo> _bsVerionSet;
        private readonly HashSet<string> _gameInstance = new();

        public Dictionary<string, string> BSInstancePath { get; } = new();
        public Dictionary<string, bool[]> InstanceSongCoreReady { get; } = new();
        public bool MultiInstanceDetect { get; private set; }
        public List<BSVerInfo> BSVersionSet => _bsVerionSet;

        public bool IsEverythingAvailable
        {
            get
            {
                if (!File.Exists("Everything64.dll"))
                    return false;
                Process[] everythingService = Process.GetProcessesByName("Everything");
                return everythingService.Length > 1 ||
                       (everythingService.Length == 1 && everythingService[0].PagedMemorySize64 > 2048 * 1024);
            }
        }

        public void LoadVersionFile(string versionFilePath)
        {
            if (File.Exists(versionFilePath))
            {
                _bsVersions = File.ReadAllBytes(versionFilePath);
                try
                {
                    _bsVerionSet = JsonConvert.DeserializeObject<List<BSVerInfo>>(Encoding.UTF8.GetString(_bsVersions));
                }
                catch
                {
                    _bsVerionSet = null;
                }
            }
        }

        public bool Detect()
        {
            bool useEverything = false;
            if (File.Exists("Everything64.dll"))
            {
                MultiInstanceDetect = IsEverythingAvailable;
                useEverything = MultiInstanceDetect;
            }

            if (useEverything)
            {
                detectMultiBeatSaberInstance();
            }
            else
            {
                detectSingleBeatSaberInstance();
            }

            return BSInstancePath.Count > 0;
        }

        public string GetVersionForPath(string path)
        {
            string globalManager = path + "\\Beat Saber_Data\\globalgamemanagers";
            if (!File.Exists(globalManager))
                return "未知版本";

            string globalManagerContent = Encoding.UTF8.GetString(File.ReadAllBytes(globalManager));
            string formatContent = null;
            if (globalManagerContent.Length > 5000)
                globalManagerContent = globalManagerContent.Substring(0, 5000);

            string pattern = @"[\w\.]+";
            MatchCollection matches = Regex.Matches(globalManagerContent, pattern);
            foreach (Match match in matches)
            {
                formatContent += match.Value;
            }

            if (_bsVerionSet != null)
            {
                try
                {
                    foreach (BSVerInfo bsver in _bsVerionSet)
                    {
                        if (formatContent.Contains(bsver.BSVersion))
                            return bsver.BSVersion;
                    }
                }
                catch { }
            }

            return "未知版本";
        }

        private string Rename(string str)
        {
            string pattern = @"\[([0-9]+)\]$";
            Match match = Regex.Match(str, pattern);
            if (match.Success)
            {
                int duplicateCount = Convert.ToInt32(match.Groups[1].Value);
                duplicateCount++;
                str = Regex.Replace(str, pattern, "[" + duplicateCount + "]");
                return str;
            }
            return str + "[1]";
        }

        private bool pirateGameDetect(string gamepath)
        {
            string steamapipath = gamepath + "\\Beat Saber_Data\\Plugins\\x86_64\\steam_api64.dll";
            if (File.Exists(steamapipath))
            {
                if (File.ReadAllBytes(steamapipath)[0] == 0x4D && File.ReadAllBytes(steamapipath)[1] == 0x5A)
                {
                    string steamapihash = GetFileMD5(steamapipath);
                    if (steamapihash == "2a09ae29b5613645a4b30e9deea68042" || steamapihash == "f3db5801dc9b75da671b39041e2e8bcf")
                        return true;
                    return false;
                }
                return false;
            }
            return false;
        }

        private static string GetFileMD5(string filePath)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private bool oculusGameDetect(string path)
        {
            string gamepath = path + "\\Software\\hyperbolic-magnetism-beat-saber";
            return Directory.Exists(gamepath) && File.Exists(gamepath + "\\Beat Saber.exe");
        }

        private void detectSingleBeatSaberInstance()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Valve\\Steam");
            RegistryKey OCKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Oculus VR, LLC\\Oculus\\Libraries");
            if (OCKey != null)
            {
                string[] folders = OCKey.GetSubKeyNames();
                foreach (string item in folders)
                {
                    RegistryKey folderkey = OCKey.OpenSubKey(item);
                    string OCGamePath = folderkey.GetValue("OriginalPath").ToString();
                    oculusGameDetect(OCGamePath);
                }
            }
            if (key == null)
            {
                key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Valve\\Steam");
                if (key == null)
                    return;
            }
            string currentSteamLibraryFolder = key.GetValue("InstallPath")?.ToString();
            if (currentSteamLibraryFolder == null)
                return;

            string vdfFile = currentSteamLibraryFolder + "\\steamapps\\libraryfolders.vdf";
            if (!File.Exists(vdfFile))
                return;

            string[] vdfLines = File.ReadAllLines(vdfFile);
            foreach (string line in vdfLines)
            {
                if (line.Contains("path"))
                {
                    string[] lineSplit = line.Split(new char[] { '"' }, StringSplitOptions.RemoveEmptyEntries);
                    string steamLibraryFolder = lineSplit[3].Replace("\\\\", "\\");
                    string beatSaberFolder = steamLibraryFolder + "\\steamapps\\common\\Beat Saber";
                    if (Directory.Exists(beatSaberFolder))
                    {
                        string gamePath = beatSaberFolder + "\\Beat Saber.exe";
                        if (File.Exists(gamePath))
                        {
                            if (pirateGameDetect(beatSaberFolder))
                            {
                                string ver = GetVersionForPath(beatSaberFolder);
                                if (!BSInstancePath.ContainsKey(ver))
                                {
                                    BSInstancePath.Add(ver, beatSaberFolder);
                                    InstanceSongCoreReady.Add(ver, modCheck(beatSaberFolder));
                                }
                                else
                                {
                                    BSInstancePath.Add(Rename(ver), beatSaberFolder);
                                    InstanceSongCoreReady.Add(Rename(ver), modCheck(beatSaberFolder));
                                }
                            }
                            else
                            {
                                MessageBox.Show("检测到盗版Beat Saber游戏，本软件只支持正版游戏的曲包管理，程序将退出！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                Environment.Exit(0);
                            }
                        }
                    }
                }
            }
        }

        private void detectMultiBeatSaberInstance()
        {
            Everything_SetSearch("Beat Saber.exe");
            Everything_SetRequestFlags(EVERYTHING_REQUEST_PATH | EVERYTHING_REQUEST_FILE_NAME);
            Everything_SetMatchWholeWord(true);
            Everything_Query(true);
            var buf = new StringBuilder(300);
            for (uint i = 0; i < Everything_GetNumResults(); i++)
            {
                buf.Clear();
                Everything_GetResultFullPathName(i, buf, 300);
                var path = Path.GetDirectoryName(buf.ToString())!;
                if (_gameInstance.Contains(path) ||
                    path.Contains("Prefetch") ||
                    path.Contains("$RECYCLE.BIN") ||
                    path.Contains("OneDrive") ||
                    File.GetAttributes(buf.ToString()).HasFlag(FileAttributes.Directory))
                    continue;
                _gameInstance.Add(path);
                try
                {
                    var gamepath = path + "\\Beat Saber.exe";
                    if (File.Exists(gamepath))
                    {
                        if (pirateGameDetect(path))
                        {
                            string ver = GetVersionForPath(path);
                            if (!BSInstancePath.ContainsKey(ver))
                            {
                                BSInstancePath.Add(ver, path);
                                InstanceSongCoreReady.Add(ver, modCheck(path));
                            }
                            else
                            {
                                string escapedPrefix = Regex.Escape(ver);
                                string pattern = @"^" + escapedPrefix + @"(\[[0-9]+\])?$";
                                List<string> duplicateTemp = new List<string>();
                                foreach (string storeVer in BSInstancePath.Keys)
                                {
                                    if (Regex.Match(storeVer, pattern).Success)
                                        duplicateTemp.Add(storeVer);
                                }
                                string newVer = Rename(duplicateTemp[^1]);
                                BSInstancePath.Add(newVer, path);
                                InstanceSongCoreReady.Add(newVer, modCheck(path));
                            }
                        }
                        else
                        {
                            MessageBox.Show("检测到盗版Beat Saber游戏，本软件只支持正版游戏的曲包管理，程序将退出！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Environment.Exit(0);
                        }
                    }
                }
                catch { }
            }
        }

        private bool[] modCheck(string path)
        {
            string modpath = path + "\\Plugins";
            string pendingpath = path + "\\IPA\\Pending";
            string[] mods = { "\\SiraUtil.dll", "\\BSML.dll", "\\SongCore.dll" };
            bool[] modStatus = new bool[5];
            for (int i = 0; i < mods.Length; i++)
            {
                modStatus[i] = File.Exists(modpath + mods[i]);
            }
            if (modStatus[0] && modStatus[1] && modStatus[2])
            {
                modStatus[3] = true;
            }
            else
            {
                for (int i = 0; i < mods.Length; i++)
                {
                    modStatus[i] = File.Exists(pendingpath + mods[i]);
                }
                if (modStatus[0] && modStatus[1] && modStatus[2])
                    modStatus[3] = false;
                else
                    modStatus[4] = true;
            }
            return modStatus;
        }
    }
}
