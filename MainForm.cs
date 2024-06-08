using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeatSaberIndependentMapsManager
{
    public partial class MainForm : Form
    {
        const string description = "操作步骤:1.给游戏安装核心Mod;2.确保装Mod之后运行过游戏，退出游戏再进行操作;\r\n3：将曲包解压到单独的文件夹；4：将解压后的文件夹拖入（也可以自动检测）\r\n注:曲包可以解压到任意位置，保存以后请不要移动或更名歌曲文件夹，否则会读取失败\r\n编辑文本可直接修改名称或删除歌单，一行一个;直接拖入图片可添加至光标所在行歌单\r\n";
        const string description_en = "Operation Steps:1:Install the core mod for the game.2:Ensure that you have run the game once after installing the mod,then exit the game before proceeding.3:Extract the music pack into a separate folder.4:Drag the extracted folder into the designated area (automatic detection is also available).Note: The music pack can be extracted to any location. Once saved, please do not move or rename the music folder, as this will result in a failure to read the data.You can directly  modify the name or delete playlists by editing the text, one line per item. Dragging and dropping an image will add it to the playlist at the current cursor position.\r\n";
        string[] groupboxInfoText = { "软件说明：", "SoftWare Description:" };
        string[] groupboxInfoControlText = { "当前歌单", "Current Music Pack" };
        string[] groupboxControlText = { "控制", "Control" };
        string[] MusicpackInfoText_zh = { "歌单名称：","歌曲数量：","所在目录：","高级选项：" };
        string[] MusicpackinfoText_en = { "Music Pack Name:","Number of Songs:","Directory:","Advanced Options:" };
        string[] tabText = { "Steam_电脑平台", "Quest_一体机平台","Steam PC Platform","Quest Platform" };
        string[] btnText_zh = { "添加目录", "保存列表", "导出歌单", "导出收藏", "捐助作者", "曲包＋教程获取" };
        string[] btnText_en = { "Add Folder", "Save List", "Export Playlist", "Export Favorites", "Donate", "Music Pack + Tutorial" };
        string[] lbltipText = { "拖入文件夹以添加歌单或拖入图片添加封面", "Drag and drop a folder to add a playlist or drop picture to add cover" };
        CultureInfo cultureInfo = CultureInfo.CurrentCulture;
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            string language = cultureInfo.TwoLetterISOLanguageName;
            setDisplayLanguage(language);
        }
        private void setDisplayLanguage(string locale)
        {
            switch (locale)
            {
                case "zh":
                    this.Text = "BSIMM 独立曲包管理器 作者 @万毒不侵";
                    tabPC.Text = tabText[0];
                    tabQuest.Text = tabText[1];
                    lblDescription.Text = description;
                    lblDescription.Font = new Font("黑体", 12);
                    groupBoxControl.Text = groupboxControlText[0];
                    groupBoxInfo.Text = groupboxInfoText[0];
                    lblMusicPack.Text = MusicpackInfoText_zh[0];
                    lblSongCount.Text = MusicpackInfoText_zh[1];
                    lblCurrentFolder.Text = MusicpackInfoText_zh[2];
                    lblAdvancedOptions.Text = MusicpackInfoText_zh[3];
                    BSIMMActionText.Text = "控制台：";
                    btnAddFolder.Text = btnText_zh[0];
                    btnSaveList.Text = btnText_zh[1];
                    btnExportPlayLists.Text = btnText_zh[2];
                    btnExportFavorites.Text = btnText_zh[3];
                    btnDonate.Text = btnText_zh[4];
                    btnGetMapsTutorial.Text = btnText_zh[5];
                    lblDescription2.Text = lbltipText[0];
                    break;
                case "en":
                    this.Text = "Beat Saber Independent Maps Manager written by @万毒不侵";
                    tabPC.Text = tabText[2];
                    tabQuest.Text = tabText[3];
                    lblDescription.Text = description_en;
                    lblDescription.Font = new Font("Times New Roman", 9);
                    groupBoxControl.Text = groupboxControlText[1];
                    groupBoxInfo.Text = groupboxInfoText[1];
                    lblMusicPack.Text = MusicpackinfoText_en[0];
                    lblSongCount.Text = MusicpackinfoText_en[1];
                    lblCurrentFolder.Text = MusicpackinfoText_en[2];
                    lblAdvancedOptions.Text = MusicpackinfoText_en[3];
                    BSIMMActionText.Text = "Console:";
                    btnAddFolder.Text = btnText_en[0];
                    btnSaveList.Text = btnText_en[1];
                    btnExportPlayLists.Text = btnText_en[2];
                    btnExportFavorites.Text = btnText_en[3];             
                    btnDonate.Text = btnText_en[4];
                    btnGetMapsTutorial.Text = btnText_en[5];
                    lblDescription2.Text = lbltipText[1];
                    break;
                default:
                    MessageBox.Show("Unsupported Language detected.Setting display language to English...");
                    setDisplayLanguage("en");
                break;
            }
        }
        private void displayMusicPackInfo(string musicPackName, string songCount, string currentFolder,string advancedOption)
        {
            lblMusicPack.Text = musicPackName;
            lblSongCount.Text = songCount;
            lblCurrentFolder.Text = currentFolder;
            lblAdvancedOptions.Text = advancedOption;
        }

        private void changeInfoView(bool status)
        {
            picMusicPack.Visible = status;
            lblMusicPack.Visible = status;
            lblSongCount.Visible = status;
            lblCurrentFolder.Visible = status;
            lblAdvancedOptions.Visible = status;
            lblDescription.Visible = !status;
            string language = cultureInfo.TwoLetterISOLanguageName;
            if (status)
            {
                groupBoxInfo.Text = language == "zh" ? groupboxInfoControlText[0] : groupboxInfoControlText[1];
            }
            else
            {
                groupBoxInfo.Text = language == "zh" ? groupboxInfoText[0] : groupboxInfoText[1];
                lblDescription.Text = language == "zh" ? description : description_en;
                lblDescription.Font = language == "zh" ? new Font("黑体", 12) : new Font("Times New Roman", 9);
            }
        }

        private void txtPath_pc_DragEnter(object sender, DragEventArgs e)
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
        private void txtPath_pc_DragDrop(object sender, DragEventArgs e)
        {
            txtPath_pc.Text = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
        }

        private void txtPath_quest_DragEnter(object sender, DragEventArgs e)
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

        private void txtPath_quest_DragDrop(object sender, DragEventArgs e)
        {
            txtPath_quest.Text = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
        }

        private void tabPlatform_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}


