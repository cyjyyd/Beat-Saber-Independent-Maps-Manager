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
        private bool isLocalCacheSearch = false; // 是否是本地缓存搜索模式
        private List<BeatSaverMap> allOrSearchResults = new List<BeatSaverMap>(); // OR 模式下缓存的所有结果
        private System.Threading.CancellationTokenSource searchCts; // 用于取消搜索任务
        private System.Threading.CancellationTokenSource imageLoadCts; // 用于取消图片加载任务
        private static readonly System.Net.Http.HttpClient imageHttpClient = new System.Net.Http.HttpClient(); // 静态HttpClient用于图片加载
        private Dictionary<string, Image> coverImageCache = new Dictionary<string, Image>(); // 图片缓存
        // 筛选构建器相关
        private FilterPreset currentFilterPreset;
        private FilterBuilderForm filterBuilderForm;
        // 本地缓存管理器
        private LocalCacheManager localCacheManager;
        // 更新管理器
        private UpdateManager updateManager;
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
            this.Text = "BSIMM-独立曲包管理/编辑器 " + AppVersion.VersionString + " " + author;
            debugLog("程序日志将自动同步到程序目录：" + Application.StartupPath + "BSIMM-" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
            if (!Directory.Exists(Application.StartupPath + "assets"))
            {
                Directory.CreateDirectory(Application.StartupPath + "assets");
                Directory.CreateDirectory(Application.StartupPath + "assets\\json");
                Directory.CreateDirectory(Application.StartupPath + "assets\\temp");
                Directory.CreateDirectory(Application.StartupPath + "assets\\scripts");
            }
            //双语的适配以后再做
            Thread update = new Thread(updateDetect) { IsBackground = true };
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
                Thread detect = new Thread(detectMultiBeatSaberInstance) { IsBackground = true };
                detect.Start();
            }
            else
            {
                Thread detect = new Thread(detectSingleBeatSaberInstance) { IsBackground = true };
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

            try
            {
                updateManager = new UpdateManager(config);
                var updateInfo = updateManager.CheckForUpdateAsync().Result;

                if (updateInfo != null)
                {
                    debugLog($"发现新版本: {updateInfo.NewVersion}");
                    ShowUpdateDialog(updateInfo);
                }
                else
                {
                    debugLog("当前已是最新版本");
                }
            }
            catch (Exception ex)
            {
                debugLog($"检查更新失败: {ex.Message}");
            }

            // 更新最后检查时间
            config.LastUpdateCheck = DateTime.Now;
            config.configUpdate();
        }

        private void ShowUpdateDialog(UpdateInfo updateInfo)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowUpdateDialog(updateInfo)));
                return;
            }

            // 构建更新提示信息
            string message = $"发现新版本 {updateInfo.NewVersion}\n\n";
            message += $"当前版本: {updateInfo.CurrentVersion}\n";
            message += $"更新大小: {updateInfo.GetFormattedSize()}\n\n";

            if (!string.IsNullOrWhiteSpace(updateInfo.ReleaseNotes))
            {
                // 限制 Release Notes 长度
                string notes = updateInfo.ReleaseNotes;
                if (notes.Length > 500)
                    notes = notes.Substring(0, 500) + "...";
                message += $"更新内容:\n{notes}\n\n";
            }

            message += "是否立即更新?";

            DialogResult result = MessageBox.Show(
                message,
                "发现新版本",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1
            );

            if (result == DialogResult.Yes)
            {
                StartUpdate(updateInfo);
            }
            else if (result == DialogResult.Cancel)
            {
                // 跳过此版本
                updateManager.SkipVersion(updateInfo.NewVersion);
                debugLog($"已跳过版本 {updateInfo.NewVersion}");
            }
        }

        private void StartUpdate(UpdateInfo updateInfo)
        {
            // 显示进度对话框
            Form progressForm = new Form
            {
                Text = "正在更新",
                Size = new Size(400, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent
            };

            ProgressBar progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Continuous,
                Dock = DockStyle.Top,
                Height = 30
            };

            Label statusLabel = new Label
            {
                Text = "正在下载更新...",
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            };

            progressForm.Controls.Add(progressBar);
            progressForm.Controls.Add(statusLabel);

            // 在后台线程执行更新
            Task.Run(async () =>
            {
                var progress = new Progress<int>(percent =>
                {
                    if (progressForm.InvokeRequired)
                    {
                        progressForm.Invoke(new Action(() =>
                        {
                            progressBar.Value = percent;
                            statusLabel.Text = $"正在下载更新... {percent}%";
                        }));
                    }
                });

                bool success = await updateManager.DownloadAndApplyUpdateAsync(updateInfo, progress);

                if (success)
                {
                    progressForm.Invoke(new Action(() =>
                    {
                        progressForm.Close();
                        MessageBox.Show(
                            "更新完成，程序将重启。",
                            "更新完成",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                        updateManager.RestartApplication();
                    }));
                }
                else
                {
                    progressForm.Invoke(new Action(() =>
                    {
                        progressForm.Close();
                        MessageBox.Show(
                            "更新失败，请稍后重试或手动下载更新。",
                            "更新失败",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }));
                }
            });

            progressForm.ShowDialog();
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
                    new Thread(() => displaySongList(musicPackName)) { IsBackground = true }.Start();
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
            this.Close();
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

            // 取消所有后台任务
            try
            {
                // 取消静态 HttpClient 的待处理请求
                imageHttpClient.CancelPendingRequests();

                if (searchCts != null && !searchCts.IsCancellationRequested)
                {
                    searchCts.Cancel();
                }

                if (imageLoadCts != null && !imageLoadCts.IsCancellationRequested)
                {
                    imageLoadCts.Cancel();
                }

                // 停止音频播放
                if (waveOut != null)
                {
                    waveOut.Stop();
                    waveOut.Dispose();
                    waveOut = null;
                }

                if (currentAudioReader != null)
                {
                    currentAudioReader.Dispose();
                    currentAudioReader = null;
                }

                // 释放本地缓存管理器
                if (localCacheManager != null)
                {
                    localCacheManager.Dispose();
                    localCacheManager = null;
                }

                // 释放 BeatSaver 客户端
                if (beatSaverClient != null)
                {
                    beatSaverClient.Dispose();
                    beatSaverClient = null;
                }

                // 释放更新管理器
                if (updateManager != null)
                {
                    updateManager.Dispose();
                    updateManager = null;
                }

                // 释放筛选构建器窗体
                if (filterBuilderForm != null && !filterBuilderForm.IsDisposed)
                {
                    filterBuilderForm.Close();
                    filterBuilderForm.Dispose();
                    filterBuilderForm = null;
                }

                // 释放图片缓存
                foreach (var img in coverImageCache.Values)
                {
                    img?.Dispose();
                }
                coverImageCache.Clear();

                // 释放曲包封面图片
                foreach (var img in musicPackCoverimgs.Values)
                {
                    img?.Dispose();
                }
                musicPackCoverimgs.Clear();
            }
            catch { }
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
                        // 使用倒序循环避免"Collection was modified"错误
                        for (int i = songListView.Items.Count - 1; i >= 0; i--)
                        {
                            ListViewItem list = songListView.Items[i];
                            if (isDuplicate(list.SubItems[1].Text))
                            {
                                string currentFolder = musicPackInfo[currentMusicPack][list.SubItems[1].Text].songFolder;
                                try
                                {
                                    Directory.Delete(currentFolder, true);
                                    debugLog("删除歌曲：" + list.SubItems[1].Text + " 所在文件夹：" + currentFolder + "成功！");
                                    songListView.Items.RemoveAt(i);
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
                new Thread(() => addFolder(folder)) { IsBackground = true }.Start();
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
        /// 显示搜索结果（支持懒加载和取消）
        /// </summary>
        private async Task DisplaySearchResults(List<BeatSaverMap> maps)
        {
            // 取消之前的图片加载任务
            if (imageLoadCts != null && !imageLoadCts.IsCancellationRequested)
            {
                imageLoadCts.Cancel();
                imageLoadCts.Dispose();
            }
            imageLoadCts = new System.Threading.CancellationTokenSource();
            var cancellationToken = imageLoadCts.Token;

            // 先快速添加所有行的基本信息（不含图片）
            for (int i = 0; i < maps.Count; i++)
            {
                var map = maps[i];
                var row = new DataGridViewRow();
                row.CreateCells(dataGridView1);

                // 列顺序: 0=Select, 1=bsr, 2=Cover, 3=Name, 4=description, 5=bpm, 6=levelAuthorName
                row.Cells[0].Value = false;  // 选择框默认不选中
                row.Cells[1].Value = map.Id;  // bsr
                row.Cells[2].Value = null;  // 封面图片稍后加载
                row.Cells[3].Value = map.Name;  // 名称
                row.Cells[4].Value = map.Description;  // 简介
                row.Cells[5].Value = map.Metadata?.Bpm.ToString("F1") ?? "N/A";  // BPM
                row.Cells[6].Value = map.Metadata?.LevelAuthorName ?? map.Uploader?.Name ?? "N/A";  // 谱面作者

                row.Tag = map;  // 存储完整数据
                dataGridView1.Rows.Add(row);
            }

            // 异步加载封面图片
            await LoadCoverImagesAsync(maps, cancellationToken);
        }

        /// <summary>
        /// 异步加载封面图片（带缓存和取消支持）
        /// </summary>
        private async Task LoadCoverImagesAsync(List<BeatSaverMap> maps, System.Threading.CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            for (int i = 0; i < maps.Count && i < dataGridView1.Rows.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var map = maps[i];
                var row = dataGridView1.Rows[i];
                var coverUrl = map.GetCoverUrl();

                if (string.IsNullOrEmpty(coverUrl))
                    continue;

                // 检查缓存
                if (coverImageCache.TryGetValue(coverUrl, out var cachedImage))
                {
                    if (!row.IsNewRow && row.Cells.Count > 2)
                    {
                        row.Cells[2].Value = cachedImage;
                    }
                    continue;
                }

                // 捕获当前索引用于闭包
                int rowIndex = i;
                string url = coverUrl;

                var task = Task.Run(async () =>
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    try
                    {
                        var imageData = await imageHttpClient.GetByteArrayAsync(url, cancellationToken);
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        Image? img = null;
                        using (var ms = new MemoryStream(imageData))
                        {
                            var originalImg = Image.FromStream(ms);
                            img = new Bitmap(originalImg, new Size(100, 100));
                        }

                        // 添加到缓存
                        lock (coverImageCache)
                        {
                            if (!coverImageCache.ContainsKey(url) && coverImageCache.Count < 500) // 限制缓存大小
                            {
                                coverImageCache[url] = img;
                            }
                        }

                        // 更新UI
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.BeginInvoke(() =>
                            {
                                if (this.IsDisposed) return;
                                if (rowIndex < dataGridView1.Rows.Count && !dataGridView1.Rows[rowIndex].IsNewRow)
                                {
                                    var targetRow = dataGridView1.Rows[rowIndex];
                                    if (targetRow.Cells.Count > 2)
                                    {
                                        targetRow.Cells[2].Value = img;
                                    }
                                }
                            });
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 取消，忽略
                    }
                    catch
                    {
                        // 加载失败，忽略
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            // 等待所有图片加载完成（或被取消）
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                // 被取消，忽略
            }
        }

        /// <summary>
        /// 更新分页控件状态
        /// </summary>
        private void UpdatePaginationControls()
        {
            btnPrevPage.Enabled = currentPage > 0;
            btnNextPage.Enabled = currentPage < totalPages - 1;
            // 只有真正是OR逻辑搜索时才显示(OR)，本地缓存搜索不显示
            if (isOrModeSearch && !isLocalCacheSearch)
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
            btnBatchOutput.Click += BtnBatchOutput_Click;

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
            // 取消之前的搜索任务
            if (searchCts != null && !searchCts.IsCancellationRequested)
            {
                searchCts.Cancel();
                searchCts.Dispose();
            }
            searchCts = new System.Threading.CancellationTokenSource();

            currentFilterPreset = preset;
            UpdateFilterSummary();
            currentPage = 0;
            // 清除之前的 OR 搜索缓存
            isOrModeSearch = false;
            isLocalCacheSearch = false;
            allOrSearchResults.Clear();

            try
            {
                await SearchMapsWithFilter(preset, searchCts.Token);
            }
            catch (OperationCanceledException)
            {
                BSIMMActionText.Text = "搜索已取消";
            }
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
        private async Task SearchMapsWithFilter(FilterPreset preset, System.Threading.CancellationToken cancellationToken = default)
        {
            try
            {
                BSIMMActionText.Text = "搜索中...";
                dataGridView1.Rows.Clear();
                currentSearchResults.Clear();

                // 检查是否需要本地缓存
                if (RequiresLocalCache(preset))
                {
                    await SearchWithLocalCache(preset, cancellationToken);
                    return;
                }

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
                    isLocalCacheSearch = false;
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
        /// 检查预设是否需要本地缓存
        /// </summary>
        private bool RequiresLocalCache(FilterPreset preset)
        {
            if (preset == null) return false;

            foreach (var group in preset.GetActiveGroups())
            {
                // Check if group has UseLocalCache enabled
                if (group.UseLocalCache)
                    return true;

                // Also check individual conditions for backward compatibility
                foreach (var condition in group.GetActiveConditions())
                {
                    if (FilterConditionMetadata.RequiresLocalCache(condition.Type))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 使用本地缓存执行搜索
        /// </summary>
        private async Task SearchWithLocalCache(FilterPreset preset, System.Threading.CancellationToken cancellationToken = default)
        {
            try
            {
                // 初始化本地缓存管理器
                if (localCacheManager == null)
                    localCacheManager = new LocalCacheManager();

                // 检查缓存是否可用
                if (!localCacheManager.IsCacheAvailable)
                {
                    // 提示用户下载缓存
                    var result = MessageBox.Show(
                        "本地缓存未下载或已过期。缓存文件约230MB，是否立即下载？\n\n下载后可使用更丰富的筛选条件（排行榜收录、统计数据等）",
                        "需要本地缓存",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result != DialogResult.Yes)
                    {
                        BSIMMStatusUpdate("取消", "已取消", 0);
                        return;
                    }

                    // 下载缓存
                    localCacheManager.DownloadProgress += (s, progress) =>
                    {
                        this.BeginInvoke(() =>
                        {
                            if (this.IsDisposed) return;
                            BSIMMProgress.ProgressBar.Value = Math.Min(100, (int)progress.Percentage);
                            BSIMMActionText.Text = "下载";
                            BSIMMStatusText.Text = progress.Status.StartsWith("正在下载")
                                ? $"{progress.Status} {progress.Percentage:F1}%"
                                : progress.Status;
                        });
                    };

                    BSIMMStatusUpdate("下载", "正在下载本地缓存...", 0);
                    bool downloaded = await localCacheManager.DownloadCacheAsync();

                    if (!downloaded)
                    {
                        MessageBox.Show("无法下载本地缓存，请检查网络连接", "下载失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        BSIMMStatusUpdate("错误", "下载失败", 0);
                        return;
                    }
                }

                BSIMMActionText.Text = "正在筛选本地缓存...";
                BSIMMStatusText.Text = "读取本地缓存...";
                currentPage = 0;
                isOrModeSearch = true; // 本地缓存模式使用分页显示（复用OR模式的分页逻辑）
                isLocalCacheSearch = true; // 标记为本地缓存搜索

                List<BeatSaverMap> results;

                // 检查是否有数量限制条件
                bool hasResultLimit = preset.HasResultLimit();

                if (hasResultLimit)
                {
                    // 使用 ParallelFilterMaps 方法（支持数量限制和排序）
                    results = await Task.Run(() =>
                    {
                        var progress = new Progress<int>(percent =>
                        {
                            this.BeginInvoke(() =>
                            {
                                if (this.IsDisposed) return;
                                BSIMMProgress.ProgressBar.Value = percent;
                                BSIMMActionText.Text = "搜索";
                                BSIMMStatusText.Text = $"正在筛选... {percent}%";
                            });
                        });
                        return localCacheManager.ParallelFilterMaps(preset, progress, cancellationToken);
                    }, cancellationToken);
                }
                else
                {
                    // 使用流式筛选（更快，更省内存）
                    results = await Task.Run(() =>
                    {
                        var tempList = new List<BeatSaverMap>();
                        int lastPercent = 0;
                        int processedCount = 0;

                        foreach (var map in localCacheManager.StreamFilterMaps(preset, null, cancellationToken))
                        {
                            if (cancellationToken.IsCancellationRequested)
                                break;

                            tempList.Add(map);
                            processedCount++;

                            // 每5000首报告一次进度
                            if (processedCount % 5000 == 0)
                            {
                                int percent = Math.Min(95, processedCount / 500); // 估算进度
                                if (percent > lastPercent)
                                {
                                    lastPercent = percent;
                                    this.BeginInvoke(() =>
                                    {
                                        if (this.IsDisposed) return;
                                        BSIMMProgress.ProgressBar.Value = percent;
                                        BSIMMActionText.Text = "搜索";
                                        BSIMMStatusText.Text = $"正在筛选... 已处理 {processedCount} 首，找到 {tempList.Count} 首";
                                    });
                                }
                            }
                        }

                        return tempList;
                    }, cancellationToken);
                }

                // 检查是否被取消
                if (cancellationToken.IsCancellationRequested)
                {
                    BSIMMActionText.Text = "搜索已取消";
                    return;
                }

                // 更新UI
                BSIMMProgress.ProgressBar.Value = 100;
                allOrSearchResults = results;

                if (results.Count > 0)
                {
                    totalPages = (results.Count + PageSize - 1) / PageSize;
                    BSIMMStatusUpdate("搜索", $"找到 {results.Count} 个结果", 100);

                    UpdatePaginationControls();
                    await DisplayOrSearchPage();
                }
                else
                {
                    BSIMMStatusUpdate("搜索", "未找到匹配的结果", 100);
                    btnPrevPage.Enabled = false;
                    btnNextPage.Enabled = false;
                    lblPageInfo.Text = "第 0/0 页";
                }
            }
            catch (OperationCanceledException)
            {
                BSIMMActionText.Text = "搜索已取消";
            }
            catch (Exception ex)
            {
                debugLog($"本地缓存筛选失败: {ex.Message}");
                MessageBox.Show($"筛选失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                BSIMMStatusUpdate("错误", "筛选失败", 0);
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
            isLocalCacheSearch = false; // OR逻辑搜索不是本地缓存搜索
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
                    // Handle SearchQueryValue with field type
                    if (condition.Value is SearchQueryValue queryValue)
                        filter.Query = queryValue.ToApiQuery();
                    else
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
            // 尝试从当前预设名称中提取封面文字
            string coverText = ExtractCoverTextFromPresetName(currentFilterPreset?.Name);
            ExportMapsToPlaylist(selectedMaps, null, coverText);
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
        /// 批处理按钮点击事件：选择预设 -> 筛选 -> 导出到歌单
        /// </summary>
        private async void BtnBatchOutput_Click(object sender, EventArgs e)
        {
            // 创建批处理窗口
            using (var batchForm = new Form())
            {
                batchForm.Text = "批处理导出";
                batchForm.ClientSize = new Size(500, 450);
                batchForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                batchForm.StartPosition = FormStartPosition.CenterParent;
                batchForm.MaximizeBox = false;
                batchForm.MinimizeBox = false;

                // 提示标签
                var lblTip = new Label
                {
                    Text = "提示：预设名称中可使用【】标记封面文字，如\"NPS6+【NPS6】\"将在封面显示\"NPS6\"",
                    Location = new Point(10, 10),
                    Size = new Size(480, 40),
                    ForeColor = Color.Gray
                };

                // 预设列表
                var lblPresets = new Label
                {
                    Text = "选择预设（可多选）：",
                    Location = new Point(10, 55),
                    Size = new Size(200, 20)
                };

                var chkPresets = new CheckedListBox
                {
                    Location = new Point(10, 80),
                    Size = new Size(470, 200),
                    CheckOnClick = true
                };

                // 加载预设
                string presetDir = Path.Combine(Application.StartupPath, "presets");
                var availablePresets = new List<FilterPreset>();
                if (Directory.Exists(presetDir))
                {
                    foreach (var file in Directory.GetFiles(presetDir, "*.bsf"))
                    {
                        var preset = FilterPreset.LoadFromFile(file);
                        if (preset != null && preset.Groups != null && preset.Groups.Count > 0)
                        {
                            availablePresets.Add(preset);
                            chkPresets.Items.Add(preset.Name, false);
                        }
                    }
                }

                // 导入预设按钮
                var btnImport = new Button
                {
                    Text = "导入预设文件...",
                    Location = new Point(10, 290),
                    Size = new Size(120, 30)
                };

                // 导入的预设列表
                var importedPresets = new List<FilterPreset>();

                btnImport.Click += (s, args) =>
                {
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Title = "导入筛选预设（支持多选）";
                        ofd.Filter = "筛选预设文件 (*.bsf)|*.bsf|JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*";
                        ofd.Multiselect = true;

                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            foreach (string filePath in ofd.FileNames)
                            {
                                try
                                {
                                    var preset = FilterPreset.LoadFromFile(filePath);
                                    if (preset != null && preset.Groups != null)
                                    {
                                        importedPresets.Add(preset);
                                        chkPresets.Items.Add($"[导入] {preset.Name}", true);
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                };

                // 全选按钮
                var btnSelectAll = new Button
                {
                    Text = "全选",
                    Location = new Point(140, 290),
                    Size = new Size(60, 30)
                };
                btnSelectAll.Click += (s, args) =>
                {
                    for (int i = 0; i < chkPresets.Items.Count; i++)
                    {
                        chkPresets.SetItemChecked(i, true);
                    }
                };

                // 反选按钮
                var btnSelectInverse = new Button
                {
                    Text = "反选",
                    Location = new Point(210, 290),
                    Size = new Size(60, 30)
                };
                btnSelectInverse.Click += (s, args) =>
                {
                    for (int i = 0; i < chkPresets.Items.Count; i++)
                    {
                        chkPresets.SetItemChecked(i, !chkPresets.GetItemChecked(i));
                    }
                };

                // 输出目录选择
                var lblOutput = new Label
                {
                    Text = "输出目录：",
                    Location = new Point(10, 330),
                    Size = new Size(80, 20)
                };

                var txtOutputDir = new TextBox
                {
                    Location = new Point(90, 330),
                    Size = new Size(300, 25),
                    Text = Path.Combine(Application.StartupPath, "playlists")
                };

                var btnBrowse = new Button
                {
                    Text = "浏览...",
                    Location = new Point(400, 328),
                    Size = new Size(80, 28)
                };

                btnBrowse.Click += (s, args) =>
                {
                    using (var fbd = new FolderBrowserDialog())
                    {
                        fbd.Description = "选择歌单输出目录";
                        fbd.ShowNewFolderButton = true;
                        if (fbd.ShowDialog() == DialogResult.OK)
                        {
                            txtOutputDir.Text = fbd.SelectedPath;
                        }
                    }
                };

                // 开始导出按钮
                var btnStart = new Button
                {
                    Text = "开始导出",
                    Location = new Point(130, 380),
                    Size = new Size(120, 40),
                    BackColor = Color.FromArgb(46, 139, 87),
                    ForeColor = Color.White,
                    DialogResult = DialogResult.OK
                };

                // 取消按钮
                var btnCancel = new Button
                {
                    Text = "取消",
                    Location = new Point(270, 380),
                    Size = new Size(100, 40),
                    DialogResult = DialogResult.Cancel
                };

                batchForm.Controls.Add(lblTip);
                batchForm.Controls.Add(lblPresets);
                batchForm.Controls.Add(chkPresets);
                batchForm.Controls.Add(btnImport);
                batchForm.Controls.Add(btnSelectAll);
                batchForm.Controls.Add(btnSelectInverse);
                batchForm.Controls.Add(lblOutput);
                batchForm.Controls.Add(txtOutputDir);
                batchForm.Controls.Add(btnBrowse);
                batchForm.Controls.Add(btnStart);
                batchForm.Controls.Add(btnCancel);

                if (batchForm.ShowDialog() == DialogResult.OK)
                {
                    // 获取选中的预设
                    var selectedPresets = new List<FilterPreset>();
                    for (int i = 0; i < chkPresets.Items.Count; i++)
                    {
                        if (chkPresets.GetItemChecked(i))
                        {
                            if (i < availablePresets.Count)
                            {
                                selectedPresets.Add(availablePresets[i]);
                            }
                            else
                            {
                                int importedIndex = i - availablePresets.Count;
                                if (importedIndex < importedPresets.Count)
                                {
                                    selectedPresets.Add(importedPresets[importedIndex]);
                                }
                            }
                        }
                    }

                    if (selectedPresets.Count == 0)
                    {
                        MessageBox.Show("请至少选择一个预设！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // 开始批处理
                    await BatchExportPresets(selectedPresets, txtOutputDir.Text);
                }
            }
        }

        /// <summary>
        /// 批量导出预设到歌单
        /// </summary>
        private async Task BatchExportPresets(List<FilterPreset> presets, string outputDir)
        {
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            int successCount = 0;
            int failCount = 0;
            int zeroResultCount = 0;

            BSIMMActionText.Text = "批处理导出中...";
            btnBatchOutput.Enabled = false;

            // 检查是否所有预设都需要本地缓存
            bool allRequireLocalCache = presets.All(p => RequiresLocalCache(p));
            bool useSharedCache = false;

            try
            {
                // 如果所有预设都需要本地缓存，预加载一次
                if (allRequireLocalCache && localCacheManager != null && localCacheManager.IsCacheAvailable)
                {
                    BSIMMStatusText.Text = "预加载本地缓存...";
                    useSharedCache = true;

                    await Task.Run(() =>
                    {
                        var progress = new Progress<int>(percent =>
                        {
                            this.BeginInvoke(() =>
                            {
                                if (this.IsDisposed) return;
                                BSIMMProgress.ProgressBar.Value = percent / 2; // 预加载占一半进度
                                BSIMMStatusText.Text = $"预加载缓存... {percent}%";
                            });
                        });
                        localCacheManager.InitializeSharedReader(true, progress);
                    });
                }

                for (int i = 0; i < presets.Count; i++)
                {
                    var preset = presets[i];
                    try
                    {
                        int overallProgress = useSharedCache ? 50 + (i * 50 / presets.Count) : (i * 100 / presets.Count);
                        BSIMMStatusText.Text = $"正在处理 ({i + 1}/{presets.Count}): {preset.Name}";
                        BSIMMProgress.ProgressBar.Value = overallProgress;

                        // 执行搜索获取所有结果
                        var maps = await FetchAllMapsForPreset(preset, useSharedCache);

                        if (maps.Count == 0)
                        {
                            debugLog($"批处理 - {preset.Name}: 无结果");
                            zeroResultCount++;
                            continue;
                        }

                        // 提取封面文字
                        string coverText = ExtractCoverTextFromPresetName(preset.Name);

                        // 导出到歌单（静默模式）
                        string filePath = Path.Combine(outputDir, $"{SanitizeFileName(preset.Name)}.bplist");
                        bool success = ExportMapsToPlaylistInternal(maps, filePath, preset.Name, coverText, silent: true);

                        if (success)
                        {
                            debugLog($"批处理成功 - {preset.Name}: {maps.Count} 首歌曲");
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        debugLog($"批处理失败 - {preset.Name}: {ex.Message}");
                        failCount++;
                    }
                }
            }
            finally
            {
                // 释放共享缓存
                if (useSharedCache && localCacheManager != null)
                {
                    localCacheManager.ClearSharedReader();
                }
            }

            btnBatchOutput.Enabled = true;
            BSIMMActionText.Text = $"批处理完成: 成功 {successCount}, 失败 {failCount}, 无结果 {zeroResultCount}";

            // 显示结果
            string message = $"批处理导出完成\n\n成功: {successCount}\n失败: {failCount}\n无结果: {zeroResultCount}\n\n输出目录: {outputDir}";
            MessageBox.Show(message, "批处理结果", MessageBoxButtons.OK,
                successCount > 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        /// <summary>
        /// 获取预设对应的所有谱面（异步）
        /// </summary>
        private async Task<List<BeatSaverMap>> FetchAllMapsForPreset(FilterPreset preset, bool useSharedCache = false)
        {
            var allMaps = new List<BeatSaverMap>();

            try
            {
                // 检查是否需要本地缓存
                if (RequiresLocalCache(preset))
                {
                    // 使用本地缓存
                    if (localCacheManager == null)
                        localCacheManager = new LocalCacheManager();

                    if (!localCacheManager.IsCacheAvailable)
                    {
                        throw new Exception("需要本地缓存但未下载");
                    }

                    allMaps = await Task.Run(() =>
                    {
                        var progress = new Progress<int>(percent =>
                        {
                            this.BeginInvoke(() =>
                            {
                                if (this.IsDisposed) return;
                                BSIMMProgress.ProgressBar.Value = percent;
                            });
                        });

                        // 使用共享缓存或流式筛选
                        if (useSharedCache)
                        {
                            var results = new List<BeatSaverMapSlim>();
                            foreach (var map in localCacheManager.StreamFilterMapsShared(preset, null))
                            {
                                results.Add(map);
                            }
                            return results.Select(m => m.ToFullMap()).ToList();
                        }
                        else
                        {
                            return localCacheManager.ParallelFilterMaps(preset, progress);
                        }
                    });
                }
                else
                {
                    // 使用API搜索
                    var filter = BuildSearchFilterFromPreset(preset);
                    int page = 0;
                    int maxPages = 100; // 限制最大页数防止无限循环

                    while (page < maxPages)
                    {
                        var response = await beatSaverClient.SearchMapsAsync(filter, page);
                        if (response?.Maps == null || response.Maps.Count == 0)
                            break;

                        allMaps.AddRange(response.Maps);

                        // 检查是否还有下一页
                        int totalPages = 0;
                        if (response.Info != null)
                            totalPages = response.Info.Pages;
                        else if (response.Metadata != null)
                            totalPages = (response.Metadata.Total + response.Metadata.PageSize - 1) / response.Metadata.PageSize;

                        if (page >= totalPages - 1)
                            break;

                        page++;
                        await Task.Delay(150); // 避免请求过快
                    }
                }
            }
            catch (Exception ex)
            {
                debugLog($"FetchAllMapsForPreset失败: {ex.Message}");
                throw;
            }

            return allMaps;
        }

        /// <summary>
        /// 从预设名称中提取【】内的文字作为封面文字
        /// </summary>
        private string ExtractCoverTextFromPresetName(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
                return null;

            int start = presetName.IndexOf('【');
            int end = presetName.IndexOf('】');

            if (start >= 0 && end > start)
            {
                return presetName.Substring(start + 1, end - start - 1);
            }

            return null;
        }

        /// <summary>
        /// 异步获取所有页面结果并导出到歌单
        /// </summary>
        private async Task ExportAllPagesToPlaylistAsync()
        {
            // 尝试从当前预设名称中提取封面文字
            string coverText = ExtractCoverTextFromPresetName(currentFilterPreset?.Name);

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
                    ExportMapsToPlaylist(allOrSearchResults, null, coverText);
                }
                return;
            }

            // 如果只有一页，直接导出当前结果
            if (totalPages <= 1)
            {
                ExportMapsToPlaylist(currentSearchResults, null, coverText);
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
                ExportMapsToPlaylist(allMaps, null, coverText);
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
        private void ExportMapsToPlaylist(List<BeatSaverMap> maps, string playlistName = null, string coverText = null)
        {
            // 如果没有指定歌单名称，弹出保存对话框
            if (string.IsNullOrEmpty(playlistName))
            {
                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "Beat Saber Playlist (*.bplist)|*.bplist";
                    saveDialog.Title = "保存歌单";
                    saveDialog.FileName = "新歌单";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        string name = System.IO.Path.GetFileNameWithoutExtension(saveDialog.FileName);
                        ExportMapsToPlaylistInternal(maps, saveDialog.FileName, name, coverText);
                    }
                }
            }
            else
            {
                // 使用指定的歌单名称，保存到预设目录
                string presetDir = Path.Combine(Application.StartupPath, "playlists");
                if (!Directory.Exists(presetDir))
                    Directory.CreateDirectory(presetDir);

                string filePath = Path.Combine(presetDir, $"{SanitizeFileName(playlistName)}.bplist");
                ExportMapsToPlaylistInternal(maps, filePath, playlistName, coverText);
            }
        }

        /// <summary>
        /// 内部方法：将谱面列表导出为歌单
        /// </summary>
        private bool ExportMapsToPlaylistInternal(List<BeatSaverMap> maps, string filePath, string playlistName, string coverText = null, bool silent = false)
        {
            try
            {
                // 如果没有指定封面文字，使用歌单名称
                if (string.IsNullOrEmpty(coverText))
                    coverText = playlistName;

                // 生成带有文件名的封面图片
                Image coverImage = GeneratePlaylistCover(coverText);
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
                File.WriteAllText(filePath, json);

                if (!silent)
                {
                    MessageBox.Show($"歌单已保存！\n共 {maps.Count} 首歌曲\n保存位置：{filePath}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                debugLog($"歌单已导出: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                if (!silent)
                {
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                debugLog($"歌单导出失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 清理文件名中的非法字符
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
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
                // 使用默认背景图片
                try
                {
                    Image bgImage = Resources.默认;
                    g.DrawImage(bgImage, new Rectangle(0, 0, width, height));
                }
                catch
                {
                    // 如果加载失败，使用渐变背景
                    using (System.Drawing.Drawing2D.LinearGradientBrush brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new Rectangle(0, 0, width, height),
                        Color.FromArgb(64, 64, 128),
                        Color.FromArgb(32, 32, 64),
                        System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                    {
                        g.FillRectangle(brush, 0, 0, width, height);
                    }
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
                    g.DrawString("BSIMM自动生成@万毒不侵", smallFont, Brushes.Gray, new RectangleF(0, height - 30, width, 25), format);
                }
            }

            return bitmap;
        }

        #endregion
    }
}


