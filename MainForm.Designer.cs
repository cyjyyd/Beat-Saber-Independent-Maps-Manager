using System.Windows.Forms;

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

            // 释放静态HttpClient资源
            try
            {
                imageHttpClient?.CancelPendingRequests();
                imageHttpClient?.Dispose();
            }
            catch { }

            // 释放BeatSaverClient（静态HttpClient）
            try
            {
                beatSaverClient?.Dispose();
            }
            catch { }

            // 释放CancellationTokenSource
            try
            {
                searchCts?.Dispose();
                imageLoadCts?.Dispose();
            }
            catch { }

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
            BSIMMStats = new StatusStrip();
            BSIMMActionText = new ToolStripStatusLabel();
            BSIMMStatusText = new ToolStripStatusLabel();
            BSIMMProgress = new ToolStripProgressBar();
            txtDebug = new RichTextBox();
            BSIMMFolderBrowser = new FolderBrowserDialog();
            btnOpenFolder = new Button();
            btnSaveMusicPack = new Button();
            tabMusicPackContorl = new TabControl();
            tabSongFolder = new TabPage();
            lblProgressText = new Label();
            btnDeduplication = new Button();
            lblVolumeText = new Label();
            btnPlay = new Button();
            trackVolume = new VolumeBarEx();
            trackProgress = new ProgressBarEx();
            btnExportFavor = new Button();
            btnInfo = new Button();
            btnSaveList = new Button();
            btnSetImg = new Button();
            label1 = new Label();
            songListView = new ListView();
            songName = new ColumnHeader();
            bsr = new ColumnHeader();
            bpm = new ColumnHeader();
            musicPackListView = new ListView();
            musicPackimg = new ImageList(components);
            lblMap = new Label();
            lblMusicPack = new Label();
            tabDelicatedSong = new TabPage();
            lblVolumeText2 = new Label();
            lblPreviewdsong = new Label();
            btnFullScan = new Button();
            btnPlay2 = new Button();
            trackProgress2 = new ProgressBarEx();
            trackVolume2 = new VolumeBarEx();
            btnMigrateFolder = new Button();
            DelicatedSongListView = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            columnHeader3 = new ColumnHeader();
            columnHeader4 = new ColumnHeader();
            tabFolderandList = new TabPage();
            dataGridView1 = new DataGridView();
            DB_Select = new DataGridViewCheckBoxColumn();
            DB_bsr = new DataGridViewTextBoxColumn();
            DB_Cover = new DataGridViewImageColumn();
            DB_Name = new DataGridViewTextBoxColumn();
            DB_description = new DataGridViewTextBoxColumn();
            DB_bpm = new DataGridViewTextBoxColumn();
            DB_levelAuthorName = new DataGridViewTextBoxColumn();
            lblFilterResult = new Label();
            pnlFilterButtons = new Panel();
            btnBatchOutput = new Button();
            btnNextPage = new Button();
            lblPageInfo = new Label();
            btnPrevPage = new Button();
            btnExportAll = new Button();
            btnExportSelected = new Button();
            btnSelectInverse = new Button();
            btnSelectAll = new Button();
            filterSummaryPanel = new Panel();
            btnOpenFilterBuilder = new Button();
            lblFilterSummary = new Label();
            btnDFSelect = new Button();
            lbldownloadFolderTip = new Label();
            lbldownloadFolder = new Label();
            textBox1 = new TextBox();
            lblbplistTip = new Label();
            PlaybackTimer = new Timer(components);
            btnSetting = new Button();
            comboBoxPlatform = new ComboBox();
            lblPlatform = new Label();
            btnInstallEverything = new Button();
            lblExtension = new Label();
            btnTutorial = new Button();
            btnExit = new Button();
            pictureBox1 = new PictureBox();
            musicPackCoverDialog = new OpenFileDialog();
            savebplistDialog = new FolderBrowserDialog();
            BSIMMStats.SuspendLayout();
            tabMusicPackContorl.SuspendLayout();
            tabSongFolder.SuspendLayout();
            tabDelicatedSong.SuspendLayout();
            tabFolderandList.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            pnlFilterButtons.SuspendLayout();
            filterSummaryPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // BSIMMStats
            // 
            BSIMMStats.ImageScalingSize = new System.Drawing.Size(24, 24);
            BSIMMStats.Items.AddRange(new ToolStripItem[] { BSIMMActionText, BSIMMStatusText, BSIMMProgress });
            BSIMMStats.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            BSIMMStats.Location = new System.Drawing.Point(0, 818);
            BSIMMStats.Name = "BSIMMStats";
            BSIMMStats.Padding = new Padding(2, 0, 25, 0);
            BSIMMStats.Size = new System.Drawing.Size(1290, 38);
            BSIMMStats.TabIndex = 1;
            BSIMMStats.Text = "statusStrip1";
            // 
            // BSIMMActionText
            // 
            BSIMMActionText.Name = "BSIMMActionText";
            BSIMMActionText.Size = new System.Drawing.Size(46, 31);
            BSIMMActionText.Text = "信息";
            // 
            // BSIMMStatusText
            // 
            BSIMMStatusText.Name = "BSIMMStatusText";
            BSIMMStatusText.Size = new System.Drawing.Size(0, 31);
            // 
            // BSIMMProgress
            // 
            BSIMMProgress.Alignment = ToolStripItemAlignment.Right;
            BSIMMProgress.AutoSize = false;
            BSIMMProgress.Margin = new Padding(50, 3, 1, 3);
            BSIMMProgress.Name = "BSIMMProgress";
            BSIMMProgress.Size = new System.Drawing.Size(302, 32);
            // 
            // txtDebug
            // 
            txtDebug.Location = new System.Drawing.Point(0, 707);
            txtDebug.Margin = new Padding(6);
            txtDebug.Name = "txtDebug";
            txtDebug.ReadOnly = true;
            txtDebug.Size = new System.Drawing.Size(1087, 104);
            txtDebug.TabIndex = 4;
            txtDebug.Text = "";
            // 
            // BSIMMFolderBrowser
            // 
            BSIMMFolderBrowser.Description = "请选择导入\\导出目录";
            // 
            // btnOpenFolder
            // 
            btnOpenFolder.Location = new System.Drawing.Point(38, 606);
            btnOpenFolder.Margin = new Padding(6);
            btnOpenFolder.Name = "btnOpenFolder";
            btnOpenFolder.Size = new System.Drawing.Size(157, 42);
            btnOpenFolder.TabIndex = 5;
            btnOpenFolder.Text = "添加曲包目录";
            btnOpenFolder.UseVisualStyleBackColor = true;
            btnOpenFolder.Click += btnOpenFolder_Click;
            // 
            // btnSaveMusicPack
            // 
            btnSaveMusicPack.Location = new System.Drawing.Point(207, 606);
            btnSaveMusicPack.Margin = new Padding(6);
            btnSaveMusicPack.Name = "btnSaveMusicPack";
            btnSaveMusicPack.Size = new System.Drawing.Size(157, 42);
            btnSaveMusicPack.TabIndex = 7;
            btnSaveMusicPack.Text = "保存曲包目录";
            btnSaveMusicPack.UseVisualStyleBackColor = true;
            btnSaveMusicPack.Click += btnSaveMusicPack_Click;
            // 
            // tabMusicPackContorl
            // 
            tabMusicPackContorl.AllowDrop = true;
            tabMusicPackContorl.Controls.Add(tabSongFolder);
            tabMusicPackContorl.Controls.Add(tabDelicatedSong);
            tabMusicPackContorl.Controls.Add(tabFolderandList);
            tabMusicPackContorl.Location = new System.Drawing.Point(0, 0);
            tabMusicPackContorl.Margin = new Padding(6);
            tabMusicPackContorl.Name = "tabMusicPackContorl";
            tabMusicPackContorl.SelectedIndex = 0;
            tabMusicPackContorl.Size = new System.Drawing.Size(1095, 702);
            tabMusicPackContorl.TabIndex = 8;
            tabMusicPackContorl.SelectedIndexChanged += tabMusicPackContorl_SelectedIndexChanged;
            // 
            // tabSongFolder
            // 
            tabSongFolder.Controls.Add(lblProgressText);
            tabSongFolder.Controls.Add(btnDeduplication);
            tabSongFolder.Controls.Add(lblVolumeText);
            tabSongFolder.Controls.Add(btnPlay);
            tabSongFolder.Controls.Add(trackVolume);
            tabSongFolder.Controls.Add(trackProgress);
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
            tabSongFolder.Controls.Add(btnOpenFolder);
            tabSongFolder.Location = new System.Drawing.Point(4, 33);
            tabSongFolder.Margin = new Padding(6);
            tabSongFolder.Name = "tabSongFolder";
            tabSongFolder.Padding = new Padding(6);
            tabSongFolder.Size = new System.Drawing.Size(1087, 665);
            tabSongFolder.TabIndex = 0;
            tabSongFolder.Text = "曲包目录管理";
            tabSongFolder.UseVisualStyleBackColor = true;
            // 
            // lblProgressText
            // 
            lblProgressText.AutoSize = true;
            lblProgressText.Location = new System.Drawing.Point(559, 569);
            lblProgressText.Margin = new Padding(6, 0, 6, 0);
            lblProgressText.Name = "lblProgressText";
            lblProgressText.Size = new System.Drawing.Size(50, 24);
            lblProgressText.TabIndex = 26;
            lblProgressText.Text = "进度:";
            // 
            // btnDeduplication
            // 
            btnDeduplication.Location = new System.Drawing.Point(919, 560);
            btnDeduplication.Margin = new Padding(6);
            btnDeduplication.Name = "btnDeduplication";
            btnDeduplication.Size = new System.Drawing.Size(157, 42);
            btnDeduplication.TabIndex = 24;
            btnDeduplication.Text = "一键去重";
            btnDeduplication.UseVisualStyleBackColor = true;
            btnDeduplication.Click += btnDeduplication_Click;
            // 
            // lblVolumeText
            // 
            lblVolumeText.AutoSize = true;
            lblVolumeText.Location = new System.Drawing.Point(559, 615);
            lblVolumeText.Margin = new Padding(6, 0, 6, 0);
            lblVolumeText.Name = "lblVolumeText";
            lblVolumeText.Size = new System.Drawing.Size(50, 24);
            lblVolumeText.TabIndex = 23;
            lblVolumeText.Text = "音量:";
            // 
            // btnPlay
            // 
            btnPlay.Location = new System.Drawing.Point(750, 606);
            btnPlay.Margin = new Padding(6);
            btnPlay.Name = "btnPlay";
            btnPlay.Size = new System.Drawing.Size(157, 42);
            btnPlay.TabIndex = 21;
            btnPlay.Text = "播放";
            btnPlay.UseVisualStyleBackColor = true;
            btnPlay.Click += btnPlay_Click;
            // 
            // trackVolume
            // 
            trackVolume.BackColor = System.Drawing.Color.Transparent;
            trackVolume.Location = new System.Drawing.Point(621, 615);
            trackVolume.Margin = new Padding(6);
            trackVolume.Name = "trackVolume";
            trackVolume.Size = new System.Drawing.Size(117, 28);
            trackVolume.TabIndex = 22;
            trackVolume.VolumeColor = System.Drawing.Color.FromArgb(46, 139, 87);
            trackVolume.ValueChanged += trackVolume_ValueChanged;
            // 
            // trackProgress
            // 
            trackProgress.BackColor = System.Drawing.Color.Transparent;
            trackProgress.Location = new System.Drawing.Point(621, 566);
            trackProgress.Margin = new Padding(6);
            trackProgress.Name = "trackProgress";
            trackProgress.Size = new System.Drawing.Size(286, 28);
            trackProgress.TabIndex = 25;
            trackProgress.ValueChanged += trackProgress_ValueChanged;
            // 
            // btnExportFavor
            // 
            btnExportFavor.Location = new System.Drawing.Point(919, 606);
            btnExportFavor.Margin = new Padding(6);
            btnExportFavor.Name = "btnExportFavor";
            btnExportFavor.Size = new System.Drawing.Size(157, 42);
            btnExportFavor.TabIndex = 20;
            btnExportFavor.Text = "导出收藏";
            btnExportFavor.UseVisualStyleBackColor = true;
            // 
            // btnInfo
            // 
            btnInfo.BackColor = System.Drawing.Color.Transparent;
            btnInfo.Location = new System.Drawing.Point(377, 606);
            btnInfo.Margin = new Padding(6);
            btnInfo.Name = "btnInfo";
            btnInfo.Size = new System.Drawing.Size(157, 42);
            btnInfo.TabIndex = 18;
            btnInfo.Text = "详细信息";
            btnInfo.UseVisualStyleBackColor = false;
            btnInfo.Click += btnInfo_Click;
            // 
            // btnSaveList
            // 
            btnSaveList.Location = new System.Drawing.Point(377, 560);
            btnSaveList.Margin = new Padding(6);
            btnSaveList.Name = "btnSaveList";
            btnSaveList.Size = new System.Drawing.Size(157, 42);
            btnSaveList.TabIndex = 16;
            btnSaveList.Text = "保存列表";
            btnSaveList.UseVisualStyleBackColor = true;
            btnSaveList.Click += btnSaveList_Click;
            // 
            // btnSetImg
            // 
            btnSetImg.Location = new System.Drawing.Point(207, 560);
            btnSetImg.Margin = new Padding(6);
            btnSetImg.Name = "btnSetImg";
            btnSetImg.Size = new System.Drawing.Size(157, 42);
            btnSetImg.TabIndex = 15;
            btnSetImg.Text = "选择图片";
            btnSetImg.UseVisualStyleBackColor = true;
            btnSetImg.Click += btnSetImg_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(44, 570);
            label1.Margin = new Padding(6, 0, 6, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(154, 24);
            label1.TabIndex = 14;
            label1.Text = "自定义曲包图片：";
            // 
            // songListView
            // 
            songListView.AllowDrop = true;
            songListView.Columns.AddRange(new ColumnHeader[] { songName, bsr, bpm });
            songListView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            songListView.Location = new System.Drawing.Point(559, 58);
            songListView.Margin = new Padding(6);
            songListView.Name = "songListView";
            songListView.Size = new System.Drawing.Size(513, 490);
            songListView.TabIndex = 13;
            songListView.UseCompatibleStateImageBehavior = false;
            songListView.View = View.Details;
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
            musicPackListView.Location = new System.Drawing.Point(14, 58);
            musicPackListView.Margin = new Padding(6);
            musicPackListView.MultiSelect = false;
            musicPackListView.Name = "musicPackListView";
            musicPackListView.Size = new System.Drawing.Size(530, 490);
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
            musicPackimg.ColorDepth = ColorDepth.Depth8Bit;
            musicPackimg.ImageSize = new System.Drawing.Size(110, 110);
            musicPackimg.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // lblMap
            // 
            lblMap.AutoSize = true;
            lblMap.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
            lblMap.Location = new System.Drawing.Point(559, 11);
            lblMap.Margin = new Padding(6, 0, 6, 0);
            lblMap.Name = "lblMap";
            lblMap.Size = new System.Drawing.Size(154, 24);
            lblMap.TabIndex = 10;
            lblMap.Text = "曲包歌曲列表";
            // 
            // lblMusicPack
            // 
            lblMusicPack.AutoSize = true;
            lblMusicPack.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 134);
            lblMusicPack.Location = new System.Drawing.Point(19, 11);
            lblMusicPack.Margin = new Padding(6, 0, 6, 0);
            lblMusicPack.Name = "lblMusicPack";
            lblMusicPack.Size = new System.Drawing.Size(154, 24);
            lblMusicPack.TabIndex = 9;
            lblMusicPack.Text = "曲包目录列表";
            // 
            // tabDelicatedSong
            // 
            tabDelicatedSong.Controls.Add(lblVolumeText2);
            tabDelicatedSong.Controls.Add(lblPreviewdsong);
            tabDelicatedSong.Controls.Add(btnFullScan);
            tabDelicatedSong.Controls.Add(btnPlay2);
            tabDelicatedSong.Controls.Add(trackProgress2);
            tabDelicatedSong.Controls.Add(trackVolume2);
            tabDelicatedSong.Controls.Add(btnMigrateFolder);
            tabDelicatedSong.Controls.Add(DelicatedSongListView);
            tabDelicatedSong.Location = new System.Drawing.Point(4, 33);
            tabDelicatedSong.Margin = new Padding(6);
            tabDelicatedSong.Name = "tabDelicatedSong";
            tabDelicatedSong.Size = new System.Drawing.Size(1087, 665);
            tabDelicatedSong.TabIndex = 2;
            tabDelicatedSong.Text = "散装歌曲列表";
            tabDelicatedSong.UseVisualStyleBackColor = true;
            // 
            // lblVolumeText2
            // 
            lblVolumeText2.AutoSize = true;
            lblVolumeText2.Location = new System.Drawing.Point(790, 606);
            lblVolumeText2.Margin = new Padding(5, 0, 5, 0);
            lblVolumeText2.Name = "lblVolumeText2";
            lblVolumeText2.Size = new System.Drawing.Size(64, 24);
            lblVolumeText2.TabIndex = 30;
            lblVolumeText2.Text = "音量：";
            // 
            // lblPreviewdsong
            // 
            lblPreviewdsong.AutoSize = true;
            lblPreviewdsong.Location = new System.Drawing.Point(363, 606);
            lblPreviewdsong.Margin = new Padding(5, 0, 5, 0);
            lblPreviewdsong.Name = "lblPreviewdsong";
            lblPreviewdsong.Size = new System.Drawing.Size(64, 24);
            lblPreviewdsong.TabIndex = 29;
            lblPreviewdsong.Text = "进度：";
            // 
            // btnFullScan
            // 
            btnFullScan.Location = new System.Drawing.Point(10, 597);
            btnFullScan.Margin = new Padding(5, 4, 5, 4);
            btnFullScan.Name = "btnFullScan";
            btnFullScan.Size = new System.Drawing.Size(176, 42);
            btnFullScan.TabIndex = 27;
            btnFullScan.Text = "全盘扫描（增强）";
            btnFullScan.UseVisualStyleBackColor = true;
            btnFullScan.Click += btnFullScan_Click;
            // 
            // btnPlay2
            // 
            btnPlay2.Location = new System.Drawing.Point(681, 597);
            btnPlay2.Margin = new Padding(6);
            btnPlay2.Name = "btnPlay2";
            btnPlay2.Size = new System.Drawing.Size(104, 42);
            btnPlay2.TabIndex = 26;
            btnPlay2.Text = "播放";
            btnPlay2.UseVisualStyleBackColor = true;
            btnPlay2.Click += btnPlay2_Click;
            // 
            // trackProgress2
            // 
            trackProgress2.BackColor = System.Drawing.Color.Transparent;
            trackProgress2.Location = new System.Drawing.Point(433, 604);
            trackProgress2.Margin = new Padding(6);
            trackProgress2.Name = "trackProgress2";
            trackProgress2.Size = new System.Drawing.Size(236, 28);
            trackProgress2.TabIndex = 28;
            trackProgress2.ValueChanged += trackProgress2_ValueChanged;
            // 
            // trackVolume2
            // 
            trackVolume2.BackColor = System.Drawing.Color.Transparent;
            trackVolume2.Location = new System.Drawing.Point(859, 604);
            trackVolume2.Margin = new Padding(6);
            trackVolume2.Name = "trackVolume2";
            trackVolume2.Size = new System.Drawing.Size(108, 28);
            trackVolume2.TabIndex = 24;
            trackVolume2.VolumeColor = System.Drawing.Color.FromArgb(46, 139, 87);
            trackVolume2.ValueChanged += trackVolume2_ValueChanged;
            // 
            // btnMigrateFolder
            // 
            btnMigrateFolder.Location = new System.Drawing.Point(196, 597);
            btnMigrateFolder.Margin = new Padding(5, 4, 5, 4);
            btnMigrateFolder.Name = "btnMigrateFolder";
            btnMigrateFolder.Size = new System.Drawing.Size(157, 42);
            btnMigrateFolder.TabIndex = 16;
            btnMigrateFolder.Text = "整合到歌曲目录";
            btnMigrateFolder.UseVisualStyleBackColor = true;
            btnMigrateFolder.Click += btnMigrateFolder_Click;
            // 
            // DelicatedSongListView
            // 
            DelicatedSongListView.AllowDrop = true;
            DelicatedSongListView.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3, columnHeader4 });
            DelicatedSongListView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            DelicatedSongListView.Location = new System.Drawing.Point(6, 6);
            DelicatedSongListView.Margin = new Padding(6);
            DelicatedSongListView.Name = "DelicatedSongListView";
            DelicatedSongListView.Size = new System.Drawing.Size(1068, 567);
            DelicatedSongListView.TabIndex = 14;
            DelicatedSongListView.UseCompatibleStateImageBehavior = false;
            DelicatedSongListView.View = View.Details;
            DelicatedSongListView.SelectedIndexChanged += DelicatedSongListView_SelectedIndexChanged;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "曲名";
            columnHeader1.Width = 160;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "bsr";
            columnHeader2.Width = 50;
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "bpm";
            columnHeader3.Width = 40;
            // 
            // columnHeader4
            // 
            columnHeader4.Text = "所在目录";
            columnHeader4.Width = 200;
            // 
            // tabFolderandList
            // 
            tabFolderandList.Controls.Add(dataGridView1);
            tabFolderandList.Controls.Add(lblFilterResult);
            tabFolderandList.Controls.Add(pnlFilterButtons);
            tabFolderandList.Controls.Add(filterSummaryPanel);
            tabFolderandList.Controls.Add(btnDFSelect);
            tabFolderandList.Controls.Add(lbldownloadFolderTip);
            tabFolderandList.Controls.Add(lbldownloadFolder);
            tabFolderandList.Controls.Add(textBox1);
            tabFolderandList.Controls.Add(lblbplistTip);
            tabFolderandList.Location = new System.Drawing.Point(4, 33);
            tabFolderandList.Margin = new Padding(6);
            tabFolderandList.Name = "tabFolderandList";
            tabFolderandList.Size = new System.Drawing.Size(1087, 665);
            tabFolderandList.TabIndex = 3;
            tabFolderandList.Text = "歌单编辑";
            tabFolderandList.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToOrderColumns = true;
            dataGridView1.Anchor = AnchorStyles.None;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { DB_Select, DB_bsr, DB_Cover, DB_Name, DB_description, DB_bpm, DB_levelAuthorName });
            dataGridView1.Location = new System.Drawing.Point(13, 105);
            dataGridView1.Margin = new Padding(5, 4, 5, 4);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.Size = new System.Drawing.Size(1040, 506);
            dataGridView1.TabIndex = 9;
            // 
            // DB_Select
            // 
            DB_Select.FalseValue = false;
            DB_Select.HeaderText = "选择";
            DB_Select.MinimumWidth = 8;
            DB_Select.Name = "DB_Select";
            DB_Select.TrueValue = true;
            DB_Select.Width = 50;
            // 
            // DB_bsr
            // 
            DB_bsr.HeaderText = "bsr";
            DB_bsr.MinimumWidth = 8;
            DB_bsr.Name = "DB_bsr";
            DB_bsr.ReadOnly = true;
            DB_bsr.Width = 60;
            // 
            // DB_Cover
            // 
            DB_Cover.HeaderText = "封面";
            DB_Cover.MinimumWidth = 8;
            DB_Cover.Name = "DB_Cover";
            DB_Cover.ReadOnly = true;
            DB_Cover.Resizable = DataGridViewTriState.True;
            DB_Cover.SortMode = DataGridViewColumnSortMode.Automatic;
            DB_Cover.Width = 80;
            // 
            // DB_Name
            // 
            DB_Name.HeaderText = "名称";
            DB_Name.MinimumWidth = 8;
            DB_Name.Name = "DB_Name";
            DB_Name.ReadOnly = true;
            DB_Name.Width = 150;
            // 
            // DB_description
            // 
            DB_description.HeaderText = "简介";
            DB_description.MinimumWidth = 8;
            DB_description.Name = "DB_description";
            DB_description.ReadOnly = true;
            DB_description.Width = 200;
            // 
            // DB_bpm
            // 
            DB_bpm.HeaderText = "bpm";
            DB_bpm.MinimumWidth = 8;
            DB_bpm.Name = "DB_bpm";
            DB_bpm.ReadOnly = true;
            DB_bpm.Width = 60;
            // 
            // DB_levelAuthorName
            // 
            DB_levelAuthorName.HeaderText = "谱面作者";
            DB_levelAuthorName.MinimumWidth = 8;
            DB_levelAuthorName.Name = "DB_levelAuthorName";
            DB_levelAuthorName.ReadOnly = true;
            DB_levelAuthorName.Width = 150;
            // 
            // lblFilterResult
            // 
            lblFilterResult.AutoSize = true;
            lblFilterResult.Location = new System.Drawing.Point(13, 77);
            lblFilterResult.Margin = new Padding(5, 0, 5, 0);
            lblFilterResult.Name = "lblFilterResult";
            lblFilterResult.Size = new System.Drawing.Size(100, 24);
            lblFilterResult.TabIndex = 10;
            lblFilterResult.Text = "筛选结果：";
            // 
            // pnlFilterButtons
            // 
            pnlFilterButtons.Controls.Add(btnBatchOutput);
            pnlFilterButtons.Controls.Add(btnNextPage);
            pnlFilterButtons.Controls.Add(lblPageInfo);
            pnlFilterButtons.Controls.Add(btnPrevPage);
            pnlFilterButtons.Controls.Add(btnExportAll);
            pnlFilterButtons.Controls.Add(btnExportSelected);
            pnlFilterButtons.Controls.Add(btnSelectInverse);
            pnlFilterButtons.Controls.Add(btnSelectAll);
            pnlFilterButtons.Dock = DockStyle.Bottom;
            pnlFilterButtons.Location = new System.Drawing.Point(0, 618);
            pnlFilterButtons.Name = "pnlFilterButtons";
            pnlFilterButtons.Size = new System.Drawing.Size(1087, 47);
            pnlFilterButtons.TabIndex = 8;
            // 
            // btnBatchOutput
            // 
            btnBatchOutput.Anchor = AnchorStyles.Top;
            btnBatchOutput.Location = new System.Drawing.Point(503, 1);
            btnBatchOutput.Name = "btnBatchOutput";
            btnBatchOutput.Size = new System.Drawing.Size(100, 47);
            btnBatchOutput.TabIndex = 7;
            btnBatchOutput.Text = "批处理";
            btnBatchOutput.UseVisualStyleBackColor = true;
            // 
            // btnNextPage
            // 
            btnNextPage.Anchor = AnchorStyles.Top;
            btnNextPage.Location = new System.Drawing.Point(953, 0);
            btnNextPage.Name = "btnNextPage";
            btnNextPage.Size = new System.Drawing.Size(100, 47);
            btnNextPage.TabIndex = 6;
            btnNextPage.Text = "下一页";
            btnNextPage.UseVisualStyleBackColor = true;
            btnNextPage.Click += btnNextPage_Click;
            // 
            // lblPageInfo
            // 
            lblPageInfo.Anchor = AnchorStyles.None;
            lblPageInfo.AutoSize = true;
            lblPageInfo.Location = new System.Drawing.Point(793, 12);
            lblPageInfo.Name = "lblPageInfo";
            lblPageInfo.Size = new System.Drawing.Size(86, 24);
            lblPageInfo.TabIndex = 5;
            lblPageInfo.Text = "第 1/1 页";
            lblPageInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnPrevPage
            // 
            btnPrevPage.Anchor = AnchorStyles.Top;
            btnPrevPage.Location = new System.Drawing.Point(618, 0);
            btnPrevPage.Name = "btnPrevPage";
            btnPrevPage.Size = new System.Drawing.Size(100, 47);
            btnPrevPage.TabIndex = 4;
            btnPrevPage.Text = "上一页";
            btnPrevPage.UseVisualStyleBackColor = true;
            btnPrevPage.Click += btnPrevPage_Click;
            // 
            // btnExportAll
            // 
            btnExportAll.Dock = DockStyle.Left;
            btnExportAll.Location = new System.Drawing.Point(336, 0);
            btnExportAll.Name = "btnExportAll";
            btnExportAll.Size = new System.Drawing.Size(161, 47);
            btnExportAll.TabIndex = 3;
            btnExportAll.Text = "导出全部到歌单";
            btnExportAll.UseVisualStyleBackColor = true;
            // 
            // btnExportSelected
            // 
            btnExportSelected.Dock = DockStyle.Left;
            btnExportSelected.Location = new System.Drawing.Point(175, 0);
            btnExportSelected.Name = "btnExportSelected";
            btnExportSelected.Size = new System.Drawing.Size(161, 47);
            btnExportSelected.TabIndex = 2;
            btnExportSelected.Text = "导出选中到歌单";
            btnExportSelected.UseVisualStyleBackColor = true;
            // 
            // btnSelectInverse
            // 
            btnSelectInverse.Dock = DockStyle.Left;
            btnSelectInverse.Location = new System.Drawing.Point(80, 0);
            btnSelectInverse.Name = "btnSelectInverse";
            btnSelectInverse.Size = new System.Drawing.Size(95, 47);
            btnSelectInverse.TabIndex = 1;
            btnSelectInverse.Text = "反选";
            btnSelectInverse.UseVisualStyleBackColor = true;
            // 
            // btnSelectAll
            // 
            btnSelectAll.Dock = DockStyle.Left;
            btnSelectAll.Location = new System.Drawing.Point(0, 0);
            btnSelectAll.Name = "btnSelectAll";
            btnSelectAll.Size = new System.Drawing.Size(80, 47);
            btnSelectAll.TabIndex = 0;
            btnSelectAll.Text = "全选";
            btnSelectAll.UseVisualStyleBackColor = true;
            // 
            // filterSummaryPanel
            // 
            filterSummaryPanel.Controls.Add(btnOpenFilterBuilder);
            filterSummaryPanel.Controls.Add(lblFilterSummary);
            filterSummaryPanel.Dock = DockStyle.Top;
            filterSummaryPanel.Location = new System.Drawing.Point(0, 0);
            filterSummaryPanel.Name = "filterSummaryPanel";
            filterSummaryPanel.Padding = new Padding(5);
            filterSummaryPanel.Size = new System.Drawing.Size(1087, 42);
            filterSummaryPanel.TabIndex = 7;
            // 
            // btnOpenFilterBuilder
            // 
            btnOpenFilterBuilder.Anchor = AnchorStyles.Top;
            btnOpenFilterBuilder.Location = new System.Drawing.Point(917, 5);
            btnOpenFilterBuilder.Name = "btnOpenFilterBuilder";
            btnOpenFilterBuilder.Size = new System.Drawing.Size(165, 32);
            btnOpenFilterBuilder.TabIndex = 1;
            btnOpenFilterBuilder.Text = "编辑筛选条件";
            btnOpenFilterBuilder.UseVisualStyleBackColor = true;
            // 
            // lblFilterSummary
            // 
            lblFilterSummary.Dock = DockStyle.Fill;
            lblFilterSummary.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            lblFilterSummary.Location = new System.Drawing.Point(5, 5);
            lblFilterSummary.Name = "lblFilterSummary";
            lblFilterSummary.Size = new System.Drawing.Size(1077, 32);
            lblFilterSummary.TabIndex = 0;
            lblFilterSummary.Text = "当前筛选：无条件";
            lblFilterSummary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnDFSelect
            // 
            btnDFSelect.Location = new System.Drawing.Point(495, 41);
            btnDFSelect.Margin = new Padding(5, 4, 5, 4);
            btnDFSelect.Name = "btnDFSelect";
            btnDFSelect.Size = new System.Drawing.Size(118, 32);
            btnDFSelect.TabIndex = 4;
            btnDFSelect.Text = "浏览";
            btnDFSelect.UseVisualStyleBackColor = true;
            btnDFSelect.Click += btnDFSelect_Click;
            // 
            // lbldownloadFolderTip
            // 
            lbldownloadFolderTip.AutoSize = true;
            lbldownloadFolderTip.Location = new System.Drawing.Point(613, 45);
            lbldownloadFolderTip.Margin = new Padding(5, 0, 5, 0);
            lbldownloadFolderTip.Name = "lbldownloadFolderTip";
            lbldownloadFolderTip.Size = new System.Drawing.Size(440, 24);
            lbldownloadFolderTip.TabIndex = 3;
            lbldownloadFolderTip.Text = "如留空则默认下载到软件运行目录下的Songs子目录！";
            // 
            // lbldownloadFolder
            // 
            lbldownloadFolder.AutoSize = true;
            lbldownloadFolder.Location = new System.Drawing.Point(13, 46);
            lbldownloadFolder.Margin = new Padding(5, 0, 5, 0);
            lbldownloadFolder.Name = "lbldownloadFolder";
            lbldownloadFolder.Size = new System.Drawing.Size(136, 24);
            lbldownloadFolder.TabIndex = 2;
            lbldownloadFolder.Text = "歌曲下载目录：";
            // 
            // textBox1
            // 
            textBox1.Location = new System.Drawing.Point(167, 41);
            textBox1.Margin = new Padding(5, 4, 5, 4);
            textBox1.Name = "textBox1";
            textBox1.Size = new System.Drawing.Size(317, 30);
            textBox1.TabIndex = 1;
            // 
            // lblbplistTip
            // 
            lblbplistTip.AutoSize = true;
            lblbplistTip.Location = new System.Drawing.Point(13, 13);
            lblbplistTip.Margin = new Padding(5, 0, 5, 0);
            lblbplistTip.Name = "lblbplistTip";
            lblbplistTip.Size = new System.Drawing.Size(766, 24);
            lblbplistTip.TabIndex = 0;
            lblbplistTip.Text = "注：如需要快速从目录生成歌单请使用曲包目录管理中的生成歌单功能！此页面为歌单细化编辑";
            // 
            // PlaybackTimer
            // 
            PlaybackTimer.Interval = 1000;
            PlaybackTimer.Tick += PlaybackTimer_Tick;
            // 
            // btnSetting
            // 
            btnSetting.Location = new System.Drawing.Point(1108, 654);
            btnSetting.Margin = new Padding(6);
            btnSetting.Name = "btnSetting";
            btnSetting.Size = new System.Drawing.Size(157, 42);
            btnSetting.TabIndex = 9;
            btnSetting.Text = "程序设置";
            btnSetting.UseVisualStyleBackColor = true;
            btnSetting.Click += btnSetting_Click;
            // 
            // comboBoxPlatform
            // 
            comboBoxPlatform.FormattingEnabled = true;
            comboBoxPlatform.Items.AddRange(new object[] { "PC-Steam", "PC-Oculus", "Quest" });
            comboBoxPlatform.Location = new System.Drawing.Point(1108, 600);
            comboBoxPlatform.Margin = new Padding(6);
            comboBoxPlatform.Name = "comboBoxPlatform";
            comboBoxPlatform.Size = new System.Drawing.Size(150, 32);
            comboBoxPlatform.TabIndex = 10;
            comboBoxPlatform.SelectedIndexChanged += comboBoxPlatform_SelectedIndexChanged;
            // 
            // lblPlatform
            // 
            lblPlatform.AutoSize = true;
            lblPlatform.Location = new System.Drawing.Point(1128, 562);
            lblPlatform.Margin = new Padding(6, 0, 6, 0);
            lblPlatform.Name = "lblPlatform";
            lblPlatform.Size = new System.Drawing.Size(100, 24);
            lblPlatform.TabIndex = 11;
            lblPlatform.Text = "平台选择：";
            // 
            // btnInstallEverything
            // 
            btnInstallEverything.Location = new System.Drawing.Point(1108, 493);
            btnInstallEverything.Margin = new Padding(6);
            btnInstallEverything.Name = "btnInstallEverything";
            btnInstallEverything.Size = new System.Drawing.Size(157, 42);
            btnInstallEverything.TabIndex = 12;
            btnInstallEverything.Text = "安装增强扩展";
            btnInstallEverything.UseVisualStyleBackColor = true;
            btnInstallEverything.Click += btnInstallEverything_Click;
            // 
            // lblExtension
            // 
            lblExtension.AutoSize = true;
            lblExtension.Location = new System.Drawing.Point(1108, 186);
            lblExtension.Margin = new Padding(6, 0, 6, 0);
            lblExtension.MaximumSize = new System.Drawing.Size(165, 0);
            lblExtension.Name = "lblExtension";
            lblExtension.Size = new System.Drawing.Size(156, 288);
            lblExtension.TabIndex = 13;
            lblExtension.Text = "提示：软件支持Everything增强扩展，该扩展可以帮助本软件更好的管理多个版本的节奏光剑，同时可以更快速的搜寻歌曲文件，如果想使用本软件的增强功能，可点击安装增强扩展下载安装（已安装请忽略本提示）";
            // 
            // btnTutorial
            // 
            btnTutorial.Location = new System.Drawing.Point(1108, 707);
            btnTutorial.Margin = new Padding(6);
            btnTutorial.Name = "btnTutorial";
            btnTutorial.Size = new System.Drawing.Size(157, 42);
            btnTutorial.TabIndex = 14;
            btnTutorial.Text = "操作指南";
            btnTutorial.UseVisualStyleBackColor = true;
            // 
            // btnExit
            // 
            btnExit.Location = new System.Drawing.Point(1108, 767);
            btnExit.Margin = new Padding(6);
            btnExit.Name = "btnExit";
            btnExit.Size = new System.Drawing.Size(157, 42);
            btnExit.TabIndex = 15;
            btnExit.Text = "退出";
            btnExit.UseVisualStyleBackColor = true;
            btnExit.Click += btnExit_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.everything;
            pictureBox1.Location = new System.Drawing.Point(1128, 37);
            pictureBox1.Margin = new Padding(6);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(118, 106);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 16;
            pictureBox1.TabStop = false;
            // 
            // musicPackCoverDialog
            // 
            musicPackCoverDialog.Filter = "JPEG  (*.jpg)|*.jpg|Pictures (*.png)|*.png|bitmap (*.bmp)|*.bmp";
            // 
            // savebplistDialog
            // 
            savebplistDialog.Description = "请选择bplist保存路径";
            // 
            // MainForm
            // 
            AllowDrop = true;
            AutoScaleDimensions = new System.Drawing.SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            ClientSize = new System.Drawing.Size(1290, 856);
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
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(6);
            MaximizeBox = false;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "BSIMM-独立曲包管理/编辑器 v1.0.0 @万毒不侵";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            BSIMMStats.ResumeLayout(false);
            BSIMMStats.PerformLayout();
            tabMusicPackContorl.ResumeLayout(false);
            tabSongFolder.ResumeLayout(false);
            tabSongFolder.PerformLayout();
            tabDelicatedSong.ResumeLayout(false);
            tabDelicatedSong.PerformLayout();
            tabFolderandList.ResumeLayout(false);
            tabFolderandList.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            pnlFilterButtons.ResumeLayout(false);
            pnlFilterButtons.PerformLayout();
            filterSummaryPanel.ResumeLayout(false);
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
        private System.Windows.Forms.Button btnSaveMusicPack;
        private System.Windows.Forms.TabControl tabMusicPackContorl;
        private System.Windows.Forms.TabPage tabSongFolder;
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
        private System.Windows.Forms.Button btnExportFavor;
        private VolumeBarEx trackVolume;
        private ProgressBarEx trackProgress;
        private System.Windows.Forms.OpenFileDialog musicPackCoverDialog;
        private System.Windows.Forms.Button btnDeduplication;
        private System.Windows.Forms.FolderBrowserDialog savebplistDialog;
        private System.Windows.Forms.ListView DelicatedSongListView;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.Timer PlaybackTimer;
        private System.Windows.Forms.Button btnPlay2;
        private System.Windows.Forms.Label lblVolumeText2;
        private VolumeBarEx trackVolume2;
        private ProgressBarEx trackProgress2;
        private System.Windows.Forms.Button btnMigrateFolder;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.Button btnFullScan;
        private System.Windows.Forms.Label lblbplistTip;
        private System.Windows.Forms.Label lbldownloadFolderTip;
        private System.Windows.Forms.Label lbldownloadFolder;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button btnDFSelect;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Label lblFilterResult;
        private System.Windows.Forms.DataGridViewTextBoxColumn DB_bsr;
        private System.Windows.Forms.DataGridViewImageColumn DB_Cover;
        private System.Windows.Forms.DataGridViewTextBoxColumn DB_Name;
        private System.Windows.Forms.DataGridViewTextBoxColumn DB_description;
        private System.Windows.Forms.DataGridViewTextBoxColumn DB_bpm;
        private System.Windows.Forms.DataGridViewTextBoxColumn DB_levelAuthorName;
        private System.Windows.Forms.DataGridViewCheckBoxColumn DB_Select;
        private System.Windows.Forms.Label lblPreviewdsong;
        private System.Windows.Forms.Label lblProgressText;
        private Label lblVolumeText;
        // 歌单编辑页签 - 筛选构建器相关控件
        private System.Windows.Forms.Panel filterSummaryPanel;
        private System.Windows.Forms.Label lblFilterSummary;
        private System.Windows.Forms.Button btnOpenFilterBuilder;
        private System.Windows.Forms.Panel pnlFilterButtons;
        private System.Windows.Forms.Button btnSelectAll;
        private System.Windows.Forms.Button btnSelectInverse;
        private System.Windows.Forms.Button btnExportSelected;
        private System.Windows.Forms.Button btnExportAll;
        private System.Windows.Forms.Button btnPrevPage;
        private System.Windows.Forms.Button btnNextPage;
        private System.Windows.Forms.Label lblPageInfo;
        private Button btnBatchOutput;
    }
}