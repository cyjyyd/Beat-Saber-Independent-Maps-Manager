using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using BeatSaberIndependentMapsManager.Properties;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.Win32;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using Microsoft.VisualBasic.FileIO;
using NAudio.Vorbis;
using NAudio.Wave;

namespace BeatSaberIndependentMapsManager
{
    public partial class MainForm : Form
    {
        #region 全局变量
        private const int EVERYTHING_REQUEST_FILE_NAME = 0x00000001;
        private const int EVERYTHING_REQUEST_PATH = 0x00000002;
        private bool multiInstanceDetect = true;
        private bool songListinited = true;
        private bool duplicateAdvance = false;
        private string currentMusicPack = "";
        private byte[] bsVersions = null;
        private readonly HashSet<string> gameInstance = new();
        private SongMap playSong;
        Dictionary<string, string> SongsHash = new Dictionary<string, string>();
        List<BSVerInfo> bsVerionSet = null;
        Config config = new Config();
        private Dictionary<string, SongMap> delicatedSongList = new Dictionary<string, SongMap>();
        Dictionary<string, Dictionary<string, SongMap>> musicPackInfo = new Dictionary<string, Dictionary<string, SongMap>>();
        Dictionary<string, string> musicPackPath = new Dictionary<string, string>();
        Dictionary<string, string> BSInstancePath = new Dictionary<string, string>();
        Dictionary<string, bool[]> InstanceSongCoreReady = new Dictionary<string, bool[]>();
        Dictionary<string, Image> musicPackCoverimgs = new Dictionary<string, Image>();
        WaveOut waveOut = new WaveOut();
        WaveStream currentAudioReader;
        // BeatSaver 搜索相关
        private BeatSaverClient beatSaverClient = new BeatSaverClient();
        private List<BeatSaverMap> currentSearchResults = new List<BeatSaverMap>();
        private int currentPage = 0;
        private int totalPages = 0;
        private const int PageSize = 20; // 每页显示数量
        private bool isOrModeSearch = false; // 是否是 OR 模式搜索
        private List<BeatSaverMap> allOrSearchResults = new List<BeatSaverMap>(); // OR 模式下缓存的所有结果
        // 筛选构建器相关
        private FilterPreset currentFilterPreset;
        private FilterBuilderForm filterBuilderForm;
        #endregion
        #region 动态库引用
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern UInt32 Everything_SetSearch(string lpSearchString);
        [DllImport("Everything64.dll")]
        public static extern bool Everything_Query(bool bWait);
        [DllImport("Everything64.dll")]
        public static extern bool Everything_SetMatchWholeWord(bool bEnable);
        [DllImport("Everything64.dll")]
        private static extern UInt32 Everything_GetNumResults();
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern void Everything_GetResultFullPathName(UInt32 nIndex, StringBuilder lpString, UInt32 nMaxCount);
        [DllImport("Everything64.dll")]
        private static extern void Everything_SetRequestFlags(UInt32 dwRequestFlags);
        const string version = "1.0.0";
        const string author = "@万毒不侵";
        CultureInfo cultureInfo = CultureInfo.CurrentCulture;
        #endregion
        #region 主窗口加载
        public MainForm()
        {
            InitializeComponent();
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            comboBoxPlatform.SelectedIndex = 0;
            string language = cultureInfo.TwoLetterISOLanguageName;
            this.Text = "BSIMM-独立曲包管理/编辑器 " + version + " " + author;
            debugLog("程序日志将自动同步到程序目录：" + Application.StartupPath + "BSIMM-" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
            if (!Directory.Exists(Application.StartupPath + "assets"))
            {
                Directory.CreateDirectory(Application.StartupPath + "assets");
                Directory.CreateDirectory(Application.StartupPath + "assets\\json");
                Directory.CreateDirectory(Application.StartupPath + "assets\\temp");
                Directory.CreateDirectory(Application.StartupPath + "assets\\scripts");
            }
            //双语的适配以后再做
            Thread update = new Thread(updateDetect);
            update.Start();
            if (!File.Exists("Everything64.dll"))
            {
                debugLog("未检测到Everything64.dll文件，所有增强功能将不可用！");
                multiInstanceDetect = false;
            }
            Process[] everythingService = Process.GetProcessesByName("Everything");
            if (everythingService.Length > 1 || (everythingService.Length == 1 && everythingService[0].PagedMemorySize64 > 2048 * 1024))
            {
                debugLog("检测到Everything增强扩展，将使用增强模式检测Beat Saber实例目录");
                debugLog("如果未检测到实例，请尝试重启Everything软件");
                multiInstanceDetect = true;
            }
            else
            {
                debugLog("未检测到Everything增强扩展，将使用普通模式检测Beat Saber实例目录");
                multiInstanceDetect = false;
            }
            if (File.Exists(Application.StartupPath + "\\assets\\json\\bs-versions.json"))
            {
                bsVersions = File.ReadAllBytes(Application.StartupPath + "\\assets\\json\\bs-versions.json");
            }
            else
            {
                debugLog("未找到Beat Saber版本控制文件，请检查程序文件目录是否完整！多版本检测将禁用！");
                multiInstanceDetect = false;
            }
            if (multiInstanceDetect)
            {
                Thread detect = new Thread(detectMultiBeatSaberInstance);
                detect.Start();
            }
            else
            {
                Thread detect = new Thread(detectSingleBeatSaberInstance);
                detect.Start();
            }
            if (config.HashCache)
            {
                readHash();
            }
            // 初始化新的筛选构建器面板
            InitializeFilterBuilderPanel();
        }
        #endregion
        #region 检查更新
        private void updateDetect()
        {
            debugLog("检查更新中...");
            //string updateUrl = "";
            debugLog("检查更新完成");
        }
        #endregion
        #region 游戏实例检测与归集
        private bool oculusGameDetect(string path)
        {
            string gamepath = path + "\\Software\\hyperbolic-magnetism-beat-saber";
            if (Directory.Exists(gamepath) && File.Exists(gamepath + "\\Beat Saber.exe"))
            {
                debugLog("检测到Beat Saber实例目录：" + gamepath);
                return true;
            }
            else
            {
                return false;
            }
        }
        private void detectSingleBeatSaberInstance()
        {
            string currentSteamLibraryFolder = null;
            bool isFound = false;
            RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Valve\\Steam");
            RegistryKey OCKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Oculus VR, LLC\\Oculus\\Libraries");
            if (OCKey != null)
            {
                string[] folders = OCKey.GetSubKeyNames();
                foreach (string item in folders)
                {
                    RegistryKey folderkey = OCKey.OpenSubKey(item);
                    string OCGamePath = folderkey.GetValue("OriginalPath").ToString();
                    isFound = oculusGameDetect(OCGamePath);
                }
            }
            if (key == null)
            {
                key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Valve\\Steam");
                if (key == null)
                {
                    debugLog("未检测到Steam安装目录，无法检测Beat Saber实例目录");
                    return;
                }
                else
                {
                    currentSteamLibraryFolder = key.GetValue("InstallPath").ToString();
                }
            }
            else
            {
                currentSteamLibraryFolder = key.GetValue("InstallPath").ToString();
            }
            if (currentSteamLibraryFolder == null)
            {
                debugLog("未检测到Steam安装目录，无法检测Beat Saber实例目录");
                return;
            }
            else
            {
                string vdfFile = currentSteamLibraryFolder + "\\steamapps\\libraryfolders.vdf";
                if (!File.Exists(vdfFile))
                {
                    debugLog("未检测到Steam库文件，无法检测Beat Saber实例目录");
                    return;
                }
                else
                {
                    string[] vdfLines = File.ReadAllLines(vdfFile);
                    isFound = false;
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
                                string modPath = beatSaberFolder + "\\Plugins\\SongCore.dll";
                                if (File.Exists(gamePath))
                                {
                                    if (pirateGameDetect(beatSaberFolder))
                                    {
                                        isFound = true;
                                        debugLog("检测到Beat Saber实例目录：" + beatSaberFolder);
                                        string ver = BeatSaberVersionDetect(beatSaberFolder);
                                        debugLog("Beat Saber版本：" + ver);
                                        if (!BSInstancePath.ContainsKey(ver))
                                        {
                                            BSInstancePath.Add(ver, beatSaberFolder);
                                            InstanceSongCoreReady.Add(ver, modCheck(beatSaberFolder));
                                        }
                                        else
                                        {
                                            debugLog("检测到相同版本，自动重命名");
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
                    if (!isFound)
                    {
                        debugLog("常规模式下未解析到Beat Saber安装目录？请确认您是否安装(推荐安装Everything插件，使用增强模式)");
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
                if (gameInstance.Contains(path) || path.Contains("Prefetch") || path.Contains("$RECYCLE.BIN") || path.Contains("OneDrive") || File.GetAttributes(buf.ToString()).HasFlag(FileAttributes.Directory)) continue;
                gameInstance.Add(path);
                var searchmod = (string path) =>
                {
                    try
                    {
                        var gamepath = path + "\\Beat Saber.exe";
                        if (File.Exists(gamepath))
                        {
                            if (pirateGameDetect(path))
                            {
                                debugLog("检测到Beat Saber实例目录：" + path);
                                string ver = BeatSaberVersionDetect(path);
                                debugLog("Beat Saber版本：" + ver);
                                if (!BSInstancePath.ContainsKey(ver))
                                {
                                    BSInstancePath.Add(ver, path);
                                    InstanceSongCoreReady.Add(ver, modCheck(path));
                                }
                                else
                                {
                                    debugLog("检测到相同版本，自动重命名");
                                    string escapedPrefix = Regex.Escape(ver);
                                    string pattern = @"^" + escapedPrefix + @"(\[[0-9]+\])?$";
                                    List<string> duplicateTemp = new List<string>();
                                    foreach (string storeVer in BSInstancePath.Keys)
                                    {
                                        if (Regex.Match(storeVer, pattern).Success)
                                        {
                                            duplicateTemp.Add(storeVer);
                                        }
                                    }
                                    string newVer = Rename(duplicateTemp.Last());
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
                    catch
                    {
                        debugLog("检测Beat Saber实例目录时出现错误！");
                    }
                };
                searchmod(path);
            }
        }
        private bool[] modCheck(string path)
        {
            string modpath = path + "\\Plugins";
            string pendingpath = path + "\\IPA\\Pending";
            string[] mods = { "\\SiraUtil.dll", "\\BSML.dll", "\\SongCore.dll" };
            bool[] modStatus = new bool[5];// 0:SiraUtil 1:BSML 2:SongCore 3:Installed(true)或Pending(false) 4:FileNotFound
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
                {
                    modStatus[3] = false;
                }
                else modStatus[4] = true;
            }
            return modStatus;
        }
        private string BeatSaberVersionDetect(string path)
        {
            string globalManager = path + "\\Beat Saber_Data\\globalgamemanagers";
            string globalManagerContent = null;
            string formatContent = null;
            if (!File.Exists(globalManager))
            {
                debugLog("未检测到globalgamemanagers文件，无法检测Beat Saber版本");
                return "未知版本";
            }
            else
            {
                globalManagerContent = Encoding.UTF8.GetString(File.ReadAllBytes(globalManager)).Substring(0, 5000);
                string pattern = @"[\w\.]+";
                MatchCollection matches = Regex.Matches(globalManagerContent, pattern);
                foreach (Match match in matches)
                {
                    formatContent += match.Value;
                }
                globalManagerContent = null;
            }
            if (bsVersions != null)
            {
                try
                {
                    bsVerionSet = JsonConvert.DeserializeObject<List<BSVerInfo>>(Encoding.UTF8.GetString(bsVersions));
                    foreach (BSVerInfo bsver in bsVerionSet)
                    {
                        if (formatContent.Contains(bsver.BSVersion))
                        {
                            return bsver.BSVersion;
                        }
                    }
                    return "未知版本";
                }
                catch (JsonException)
                {
                    debugLog("Beat Saber版本控制文件解析失败！文件内容非法！");
                    return "未知版本";
                }
                catch (Exception)
                {
                    debugLog("Beat Saber版本控制文件解析失败！文件可能被占用！");
                    return "未知版本";
                }

            }
            if (File.Exists(globalManager))
            {
                byte[] globalManagerBytes = File.ReadAllBytes(globalManager);
                string globalManagerString = Encoding.UTF8.GetString(globalManagerBytes);
                string pattern = @"m_Version: (\d+)";
                Match match = Regex.Match(globalManagerString, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                else
                {
                    return "未知版本";
                }
            }
            else
            {
                return "未知版本";
            }
        }
        #endregion
        #region 工具方法
        void AudioPlayer(SongMap playSong)
        {
            try
            {
                string playPath = playSong.songFolder + "\\" + playSong._songFilename;
                if (File.Exists(playPath))
                {
                    // 释放旧的音频流
                    currentAudioReader?.Dispose();
                    waveOut.Stop();

                    if (playSong._songFilename.EndsWith("wav"))
                    {
                        currentAudioReader = new WaveFileReader(playPath);
                    }
                    else if (playSong._songFilename.EndsWith("ogg") || playSong._songFilename.EndsWith("egg"))
                    {
                        currentAudioReader = new VorbisWaveReader(playPath);
                    }
                    else
                    {
                        debugLog("播放失败！文件：" + playSong._songFilename + "格式不被支持！");
                        BSIMMStatusUpdate("播放器：", "已停止", 0);
                        return;
                    }

                    waveOut.Init(currentAudioReader);
                    waveOut.Play();
                    BSIMMStatusUpdate("播放器：", "正在播放" + playSong._songName, 50);
                    btnPlay.Text = btnPlay2.Text = "暂停";
                    PlaybackTimer.Enabled = true;

                    // 设置进度条
                    trackProgress.Maximum = (int)currentAudioReader.TotalTime.TotalSeconds;
                    trackProgress.SetValueSilent(0);
                    trackProgress2.Maximum = trackProgress.Maximum;
                    trackProgress2.SetValueSilent(0);
                }
                else
                {
                    debugLog("播放失败！文件不存在！");
                    BSIMMStatusUpdate("播放器：", "已停止", 0);
                }
            }
            catch (IOException)
            {
                debugLog("播放失败！文件可能被占用或已损坏！");
                BSIMMStatusUpdate("播放器：", "已停止", 0);
            }
            catch (Exception)
            {
                debugLog("播放失败,未知错误");
            }
        }
        public void readHash()
        {
            if (File.Exists("hash.cache"))
            {
                try
                {
                    byte[] encryptedHash = Convert.FromBase64String(File.ReadAllText("hash.cache"));
                    SongsHash = JsonConvert.DeserializeObject<Dictionary<string, string>>(Encoding.UTF8.GetString(encryptedHash));
                }
                catch (JsonException)
                {
                    debugLog("错误！检测到缓存损坏，将自动删除待稍后重建！");
                    File.Delete("hash.cache");
                }
            }
        }
        public static string ImageToBase64(Image image, ImageFormat format)
        {
            using (var memoryStream = new MemoryStream())
            {
                image.Save(memoryStream, format);
                byte[] imageBytes = memoryStream.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
        }
        public int calcProgress(int current, int total)
        {
            double percentage = ((double)current / total) * 100;
            return (int)Math.Round(percentage);
        }
        private Bitmap ReadImageFile(string path)
        {
            Bitmap bitmap = null;
            try
            {
                FileStream fileStream = File.OpenRead(path);
                Int32 filelength = 0;
                filelength = (int)fileStream.Length;
                Byte[] image = new Byte[filelength];
                fileStream.ReadExactly(image, 0, filelength);
                System.Drawing.Image result = System.Drawing.Image.FromStream(fileStream);
                fileStream.Close();
                bitmap = new Bitmap(result);
            }
            catch (IOException)
            {
                debugLog("读取cover.jpg文件时出现错误");
            }
            return bitmap;
        }
        private void debugLog(string text)
        {
            Invoke(new System.Windows.Forms.MethodInvoker(delegate
            {
                if (txtDebug.Text == "")
                {
                    txtDebug.AppendText(DateTime.Now.ToString() + ":" + text);
                }
                else
                {
                    txtDebug.AppendText("\n" + DateTime.Now.ToString() + ":" + text);
                }
                txtDebug.SelectionStart = txtDebug.Text.Length;
                txtDebug.ScrollToCaret();
                string logEntry = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} - {text}{Environment.NewLine}";
                File.AppendAllText(Application.StartupPath + "BSIMM-" + DateTime.Now.ToString("yyyy-MM-dd") + ".log", logEntry);
            }));
        }
        public static string GetFileMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        public static T ToEntity<T>(IDictionary<string, object> dictionary) where T : new()
        {
            T entity = new T();
            Type type = typeof(T);
            foreach (KeyValuePair<string, object> pair in dictionary)
            {
                PropertyInfo property = type.GetProperty(pair.Key);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(entity, pair.Value, null);
                }
            }
            return entity;
        }
        public int CalculateFolderDepth(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException("The specified path does not exist.");
            return CalculateFolderDepthRecursive(folderPath, 0);
        }

        private int CalculateFolderDepthRecursive(string folderPath, int currentDepth)
        {
            int maxDepth = currentDepth;
            DirectoryInfo di = new DirectoryInfo(folderPath);
            foreach (DirectoryInfo subDir in di.GetDirectories())
            {
                if (subDir.GetDirectories().Length == 0 && subDir.GetFiles().Length == 0)
                {
                    if (di.GetDirectories().Length > 0)
                    {
                        maxDepth++;
                    }
                    else return maxDepth;
                }
                else
                {
                    int subDepth = CalculateFolderDepthRecursive(subDir.FullName, currentDepth + 1);
                    maxDepth = Math.Max(maxDepth, subDepth);
                }
            }
            return maxDepth;
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
            else
            {
                return str + "[1]";
            }
        }

        private bool isDuplicate(string str)
        {
            string pattern = @"\[([0-9]+)\]$";
            Match match = Regex.Match(str, pattern);
            if (match.Success)
            {
                return true;
            }
            else
            {
                return false;
            }
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
                    {
                        return true;
                    }
                    else return false;
                }
                else
                {
                    return false;
                }
            }
            else return false;
        }
        #endregion
        #region 曲包和歌单管理
        private void addFolder(string path)
        {
            int depth = CalculateFolderDepth(path);
            switch (depth)
            {
                case 0:
                    if (addDelicatedSong(path) == 2)
                    {
                        debugLog("该目录只有一首歌曲，将添加到独立歌曲列表中：" + path);
                    }
                    break;
                case 1:
                    if (Directory.GetDirectories(path).Length >= 2)
                    {
                        addMusicPack(path);
                    }
                    else
                    {
                        foreach (string subdir in Directory.GetDirectories(path))
                        {
                            addDelicatedSong(subdir);
                        }
                    }
                    break;
                default:
                    string[] folders = Directory.GetDirectories(path);
                    int[] depths = new int[folders.Length];
                    for (int i = 0; i < folders.Length; i++)
                    {
                        depths[i] = CalculateFolderDepth(folders[i]);
                    }
                    var populardepth = depths.GroupBy(x => x).OrderByDescending(g => g.Count()).FirstOrDefault();
                    if (populardepth.Key == 0)
                    {
                        addMusicPack(path);
                    }
                    else
                    {
                        foreach (var folder in folders)
                        {
                            addFolder(folder);
                        }
                    }
                    break;
            }
        }
        private void saveSongFolderDSong(string path, bool copyFile)
        {
            try
            {
                foreach (SongMap item in delicatedSongList.Values)
                {
                    DirectoryInfo song = new DirectoryInfo(item.songFolder);
                    string newPath = Path.Combine(path, song.Name);
                    if (Directory.Exists(newPath) && newPath != item.songFolder)
                    {
                        Directory.Delete(newPath, true);
                    }
                    else if (Directory.Exists(newPath) && newPath == item.songFolder)
                    {
                        continue;
                    }
                    if (copyFile)
                    {
                        FileSystem.CopyDirectory(item.songFolder, newPath);
                        debugLog("歌曲：" + item._songName + "复制到目录：" + path);
                    }
                    else
                    {
                        FileSystem.MoveDirectory(item.songFolder, newPath);
                        debugLog("歌曲：" + item._songName + "移动到目录：" + path);
                    }
                }
                if (!copyFile)
                {
                    delicatedSongList.Clear();
                    DelicatedSongListView.Clear();
                }
            }
            catch (IOException)
            {
                debugLog("移动文件夹到目录:" + path + "失败！请检查文件和文件夹占用情况！");
            }

        }
        private int addDelicatedSong(string mapDir, string musicPackName = null)
        {
            DirectoryInfo dir = new DirectoryInfo(mapDir);
            SongMap songMap = null;
            string dirName = dir.Name;
            string bsr = "";
            byte[] mapInfo = null;
            int spaceIndex = dirName.IndexOf(' ');
            if (spaceIndex == -1)
            {
                bsr = dirName;//如果没有空格，直接使用文件夹名
                if (bsr.Length > 5)
                {
                    debugLog("非标准bsr命名格式! 目录" + mapDir + "可能不是歌曲谱面目录！请检查文件夹的命名格式！");
                    bsr = "unknown";
                }
            }
            else
            {
                bsr = dirName.Substring(0, spaceIndex);//如果有空格，使用空格前
            }
            if (bsr.Length > 5 && spaceIndex != -1)
            {
                bsr = dirName.Substring(0, 5);
                if (!Regex.IsMatch(bsr, @"^[a-fA-F0-9]+$"))
                {
                    bsr = dirName.Substring(0, 4);
                    if (!Regex.IsMatch(bsr, @"^[a-fA-F0-9]+$"))
                    {
                        debugLog("未知的bsr! 目录" + mapDir + "可能不是歌曲谱面目录！请检查文件夹的命名格式！");
                    }
                }
            }
            if (File.Exists(mapDir + "\\Info.dat"))
            {
                mapInfo = File.ReadAllBytes(mapDir + "\\Info.dat");
                Dictionary<string, object> mapStruct = new Dictionary<string, object>();
                try
                {
                    mapStruct = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(mapInfo));
                    songMap = ToEntity<SongMap>(mapStruct);
                    songMap.songFolder = mapDir;
                }
                catch (JsonException)
                {
                    debugLog("警告:解析歌曲信息文件失败！" + mapDir + "目录下的info.dat或info.json文件损坏！");
                    return 0;
                }
                catch (Exception)
                {
                    debugLog("警告:解析歌曲信息文件失败！" + mapDir + "下的文件可能被锁定或占用！");
                    return 0;
                }
                if (mapIntergrityCheck(mapDir, songMap))
                {
                    if (musicPackName != null)
                    {
                        if (!musicPackInfo[musicPackName].TryAdd(bsr, songMap))
                        {
                            debugLog("警告:检测到重复歌曲：" + songMap._songName + "将自动重命名标注");
                            string escapedPrefix = Regex.Escape(bsr);
                            string pattern = @"^" + escapedPrefix + @"(\[[0-9]+\])?$";
                            List<string> duplicateTemp = new List<string>();
                            foreach (string storeBsr in musicPackInfo[musicPackName].Keys)
                            {
                                if (Regex.Match(storeBsr, pattern).Success)
                                {
                                    duplicateTemp.Add(storeBsr);
                                }
                            }
                            string newBsr = Rename(duplicateTemp.Last());
                            musicPackInfo[musicPackName].Add(newBsr, songMap);
                            return 3;
                        }
                    }
                    else
                    {
                        lock (delicatedSongList)
                        {
                            if (!delicatedSongList.TryAdd(bsr, songMap))
                            {
                                debugLog("警告:检测到重复歌曲：" + bsr + "，将不会添加到独立歌曲列表中");
                                return 3;
                            }
                        }
                    }
                    return 2;
                }
                else return 1;
            }
            else
            {
                return 0;
            }
        }

        private bool mapIntergrityCheck(string mapDir, SongMap map)
        {
            string[] mapStruct = map.GetDifficultiesFiles();
            string coverImg = map._coverImageFilename;
            string musicFile = map._songFilename;
            if (!File.Exists(mapDir + "\\" + coverImg))
            {
                debugLog("警告:检测到缺失封面文件：" + coverImg + "目录：" + mapDir);
                return false;
            }
            if (!File.Exists(mapDir + "\\" + musicFile))
            {
                debugLog("警告:检测到缺失音乐文件：" + musicFile + "目录：" + mapDir);
                return false;
            }
            foreach (string mapFile in mapStruct)
            {
                if (!File.Exists(mapDir + "\\" + mapFile))
                {
                    debugLog("警告:检测到缺失谱面文件：" + mapFile + "目录：" + mapDir);
                    return false;
                }
            }
            return true;

        }
        private void saveSongFolderSync(XElement element)
        {
            foreach (string instance in BSInstancePath.Keys)
            {
                if (InstanceSongCoreReady[instance][3])
                {
                    if (!Directory.Exists(BSInstancePath[instance] + "\\UserData\\SongCore"))
                    {
                        debugLog("实例：" + instance + "未检测到有效插件目录，请启动游戏并检查检查SongCore插件是否正确运行！");
                    }
                    else
                    {
                        element.Save(BSInstancePath[instance] + "\\UserData\\SongCore\\folders.xml");
                        debugLog("实例：" + instance + "保存曲包列表成功！");
                    }
                }
                else
                {
                    if (InstanceSongCoreReady[instance][4])
                    {
                        debugLog("实例：" + instance + "未安装SongCore插件，将不会保存曲包列表！");
                    }
                    else
                    {
                        debugLog("实例：" + instance + "安装mod后未启动游戏，将不会保存曲包列表！");
                    }
                }
                ;
            }
        }
        private void addMusicPack(string path)
        {
            string language = cultureInfo.TwoLetterISOLanguageName;
            string pattern = @"【(.+?)】";
            Match match = Regex.Match(path, pattern);
            string musicPackName = "";
            int finishcount = 0;
            int mapsCount = 0;
            int otherCount = 0;
            int intergrityCount = 0;
            int duplicateCount = 0;
            if (match.Success)
            {
                musicPackName = match.Groups[1].Value;
                debugLog("获取到曲包名称【" + musicPackName + "】");
                if (musicPackInfo.ContainsKey(musicPackName))
                {
                    debugLog("警告:检测到重复括号内曲包命名：" + musicPackName + "，使用完整目录名称");
                    musicPackName = new DirectoryInfo(path).Name;
                }
            }
            else
            {
                debugLog(path + "未检测到曲包名称【】，使用文件夹内名称");
                musicPackName = new DirectoryInfo(path).Name;
            }
            if (musicPackInfo.ContainsKey(musicPackName))
            {
                debugLog("警告:检测到重复曲包名称：" + musicPackName + "，将自动重命名！");
                string escapedPrefix = Regex.Escape(musicPackName);
                string renamepattern = @"^" + escapedPrefix + @"(\[[0-9]+\])?$";
                List<string> duplicateTemp = new List<string>();
                foreach (string storeName in musicPackInfo.Keys)
                {
                    if (Regex.Match(storeName, renamepattern).Success)
                    {
                        duplicateTemp.Add(storeName);
                    }
                }
                musicPackName = Rename(duplicateTemp.Last());
            }
            musicPackInfo.Add(musicPackName, new Dictionary<string, SongMap>());
            string[] mapsDir = Directory.GetDirectories(path);
            BSIMMStatusUpdate("解析：", "正在解析曲包：" + musicPackName, 0);
            foreach (string mapDir in mapsDir)
            {
                switch (addDelicatedSong(mapDir, musicPackName))
                {
                    case 1:
                        intergrityCount++;
                        break;
                    case 2:
                        mapsCount++;
                        break;
                    case 3:
                        duplicateCount++;
                        break;
                    default:
                        otherCount++;
                        break;
                }
                finishcount++;
                BSIMMProgressUpdate(calcProgress(finishcount, mapsDir.Count()));
            }
            if (mapsCount == 0)
            {
                debugLog("警告:目录" + path + "下没有歌曲！将不会添加曲包");
                musicPackInfo.Remove(musicPackName);
                BSIMMStatusUpdate("解析：", "未检测到完整歌曲目录！跳过", 100);
            }
            else
            {
                debugLog("曲包:" + musicPackName + " 检测到" + mapsCount + "个完整歌曲目录  " + duplicateCount + "个重复歌曲目录  " + intergrityCount + "个不完整目录 " + otherCount + "个非曲包目录");
                musicPackPath.Add(musicPackName, path);
                displayMusicpack(musicPackName);
                BSIMMStatusUpdate("就绪", "曲包：" + musicPackName + "添加完成", 100);
            }
            GC.Collect();
        }
        public async Task HashCachePack(string musicPackName)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            int count = 0;
            if (SongsHash != null)
            {
                foreach (KeyValuePair<string, SongMap> hashlist in musicPackInfo[musicPackName])
                {
                    if (!SongsHash.ContainsKey(hashlist.Key))
                    {
                        SongsHash.Add(hashlist.Key, await getSongHash(hashlist.Value));
                    }
                    else
                    {
                        count++;
                        BSIMMProgressUpdate(calcProgress(count, SongsHash.Count));
                    }
                }
                stopwatch.Stop();
                debugLog(($"曲包歌曲已有缓存，查漏补缺！总耗时： {(double)stopwatch.ElapsedMilliseconds / 1000} 秒"));
                BSIMMStatusUpdate("缓存：", "缓存完成！", 100);
            }
            else
            {
                SongsHash = new Dictionary<string, string>();
                foreach (KeyValuePair<string, SongMap> hashlist in musicPackInfo[musicPackName])
                {
                    SongsHash.Add(hashlist.Key, await getSongHash(hashlist.Value));
                }
                stopwatch.Stop();
                debugLog(($"建立缓存成功！总耗时： {(double)stopwatch.ElapsedMilliseconds / 1000} 秒"));
                BSIMMStatusUpdate("缓存：", "缓存完成！", 100);
            }
            GC.Collect();
        }
        private void displayMusicpack(string musicPackName)
        {
            Bitmap musicPackCover = null;
            if (File.Exists(musicPackPath[musicPackName] + "\\cover.jpg"))
            {
                musicPackCover = ReadImageFile(musicPackPath[musicPackName] + "\\cover.jpg");
            }
            else
            {
                musicPackCover = Resources.默认;
                switch (musicPackName)
                {
                    case "1.菜鸡包":
                        musicPackCover = Resources.菜鸡包;
                        break;
                    case "排位萌新包":
                        musicPackCover = Resources.排位萌新包;
                        break;
                    case "热门曲目包":
                        musicPackCover = Resources.热门包;
                        break;
                    case "火爆曲目包":
                        musicPackCover = Resources.超热门包;
                        break;
                    case "谱师Joetastic":
                        musicPackCover = Resources.Joetastic新手包;
                        break;
                    case "bsaber.com":
                        musicPackCover = Resources.奇妙流动包;
                        break;
                    default:
                        musicPackCover = Resources.默认;
                        using (Graphics gfx = Graphics.FromImage(musicPackCover))
                        {
                            StringFormat format = new StringFormat()
                            {
                                Alignment = StringAlignment.Center,
                                LineAlignment = StringAlignment.Center
                            };
                            Font font = new Font("黑体", 72, FontStyle.Bold);
                            Brush brush = Brushes.White;
                            gfx.DrawString(musicPackName, font, brush, new RectangleF(0, 0, musicPackCover.Width, musicPackCover.Height), format);
                            font = new Font("黑体", 24, FontStyle.Bold);
                            SizeF textSize = gfx.MeasureString("BSIMM自动生成@万毒不侵", font);
                            float x = musicPackCover.Width - textSize.Width;
                            float y = musicPackCover.Height - textSize.Height;
                            gfx.DrawString("BSIMM自动生成@万毒不侵", font, brush, x, y);
                        }
                        break;
                }
                using (Bitmap scaleCover = new Bitmap(256, 256))
                {
                    using (Graphics gfx = Graphics.FromImage(scaleCover))
                    {
                        gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        gfx.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        gfx.DrawImage(musicPackCover, 0, 0, 256, 256);
                    }
                    musicPackCover = new Bitmap(scaleCover);
                }
            }
            Invoke(new System.Windows.Forms.MethodInvoker(delegate
            {
                musicPackCoverimgs.Add(musicPackName, musicPackCover);
                musicPackimg.Images.Add(musicPackName, musicPackCover);
                ListViewItem item = new ListViewItem(musicPackName, musicPackName);
                item.ImageIndex = musicPackimg.Images.IndexOfKey(musicPackName);
                item.ToolTipText = musicPackPath[musicPackName];
                musicPackListView.Items.Add(item);
            }));
        }
        private void displaySongList(string musicPackName)
        {
            currentMusicPack = musicPackName;
            songListinited = false;
            Dictionary<string, SongMap> songList = musicPackInfo[musicPackName];
            Invoke(new System.Windows.Forms.MethodInvoker(delegate
            {
                songListView.BeginUpdate();
                songListView.Items.Clear();
                foreach (KeyValuePair<string, SongMap> valuePair in songList)
                {
                    ListViewItem item = new ListViewItem(valuePair.Value._songName);
                    item.SubItems.Add(valuePair.Key);
                    item.SubItems.Add(valuePair.Value._beatsPerMinute.ToString());
                    songListView.Items.Add(item);
                }
                songListView.EndUpdate();
            }));
            songListinited = true;
            BSIMMStatusUpdate("就绪", "曲包：" + musicPackName + "歌曲列表加载完成", 100);

        }

        private void displayDelicatedSongList()
        {
            Invoke(new System.Windows.Forms.MethodInvoker(delegate
            {
                DelicatedSongListView.BeginUpdate();
                DelicatedSongListView.Items.Clear();
                foreach (KeyValuePair<string, SongMap> valuePair in delicatedSongList)
                {
                    ListViewItem item = new ListViewItem(valuePair.Value._songName);
                    item.SubItems.Add(valuePair.Key);
                    item.SubItems.Add(valuePair.Value._beatsPerMinute.ToString());
                    item.SubItems.Add(valuePair.Value.songFolder.ToString());
                    DelicatedSongListView.Items.Add(item);
                }
                DelicatedSongListView.EndUpdate();
            }));
        }
        private void Exportbplist(string path, string musicPackName = null)
        {
            if (musicPackName != null)
            {
                debugLog("开始导出bplist文件：" + path + " 曲包：" + musicPackName);
                Task.Run(async () => await saveMusicPackSonginfo(musicPackName, path));
            }
            else
            {
                debugLog("开始导出bplist文件：" + path);
                List<Task> tasks = new List<Task>();
                Task.Run(async () =>
                {
                    foreach (string bplistName in musicPackInfo.Keys)
                    {
                        string currentBplistName = bplistName;
                        tasks.Add(Task.Run(() => saveMusicPackSonginfo(currentBplistName, path)));
                    }
                    await Task.WhenAll(tasks);
                }).ContinueWith(t =>
                {
                    if (config.HashCache)
                    {
                        string HashResults = JsonConvert.SerializeObject(SongsHash, Formatting.None);
                        File.WriteAllText("hash.cache", Convert.ToBase64String(Encoding.UTF8.GetBytes(HashResults)));
                        HashResults = "";
                    }
                });
            }
        }

        private async Task saveMusicPackSonginfo(string musicPackName, string path)//TODO 线程读写冲突修复
        {
            int finishcount = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            string imgBytes = "data:image/jpg;base64," + ImageToBase64(musicPackCoverimgs[musicPackName], ImageFormat.Jpeg);
            string author = Environment.UserName + "使用BSIMM@万毒不侵 生成";
            string description = "本歌单由" + Environment.UserName + "使用BSIMM生成\r\nBSIMM由万毒不侵开发，开源且免费，如果你是购买的请要求商家退款\r\n项目地址：https://github.com/cyjyyd/Beat-Saber-Independent-Maps-Manager";
            PlayList playList = new PlayList(musicPackName, author, description, imgBytes);
            if (config.HashCache)
            {
                if (SongsHash != null)
                {
                    foreach (KeyValuePair<string, SongMap> hashlist in musicPackInfo[musicPackName])
                    {
                        if (!SongsHash.ContainsKey(hashlist.Key))
                        {
                            string hashValue = await getSongHash(hashlist.Value);
                            SongsHash.Add(hashlist.Key, hashValue);
                            playList.AddSongHash(hashValue);
                        }
                        else
                        {
                            playList.AddSongHash(SongsHash[hashlist.Key]);
                        }
                    }
                    stopwatch.Stop();
                    debugLog(("曲包：" + musicPackName + $"导出使用缓存加速成功！总耗时： {(double)stopwatch.ElapsedMilliseconds / 1000} 秒"));
                    BSIMMStatusUpdate("缓存：", "缓存完成！", 100);
                }
                else
                {
                    SongsHash = new Dictionary<string, string>();
                    foreach (KeyValuePair<string, SongMap> hashlist in musicPackInfo[musicPackName])
                    {
                        if (!SongsHash.ContainsKey(hashlist.Key))
                        {
                            string hashValue = await getSongHash(hashlist.Value);
                            SongsHash.Add(hashlist.Key, hashValue);
                            playList.AddSongHash(hashValue);
                        }
                        else playList.AddSongHash(SongsHash[hashlist.Key]);
                    }
                    stopwatch.Stop();
                    debugLog(("曲包：" + musicPackName + $"未找到缓存，已在计算同时缓存！总耗时： {(double)stopwatch.ElapsedMilliseconds / 1000} 秒"));
                    BSIMMStatusUpdate("缓存：", "缓存完成！", 100);
                }
            }
            else
            {
                foreach (SongMap song in musicPackInfo[musicPackName].Values)
                {
                    playList.AddSongHash(await getSongHash(song));
                    finishcount++;
                    BSIMMProgressUpdate(calcProgress(finishcount, musicPackInfo[musicPackName].Count));
                }
                stopwatch.Stop();
                debugLog(("曲包：" + musicPackName + $"不使用缓存导出成功！总耗时： {(double)stopwatch.ElapsedMilliseconds / 1000} 秒"));
                BSIMMStatusUpdate("导出：", "导出完成！", 100);
            }
            FileStream output = File.Create(path + "\\" + musicPackName + ".bplist");
            byte[] playListContent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(playList, Formatting.None, settings));
            output.Write(playListContent, 0, playListContent.Length);
            debugLog("曲包:" + musicPackName + "导出成功！路径：" + path);
            BSIMMStatusUpdate("曲包：", musicPackName + "bplist导出成功！路径：" + path, 100);
            output.Close(); output.Dispose();
            GC.Collect();
        }
        private async Task<string> getSongHash(SongMap songMap)
        {
            using SHA1 sha1 = SHA1.Create();
            foreach (string filePath in new[] { songMap.songFolder + "\\Info.dat" }.Concat(songMap.GetDifficultiesFiles().Select(f => songMap.songFolder + "\\" + f)))
            {
                using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                byte[] buffer = new byte[65536]; // 64KB buffer
                int bytesRead;
                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    sha1.TransformBlock(buffer, 0, bytesRead, null, 0);
                }
            }
            sha1.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            byte[] hashBytes = sha1.Hash;
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
        #endregion
        #region 窗口事件
        private void BSIMMStatusUpdate(string action, string status, int progress)
        {
            Invoke(new System.Windows.Forms.MethodInvoker(delegate
            {
                BSIMMActionText.Text = action;
                BSIMMStatusText.Text = status;
                BSIMMProgress.ProgressBar.Value = progress;
            }));
        }
        private void BSIMMProgressUpdate(int progress)
        {
            Invoke(new System.Windows.Forms.MethodInvoker(delegate
            {
                BSIMMProgress.ProgressBar.Value = progress;
            }));
        }
        private void DragEvent(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        private void DropEvent(object sender, DragEventArgs e)
        {
            BSIMMStatusUpdate("解析：", "正在解析拖放文件夹", 0);
            string[] folderPaths = ((string[])e.Data.GetData(DataFormats.FileDrop));
            List<string> verifiedPaths = new List<string>();
            for (int i = 0; i < folderPaths.Length; i++)
            {
                if (Directory.Exists(folderPaths[i]))
                {
                    verifiedPaths.Add(folderPaths[i]);
                }
                else
                {
                    debugLog("您拖放的路径：'" + folderPaths[i] + "'似乎不是文件夹!");
                    BSIMMStatusUpdate("解析：", "'" + folderPaths[i] + "'不是文件夹!跳过添加", 100);
                }
            }
            try
            {
                Task.Run(async () =>
                {
                    for (int i = 0; i < verifiedPaths.Count; i++)
                    {
                        addFolder(verifiedPaths[i]);
                    }
                    if (config.HashCache)
                    {
                        foreach (KeyValuePair<string, Dictionary<string, SongMap>> HashMusicPack in musicPackInfo)
                        {
                            await HashCachePack(HashMusicPack.Key);
                        }
                    }
                }).ContinueWith(t =>
                {
                    if (config.HashCache)
                    {
                        string HashResults = JsonConvert.SerializeObject(SongsHash, Formatting.None);
                        File.WriteAllText("hash.cache", Convert.ToBase64String(Encoding.UTF8.GetBytes(HashResults)));
                        HashResults = "";
                    }
                });
            }
            catch (Exception)
            {
                throw;
            }
        }
        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            BSIMMFolderBrowser.ShowDialog();
            if (BSIMMFolderBrowser.SelectedPath == "")
            {
                return;
            }
            string[] folderPaths = BSIMMFolderBrowser.SelectedPath.Split(';');
            List<string> verifiedPaths = new List<string>();
            BSIMMFolderBrowser.SelectedPath = "";
            for (int i = 0; i < folderPaths.Length; i++)
            {
                if (Directory.Exists(folderPaths[i]))
                {
                    verifiedPaths.Add(folderPaths[i]);
                }
                else
                {
                    debugLog("您选择的路径：'" + folderPaths[i] + "'似乎不是文件夹!");
                    BSIMMStatusUpdate("解析：", "'" + folderPaths[i] + "'不是文件夹!跳过添加", 100);
                }
            }
            Task.Run(async () =>
            {
                addFolder(verifiedPaths[0]);
                if (config.HashCache)
                {
                    foreach (KeyValuePair<string, Dictionary<string, SongMap>> HashMusicPack in musicPackInfo)
                    {
                        await HashCachePack(HashMusicPack.Key);
                    }
                }
            }).ContinueWith(t =>
            {
                if (config.HashCache)
                {
                    string HashResults = JsonConvert.SerializeObject(SongsHash, Formatting.None);
                    File.WriteAllText("hash.cache", Convert.ToBase64String(Encoding.UTF8.GetBytes(HashResults)));
                    HashResults = "";
                }
            });
        }
        private void musicPackListView_Click(object sender, EventArgs e)
        {
            int index = musicPackListView.SelectedItems[0].Index;
            if (index != -1)
            {
                string musicPackName = musicPackListView.Items[index].Text;
                if (songListinited)
                {
                    new Thread(() => displaySongList(musicPackName)).Start();
                }
                else
                {
                    BSIMMStatusUpdate("繁忙", "正在加载歌曲信息，请稍后", 0);
                }
            }
        }
        private void songListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 停止当前播放
            PlaybackTimer.Enabled = false;
            waveOut.Stop();
            btnPlay.Text = "播放";
            trackProgress.SetValueSilent(0);
            trackProgress2.SetValueSilent(0);

            if (songListView.SelectedItems.Count == 0)
            {
                playSong = null;
                return;
            }

            int index = songListView.SelectedItems[0].Index;
            if (index == -1)
            {
                playSong = null;
                return;
            }

            string playKey = songListView.SelectedItems[0].SubItems[1].Text;

            // 优先从选中的曲包中查找
            if (musicPackListView.SelectedItems.Count > 0)
            {
                string selectedItemText = musicPackListView.SelectedItems[0].Text;
                if (musicPackInfo.TryGetValue(selectedItemText, out Dictionary<string, SongMap> innerDictionary))
                {
                    if (innerDictionary.TryGetValue(playKey, out SongMap Songdetails))
                    {
                        playSong = Songdetails;
                        return;
                    }
                    else
                    {
                        debugLog("未找到" + playKey + "对应的歌曲，可能已经被删除，请点击曲包刷新列表后再试！");
                        playSong = null;
                        return;
                    }
                }
                else
                {
                    debugLog("错误，曲包名称不一致");
                    playSong = null;
                    return;
                }
            }

            // 没有选中曲包时，从所有曲包中查找
            foreach (Dictionary<string, SongMap> musicPack in musicPackInfo.Values)
            {
                if (musicPack.TryGetValue(playKey, out SongMap foundSong))
                {
                    playSong = foundSong;
                    return;
                }
            }

            debugLog("未找到" + playKey + "对应的歌曲，请检查该key是否存在！");
            playSong = null;
        }

        private void trackVolume_ValueChanged(object sender, EventArgs e)
        {
            waveOut.Volume = (float)trackVolume.Value / 100;
        }
        private void trackVolume2_ValueChanged(object sender, EventArgs e)
        {
            waveOut.Volume = (float)trackVolume2.Value / 100;
        }

        private void trackProgress_ValueChanged(object sender, EventArgs e)
        {
            if (currentAudioReader != null && waveOut.PlaybackState != PlaybackState.Stopped)
            {
                currentAudioReader.CurrentTime = TimeSpan.FromSeconds(trackProgress.Value);
                trackProgress2.SetValueSilent(trackProgress.Value);
            }
        }

        private void trackProgress2_ValueChanged(object sender, EventArgs e)
        {
            if (currentAudioReader != null && waveOut.PlaybackState != PlaybackState.Stopped)
            {
                currentAudioReader.CurrentTime = TimeSpan.FromSeconds(trackProgress2.Value);
                trackProgress.SetValueSilent(trackProgress2.Value);
            }
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (btnPlay.Text == "播放")
            {
                if (playSong != null)
                {
                    AudioPlayer(playSong);
                }
            }
            else
            {
                // 暂停
                if (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    waveOut.Pause();
                    btnPlay.Text = btnPlay2.Text = "播放";
                    BSIMMStatusUpdate("播放器：", "已暂停", 0);
                }
            }
        }

        private void btnPlay2_Click(object sender, EventArgs e)
        {
            if (btnPlay2.Text == "播放")
            {
                if (DelicatedSongListView.SelectedItems.Count != 0)
                {
                    int index = DelicatedSongListView.SelectedItems[0].Index;
                    if (index != -1)
                    {
                        string playKey = DelicatedSongListView.SelectedItems[0].SubItems[1].Text;
                        if (delicatedSongList.TryGetValue(playKey, out SongMap selectedSong))
                        {
                            AudioPlayer(selectedSong);
                        }
                    }
                }
            }
            else
            {
                // 暂停
                if (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    waveOut.Pause();
                    btnPlay.Text = btnPlay2.Text = "播放";
                    BSIMMStatusUpdate("播放器：", "已暂停", 0);
                }
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void PlaybackTimer_Tick(object sender, EventArgs e)
        {
            if (waveOut.PlaybackState == PlaybackState.Stopped)
            {
                btnPlay.Text = btnPlay2.Text = "播放";
                BSIMMStatusUpdate("播放器：", "已停止", 0);
                PlaybackTimer.Enabled = false;
                // 重置进度条
                trackProgress.SetValueSilent(0);
                trackProgress2.SetValueSilent(0);
            }
            else if (currentAudioReader != null)
            {
                // 更新进度条位置
                int currentPos = (int)currentAudioReader.CurrentTime.TotalSeconds;
                if (currentPos <= trackProgress.Maximum)
                {
                    trackProgress.SetValueSilent(currentPos);
                    trackProgress2.SetValueSilent(currentPos);
                }
            }
        }
        private void btnSetImg_Click(object sender, EventArgs e)
        {
            if (musicPackListView.Items.Count > 0)
            {
                if (musicPackListView.SelectedItems.Count > 0)
                {
                    int index = musicPackListView.SelectedItems[0].Index;
                    string musicPackName = musicPackListView.Items[index].Text;
                    musicPackCoverDialog.ShowDialog();
                    string imgPath = musicPackCoverDialog.FileName;
                    musicPackCoverDialog.FileName = "";
                    if (imgPath != "")
                    {
                        Image img = Image.FromFile(imgPath);
                        musicPackimg.Images.RemoveByKey(musicPackName);
                        musicPackimg.Images.Add(musicPackName, img);
                        using (Bitmap scaleCover = new Bitmap(256, 256))
                        {
                            using (Graphics gfx = Graphics.FromImage(scaleCover))
                            {
                                gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                gfx.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                                gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                gfx.DrawImage(img, 0, 0, 256, 256);
                            }
                            img = new Bitmap(scaleCover);
                        }
                        musicPackCoverimgs[musicPackName] = img;
                        foreach (ListViewItem item in musicPackListView.Items)
                        {
                            musicPackName = item.Text;
                            item.ImageIndex = musicPackimg.Images.IndexOfKey(musicPackName);
                        }
                        musicPackListView.Refresh();
                    }
                    else
                    {
                        debugLog("未选择图片文件！");
                    }
                }
                else
                {
                    MessageBox.Show("请先选择要设置封面的曲包！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("请先添加曲包目录！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnInfo_Click(object sender, EventArgs e)
        {
            if (songListView.Items.Count == 0) { MessageBox.Show("请先点击左侧曲包以获取歌曲列表！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (songListView.SelectedItems.Count == 0) { MessageBox.Show("请先选择要查看的歌曲！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int index = songListView.SelectedItems[0].Index;
            if (index != -1)
            {
                string playKey = songListView.SelectedItems[0].SubItems[1].Text;
                SongMap playSong = musicPackInfo[musicPackListView.SelectedItems[0].Text][playKey];
                StringBuilder diffinfo = new StringBuilder();
                StringBuilder difffile = new StringBuilder();
                foreach (string diff in playSong.GetDifficulties())
                {
                    diffinfo.Append(diff + " ");
                }
                foreach (string files in playSong.GetDifficultiesFiles())
                {
                    difffile.Append(files + " ");
                }
                string info = "歌曲名称：" + playSong._songName +
                    "\n歌曲作者：" + playSong._songAuthorName +
                    "\n谱面作者：" + playSong._levelAuthorName +
                    "\nBPM：" + playSong._beatsPerMinute +
                    "\n难度：" + diffinfo.ToString() +
                    "\n谱面文件：" + difffile.ToString() +
                    "\n谱面文件夹：" + playSong.songFolder;
                MessageBox.Show(info, "歌曲信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("确定要退出吗？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }
        }

        private void btnDeduplication_Click(object sender, EventArgs e)
        {
            if (duplicateAdvance)
            {
                MessageBox.Show("该功能尚未实现！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                if (musicPackListView.Items.Count == 0)
                {
                    MessageBox.Show("请先添加曲包目录！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                else if (musicPackListView.SelectedItems.Count == 0)
                {
                    MessageBox.Show("请先选择要去重的曲包！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                else
                {
                    if (MessageBox.Show("一键去重将默认保留检索到的第一个曲目，确认继续吗（可在设置中开启高级模式选择要保留的曲目）", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        foreach (ListViewItem list in songListView.Items)
                        {
                            if (isDuplicate(list.SubItems[1].Text))
                            {
                                string currentFolder = musicPackInfo[currentMusicPack][list.SubItems[1].Text].songFolder;
                                try
                                {
                                    Directory.Delete(currentFolder, true);
                                    debugLog("删除歌曲：" + list.SubItems[1].Text + " 所在文件夹：" + currentFolder + "成功！");
                                    songListView.Items.Remove(list);
                                }
                                catch (Exception)
                                {
                                    debugLog("删除文件夹失败：" + currentFolder + " 文件夹可能不存在或文件被其他程序占用！ ");
                                }
                            }
                        }
                    }
                }
            }
        }
        private void comboBoxPlatform_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxPlatform.SelectedIndex == 2)
            {
                btnSaveMusicPack.Enabled = false;
            }
            else
            {
                btnSaveMusicPack.Enabled = true;
            }
        }

        private void btnSaveMusicPack_Click(object sender, EventArgs e)
        {
            if (musicPackListView.Items.Count > 0)
            {
                XElement rootElement = new XElement("folders");
                foreach (ListViewItem musicPack in musicPackListView.Items)
                {
                    try
                    {
                        if (!File.Exists(musicPackPath[musicPack.Text] + "\\cover.jpg"))
                        {
                            musicPackCoverimgs[musicPack.Text].Save(musicPackPath[musicPack.Text] + "\\cover.jpg");
                        }
                        XElement folder = new XElement("folder",
                            new XElement("Name", musicPack.Text),
                            new XElement("Path", musicPackPath[musicPack.Text]),
                            new XElement("Pack", "2"),
                            new XElement("ImagePath", musicPackPath[musicPack.Text] + "\\cover.jpg") // 填充 <ImagePath> 元素
                        );
                        rootElement.Add(folder);
                    }
                    catch (Exception)
                    {
                        debugLog("曲包：" + musicPack.Text + "保存封面失败！");
                        debugLog("预期外的错误，导出取消，请尝试更换封面或删除对应歌曲目录下的cover.jpg文件");
                        return;
                    }
                }
                saveSongFolderSync(rootElement);
            }
            else
            {
                MessageBox.Show("请先添加曲包目录！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void btnSaveList_Click(object sender, EventArgs e)
        {
            if (musicPackListView.Items.Count > 0)
            {
                if (musicPackListView.SelectedItems.Count > 0)
                {
                    int index = musicPackListView.SelectedItems[0].Index;
                    if (index == -1)
                    {
                        MessageBox.Show("请先选择要更改封面的曲包！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        DialogResult result = MessageBox.Show("您在曲包列表中选择了曲包，你想要导出这个曲包（是）还是导出全部曲包（否）？", "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                        if (result == DialogResult.Yes)
                        {
                            string musicPackName = musicPackListView.Items[index].Text;
                            savebplistDialog.ShowDialog();
                            string path = savebplistDialog.SelectedPath;
                            savebplistDialog.SelectedPath = "";
                            if (path != "")
                            {
                                Exportbplist(path, musicPackName);
                            }
                            else
                            {
                                debugLog("未选择保存路径！");
                            }
                        }
                        else if (result == DialogResult.No)
                        {
                            savebplistDialog.ShowDialog();
                            string path = savebplistDialog.SelectedPath;
                            if (path != "")
                            {
                                Exportbplist(path);
                            }
                            else
                            {
                                debugLog("未选择保存路径！");
                            }
                        }
                        else
                        {
                            debugLog("取消导出！");
                        }
                    }
                }
                else
                {
                    savebplistDialog.ShowDialog();
                    string path = savebplistDialog.SelectedPath;
                    if (path != "")
                    {
                        Exportbplist(path);
                    }
                    else
                    {
                        debugLog("未选择保存路径！");
                    }
                }
            }
            else
            {
                MessageBox.Show("请先添加曲包目录！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnAutoFill_Click(object sender, EventArgs e)
        {
            debugLog("开始自动填充曲包列表，自动读取程序所在目录.....");
            string[] folders = Directory.GetDirectories(Application.StartupPath);
            foreach (string folder in folders)
            {
                new Thread(() => addFolder(folder)).Start();
            }
        }

        private void musicPackListView_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (e.Label == null)
            {
                debugLog("新修改的名称不能为空！");
                e.CancelEdit = true;
            }
            else
            {
                foreach (ListViewItem item in musicPackListView.Items)
                {
                    if (item.Text == e.Label)
                    {
                        debugLog("已经有曲包名称" + e.Label + "了哦，试试换一个名称吧！");
                        e.CancelEdit = true;
                    }
                }
                if (!e.CancelEdit)
                {
                    musicPackInfo.Add(e.Label, musicPackInfo[musicPackListView.SelectedItems[0].Text]);
                    musicPackInfo.Remove(musicPackListView.SelectedItems[0].Text);
                    Image newImg = musicPackimg.Images[musicPackListView.SelectedItems[0].Text];
                    musicPackimg.Images.Add(e.Label, newImg);
                    musicPackListView.SelectedItems[0].ImageIndex = musicPackimg.Images.IndexOfKey(e.Label);
                    musicPackimg.Images.RemoveByKey(musicPackListView.SelectedItems[0].Text);
                    Image newCover = musicPackCoverimgs[musicPackListView.SelectedItems[0].Text];
                    musicPackCoverimgs.Add(e.Label, newCover);
                    musicPackCoverimgs.Remove(musicPackListView.SelectedItems[0].Text);
                    string newPath = musicPackPath[musicPackListView.SelectedItems[0].Text];
                    musicPackPath.Add(e.Label, newPath);
                    musicPackPath.Remove(musicPackListView.SelectedItems[0].Text);
                    debugLog("曲包名称修改：" + musicPackListView.SelectedItems[0].Text + " 修改为：" + e.Label);
                }
            }
        }

        private void musicPackListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            BSIMMStatusUpdate("查看", "曲包路径：" + musicPackPath[musicPackListView.SelectedItems[0].Text], 0);
        }

        private void btnInstallEverything_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.voidtools.com/zh-cn/") { UseShellExecute = true });
        }

        private void tabMusicPackContorl_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tabMusicPackContorl.SelectedIndex)
            {
                case 0:
                    break;
                case 1:
                    break;
                case 2:
                    displayDelicatedSongList();
                    break;
                default:
                    break;
            }
        }
        private void DelicatedSongListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 停止当前播放
            PlaybackTimer.Enabled = false;
            waveOut.Stop();
            btnPlay.Text = "播放";
            btnPlay2.Text = "播放";
            trackProgress.SetValueSilent(0);
            trackProgress2.SetValueSilent(0);

            if (DelicatedSongListView.SelectedItems.Count == 0)
            {
                playSong = null;
                return;
            }

            int index = DelicatedSongListView.SelectedItems[0].Index;
            if (index == -1)
            {
                playSong = null;
                return;
            }

            string playKey = DelicatedSongListView.SelectedItems[0].SubItems[1].Text;
            if (delicatedSongList.TryGetValue(playKey, out SongMap selectedSong))
            {
                playSong = selectedSong;
            }
            else
            {
                debugLog("未找到" + playKey + "对应的歌曲！");
                playSong = null;
            }
        }

        private void btnMigrateFolder_Click(object sender, EventArgs e)
        {
            BSIMMFolderBrowser.ShowDialog();
            string path = BSIMMFolderBrowser.SelectedPath;
            if (path != "")
            {
                DialogResult saveSrcFile = MessageBox.Show("需要保留原目录文件吗？", "提示", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (saveSrcFile == DialogResult.Yes)
                {
                    saveSongFolderDSong(path, true);
                    debugLog("散装歌曲已经全部复制到目录：" + path + " 可以在第一页添加此目录！");
                }
                else if (saveSrcFile == DialogResult.No)
                {
                    saveSongFolderDSong(path, false);
                    debugLog("散装歌曲已经全部复制到目录：" + path + " 可以在第一页添加此目录！");
                }
                else debugLog("操作取消：整合散装歌曲到目录：" + path);
            }
            else
            {
                debugLog("未选择保存路径！");
            }
        }

        private async void btnFullScan_Click(object sender, EventArgs e)
        {
            if (multiInstanceDetect)
            {
                if (MessageBox.Show("全盘扫描旨在扫描散落的歌曲,建议先在第一页添加歌曲目录，软件会自动排除扫描\n如果已添加歌曲目录，请忽略本提示并点击确认，否则请点击取消", "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
                {
                    Everything_SetSearch("info.dat");
                    Everything_SetRequestFlags(EVERYTHING_REQUEST_PATH | EVERYTHING_REQUEST_FILE_NAME);
                    Everything_SetMatchWholeWord(true);
                    Everything_Query(true);
                    StringBuilder buf = new StringBuilder(300);
                    List<Task> tasks = new List<Task>();
                    List<string> paths = new List<string>();
                    SemaphoreSlim semaphore = new SemaphoreSlim(10);
                    for (uint i = 0; i < Everything_GetNumResults(); i++)
                    {
                        buf.Clear();
                        Everything_GetResultFullPathName(i, buf, 300);
                        var path = Path.GetDirectoryName(buf.ToString())!;
                        if (path.Contains("Prefetch") || path.Contains("$RECYCLE.BIN") || path.Contains("OneDrive") || Directory.Exists(buf.ToString())) continue;
                        paths.Add(path);
                    }
                    foreach (string path in paths)
                    {
                        bool excluded = false;
                        foreach (string MusicPackDir in musicPackPath.Values)
                        {
                            string partpath = MusicPackDir.ToString();
                            if (path.Contains(partpath))
                            {
                                excluded = true;
                                break;
                            }
                        }
                        if (!excluded)
                        {
                            await semaphore.WaitAsync();
                            tasks.Add(Task.Run(() => { try { addDelicatedSong(path); } finally { semaphore.Release(); } }));
                        }
                        excluded = false;
                    }
                    await Task.WhenAll(tasks);
                    displayDelicatedSongList();
                }
            }
            else
            {
                debugLog("未检测到Everthing增强扩展或扩展状态异常，无法全盘扫描！");
            }
        }

        private void btnSetting_Click(object sender, EventArgs e)
        {
            SettingsForm settingsForm = new SettingsForm();
            settingsForm.Owner = this;
            if (settingsForm.ShowDialog() == DialogResult.Cancel)
            {
                config.readset();
                if (config.HashCache)
                {
                    readHash();
                }
            }
        }

        private void btnDFSelect_Click(object sender, EventArgs e)
        {
            BSIMMFolderBrowser.ShowDialog();
            if (BSIMMFolderBrowser.SelectedPath == "")
            {
                return;
            }
            string[] folderPaths = BSIMMFolderBrowser.SelectedPath.Split(';');
        }
        #endregion

        #region BeatSaver 搜索

        /// <summary>
        /// 显示搜索结果
        /// </summary>
        private async Task DisplaySearchResults(List<BeatSaverMap> maps)
        {
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                foreach (var map in maps)
                {
                    var row = new DataGridViewRow();
                    row.CreateCells(dataGridView1);

                    // 列顺序: 0=Select, 1=bsr, 2=Cover, 3=Name, 4=description, 5=bpm, 6=levelAuthorName
                    row.Cells[0].Value = false;  // 选择框默认不选中
                    row.Cells[1].Value = map.Id;  // bsr

                    // 加载封面图片
                    try
                    {
                        var coverUrl = map.GetCoverUrl();
                        if (!string.IsNullOrEmpty(coverUrl))
                        {
                            var imageData = await httpClient.GetByteArrayAsync(coverUrl);
                            using (var ms = new MemoryStream(imageData))
                            {
                                var img = Image.FromStream(ms);
                                row.Cells[2].Value = new Bitmap(img, new Size(100, 100));
                            }
                        }
                    }
                    catch
                    {
                        // 封面加载失败时使用默认图片
                        row.Cells[2].Value = null;
                    }

                    row.Cells[3].Value = map.Name;  // 名称
                    row.Cells[4].Value = map.Description;  // 简介
                    row.Cells[5].Value = map.Metadata?.Bpm.ToString("F1") ?? "N/A";  // BPM
                    row.Cells[6].Value = map.Metadata?.LevelAuthorName ?? map.Uploader?.Name ?? "N/A";  // 谱面作者

                    row.Tag = map;  // 存储完整数据
                    dataGridView1.Rows.Add(row);
                }
            }
        }

        /// <summary>
        /// 更新分页控件状态
        /// </summary>
        private void UpdatePaginationControls()
        {
            btnPrevPage.Enabled = currentPage > 0;
            btnNextPage.Enabled = currentPage < totalPages - 1;
            if (isOrModeSearch)
            {
                lblPageInfo.Text = $"第 {currentPage + 1}/{totalPages} 页 (OR)";
            }
            else
            {
                lblPageInfo.Text = $"第 {currentPage + 1}/{totalPages} 页";
            }
        }

        /// <summary>
        /// 上一页按钮点击事件
        /// </summary>
        private async void btnPrevPage_Click(object sender, EventArgs e)
        {
            if (currentPage > 0)
            {
                currentPage--;
                if (isOrModeSearch)
                {
                    // OR 模式下从缓存中获取数据
                    await DisplayOrSearchPage();
                }
                else
                {
                    await SearchMapsWithFilter(currentFilterPreset);
                }
            }
        }

        /// <summary>
        /// 下一页按钮点击事件
        /// </summary>
        private async void btnNextPage_Click(object sender, EventArgs e)
        {
            if (currentPage < totalPages - 1)
            {
                currentPage++;
                if (isOrModeSearch)
                {
                    // OR 模式下从缓存中获取数据
                    await DisplayOrSearchPage();
                }
                else
                {
                    await SearchMapsWithFilter(currentFilterPreset);
                }
            }
        }

        #endregion

        #region 筛选构建器面板初始化

        /// <summary>
        /// 初始化筛选构建器UI（使用子窗口模式）
        /// </summary>
        private void InitializeFilterBuilderPanel()
        {
            // 创建默认预设
            currentFilterPreset = new FilterPreset("新建筛选");
            currentFilterPreset.AddGroup(new FilterGroup("条件组1"));

            // 绑定按钮事件（Designer不处理事件绑定）
            btnOpenFilterBuilder.Click += BtnOpenFilterBuilder_Click;
            btnSelectAll.Click += BtnSelectAll_Click;
            btnSelectInverse.Click += BtnSelectInverse_Click;
            btnExportSelected.Click += BtnExportSelected_Click;
            btnExportAll.Click += BtnExportAll_Click;

            // 更新筛选概要显示
            UpdateFilterSummary();
        }

        /// <summary>
        /// 打开筛选构建器窗口
        /// </summary>
        private void BtnOpenFilterBuilder_Click(object sender, EventArgs e)
        {
            if (filterBuilderForm == null || filterBuilderForm.IsDisposed)
            {
                filterBuilderForm = new FilterBuilderForm(currentFilterPreset);
                filterBuilderForm.SearchRequested += FilterBuilderForm_SearchRequested;
            }
            filterBuilderForm.ShowDialog(this);
        }

        /// <summary>
        /// 筛选构建器窗口请求搜索
        /// </summary>
        private async void FilterBuilderForm_SearchRequested(object sender, FilterPreset preset)
        {
            currentFilterPreset = preset;
            UpdateFilterSummary();
            currentPage = 0;
            // 清除之前的 OR 搜索缓存
            isOrModeSearch = false;
            allOrSearchResults.Clear();
            await SearchMapsWithFilter(preset);
        }

        /// <summary>
        /// 更新筛选条件概要显示
        /// </summary>
        private void UpdateFilterSummary()
        {
            if (currentFilterPreset == null || !currentFilterPreset.Groups.Any(g => g.HasActiveConditions()))
            {
                lblFilterSummary.Text = "当前筛选：无条件";
                return;
            }

            var summaryParts = new System.Collections.Generic.List<string>();
            foreach (var group in currentFilterPreset.GetActiveGroups())
            {
                var activeConditions = group.GetActiveConditions();
                if (!activeConditions.Any()) continue;

                var conditionSummaries = new System.Collections.Generic.List<string>();
                for (int i = 0; i < activeConditions.Count; i++)
                {
                    var c = activeConditions[i];
                    conditionSummaries.Add($"{c.DisplayName}={c.Value}");
                    // 在条件之间添加逻辑运算符（最后一个条件后不加）
                    if (i < activeConditions.Count - 1)
                    {
                        conditionSummaries.Add(c.Operator == LogicOperator.Or ? "OR" : "AND");
                    }
                }

                // 添加组间的逻辑运算符
                if (conditionSummaries.Any())
                {
                    string groupSummary = string.Join(" ", conditionSummaries);
                    if (summaryParts.Any())
                    {
                        string groupOp = group.GroupOperator == LogicOperator.Or ? "OR" : "AND";
                        summaryParts.Add($"{groupOp} [{groupSummary}]");
                    }
                    else
                    {
                        summaryParts.Add($"[{groupSummary}]");
                    }
                }
            }

            lblFilterSummary.Text = "当前筛选：" + string.Join(" ", summaryParts);
        }

        #endregion

        #region 筛选构建器事件处理

        /// <summary>
        /// 使用筛选预设执行搜索
        /// </summary>
        private async Task SearchMapsWithFilter(FilterPreset preset)
        {
            try
            {
                BSIMMActionText.Text = "搜索中...";
                dataGridView1.Rows.Clear();
                currentSearchResults.Clear();

                // 检查是否有 OR 逻辑需要处理
                bool hasOrLogic = HasOrLogic(preset);

                if (hasOrLogic)
                {
                    // 使用 OR 逻辑处理：执行多次搜索然后合并
                    await SearchWithOrLogic(preset);
                }
                else
                {
                    // 纯 AND 逻辑：清除 OR 模式标志
                    isOrModeSearch = false;
                    allOrSearchResults.Clear();

                    var filter = BuildSearchFilterFromPreset(preset);
                    var response = await beatSaverClient.SearchMapsAsync(filter, currentPage);

                    if (response?.Maps != null && response.Maps.Count > 0)
                    {
                        currentSearchResults = response.Maps;

                        if (response.Info != null)
                        {
                            totalPages = response.Info.Pages;
                            BSIMMActionText.Text = $"找到 {response.Info.Total} 个结果";
                        }
                        else if (response.Metadata != null)
                        {
                            totalPages = (response.Metadata.Total + response.Metadata.PageSize - 1) / response.Metadata.PageSize;
                            BSIMMActionText.Text = $"找到 {response.Metadata.Total} 个结果";
                        }
                        else
                        {
                            totalPages = 1;
                            BSIMMActionText.Text = $"找到 {response.Maps.Count} 个结果";
                        }

                        UpdatePaginationControls();
                        await DisplaySearchResults(response.Maps);
                    }
                    else
                    {
                        BSIMMActionText.Text = "未找到匹配的结果";
                        btnPrevPage.Enabled = false;
                        btnNextPage.Enabled = false;
                        lblPageInfo.Text = "第 0/0 页";
                    }
                }
            }
            catch (Exception ex)
            {
                debugLog($"搜索失败: {ex.Message}");
                BSIMMActionText.Text = "搜索失败";
            }
        }

        /// <summary>
        /// 检查预设中是否包含 OR 逻辑
        /// </summary>
        private bool HasOrLogic(FilterPreset preset)
        {
            if (preset == null) return false;

            var activeGroups = preset.GetActiveGroups();

            // 检查组间是否有 OR
            foreach (var group in activeGroups)
            {
                if (group.GroupOperator == LogicOperator.Or)
                    return true;

                // 检查组内条件是否有 OR
                var conditions = group.GetActiveConditions();
                foreach (var condition in conditions)
                {
                    if (condition.Operator == LogicOperator.Or)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 使用 OR 逻辑执行搜索（需要多次请求并合并结果）
        /// </summary>
        private async Task SearchWithOrLogic(FilterPreset preset)
        {
            var activeGroups = preset.GetActiveGroups();
            if (!activeGroups.Any())
            {
                BSIMMActionText.Text = "未找到匹配的结果";
                return;
            }

            // 分析条件结构并生成搜索任务
            var searchFilters = BuildSearchFiltersWithOrLogic(preset);

            BSIMMActionText.Text = $"正在执行 {searchFilters.Count} 个搜索任务...";

            // 收集所有搜索的所有页面结果
            var allResults = new Dictionary<string, BeatSaverMap>(); // 使用字典去重

            foreach (var filter in searchFilters)
            {
                // 对每个搜索任务获取所有页面的结果
                int page = 0;
                while (true)
                {
                    try
                    {
                        var response = await beatSaverClient.SearchMapsAsync(filter, page);
                        if (response?.Maps == null || response.Maps.Count == 0)
                            break;

                        foreach (var map in response.Maps)
                        {
                            if (!string.IsNullOrEmpty(map.Id) && !allResults.ContainsKey(map.Id))
                            {
                                allResults[map.Id] = map;
                            }
                        }

                        // 检查是否还有更多页面
                        if (response.Info != null && page < response.Info.Pages - 1)
                        {
                            page++;
                            BSIMMActionText.Text = $"正在搜索... 已找到 {allResults.Count} 个结果";
                        }
                        else if (response.Metadata != null && (page + 1) * response.Metadata.PageSize < response.Metadata.Total)
                        {
                            page++;
                            BSIMMActionText.Text = $"正在搜索... 已找到 {allResults.Count} 个结果";
                        }
                        else
                        {
                            break;
                        }

                        // 添加小延迟避免请求过快
                        await Task.Delay(100);
                    }
                    catch
                    {
                        break;
                    }
                }
            }

            // 缓存所有结果
            allOrSearchResults = allResults.Values.ToList();
            isOrModeSearch = true;
            currentPage = 0;

            // 计算总页数
            totalPages = (int)Math.Ceiling((double)allOrSearchResults.Count / PageSize);

            if (allOrSearchResults.Count > 0)
            {
                BSIMMActionText.Text = $"找到 {allOrSearchResults.Count} 个结果（OR 合并）";
                await DisplayOrSearchPage();
            }
            else
            {
                BSIMMActionText.Text = "未找到匹配的结果";
                btnPrevPage.Enabled = false;
                btnNextPage.Enabled = false;
                lblPageInfo.Text = "第 0/0 页";
            }
        }

        /// <summary>
        /// 显示 OR 搜索的当前页结果
        /// </summary>
        private async Task DisplayOrSearchPage()
        {
            dataGridView1.Rows.Clear();
            currentSearchResults.Clear();

            // 计算当前页的数据范围
            int startIndex = currentPage * PageSize;
            int endIndex = Math.Min(startIndex + PageSize, allOrSearchResults.Count);

            // 获取当前页的数据
            var pageResults = allOrSearchResults.Skip(startIndex).Take(PageSize).ToList();
            currentSearchResults = pageResults;

            // 显示结果
            await DisplaySearchResults(pageResults);

            // 更新分页控件
            UpdatePaginationControls();
        }

        /// <summary>
        /// 执行单个搜索任务
        /// </summary>
        private async Task<List<BeatSaverMap>> ExecuteSearchTask(BeatSaverSearchFilter filter, int page)
        {
            try
            {
                var response = await beatSaverClient.SearchMapsAsync(filter, page);
                return response?.Maps ?? new List<BeatSaverMap>();
            }
            catch
            {
                return new List<BeatSaverMap>();
            }
        }

        /// <summary>
        /// 根据 OR 逻辑拆分生成多个搜索过滤器
        /// </summary>
        private List<BeatSaverSearchFilter> BuildSearchFiltersWithOrLogic(FilterPreset preset)
        {
            var filters = new List<BeatSaverSearchFilter>();
            var activeGroups = preset.GetActiveGroups();

            if (!activeGroups.Any())
            {
                filters.Add(new BeatSaverSearchFilter());
                return filters;
            }

            // 简化处理：将 OR 逻辑拆分为多个独立的搜索
            // 每遇到 OR，就拆分为一个新的搜索任务

            var currentFilter = new BeatSaverSearchFilter();
            filters.Add(currentFilter);

            for (int i = 0; i < activeGroups.Count; i++)
            {
                var group = activeGroups[i];
                var conditions = group.GetActiveConditions();

                // 处理组内条件
                for (int j = 0; j < conditions.Count; j++)
                {
                    var condition = conditions[j];

                    if (j == 0 && filters.Count > 1 && i > 0)
                    {
                        // 为新的 OR 分支创建新过滤器
                        if (group.GroupOperator == LogicOperator.Or)
                        {
                            currentFilter = new BeatSaverSearchFilter();
                            filters.Add(currentFilter);
                        }
                    }

                    // 如果条件是 OR，需要创建新的过滤器
                    if (condition.Operator == LogicOperator.Or && j > 0)
                    {
                        // 复制当前过滤器的状态
                        var newFilter = CloneFilter(currentFilter);
                        // 清除当前条件在新过滤器中的影响（由后续条件添加）
                        filters.Add(newFilter);
                        currentFilter = newFilter;
                    }

                    ApplyConditionToFilter(currentFilter, condition);
                }

                // 检查下一个组的运算符
                if (i < activeGroups.Count - 1)
                {
                    var nextGroup = activeGroups[i + 1];
                    if (nextGroup.GroupOperator == LogicOperator.Or)
                    {
                        // 准备创建新的 OR 分支
                        currentFilter = new BeatSaverSearchFilter();
                        filters.Add(currentFilter);
                    }
                }
            }

            return filters;
        }

        /// <summary>
        /// 克隆搜索过滤器
        /// </summary>
        private BeatSaverSearchFilter CloneFilter(BeatSaverSearchFilter source)
        {
            return new BeatSaverSearchFilter
            {
                Query = source.Query,
                Order = source.Order,
                MinBpm = source.MinBpm,
                MaxBpm = source.MaxBpm,
                MinNps = source.MinNps,
                MaxNps = source.MaxNps,
                MinDuration = source.MinDuration,
                MaxDuration = source.MaxDuration,
                MinSsStars = source.MinSsStars,
                MaxSsStars = source.MaxSsStars,
                MinBlStars = source.MinBlStars,
                MaxBlStars = source.MaxBlStars,
                Chroma = source.Chroma,
                Noodle = source.Noodle,
                Me = source.Me,
                Cinema = source.Cinema,
                Vivify = source.Vivify,
                Automapper = source.Automapper,
                Leaderboard = source.Leaderboard,
                Curated = source.Curated,
                Verified = source.Verified
            };
        }

        /// <summary>
        /// 从筛选预设构建搜索过滤器
        /// </summary>
        private BeatSaverSearchFilter BuildSearchFilterFromPreset(FilterPreset preset)
        {
            var filter = new BeatSaverSearchFilter();

            if (preset == null) return filter;

            foreach (var group in preset.GetActiveGroups())
            {
                foreach (var condition in group.GetActiveConditions())
                {
                    ApplyConditionToFilter(filter, condition);
                }
            }

            return filter;
        }

        /// <summary>
        /// 将条件应用到搜索过滤器
        /// </summary>
        private void ApplyConditionToFilter(BeatSaverSearchFilter filter, FilterCondition condition)
        {
            if (condition.Value == null) return;

            switch (condition.Type)
            {
                case FilterConditionType.Custom:
                    // 自定义条件：将自定义名称和值组合作为额外搜索关键词
                    if (!string.IsNullOrWhiteSpace(condition.CustomName) && !string.IsNullOrWhiteSpace(condition.Value?.ToString()))
                    {
                        // 追加到现有查询中
                        filter.Query = string.IsNullOrWhiteSpace(filter.Query)
                            ? $"{condition.CustomName}:{condition.Value}"
                            : $"{filter.Query} {condition.CustomName}:{condition.Value}";
                    }
                    break;
                case FilterConditionType.Query:
                    filter.Query = condition.Value.ToString();
                    break;
                case FilterConditionType.Order:
                    filter.Order = condition.Value.ToString();
                    break;
                case FilterConditionType.MinBpm:
                    if (double.TryParse(condition.Value.ToString(), out double minBpm))
                        filter.MinBpm = minBpm;
                    break;
                case FilterConditionType.MaxBpm:
                    if (double.TryParse(condition.Value.ToString(), out double maxBpm))
                        filter.MaxBpm = maxBpm;
                    break;
                case FilterConditionType.MinNps:
                    if (double.TryParse(condition.Value.ToString(), out double minNps))
                        filter.MinNps = minNps;
                    break;
                case FilterConditionType.MaxNps:
                    if (double.TryParse(condition.Value.ToString(), out double maxNps))
                        filter.MaxNps = maxNps;
                    break;
                case FilterConditionType.MinDuration:
                    if (int.TryParse(condition.Value.ToString(), out int minDur))
                        filter.MinDuration = minDur;
                    break;
                case FilterConditionType.MaxDuration:
                    if (int.TryParse(condition.Value.ToString(), out int maxDur))
                        filter.MaxDuration = maxDur;
                    break;
                case FilterConditionType.MinSsStars:
                    if (double.TryParse(condition.Value.ToString(), out double minSs))
                        filter.MinSsStars = minSs;
                    break;
                case FilterConditionType.MaxSsStars:
                    if (double.TryParse(condition.Value.ToString(), out double maxSs))
                        filter.MaxSsStars = maxSs;
                    break;
                case FilterConditionType.MinBlStars:
                    if (double.TryParse(condition.Value.ToString(), out double minBl))
                        filter.MinBlStars = minBl;
                    break;
                case FilterConditionType.MaxBlStars:
                    if (double.TryParse(condition.Value.ToString(), out double maxBl))
                        filter.MaxBlStars = maxBl;
                    break;
                case FilterConditionType.Chroma:
                    filter.Chroma = Convert.ToBoolean(condition.Value);
                    break;
                case FilterConditionType.Noodle:
                    filter.Noodle = Convert.ToBoolean(condition.Value);
                    break;
                case FilterConditionType.Me:
                    filter.Me = Convert.ToBoolean(condition.Value);
                    break;
                case FilterConditionType.Cinema:
                    filter.Cinema = Convert.ToBoolean(condition.Value);
                    break;
                case FilterConditionType.Vivify:
                    filter.Vivify = Convert.ToBoolean(condition.Value);
                    break;
                case FilterConditionType.Automapper:
                    var autoVal = condition.Value.ToString();
                    if (autoVal == "仅AI谱")
                        filter.Automapper = true;
                    else if (autoVal == "排除AI谱")
                        filter.Automapper = false;
                    break;
                case FilterConditionType.Leaderboard:
                    filter.Leaderboard = condition.Value.ToString();
                    break;
                case FilterConditionType.Curated:
                    filter.Curated = Convert.ToBoolean(condition.Value);
                    break;
                case FilterConditionType.Verified:
                    filter.Verified = Convert.ToBoolean(condition.Value);
                    break;
            }
        }

        #endregion

        #region 选择和导出功能

        private void BtnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.Cells["DB_Select"].Value = true;
            }
        }

        private void BtnSelectInverse_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.Cells["DB_Select"].Value = !(row.Cells["DB_Select"].Value as bool? ?? false);
            }
        }

        private void BtnExportSelected_Click(object sender, EventArgs e)
        {
            var selectedMaps = GetSelectedMaps();
            if (selectedMaps.Count == 0)
            {
                MessageBox.Show("请先选择要导出的歌曲！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ExportMapsToPlaylist(selectedMaps);
        }

        private void BtnExportAll_Click(object sender, EventArgs e)
        {
            if (currentSearchResults.Count == 0)
            {
                MessageBox.Show("当前没有搜索结果！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _ = ExportAllPagesToPlaylistAsync();
        }

        /// <summary>
        /// 异步获取所有页面结果并导出到歌单
        /// </summary>
        private async Task ExportAllPagesToPlaylistAsync()
        {
            // OR 模式下，结果已经全部缓存
            if (isOrModeSearch && allOrSearchResults.Count > 0)
            {
                var result = MessageBox.Show(
                    $"当前共 {allOrSearchResults.Count} 个结果（OR 模式）\n" +
                    "是否导出全部？",
                    "导出全部确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    ExportMapsToPlaylist(allOrSearchResults);
                }
                return;
            }

            // 如果只有一页，直接导出当前结果
            if (totalPages <= 1)
            {
                ExportMapsToPlaylist(currentSearchResults);
                return;
            }

            var confirmResult = MessageBox.Show(
                $"当前共 {totalPages} 页结果\n" +
                $"导出全部将从第一页开始重新请求所有页面\n" +
                "是否继续？",
                "导出全部确认",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmResult != DialogResult.Yes)
                return;

            try
            {
                BSIMMActionText.Text = "正在获取所有结果...";
                btnExportAll.Enabled = false;
                btnExportSelected.Enabled = false;

                var allMaps = new List<BeatSaverMap>();
                var filter = BuildSearchFilterFromPreset(currentFilterPreset);

                // 从第一页开始请求所有页面
                for (int page = 0; page < totalPages; page++)
                {
                    BSIMMActionText.Text = $"正在获取第 {page + 1}/{totalPages} 页...";

                    var response = await beatSaverClient.SearchMapsAsync(filter, page);
                    if (response?.Maps != null && response.Maps.Count > 0)
                    {
                        allMaps.AddRange(response.Maps);
                    }

                    // 更新进度条
                    BSIMMProgressUpdate((page + 1) * 100 / totalPages);

                    // 添加小延迟避免请求过快
                    if (page < totalPages - 1)
                        await Task.Delay(150);
                }

                BSIMMActionText.Text = $"已获取 {allMaps.Count} 首歌曲";
                debugLog($"导出全部：成功获取 {allMaps.Count} 首歌曲（共 {totalPages} 页）");

                // 导出所有结果
                ExportMapsToPlaylist(allMaps);
            }
            catch (Exception ex)
            {
                debugLog($"导出全部失败: {ex.Message}");
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                BSIMMActionText.Text = "导出失败";
            }
            finally
            {
                btnExportAll.Enabled = true;
                btnExportSelected.Enabled = true;
            }
        }

        /// <summary>
        /// 获取选中的谱面列表
        /// </summary>
        private List<BeatSaverMap> GetSelectedMaps()
        {
            var selectedMaps = new List<BeatSaverMap>();
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["DB_Select"].Value as bool? == true && row.Tag is BeatSaverMap map)
                {
                    selectedMaps.Add(map);
                }
            }
            return selectedMaps;
        }

        /// <summary>
        /// 将谱面列表导出为歌单
        /// </summary>
        private void ExportMapsToPlaylist(List<BeatSaverMap> maps)
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Beat Saber Playlist (*.bplist)|*.bplist";
                saveDialog.Title = "保存歌单";
                saveDialog.FileName = "新歌单";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string playlistName = System.IO.Path.GetFileNameWithoutExtension(saveDialog.FileName);

                        // 生成带有文件名的封面图片
                        Image coverImage = GeneratePlaylistCover(playlistName);
                        string imgBytes = "data:image/jpg;base64," + ImageToBase64(coverImage, ImageFormat.Jpeg);

                        // 使用与曲包导出一致的格式
                        string author = Environment.UserName + "使用BSIMM@万毒不侵 生成";
                        string description = "本歌单由" + Environment.UserName + "使用BSIMM生成\r\nBSIMM由万毒不侵开发，开源且免费，如果你是购买的请要求商家退款\r\n项目地址：https://github.com/cyjyyd/Beat-Saber-Independent-Maps-Manager";

                        JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                        PlayList playlist = new PlayList(playlistName, author, description, imgBytes);

                        foreach (var map in maps)
                        {
                            string hash = map.GetHash();
                            if (!string.IsNullOrEmpty(hash))
                            {
                                playlist.AddSongHash(hash);
                            }
                        }

                        string json = JsonConvert.SerializeObject(playlist, Formatting.None, settings);
                        File.WriteAllText(saveDialog.FileName, json);

                        MessageBox.Show($"歌单已保存！\n共 {maps.Count} 首歌曲", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        debugLog($"歌单已导出: {saveDialog.FileName}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        debugLog($"歌单导出失败: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 生成歌单封面图片（带有歌单名称）
        /// </summary>
        private Image GeneratePlaylistCover(string playlistName)
        {
            int width = 256;
            int height = 256;
            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // 背景渐变
                using (System.Drawing.Drawing2D.LinearGradientBrush brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, width, height),
                    Color.FromArgb(64, 64, 128),
                    Color.FromArgb(32, 32, 64),
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                {
                    g.FillRectangle(brush, 0, 0, width, height);
                }

                // 绘制歌单名称
                using (Font font = new Font("Microsoft YaHei UI", 14f, FontStyle.Bold))
                {
                    StringFormat format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    // 自动换行绘制文本
                    RectangleF textRect = new RectangleF(10, 10, width - 20, height - 20);
                    g.DrawString(playlistName, font, Brushes.White, textRect, format);
                }

                // 底部添加BSIMM标识
                using (Font smallFont = new Font("Microsoft YaHei UI", 8f))
                {
                    StringFormat format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Far
                    };
                    g.DrawString("BSIMM", smallFont, Brushes.Gray, new RectangleF(0, height - 30, width, 25), format);
                }
            }

            return bitmap;
        }

        #endregion
    }
}


