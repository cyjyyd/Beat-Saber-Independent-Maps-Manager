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
using NAudio.Wave;
namespace BeatSaberIndependentMapsManager
{
    public partial class MainForm : Form
    {
        private const int EVERYTHING_REQUEST_FILE_NAME = 0x00000001;
        private const int EVERYTHING_REQUEST_PATH = 0x00000002;
        private bool multiInstanceDetect = true;
        private bool songListinited = true;
        private readonly HashSet<string> gameInstance = new();
        private Dictionary<string,SongMap> delicatedSongList = new Dictionary<string, SongMap>();
        Dictionary<string,Dictionary<string,SongMap>> musicPackInfo = new Dictionary<string, Dictionary<string, SongMap>>();
        Dictionary<string, string> musicPackPath = new Dictionary<string, string>();
        WaveOut waveOut = new WaveOut();

        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern UInt32 Everything_SetSearch(string lpSearchString);

        [DllImport("Everything64.dll")]
        public static extern bool Everything_Query(bool bWait);
        [DllImport("Everything64.dll")]
        private static extern UInt32 Everything_GetNumResults();
        [DllImport("Everything64.dll", CharSet = CharSet.Unicode)]
        private static extern void Everything_GetResultFullPathName(UInt32 nIndex, StringBuilder lpString, UInt32 nMaxCount);

        [DllImport("Everything64.dll")]
        private static extern void Everything_SetRequestFlags(UInt32 dwRequestFlags);

        const string version = "1.0.0";
        const string author = "@万毒不侵";
        CultureInfo cultureInfo = CultureInfo.CurrentCulture;
        public MainForm()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            string language = cultureInfo.TwoLetterISOLanguageName;
            this.Text= "BSIMM-独立曲包管理/编辑器 " + version + " " + author;
            waveOut.Volume = (float)trackVolume.Value/100;
            //双语的适配以后再做
            Thread update = new Thread(updateDetect);
            update.Start();
            if (multiInstanceDetect)
            {
                Thread detect = new Thread(detectMultiBeatSaberInstance);
                detect.Start();
            }

        }
        private void updateDetect()
        {
            debugLog("检查更新中...\r\n");
            //string updateUrl = "";
            debugLog("检查更新完成\r\n");
        }
        private void detectMultiBeatSaberInstance()
        {
            Everything_SetSearch("Beat Saber.exe");
            Everything_SetRequestFlags(EVERYTHING_REQUEST_PATH | EVERYTHING_REQUEST_FILE_NAME);
            Everything_Query(true);
            var buf = new StringBuilder(300);
            for(uint i = 0; i < Everything_GetNumResults(); i++)
            {
                buf.Clear();
                Everything_GetResultFullPathName(i, buf, 300);
                var path = Path.GetDirectoryName(buf.ToString())!;
                if (gameInstance.Contains(path) || path.Contains("Prefetch") || path.Contains("$RECYCLE.BIN") || path.Contains("OneDrive") || File.GetAttributes(buf.ToString()).HasFlag(FileAttributes.Directory)) continue;
                gameInstance.Add(path);
                var searchmod =  (string path) =>
                {
                    try
                    {
                        var modpath = path+"\\Plugins\\SongCore.dll";
                        var gamepath = path + "\\Beat Saber.exe";
                        if (File.Exists(gamepath))
                        {
                            if(pirateGameDetect(path))
                            {
                                debugLog("检测到Beat Saber实例目录：" + path + "\r\n");
                                if (File.Exists(modpath))
                                {
                                    debugLog("检测到SongCore.dll，该实例安装了独立谱面插件\r\n");
                                }
                                else
                                {
                                    debugLog("未检测到SongCore.dll，该实例未安装独立谱面插件\r\n");
                                }
                            }
                            else
                            {
                                MessageBox.Show("检测到盗版Beat Saber游戏，本软件只支持正版游戏的曲包管理，程序将退出！","错误",MessageBoxButtons.OK,MessageBoxIcon.Error);
                                Environment.Exit(0);
                            }
                        }
                    }
                    catch
                    {
                    }
                };
                searchmod(path);
            }
        }
        private void debugLog(string text)
        {
            Invoke(new MethodInvoker(delegate
            {
                txtDebug.AppendText(text);
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

        private void DragEvent(object sender,DragEventArgs e)
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
            BSIMMProgress.ProgressBar.Value = 0;
            BSIMMProgress.ForeColor=Color.Green;
            BSIMMStatusText.Text = "分析歌单目录中......";
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
                    BSIMMStatusText.Text = "'" + folderPaths[i] + "'不是文件夹!跳过添加";
                }
                BSIMMProgress.ProgressBar.Value += (int)(100 / folderPaths.Length);
            }
            for (int i = 0; i < verifiedPaths.Count; i++)
            {
                addFolder(verifiedPaths[i]);
            }
            BSIMMProgress.ProgressBar.Value = 100;
        }
        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            BSIMMFolderBrowser.ShowDialog();
            string[] folderPaths = BSIMMFolderBrowser.SelectedPath.Split(';');
            List<string> verifiedPaths = new List<string>();
            for (int i = 0; i < folderPaths.Length; i++)
            {
                if (Directory.Exists(folderPaths[i]))
                {
                    verifiedPaths.Add(folderPaths[i]);
                }
                else
                {
                    debugLog("您选择的路径：'" + folderPaths[i] + "'似乎不是文件夹!\r\n");
                    BSIMMStatusText.Text = "'" + folderPaths[i] + "'不是文件夹!跳过添加";
                }
            }
            new Thread(() => addFolder(verifiedPaths[0])).Start();
        }

        private void addFolder(string path)
        {
            int depth = CalculateFolderDepth(path);
            switch (depth)
            {
                case 0:
                    if(addDelicatedSong(path)==2)
                    {
                        debugLog("该目录只有一首歌曲，将添加到独立歌曲列表中：" + path + "\r\n");
                    }
                    break;
                case 1:
                    addMusicPack(path);
                    break;
                default:
                    string[] folders = Directory.GetDirectories(path);
                    foreach (string folder in folders)
                    {
                        addFolder(folder);
                    }
                    break;
            }
        }
        public static int CalculateFolderDepth(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException("The specified path does not exist.");
            return CalculateFolderDepthRecursive(folderPath, 0);
        }

        private static int CalculateFolderDepthRecursive(string folderPath, int currentDepth)
        {
            int maxDepth = currentDepth;
            DirectoryInfo di = new DirectoryInfo(folderPath);
            foreach (DirectoryInfo subDir in di.GetDirectories())
            {
                if(subDir.GetDirectories().Length==0 && subDir.GetFiles().Length == 0)
                {
                    return maxDepth;
                }
                else
                {
                    int subDepth = CalculateFolderDepthRecursive(subDir.FullName, currentDepth + 1);
                    maxDepth = Math.Max(maxDepth, subDepth);
                }
            }
            return maxDepth;
        }
        private int addDelicatedSong(string mapDir,string musicPackName = null)
        {
            string language = cultureInfo.TwoLetterISOLanguageName;
            DirectoryInfo dir = new DirectoryInfo(mapDir);
            string dirName = dir.Name;
            string bsr = "";
            int spaceIndex = dirName.IndexOf(' ');
            if (spaceIndex == -1)
            {
                bsr = dirName;//如果没有空格，直接使用文件夹名
            }
            else
            {
                bsr = dirName.Substring(0, spaceIndex);//如果有空格，使用空格前的部分
            }
            if (!Regex.IsMatch(bsr, @"^[a-fA-F0-9]+$"))
            {
                debugLog("未知的bsr! 目录" + mapDir + "可能不是歌曲谱面目录！请检查文件夹的命名格式！\r\n");
            }
            if (File.Exists(mapDir + "\\Info.dat"))
            {
                byte[] mapInfo = File.ReadAllBytes(mapDir + "\\Info.dat");
                Dictionary<string, object> mapStruct = new Dictionary<string, object>();
                mapStruct = JsonConvert.DeserializeObject<Dictionary<string, object>>(Encoding.UTF8.GetString(mapInfo));
                SongMap songMap = ToEntity<SongMap>(mapStruct);
                songMap.songFolder = mapDir;
                if (mapIntergrityCheck(mapDir, songMap))
                {
                    if(musicPackName!=null)
                    {
                        if (musicPackInfo[musicPackName].ContainsKey(bsr))
                        {
                            debugLog("警告:检测到重复歌曲：" + songMap._songName + "\r\n");
                            return 3;
                        }
                        else 
                        { 
                            musicPackInfo[musicPackName].Add(bsr, songMap);            
                        }
                    }
                    else
                    {
                        if(delicatedSongList.ContainsKey(bsr))
                        {
                            debugLog("警告:检测到重复歌曲：" + bsr + "，将不会添加到独立歌曲列表中\r\n");
                            return 3;
                        }
                        else
                        {
                            delicatedSongList.Add(bsr, songMap);
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

        private bool mapIntergrityCheck(string mapDir,SongMap map)
        {
            string[] mapStruct = map.GetDifficultiesFiles();
            string coverImg = map._coverImageFilename;
            string musicFile = map._songFilename;
            if (!File.Exists(mapDir + "\\" + coverImg))
            {
                debugLog("警告:检测到缺失封面文件：" + coverImg + "\r\n");
                return false;
            }
            if (!File.Exists(mapDir + "\\" + musicFile))
            {
                debugLog("警告:检测到缺失音乐文件：" + musicFile + "\r\n");
                return false;
            }
            foreach (string mapFile in mapStruct)
            {
                if (!File.Exists(mapDir + "\\" + mapFile))
                {
                    debugLog("警告:检测到缺失谱面文件：" + mapFile + "\r\n");
                    return false;
                }
            }
            return true;
            
        }
        private void addMusicPack(string path)
        {
            string language = cultureInfo.TwoLetterISOLanguageName;
            string pattern = @"【(.+?)】";
            Match match = Regex.Match(path, pattern);
            string musicPackName = "";
            int mapsCount = 0;
            int otherCount = 0;
            int intergrityCount = 0;
            int duplicateCount = 0;
            if (match.Success)
            {
                musicPackName = match.Groups[1].Value;
                debugLog("获取到曲包名称【" + musicPackName + "】\r\n");
                if(musicPackInfo.ContainsKey(musicPackName))
                {
                    debugLog("警告:检测到重复括号内曲包命名：" + musicPackName + "，使用完整目录名称\r\n");
                    musicPackName = new DirectoryInfo(path).Name;
                }
            }
            else
            {
                debugLog(path + "未检测到曲包名称【】，使用文件夹内名称\r\n");
                musicPackName = new DirectoryInfo(path).Name;
            }
            if (musicPackInfo.ContainsKey(musicPackName))
            {
                debugLog("警告:检测到重复曲包：" + musicPackName + "，将不会添加到曲包列表中\r\n");
                return;
            }
            musicPackInfo.Add(musicPackName, new Dictionary<string, SongMap>());
            string[] mapsDir = Directory.GetDirectories(path);
            BSIMMActionText.Text = "正在添加曲包：" + musicPackName;
            BSIMMProgress.ProgressBar.Value = 0;
            foreach (string mapDir in mapsDir)
            {
                switch (addDelicatedSong(mapDir,musicPackName))
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
            }
            if(mapsCount==0)
            {
                debugLog("警告:未检测到完整歌曲目录！将不会添加曲包\r\n");
                musicPackInfo.Remove(musicPackName);
                BSIMMStatusText.Text = "未检测到完整歌曲目录！跳过";
                BSIMMProgress.ForeColor = Color.Red;
                BSIMMProgress.ProgressBar.Value = 100;
            }
            else
            {
                debugLog("曲包:" + musicPackName + " 检测到" + mapsCount + "个完整歌曲目录  "+duplicateCount+"个重复歌曲目录  "+intergrityCount+"个不完整目录\r\n"); 
                musicPackPath.Add(musicPackName, path);
                displayMusicpack(musicPackName);
                BSIMMStatusText.Text = "曲包：" + musicPackName + "添加完成";
                BSIMMActionText.Text = "就绪";
                BSIMMProgress.ForeColor = Color.Green;
                BSIMMProgress.ProgressBar.Value = 100;
            }
        }
        private void displayMusicpack(string musicPackName)
        {
            Bitmap musicPackCover = Resources.默认;
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
                        gfx.DrawString(musicPackName, font, brush, new RectangleF(0, 0, musicPackCover.Width, musicPackCover.Height),format);
                        font = new Font("黑体", 24, FontStyle.Bold);
                        SizeF textSize = gfx.MeasureString("BSIMM自动生成@万毒不侵", font);
                        float x = musicPackCover.Width - textSize.Width;
                        float y = musicPackCover.Height - textSize.Height;
                        gfx.DrawString("BSIMM自动生成@万毒不侵", font, brush, x, y);
                    }
                    break;
            }
            musicPackimg.Images.Add(musicPackName, musicPackCover);
            ListViewItem item = new ListViewItem(musicPackName, musicPackName);
            musicPackListView.Items.Add(item);
        }
        private void displaySongList(string musicPackName)
        {
            songListinited = false;
            Dictionary<string, SongMap> songList = musicPackInfo[musicPackName];
            Invoke(new MethodInvoker(delegate
            {
                songListView.BeginUpdate();
                songListView.Items.Clear();
                foreach (KeyValuePair<string, SongMap> valuePair in songList)
                {
                    ListViewItem item = new ListViewItem(valuePair.Key);
                    item.SubItems.Add(valuePair.Value._songName);
                    item.SubItems.Add(valuePair.Value._beatsPerMinute.ToString());
                    songListView.Items.Add(item);
                }
                songListView.EndUpdate();
            }));
            songListinited = true;
            Invoke(new MethodInvoker(delegate
            {
                BSIMMActionText.Text = "就绪";
                BSIMMStatusText.Text = "曲包：" + musicPackName + "歌曲列表加载完成";
            }));

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
                    BSIMMStatusText.Text = "正在加载歌曲信息，请稍后";
                    BSIMMActionText.Text = "繁忙";
                }
                
            }
        }
            private void btnPlay_Click(object sender, EventArgs e)
        {
            int index = songListView.SelectedItems[0].Index;
            if(index != -1)
            {
                string playKey = songListView.SelectedItems[0].Text;
                SongMap playSong = musicPackInfo[musicPackListView.SelectedItems[0].Text][playKey];
                if (btnPlay.Text == "播放")
                {
                    if (waveOut.PlaybackState == PlaybackState.Paused)
                    {
                        waveOut.Resume();
                        btnPlay.Text = "暂停";
                        BSIMMActionText.Text = "播放器：";
                        BSIMMStatusText.Text = "正在播放：" + playSong._songName;
                    }
                    else
                    {

                        string playPath = playSong.songFolder + "\\" + playSong._songFilename;
                        NAudio.Vorbis.VorbisWaveReader vorbis = new NAudio.Vorbis.VorbisWaveReader(playPath);
                        waveOut.Init(vorbis);
                        waveOut.Play();
                        BSIMMActionText.Text = "播放器：";
                        BSIMMStatusText.Text = "正在播放：" + playSong._songName;
                        btnPlay.Text = "暂停";
                    }
                }
                else if (btnPlay.Text == "暂停")
                {
                    waveOut.Pause();
                    btnPlay.Text = "播放";
                    BSIMMActionText.Text = "播放器：";
                    BSIMMStatusText.Text = "已暂停";
                }
            }
        }

        private void songListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            waveOut.Stop();
            btnPlay.Text = "播放";
            BSIMMActionText.Text = "播放器：";
            BSIMMStatusText.Text = "已停止";
        }

        private void trackVolume_Scroll(object sender, EventArgs e)
        {
            waveOut.Volume = (float)trackVolume.Value / 100;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {

        }
    }
}


