namespace BeatSaberIndependentMapsManager
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            BSIMMStats = new System.Windows.Forms.StatusStrip();
            BSIMMActionText = new System.Windows.Forms.ToolStripStatusLabel();
            BSIMMStatusText = new System.Windows.Forms.ToolStripStatusLabel();
            BSIMMProgress = new System.Windows.Forms.ToolStripProgressBar();
            txtDebug = new System.Windows.Forms.RichTextBox();
            BSIMMFolderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            btnOpenFolder = new System.Windows.Forms.Button();
            btnAutoFill = new System.Windows.Forms.Button();
            btnSaveMusicPack = new System.Windows.Forms.Button();
            tabMusicPackContorl = new System.Windows.Forms.TabControl();
            tabSongFolder = new System.Windows.Forms.TabPage();
            btnDeduplication = new System.Windows.Forms.Button();
            label3 = new System.Windows.Forms.Label();
            trackVolume = new System.Windows.Forms.TrackBar();
            btnPlay = new System.Windows.Forms.Button();
            btnExportFavor = new System.Windows.Forms.Button();
            btnInfo = new System.Windows.Forms.Button();
            btnSaveList = new System.Windows.Forms.Button();
            btnSetImg = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            songListView = new System.Windows.Forms.ListView();
            songName = new System.Windows.Forms.ColumnHeader();
            bsr = new System.Windows.Forms.ColumnHeader();
            bpm = new System.Windows.Forms.ColumnHeader();
            musicPackListView = new System.Windows.Forms.ListView();
            musicPackimg = new System.Windows.Forms.ImageList(components);
            lblMap = new System.Windows.Forms.Label();
            lblMusicPack = new System.Windows.Forms.Label();
            tabBSVer = new System.Windows.Forms.TabPage();
            tabDelicatedSong = new System.Windows.Forms.TabPage();
            tabFolderandList = new System.Windows.Forms.TabPage();
            btnSetting = new System.Windows.Forms.Button();
            comboBoxPlatform = new System.Windows.Forms.ComboBox();
            lblPlatform = new System.Windows.Forms.Label();
            btnInstallEverything = new System.Windows.Forms.Button();
            lblExtension = new System.Windows.Forms.Label();
            btnTutorial = new System.Windows.Forms.Button();
            btnExit = new System.Windows.Forms.Button();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            PlaybackTimer = new System.Windows.Forms.Timer(components);
            musicPackCoverDialog = new System.Windows.Forms.OpenFileDialog();
            savebplistDialog = new System.Windows.Forms.FolderBrowserDialog();
            BSIMMStats.SuspendLayout();
            tabMusicPackContorl.SuspendLayout();
            tabSongFolder.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackVolume).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // BSIMMStats
            // 
            BSIMMStats.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { BSIMMActionText, BSIMMStatusText, BSIMMProgress });
            BSIMMStats.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            BSIMMStats.Location = new System.Drawing.Point(0, 577);
            BSIMMStats.Name = "BSIMMStats";
            BSIMMStats.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
            BSIMMStats.Size = new System.Drawing.Size(821, 29);
            BSIMMStats.TabIndex = 1;
            BSIMMStats.Text = "statusStrip1";
            // 
            // BSIMMActionText
            // 
            BSIMMActionText.Name = "BSIMMActionText";
            BSIMMActionText.Size = new System.Drawing.Size(32, 24);
            BSIMMActionText.Text = "信息";
            // 
            // BSIMMStatusText
            // 
            BSIMMStatusText.Name = "BSIMMStatusText";
            BSIMMStatusText.Size = new System.Drawing.Size(0, 24);
            // 
            // BSIMMProgress
            // 
            BSIMMProgress.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            BSIMMProgress.AutoSize = false;
            BSIMMProgress.Margin = new System.Windows.Forms.Padding(50, 3, 1, 3);
            BSIMMProgress.Name = "BSIMMProgress";
            BSIMMProgress.Size = new System.Drawing.Size(192, 23);
            // 
            // txtDebug
            // 
            txtDebug.Location = new System.Drawing.Point(0, 501);
            txtDebug.Margin = new System.Windows.Forms.Padding(4);
            txtDebug.Name = "txtDebug";
            txtDebug.ReadOnly = true;
            txtDebug.Size = new System.Drawing.Size(693, 75);
            txtDebug.TabIndex = 4;
            txtDebug.Text = "程序日志：日志文件将同步到软件目录下*.log文件\n";
            // 
            // btnOpenFolder
            // 
            btnOpenFolder.Location = new System.Drawing.Point(132, 435);
            btnOpenFolder.Margin = new System.Windows.Forms.Padding(4);
            btnOpenFolder.Name = "btnOpenFolder";
            btnOpenFolder.Size = new System.Drawing.Size(100, 30);
            btnOpenFolder.TabIndex = 5;
            btnOpenFolder.Text = "添加曲包目录";
            btnOpenFolder.UseVisualStyleBackColor = true;
            btnOpenFolder.Click += btnOpenFolder_Click;
            // 
            // btnAutoFill
            // 
            btnAutoFill.Location = new System.Drawing.Point(15, 435);
            btnAutoFill.Margin = new System.Windows.Forms.Padding(4);
            btnAutoFill.Name = "btnAutoFill";
            btnAutoFill.Size = new System.Drawing.Size(100, 30);
            btnAutoFill.TabIndex = 6;
            btnAutoFill.Text = "自动检测";
            btnAutoFill.UseVisualStyleBackColor = true;
            btnAutoFill.Click += btnAutoFill_Click;
            // 
            // btnSaveMusicPack
            // 
            btnSaveMusicPack.Location = new System.Drawing.Point(248, 435);
            btnSaveMusicPack.Margin = new System.Windows.Forms.Padding(4);
            btnSaveMusicPack.Name = "btnSaveMusicPack";
            btnSaveMusicPack.Size = new System.Drawing.Size(100, 30);
            btnSaveMusicPack.TabIndex = 7;
            btnSaveMusicPack.Text = "保存曲包目录";
            btnSaveMusicPack.UseVisualStyleBackColor = true;
            btnSaveMusicPack.Click += btnSaveMusicPack_Click;
            // 
            // tabMusicPackContorl
            // 
            tabMusicPackContorl.AllowDrop = true;
            tabMusicPackContorl.Controls.Add(tabSongFolder);
            tabMusicPackContorl.Controls.Add(tabBSVer);
            tabMusicPackContorl.Controls.Add(tabDelicatedSong);
            tabMusicPackContorl.Controls.Add(tabFolderandList);
            tabMusicPackContorl.Location = new System.Drawing.Point(0, 0);
            tabMusicPackContorl.Margin = new System.Windows.Forms.Padding(4);
            tabMusicPackContorl.Name = "tabMusicPackContorl";
            tabMusicPackContorl.SelectedIndex = 0;
            tabMusicPackContorl.Size = new System.Drawing.Size(697, 497);
            tabMusicPackContorl.TabIndex = 8;
            // 
            // tabSongFolder
            // 
            tabSongFolder.Controls.Add(btnDeduplication);
            tabSongFolder.Controls.Add(label3);
            tabSongFolder.Controls.Add(trackVolume);
            tabSongFolder.Controls.Add(btnPlay);
            tabSongFolder.Controls.Add(btnExportFavor);
            tabSongFolder.Controls.Add(btnInfo);
            tabSongFolder.Controls.Add(btnSaveList);
            tabSongFolder.Controls.Add(btnSetImg);
            tabSongFolder.Controls.Add(label1);
            tabSongFolder.Controls.Add(songListView);
            tabSongFolder.Controls.Add(musicPackListView);
            tabSongFolder.Controls.Add(lblMap);
            tabSongFolder.Controls.Add(lblMusicPack);
            tabSongFolder.Controls.Add(btnSaveMusicPack);
            tabSongFolder.Controls.Add(btnAutoFill);
            tabSongFolder.Controls.Add(btnOpenFolder);
            tabSongFolder.Location = new System.Drawing.Point(4, 26);
            tabSongFolder.Margin = new System.Windows.Forms.Padding(4);
            tabSongFolder.Name = "tabSongFolder";
            tabSongFolder.Padding = new System.Windows.Forms.Padding(4);
            tabSongFolder.Size = new System.Drawing.Size(689, 467);
            tabSongFolder.TabIndex = 0;
            tabSongFolder.Text = "歌曲目录列表";
            tabSongFolder.UseVisualStyleBackColor = true;
            // 
            // btnDeduplication
            // 
            btnDeduplication.Location = new System.Drawing.Point(585, 397);
            btnDeduplication.Margin = new System.Windows.Forms.Padding(4);
            btnDeduplication.Name = "btnDeduplication";
            btnDeduplication.Size = new System.Drawing.Size(100, 30);
            btnDeduplication.TabIndex = 24;
            btnDeduplication.Text = "一键去重";
            btnDeduplication.UseVisualStyleBackColor = true;
            btnDeduplication.Click += btnDeduplication_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(369, 404);
            label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(35, 17);
            label3.TabIndex = 23;
            label3.Text = "音量:";
            // 
            // trackVolume
            // 
            trackVolume.BackColor = System.Drawing.Color.White;
            trackVolume.Location = new System.Drawing.Point(412, 402);
            trackVolume.Margin = new System.Windows.Forms.Padding(4);
            trackVolume.Maximum = 100;
            trackVolume.MaximumSize = new System.Drawing.Size(155, 30);
            trackVolume.Name = "trackVolume";
            trackVolume.Size = new System.Drawing.Size(155, 30);
            trackVolume.TabIndex = 22;
            trackVolume.TickStyle = System.Windows.Forms.TickStyle.None;
            trackVolume.Value = 20;
            trackVolume.Scroll += trackVolume_Scroll;
            // 
            // btnPlay
            // 
            btnPlay.Location = new System.Drawing.Point(364, 435);
            btnPlay.Margin = new System.Windows.Forms.Padding(4);
            btnPlay.Name = "btnPlay";
            btnPlay.Size = new System.Drawing.Size(100, 30);
            btnPlay.TabIndex = 21;
            btnPlay.Text = "播放";
            btnPlay.UseVisualStyleBackColor = true;
            btnPlay.Click += btnPlay_Click;
            // 
            // btnExportFavor
            // 
            btnExportFavor.Location = new System.Drawing.Point(585, 435);
            btnExportFavor.Margin = new System.Windows.Forms.Padding(4);
            btnExportFavor.Name = "btnExportFavor";
            btnExportFavor.Size = new System.Drawing.Size(100, 30);
            btnExportFavor.TabIndex = 20;
            btnExportFavor.Text = "导出收藏";
            btnExportFavor.UseVisualStyleBackColor = true;
            // 
            // btnInfo
            // 
            btnInfo.BackColor = System.Drawing.Color.Transparent;
            btnInfo.Location = new System.Drawing.Point(476, 435);
            btnInfo.Margin = new System.Windows.Forms.Padding(4);
            btnInfo.Name = "btnInfo";
            btnInfo.Size = new System.Drawing.Size(100, 30);
            btnInfo.TabIndex = 18;
            btnInfo.Text = "详细信息";
            btnInfo.UseVisualStyleBackColor = false;
            btnInfo.Click += btnInfo_Click;
            // 
            // btnSaveList
            // 
            btnSaveList.Location = new System.Drawing.Point(248, 397);
            btnSaveList.Margin = new System.Windows.Forms.Padding(4);
            btnSaveList.Name = "btnSaveList";
            btnSaveList.Size = new System.Drawing.Size(100, 30);
            btnSaveList.TabIndex = 16;
            btnSaveList.Text = "保存列表";
            btnSaveList.UseVisualStyleBackColor = true;
            btnSaveList.Click += btnSaveList_Click;
            // 
            // btnSetImg
            // 
            btnSetImg.Location = new System.Drawing.Point(132, 397);
            btnSetImg.Margin = new System.Windows.Forms.Padding(4);
            btnSetImg.Name = "btnSetImg";
            btnSetImg.Size = new System.Drawing.Size(100, 30);
            btnSetImg.TabIndex = 15;
            btnSetImg.Text = "选择图片";
            btnSetImg.UseVisualStyleBackColor = true;
            btnSetImg.Click += btnSetImg_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(28, 404);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(104, 17);
            label1.TabIndex = 14;
            label1.Text = "自定义曲包图片：";
            // 
            // songListView
            // 
            songListView.AllowDrop = true;
            songListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { songName, bsr, bpm });
            songListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            songListView.Location = new System.Drawing.Point(356, 41);
            songListView.Margin = new System.Windows.Forms.Padding(4);
            songListView.Name = "songListView";
            songListView.Size = new System.Drawing.Size(328, 348);
            songListView.TabIndex = 13;
            songListView.UseCompatibleStateImageBehavior = false;
            songListView.View = System.Windows.Forms.View.Details;
            songListView.SelectedIndexChanged += songListView_SelectedIndexChanged;
            // 
            // songName
            // 
            songName.Text = "曲名";
            songName.Width = 140;
            // 
            // bsr
            // 
            bsr.Text = "bsr";
            bsr.Width = 50;
            // 
            // bpm
            // 
            bpm.Text = "bpm";
            bpm.Width = 40;
            // 
            // musicPackListView
            // 
            musicPackListView.AllowDrop = true;
            musicPackListView.LabelEdit = true;
            musicPackListView.LargeImageList = musicPackimg;
            musicPackListView.Location = new System.Drawing.Point(9, 41);
            musicPackListView.Margin = new System.Windows.Forms.Padding(4);
            musicPackListView.MultiSelect = false;
            musicPackListView.Name = "musicPackListView";
            musicPackListView.Size = new System.Drawing.Size(339, 348);
            musicPackListView.TabIndex = 12;
            musicPackListView.UseCompatibleStateImageBehavior = false;
            musicPackListView.AfterLabelEdit += musicPackListView_AfterLabelEdit;
            musicPackListView.Click += musicPackListView_Click;
            musicPackListView.DragDrop += DropEvent;
            musicPackListView.DragEnter += DragEvent;
            musicPackListView.MouseDoubleClick += musicPackListView_MouseDoubleClick;
            // 
            // musicPackimg
            // 
            musicPackimg.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            musicPackimg.ImageSize = new System.Drawing.Size(110, 110);
            musicPackimg.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // lblMap
            // 
            lblMap.AutoSize = true;
            lblMap.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
            lblMap.Location = new System.Drawing.Point(356, 8);
            lblMap.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblMap.Name = "lblMap";
            lblMap.Size = new System.Drawing.Size(103, 16);
            lblMap.TabIndex = 10;
            lblMap.Text = "曲包歌曲列表";
            // 
            // lblMusicPack
            // 
            lblMusicPack.AutoSize = true;
            lblMusicPack.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
            lblMusicPack.Location = new System.Drawing.Point(12, 8);
            lblMusicPack.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblMusicPack.Name = "lblMusicPack";
            lblMusicPack.Size = new System.Drawing.Size(103, 16);
            lblMusicPack.TabIndex = 9;
            lblMusicPack.Text = "曲包目录列表";
            // 
            // tabBSVer
            // 
            tabBSVer.Location = new System.Drawing.Point(4, 26);
            tabBSVer.Margin = new System.Windows.Forms.Padding(4);
            tabBSVer.Name = "tabBSVer";
            tabBSVer.Padding = new System.Windows.Forms.Padding(4);
            tabBSVer.Size = new System.Drawing.Size(689, 467);
            tabBSVer.TabIndex = 1;
            tabBSVer.Text = "节奏光剑版本管理";
            tabBSVer.UseVisualStyleBackColor = true;
            // 
            // tabDelicatedSong
            // 
            tabDelicatedSong.Location = new System.Drawing.Point(4, 26);
            tabDelicatedSong.Margin = new System.Windows.Forms.Padding(4);
            tabDelicatedSong.Name = "tabDelicatedSong";
            tabDelicatedSong.Size = new System.Drawing.Size(689, 467);
            tabDelicatedSong.TabIndex = 2;
            tabDelicatedSong.Text = "散装歌曲列表";
            tabDelicatedSong.UseVisualStyleBackColor = true;
            // 
            // tabFolderandList
            // 
            tabFolderandList.Location = new System.Drawing.Point(4, 26);
            tabFolderandList.Margin = new System.Windows.Forms.Padding(4);
            tabFolderandList.Name = "tabFolderandList";
            tabFolderandList.Size = new System.Drawing.Size(689, 467);
            tabFolderandList.TabIndex = 3;
            tabFolderandList.Text = "歌曲目录编辑/歌单编辑";
            tabFolderandList.UseVisualStyleBackColor = true;
            // 
            // btnSetting
            // 
            btnSetting.Location = new System.Drawing.Point(705, 463);
            btnSetting.Margin = new System.Windows.Forms.Padding(4);
            btnSetting.Name = "btnSetting";
            btnSetting.Size = new System.Drawing.Size(100, 30);
            btnSetting.TabIndex = 9;
            btnSetting.Text = "程序设置";
            btnSetting.UseVisualStyleBackColor = true;
            // 
            // comboBoxPlatform
            // 
            comboBoxPlatform.FormattingEnabled = true;
            comboBoxPlatform.Items.AddRange(new object[] { "PC-Steam", "PC-Oculus", "Quest" });
            comboBoxPlatform.Location = new System.Drawing.Point(705, 425);
            comboBoxPlatform.Margin = new System.Windows.Forms.Padding(4);
            comboBoxPlatform.Name = "comboBoxPlatform";
            comboBoxPlatform.Size = new System.Drawing.Size(97, 25);
            comboBoxPlatform.TabIndex = 10;
            comboBoxPlatform.SelectedIndexChanged += comboBoxPlatform_SelectedIndexChanged;
            // 
            // lblPlatform
            // 
            lblPlatform.AutoSize = true;
            lblPlatform.Location = new System.Drawing.Point(718, 398);
            lblPlatform.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblPlatform.Name = "lblPlatform";
            lblPlatform.Size = new System.Drawing.Size(68, 17);
            lblPlatform.TabIndex = 11;
            lblPlatform.Text = "平台选择：";
            // 
            // btnInstallEverything
            // 
            btnInstallEverything.Location = new System.Drawing.Point(705, 349);
            btnInstallEverything.Margin = new System.Windows.Forms.Padding(4);
            btnInstallEverything.Name = "btnInstallEverything";
            btnInstallEverything.Size = new System.Drawing.Size(100, 30);
            btnInstallEverything.TabIndex = 12;
            btnInstallEverything.Text = "安装增强扩展";
            btnInstallEverything.UseVisualStyleBackColor = true;
            // 
            // lblExtension
            // 
            lblExtension.AutoSize = true;
            lblExtension.Location = new System.Drawing.Point(705, 132);
            lblExtension.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblExtension.MaximumSize = new System.Drawing.Size(105, 0);
            lblExtension.Name = "lblExtension";
            lblExtension.Size = new System.Drawing.Size(104, 204);
            lblExtension.TabIndex = 13;
            lblExtension.Text = "提示：软件支持Everything增强扩展，该扩展可以帮助本软件更好的管理多个版本的节奏光剑，同时可以更快速的搜寻歌曲文件，如果想使用本软件的增强功能，可点击安装增强扩展一件安装（已安装请忽略本提示）";
            // 
            // btnTutorial
            // 
            btnTutorial.Location = new System.Drawing.Point(705, 501);
            btnTutorial.Margin = new System.Windows.Forms.Padding(4);
            btnTutorial.Name = "btnTutorial";
            btnTutorial.Size = new System.Drawing.Size(100, 30);
            btnTutorial.TabIndex = 14;
            btnTutorial.Text = "操作指南";
            btnTutorial.UseVisualStyleBackColor = true;
            // 
            // btnExit
            // 
            btnExit.Location = new System.Drawing.Point(705, 543);
            btnExit.Margin = new System.Windows.Forms.Padding(4);
            btnExit.Name = "btnExit";
            btnExit.Size = new System.Drawing.Size(100, 30);
            btnExit.TabIndex = 15;
            btnExit.Text = "退出";
            btnExit.UseVisualStyleBackColor = true;
            btnExit.Click += btnExit_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.everything;
            pictureBox1.Location = new System.Drawing.Point(718, 26);
            pictureBox1.Margin = new System.Windows.Forms.Padding(4);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(75, 75);
            pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 16;
            pictureBox1.TabStop = false;
            // 
            // PlaybackTimer
            // 
            PlaybackTimer.Tick += PlaybackTimer_Tick;
            // 
            // musicPackCoverDialog
            // 
            musicPackCoverDialog.Filter = "JPEG  (*.jpg)|*.jpg|Pictures (*.png)|*.png|bitmap (*.bmp)|*.bmp";
            // 
            // MainForm
            // 
            AllowDrop = true;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoSize = true;
            ClientSize = new System.Drawing.Size(821, 606);
            Controls.Add(pictureBox1);
            Controls.Add(btnExit);
            Controls.Add(btnTutorial);
            Controls.Add(lblExtension);
            Controls.Add(btnInstallEverything);
            Controls.Add(lblPlatform);
            Controls.Add(comboBoxPlatform);
            Controls.Add(btnSetting);
            Controls.Add(tabMusicPackContorl);
            Controls.Add(txtDebug);
            Controls.Add(BSIMMStats);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(4);
            MaximizeBox = false;
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "BSIMM-独立曲包管理/编辑器 v1.0.0 @万毒不侵";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            BSIMMStats.ResumeLayout(false);
            BSIMMStats.PerformLayout();
            tabMusicPackContorl.ResumeLayout(false);
            tabSongFolder.ResumeLayout(false);
            tabSongFolder.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackVolume).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.StatusStrip BSIMMStats;
        private System.Windows.Forms.ToolStripStatusLabel BSIMMActionText;
        private System.Windows.Forms.ToolStripStatusLabel BSIMMStatusText;
        private System.Windows.Forms.ToolStripProgressBar BSIMMProgress;
        private System.Windows.Forms.RichTextBox txtDebug;
        private System.Windows.Forms.FolderBrowserDialog BSIMMFolderBrowser;
        private System.Windows.Forms.Button btnOpenFolder;
        private System.Windows.Forms.Button btnAutoFill;
        private System.Windows.Forms.Button btnSaveMusicPack;
        private System.Windows.Forms.TabControl tabMusicPackContorl;
        private System.Windows.Forms.TabPage tabSongFolder;
        private System.Windows.Forms.TabPage tabBSVer;
        private System.Windows.Forms.TabPage tabDelicatedSong;
        private System.Windows.Forms.TabPage tabFolderandList;
        private System.Windows.Forms.Button btnSetting;
        private System.Windows.Forms.ComboBox comboBoxPlatform;
        private System.Windows.Forms.Label lblPlatform;
        private System.Windows.Forms.Button btnInstallEverything;
        private System.Windows.Forms.Label lblExtension;
        private System.Windows.Forms.Button btnTutorial;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ImageList musicPackimg;
        private System.Windows.Forms.Label lblMap;
        private System.Windows.Forms.Label lblMusicPack;
        private System.Windows.Forms.ListView musicPackListView;
        private System.Windows.Forms.ListView songListView;
        private System.Windows.Forms.ColumnHeader bsr;
        private System.Windows.Forms.ColumnHeader songName;
        private System.Windows.Forms.ColumnHeader bpm;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnInfo;
        private System.Windows.Forms.Button btnSaveList;
        private System.Windows.Forms.Button btnSetImg;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.Button btnExportFavor;
        private System.Windows.Forms.TrackBar trackVolume;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Timer PlaybackTimer;
        private System.Windows.Forms.OpenFileDialog musicPackCoverDialog;
        private System.Windows.Forms.Button btnDeduplication;
        private System.Windows.Forms.FolderBrowserDialog savebplistDialog;
    }
}