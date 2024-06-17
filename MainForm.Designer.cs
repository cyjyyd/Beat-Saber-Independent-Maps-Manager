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
            this.components = new System.ComponentModel.Container();
            this.BSIMMStats = new System.Windows.Forms.StatusStrip();
            this.BSIMMActionText = new System.Windows.Forms.ToolStripStatusLabel();
            this.BSIMMStatusText = new System.Windows.Forms.ToolStripStatusLabel();
            this.BSIMMProgress = new System.Windows.Forms.ToolStripProgressBar();
            this.txtDebug = new System.Windows.Forms.RichTextBox();
            this.BSIMMFolderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            this.btnOpenFolder = new System.Windows.Forms.Button();
            this.btnAutoFill = new System.Windows.Forms.Button();
            this.btnSaveMusicPack = new System.Windows.Forms.Button();
            this.tabMusicPackContorl = new System.Windows.Forms.TabControl();
            this.tabSongFolder = new System.Windows.Forms.TabPage();
            this.btnDeduplication = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.trackVolume = new System.Windows.Forms.TrackBar();
            this.btnPlay = new System.Windows.Forms.Button();
            this.btnExportFavor = new System.Windows.Forms.Button();
            this.btnInfo = new System.Windows.Forms.Button();
            this.btnSaveList = new System.Windows.Forms.Button();
            this.btnSetImg = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.songListView = new System.Windows.Forms.ListView();
            this.songName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.bsr = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.bpm = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.musicPackListView = new System.Windows.Forms.ListView();
            this.musicPackimg = new System.Windows.Forms.ImageList(this.components);
            this.lblMap = new System.Windows.Forms.Label();
            this.lblMusicPack = new System.Windows.Forms.Label();
            this.tabBSVer = new System.Windows.Forms.TabPage();
            this.tabDelicatedSong = new System.Windows.Forms.TabPage();
            this.tabFolderandList = new System.Windows.Forms.TabPage();
            this.btnSetting = new System.Windows.Forms.Button();
            this.comboBoxPlatform = new System.Windows.Forms.ComboBox();
            this.lblPlatform = new System.Windows.Forms.Label();
            this.btnInstallEverything = new System.Windows.Forms.Button();
            this.lblExtension = new System.Windows.Forms.Label();
            this.btnTutorial = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.PlaybackTimer = new System.Windows.Forms.Timer(this.components);
            this.musicPackCoverDialog = new System.Windows.Forms.OpenFileDialog();
            this.savebplistDialog = new System.Windows.Forms.SaveFileDialog();
            this.BSIMMStats.SuspendLayout();
            this.tabMusicPackContorl.SuspendLayout();
            this.tabSongFolder.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackVolume)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // BSIMMStats
            // 
            this.BSIMMStats.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.BSIMMActionText,
            this.BSIMMStatusText,
            this.BSIMMProgress});
            this.BSIMMStats.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.BSIMMStats.Location = new System.Drawing.Point(0, 479);
            this.BSIMMStats.Name = "BSIMMStats";
            this.BSIMMStats.Size = new System.Drawing.Size(704, 22);
            this.BSIMMStats.TabIndex = 1;
            this.BSIMMStats.Text = "statusStrip1";
            // 
            // BSIMMActionText
            // 
            this.BSIMMActionText.Name = "BSIMMActionText";
            this.BSIMMActionText.Size = new System.Drawing.Size(32, 17);
            this.BSIMMActionText.Text = "信息";
            // 
            // BSIMMStatusText
            // 
            this.BSIMMStatusText.Name = "BSIMMStatusText";
            this.BSIMMStatusText.Size = new System.Drawing.Size(0, 17);
            // 
            // BSIMMProgress
            // 
            this.BSIMMProgress.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.BSIMMProgress.AutoSize = false;
            this.BSIMMProgress.Margin = new System.Windows.Forms.Padding(50, 3, 1, 3);
            this.BSIMMProgress.Name = "BSIMMProgress";
            this.BSIMMProgress.Size = new System.Drawing.Size(165, 16);
            // 
            // txtDebug
            // 
            this.txtDebug.Location = new System.Drawing.Point(0, 397);
            this.txtDebug.Name = "txtDebug";
            this.txtDebug.ReadOnly = true;
            this.txtDebug.Size = new System.Drawing.Size(587, 79);
            this.txtDebug.TabIndex = 4;
            this.txtDebug.Text = "程序日志：日志文件将同步到软件目录下*.log文件\n";
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.Location = new System.Drawing.Point(105, 330);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(91, 32);
            this.btnOpenFolder.TabIndex = 5;
            this.btnOpenFolder.Text = "添加曲包目录";
            this.btnOpenFolder.UseVisualStyleBackColor = true;
            this.btnOpenFolder.Click += new System.EventHandler(this.btnOpenFolder_Click);
            // 
            // btnAutoFill
            // 
            this.btnAutoFill.Location = new System.Drawing.Point(9, 330);
            this.btnAutoFill.Name = "btnAutoFill";
            this.btnAutoFill.Size = new System.Drawing.Size(90, 32);
            this.btnAutoFill.TabIndex = 6;
            this.btnAutoFill.Text = "自动检测";
            this.btnAutoFill.UseVisualStyleBackColor = true;
            // 
            // btnSaveMusicPack
            // 
            this.btnSaveMusicPack.Location = new System.Drawing.Point(202, 330);
            this.btnSaveMusicPack.Name = "btnSaveMusicPack";
            this.btnSaveMusicPack.Size = new System.Drawing.Size(88, 32);
            this.btnSaveMusicPack.TabIndex = 7;
            this.btnSaveMusicPack.Text = "保存曲包目录";
            this.btnSaveMusicPack.UseVisualStyleBackColor = true;
            this.btnSaveMusicPack.Click += new System.EventHandler(this.btnSaveMusicPack_Click);
            // 
            // tabMusicPackContorl
            // 
            this.tabMusicPackContorl.AllowDrop = true;
            this.tabMusicPackContorl.Controls.Add(this.tabSongFolder);
            this.tabMusicPackContorl.Controls.Add(this.tabBSVer);
            this.tabMusicPackContorl.Controls.Add(this.tabDelicatedSong);
            this.tabMusicPackContorl.Controls.Add(this.tabFolderandList);
            this.tabMusicPackContorl.Location = new System.Drawing.Point(0, 0);
            this.tabMusicPackContorl.Name = "tabMusicPackContorl";
            this.tabMusicPackContorl.SelectedIndex = 0;
            this.tabMusicPackContorl.Size = new System.Drawing.Size(591, 391);
            this.tabMusicPackContorl.TabIndex = 8;
            // 
            // tabSongFolder
            // 
            this.tabSongFolder.Controls.Add(this.btnDeduplication);
            this.tabSongFolder.Controls.Add(this.label3);
            this.tabSongFolder.Controls.Add(this.trackVolume);
            this.tabSongFolder.Controls.Add(this.btnPlay);
            this.tabSongFolder.Controls.Add(this.btnExportFavor);
            this.tabSongFolder.Controls.Add(this.btnInfo);
            this.tabSongFolder.Controls.Add(this.btnSaveList);
            this.tabSongFolder.Controls.Add(this.btnSetImg);
            this.tabSongFolder.Controls.Add(this.label1);
            this.tabSongFolder.Controls.Add(this.songListView);
            this.tabSongFolder.Controls.Add(this.musicPackListView);
            this.tabSongFolder.Controls.Add(this.lblMap);
            this.tabSongFolder.Controls.Add(this.lblMusicPack);
            this.tabSongFolder.Controls.Add(this.btnSaveMusicPack);
            this.tabSongFolder.Controls.Add(this.btnAutoFill);
            this.tabSongFolder.Controls.Add(this.btnOpenFolder);
            this.tabSongFolder.Location = new System.Drawing.Point(4, 22);
            this.tabSongFolder.Name = "tabSongFolder";
            this.tabSongFolder.Padding = new System.Windows.Forms.Padding(3);
            this.tabSongFolder.Size = new System.Drawing.Size(583, 365);
            this.tabSongFolder.TabIndex = 0;
            this.tabSongFolder.Text = "歌曲目录列表";
            this.tabSongFolder.UseVisualStyleBackColor = true;
            // 
            // btnDeduplication
            // 
            this.btnDeduplication.Location = new System.Drawing.Point(489, 287);
            this.btnDeduplication.Name = "btnDeduplication";
            this.btnDeduplication.Size = new System.Drawing.Size(88, 31);
            this.btnDeduplication.TabIndex = 24;
            this.btnDeduplication.Text = "一键去重";
            this.btnDeduplication.UseVisualStyleBackColor = true;
            this.btnDeduplication.Click += new System.EventHandler(this.btnDeduplication_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(299, 287);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 12);
            this.label3.TabIndex = 23;
            this.label3.Text = "音量:";
            // 
            // trackVolume
            // 
            this.trackVolume.BackColor = System.Drawing.Color.White;
            this.trackVolume.Location = new System.Drawing.Point(340, 284);
            this.trackVolume.Maximum = 100;
            this.trackVolume.Name = "trackVolume";
            this.trackVolume.Size = new System.Drawing.Size(88, 45);
            this.trackVolume.TabIndex = 22;
            this.trackVolume.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trackVolume.Value = 20;
            this.trackVolume.Scroll += new System.EventHandler(this.trackVolume_Scroll);
            // 
            // btnPlay
            // 
            this.btnPlay.Location = new System.Drawing.Point(301, 330);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(88, 31);
            this.btnPlay.TabIndex = 21;
            this.btnPlay.Text = "播放";
            this.btnPlay.UseVisualStyleBackColor = true;
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
            // 
            // btnExportFavor
            // 
            this.btnExportFavor.Location = new System.Drawing.Point(489, 330);
            this.btnExportFavor.Name = "btnExportFavor";
            this.btnExportFavor.Size = new System.Drawing.Size(88, 31);
            this.btnExportFavor.TabIndex = 20;
            this.btnExportFavor.Text = "导出收藏";
            this.btnExportFavor.UseVisualStyleBackColor = true;
            // 
            // btnInfo
            // 
            this.btnInfo.BackColor = System.Drawing.Color.Transparent;
            this.btnInfo.Location = new System.Drawing.Point(395, 330);
            this.btnInfo.Name = "btnInfo";
            this.btnInfo.Size = new System.Drawing.Size(88, 31);
            this.btnInfo.TabIndex = 18;
            this.btnInfo.Text = "详细信息";
            this.btnInfo.UseVisualStyleBackColor = false;
            this.btnInfo.Click += new System.EventHandler(this.btnInfo_Click);
            // 
            // btnSaveList
            // 
            this.btnSaveList.Location = new System.Drawing.Point(202, 291);
            this.btnSaveList.Name = "btnSaveList";
            this.btnSaveList.Size = new System.Drawing.Size(88, 31);
            this.btnSaveList.TabIndex = 16;
            this.btnSaveList.Text = "保存列表";
            this.btnSaveList.UseVisualStyleBackColor = true;
            this.btnSaveList.Click += new System.EventHandler(this.btnSaveList_Click);
            // 
            // btnSetImg
            // 
            this.btnSetImg.Location = new System.Drawing.Point(105, 290);
            this.btnSetImg.Name = "btnSetImg";
            this.btnSetImg.Size = new System.Drawing.Size(91, 32);
            this.btnSetImg.TabIndex = 15;
            this.btnSetImg.Text = "选择图片";
            this.btnSetImg.UseVisualStyleBackColor = true;
            this.btnSetImg.Click += new System.EventHandler(this.btnSetImg_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 300);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(101, 12);
            this.label1.TabIndex = 14;
            this.label1.Text = "自定义曲包图片：";
            // 
            // songListView
            // 
            this.songListView.AllowDrop = true;
            this.songListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.songName,
            this.bsr,
            this.bpm});
            this.songListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.songListView.HideSelection = false;
            this.songListView.Location = new System.Drawing.Point(295, 29);
            this.songListView.Name = "songListView";
            this.songListView.Size = new System.Drawing.Size(282, 247);
            this.songListView.TabIndex = 13;
            this.songListView.UseCompatibleStateImageBehavior = false;
            this.songListView.View = System.Windows.Forms.View.Details;
            this.songListView.SelectedIndexChanged += new System.EventHandler(this.songListView_SelectedIndexChanged);
            // 
            // songName
            // 
            this.songName.Text = "曲名";
            this.songName.Width = 140;
            // 
            // bsr
            // 
            this.bsr.Text = "bsr";
            this.bsr.Width = 42;
            // 
            // bpm
            // 
            this.bpm.Text = "bpm";
            this.bpm.Width = 40;
            // 
            // musicPackListView
            // 
            this.musicPackListView.AllowDrop = true;
            this.musicPackListView.HideSelection = false;
            this.musicPackListView.LargeImageList = this.musicPackimg;
            this.musicPackListView.Location = new System.Drawing.Point(8, 29);
            this.musicPackListView.MultiSelect = false;
            this.musicPackListView.Name = "musicPackListView";
            this.musicPackListView.Size = new System.Drawing.Size(282, 247);
            this.musicPackListView.TabIndex = 12;
            this.musicPackListView.UseCompatibleStateImageBehavior = false;
            this.musicPackListView.ItemMouseHover += new System.Windows.Forms.ListViewItemMouseHoverEventHandler(this.musicPackListView_ItemMouseHover);
            this.musicPackListView.Click += new System.EventHandler(this.musicPackListView_Click);
            this.musicPackListView.DragDrop += new System.Windows.Forms.DragEventHandler(this.DropEvent);
            this.musicPackListView.DragEnter += new System.Windows.Forms.DragEventHandler(this.DragEvent);
            // 
            // musicPackimg
            // 
            this.musicPackimg.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.musicPackimg.ImageSize = new System.Drawing.Size(84, 84);
            this.musicPackimg.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // lblMap
            // 
            this.lblMap.AutoSize = true;
            this.lblMap.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblMap.Location = new System.Drawing.Point(293, 6);
            this.lblMap.Name = "lblMap";
            this.lblMap.Size = new System.Drawing.Size(103, 16);
            this.lblMap.TabIndex = 10;
            this.lblMap.Text = "曲包歌曲列表";
            // 
            // lblMusicPack
            // 
            this.lblMusicPack.AutoSize = true;
            this.lblMusicPack.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblMusicPack.Location = new System.Drawing.Point(10, 6);
            this.lblMusicPack.Name = "lblMusicPack";
            this.lblMusicPack.Size = new System.Drawing.Size(103, 16);
            this.lblMusicPack.TabIndex = 9;
            this.lblMusicPack.Text = "曲包目录列表";
            // 
            // tabBSVer
            // 
            this.tabBSVer.Location = new System.Drawing.Point(4, 22);
            this.tabBSVer.Name = "tabBSVer";
            this.tabBSVer.Padding = new System.Windows.Forms.Padding(3);
            this.tabBSVer.Size = new System.Drawing.Size(583, 365);
            this.tabBSVer.TabIndex = 1;
            this.tabBSVer.Text = "节奏光剑版本管理";
            this.tabBSVer.UseVisualStyleBackColor = true;
            // 
            // tabDelicatedSong
            // 
            this.tabDelicatedSong.Location = new System.Drawing.Point(4, 22);
            this.tabDelicatedSong.Name = "tabDelicatedSong";
            this.tabDelicatedSong.Size = new System.Drawing.Size(583, 365);
            this.tabDelicatedSong.TabIndex = 2;
            this.tabDelicatedSong.Text = "散装歌曲列表";
            this.tabDelicatedSong.UseVisualStyleBackColor = true;
            // 
            // tabFolderandList
            // 
            this.tabFolderandList.Location = new System.Drawing.Point(4, 22);
            this.tabFolderandList.Name = "tabFolderandList";
            this.tabFolderandList.Size = new System.Drawing.Size(583, 365);
            this.tabFolderandList.TabIndex = 3;
            this.tabFolderandList.Text = "歌曲目录编辑/歌单编辑";
            this.tabFolderandList.UseVisualStyleBackColor = true;
            // 
            // btnSetting
            // 
            this.btnSetting.Location = new System.Drawing.Point(602, 359);
            this.btnSetting.Name = "btnSetting";
            this.btnSetting.Size = new System.Drawing.Size(91, 32);
            this.btnSetting.TabIndex = 9;
            this.btnSetting.Text = "程序设置";
            this.btnSetting.UseVisualStyleBackColor = true;
            // 
            // comboBoxPlatform
            // 
            this.comboBoxPlatform.FormattingEnabled = true;
            this.comboBoxPlatform.Items.AddRange(new object[] {
            "PC-Steam",
            "PC-Oculus",
            "Quest"});
            this.comboBoxPlatform.Location = new System.Drawing.Point(604, 333);
            this.comboBoxPlatform.Name = "comboBoxPlatform";
            this.comboBoxPlatform.Size = new System.Drawing.Size(88, 20);
            this.comboBoxPlatform.TabIndex = 10;
            this.comboBoxPlatform.SelectedIndexChanged += new System.EventHandler(this.comboBoxPlatform_SelectedIndexChanged);
            // 
            // lblPlatform
            // 
            this.lblPlatform.AutoSize = true;
            this.lblPlatform.Location = new System.Drawing.Point(615, 311);
            this.lblPlatform.Name = "lblPlatform";
            this.lblPlatform.Size = new System.Drawing.Size(65, 12);
            this.lblPlatform.TabIndex = 11;
            this.lblPlatform.Text = "平台选择：";
            // 
            // btnInstallEverything
            // 
            this.btnInstallEverything.Location = new System.Drawing.Point(602, 266);
            this.btnInstallEverything.Name = "btnInstallEverything";
            this.btnInstallEverything.Size = new System.Drawing.Size(90, 32);
            this.btnInstallEverything.TabIndex = 12;
            this.btnInstallEverything.Text = "安装增强扩展";
            this.btnInstallEverything.UseVisualStyleBackColor = true;
            // 
            // lblExtension
            // 
            this.lblExtension.AutoSize = true;
            this.lblExtension.Location = new System.Drawing.Point(602, 83);
            this.lblExtension.MaximumSize = new System.Drawing.Size(90, 0);
            this.lblExtension.Name = "lblExtension";
            this.lblExtension.Size = new System.Drawing.Size(89, 168);
            this.lblExtension.TabIndex = 13;
            this.lblExtension.Text = "提示：软件支持Everything增强扩展，该扩展可以帮助本软件更好的管理多个版本的节奏光剑，同时可以更快速的搜寻歌曲文件，如果想使用本软件的增强功能，可点击安装" +
    "增强扩展一件安装（已安装请忽略本提示）";
            // 
            // btnTutorial
            // 
            this.btnTutorial.Location = new System.Drawing.Point(602, 397);
            this.btnTutorial.Name = "btnTutorial";
            this.btnTutorial.Size = new System.Drawing.Size(91, 32);
            this.btnTutorial.TabIndex = 14;
            this.btnTutorial.Text = "操作指南";
            this.btnTutorial.UseVisualStyleBackColor = true;
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(602, 435);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(91, 32);
            this.btnExit.TabIndex = 15;
            this.btnExit.Text = "退出";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::BeatSaberIndependentMapsManager.Properties.Resources.everything;
            this.pictureBox1.Location = new System.Drawing.Point(615, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(64, 64);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 16;
            this.pictureBox1.TabStop = false;
            // 
            // PlaybackTimer
            // 
            this.PlaybackTimer.Tick += new System.EventHandler(this.PlaybackTimer_Tick);
            // 
            // musicPackCoverDialog
            // 
            this.musicPackCoverDialog.Filter = "JPEG  (*.jpg)|*.jpg|Pictures (*.png)|*.png|bitmap (*.bmp)|*.bmp";
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(704, 501);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.btnTutorial);
            this.Controls.Add(this.lblExtension);
            this.Controls.Add(this.btnInstallEverything);
            this.Controls.Add(this.lblPlatform);
            this.Controls.Add(this.comboBoxPlatform);
            this.Controls.Add(this.btnSetting);
            this.Controls.Add(this.tabMusicPackContorl);
            this.Controls.Add(this.txtDebug);
            this.Controls.Add(this.BSIMMStats);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BSIMM-独立曲包管理/编辑器 v1.0.0 @万毒不侵";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.BSIMMStats.ResumeLayout(false);
            this.BSIMMStats.PerformLayout();
            this.tabMusicPackContorl.ResumeLayout(false);
            this.tabSongFolder.ResumeLayout(false);
            this.tabSongFolder.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackVolume)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private System.Windows.Forms.SaveFileDialog savebplistDialog;
    }
}