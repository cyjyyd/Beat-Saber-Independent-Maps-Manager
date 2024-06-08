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
            this.tabPlatform = new System.Windows.Forms.TabControl();
            this.tabPC = new System.Windows.Forms.TabPage();
            this.txtPath_pc = new System.Windows.Forms.TextBox();
            this.tabQuest = new System.Windows.Forms.TabPage();
            this.txtPath_quest = new System.Windows.Forms.TextBox();
            this.BSIMMStats = new System.Windows.Forms.StatusStrip();
            this.BSIMMActionText = new System.Windows.Forms.ToolStripStatusLabel();
            this.BSIMMStatusText = new System.Windows.Forms.ToolStripStatusLabel();
            this.BSIMMProgress = new System.Windows.Forms.ToolStripProgressBar();
            this.groupBoxInfo = new System.Windows.Forms.GroupBox();
            this.lblAdvancedOptions = new System.Windows.Forms.Label();
            this.lblCurrentFolder = new System.Windows.Forms.Label();
            this.lblSongCount = new System.Windows.Forms.Label();
            this.lblMusicPack = new System.Windows.Forms.Label();
            this.picMusicPack = new System.Windows.Forms.PictureBox();
            this.lblDescription = new System.Windows.Forms.Label();
            this.groupBoxControl = new System.Windows.Forms.GroupBox();
            this.lblDescription2 = new System.Windows.Forms.Label();
            this.btnGetMapsTutorial = new System.Windows.Forms.Button();
            this.btnDonate = new System.Windows.Forms.Button();
            this.btnExportFavorites = new System.Windows.Forms.Button();
            this.btnExportPlayLists = new System.Windows.Forms.Button();
            this.btnSaveList = new System.Windows.Forms.Button();
            this.btnAddFolder = new System.Windows.Forms.Button();
            this.tabPlatform.SuspendLayout();
            this.tabPC.SuspendLayout();
            this.tabQuest.SuspendLayout();
            this.BSIMMStats.SuspendLayout();
            this.groupBoxInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picMusicPack)).BeginInit();
            this.groupBoxControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabPlatform
            // 
            this.tabPlatform.AllowDrop = true;
            this.tabPlatform.Controls.Add(this.tabPC);
            this.tabPlatform.Controls.Add(this.tabQuest);
            this.tabPlatform.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tabPlatform.Location = new System.Drawing.Point(0, 0);
            this.tabPlatform.Name = "tabPlatform";
            this.tabPlatform.SelectedIndex = 0;
            this.tabPlatform.Size = new System.Drawing.Size(507, 383);
            this.tabPlatform.TabIndex = 0;
            this.tabPlatform.SelectedIndexChanged += new System.EventHandler(this.tabPlatform_SelectedIndexChanged);
            // 
            // tabPC
            // 
            this.tabPC.AllowDrop = true;
            this.tabPC.Controls.Add(this.txtPath_pc);
            this.tabPC.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tabPC.Location = new System.Drawing.Point(4, 26);
            this.tabPC.Name = "tabPC";
            this.tabPC.Padding = new System.Windows.Forms.Padding(3);
            this.tabPC.Size = new System.Drawing.Size(499, 353);
            this.tabPC.TabIndex = 0;
            this.tabPC.Text = "Steam_电脑平台";
            this.tabPC.UseVisualStyleBackColor = true;
            // 
            // txtPath_pc
            // 
            this.txtPath_pc.AllowDrop = true;
            this.txtPath_pc.Location = new System.Drawing.Point(0, 0);
            this.txtPath_pc.Multiline = true;
            this.txtPath_pc.Name = "txtPath_pc";
            this.txtPath_pc.Size = new System.Drawing.Size(501, 354);
            this.txtPath_pc.TabIndex = 0;
            this.txtPath_pc.DragDrop += new System.Windows.Forms.DragEventHandler(this.txtPath_pc_DragDrop);
            this.txtPath_pc.DragEnter += new System.Windows.Forms.DragEventHandler(this.txtPath_pc_DragEnter);
            // 
            // tabQuest
            // 
            this.tabQuest.AllowDrop = true;
            this.tabQuest.Controls.Add(this.txtPath_quest);
            this.tabQuest.Location = new System.Drawing.Point(4, 26);
            this.tabQuest.Name = "tabQuest";
            this.tabQuest.Padding = new System.Windows.Forms.Padding(3);
            this.tabQuest.Size = new System.Drawing.Size(499, 353);
            this.tabQuest.TabIndex = 1;
            this.tabQuest.Text = "Quest_一体机平台";
            this.tabQuest.UseVisualStyleBackColor = true;
            // 
            // txtPath_quest
            // 
            this.txtPath_quest.AllowDrop = true;
            this.txtPath_quest.Location = new System.Drawing.Point(0, 0);
            this.txtPath_quest.Multiline = true;
            this.txtPath_quest.Name = "txtPath_quest";
            this.txtPath_quest.Size = new System.Drawing.Size(501, 354);
            this.txtPath_quest.TabIndex = 0;
            this.txtPath_quest.DragDrop += new System.Windows.Forms.DragEventHandler(this.txtPath_quest_DragDrop);
            this.txtPath_quest.DragEnter += new System.Windows.Forms.DragEventHandler(this.txtPath_quest_DragEnter);
            // 
            // BSIMMStats
            // 
            this.BSIMMStats.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.BSIMMActionText,
            this.BSIMMStatusText,
            this.BSIMMProgress});
            this.BSIMMStats.Location = new System.Drawing.Point(0, 479);
            this.BSIMMStats.Name = "BSIMMStats";
            this.BSIMMStats.Size = new System.Drawing.Size(704, 22);
            this.BSIMMStats.TabIndex = 1;
            this.BSIMMStats.Text = "statusStrip1";
            // 
            // BSIMMActionText
            // 
            this.BSIMMActionText.Name = "BSIMMActionText";
            this.BSIMMActionText.Size = new System.Drawing.Size(56, 17);
            this.BSIMMActionText.Text = "控制台：";
            // 
            // BSIMMStatusText
            // 
            this.BSIMMStatusText.Name = "BSIMMStatusText";
            this.BSIMMStatusText.Size = new System.Drawing.Size(0, 17);
            // 
            // BSIMMProgress
            // 
            this.BSIMMProgress.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.BSIMMProgress.Margin = new System.Windows.Forms.Padding(465, 3, 1, 3);
            this.BSIMMProgress.Name = "BSIMMProgress";
            this.BSIMMProgress.Size = new System.Drawing.Size(165, 16);
            // 
            // groupBoxInfo
            // 
            this.groupBoxInfo.Controls.Add(this.lblAdvancedOptions);
            this.groupBoxInfo.Controls.Add(this.lblCurrentFolder);
            this.groupBoxInfo.Controls.Add(this.lblSongCount);
            this.groupBoxInfo.Controls.Add(this.lblMusicPack);
            this.groupBoxInfo.Controls.Add(this.picMusicPack);
            this.groupBoxInfo.Controls.Add(this.lblDescription);
            this.groupBoxInfo.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBoxInfo.Location = new System.Drawing.Point(6, 386);
            this.groupBoxInfo.Name = "groupBoxInfo";
            this.groupBoxInfo.Size = new System.Drawing.Size(686, 91);
            this.groupBoxInfo.TabIndex = 2;
            this.groupBoxInfo.TabStop = false;
            this.groupBoxInfo.Text = "软件说明";
            // 
            // lblAdvancedOptions
            // 
            this.lblAdvancedOptions.AutoSize = true;
            this.lblAdvancedOptions.Location = new System.Drawing.Point(81, 68);
            this.lblAdvancedOptions.Name = "lblAdvancedOptions";
            this.lblAdvancedOptions.Size = new System.Drawing.Size(92, 16);
            this.lblAdvancedOptions.TabIndex = 5;
            this.lblAdvancedOptions.Text = "高级选项：";
            this.lblAdvancedOptions.Visible = false;
            // 
            // lblCurrentFolder
            // 
            this.lblCurrentFolder.AutoSize = true;
            this.lblCurrentFolder.Location = new System.Drawing.Point(81, 52);
            this.lblCurrentFolder.Name = "lblCurrentFolder";
            this.lblCurrentFolder.Size = new System.Drawing.Size(92, 16);
            this.lblCurrentFolder.TabIndex = 4;
            this.lblCurrentFolder.Text = "所在目录：";
            this.lblCurrentFolder.Visible = false;
            // 
            // lblSongCount
            // 
            this.lblSongCount.AutoSize = true;
            this.lblSongCount.Location = new System.Drawing.Point(81, 36);
            this.lblSongCount.Name = "lblSongCount";
            this.lblSongCount.Size = new System.Drawing.Size(92, 16);
            this.lblSongCount.TabIndex = 3;
            this.lblSongCount.Text = "歌曲数量：";
            this.lblSongCount.Visible = false;
            // 
            // lblMusicPack
            // 
            this.lblMusicPack.AutoSize = true;
            this.lblMusicPack.Location = new System.Drawing.Point(81, 20);
            this.lblMusicPack.Name = "lblMusicPack";
            this.lblMusicPack.Size = new System.Drawing.Size(92, 16);
            this.lblMusicPack.TabIndex = 2;
            this.lblMusicPack.Text = "歌单名称：";
            this.lblMusicPack.Visible = false;
            // 
            // picMusicPack
            // 
            this.picMusicPack.Location = new System.Drawing.Point(11, 20);
            this.picMusicPack.Name = "picMusicPack";
            this.picMusicPack.Size = new System.Drawing.Size(64, 64);
            this.picMusicPack.TabIndex = 1;
            this.picMusicPack.TabStop = false;
            this.picMusicPack.Visible = false;
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.Font = new System.Drawing.Font("黑体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblDescription.Location = new System.Drawing.Point(6, 17);
            this.lblDescription.Margin = new System.Windows.Forms.Padding(0);
            this.lblDescription.MaximumSize = new System.Drawing.Size(675, 80);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(0, 12);
            this.lblDescription.TabIndex = 0;
            // 
            // groupBoxControl
            // 
            this.groupBoxControl.Controls.Add(this.lblDescription2);
            this.groupBoxControl.Controls.Add(this.btnGetMapsTutorial);
            this.groupBoxControl.Controls.Add(this.btnDonate);
            this.groupBoxControl.Controls.Add(this.btnExportFavorites);
            this.groupBoxControl.Controls.Add(this.btnExportPlayLists);
            this.groupBoxControl.Controls.Add(this.btnSaveList);
            this.groupBoxControl.Controls.Add(this.btnAddFolder);
            this.groupBoxControl.Font = new System.Drawing.Font("黑体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBoxControl.Location = new System.Drawing.Point(519, 23);
            this.groupBoxControl.Name = "groupBoxControl";
            this.groupBoxControl.Size = new System.Drawing.Size(172, 355);
            this.groupBoxControl.TabIndex = 3;
            this.groupBoxControl.TabStop = false;
            this.groupBoxControl.Text = "控制";
            // 
            // lblDescription2
            // 
            this.lblDescription2.AutoSize = true;
            this.lblDescription2.Location = new System.Drawing.Point(20, 102);
            this.lblDescription2.MaximumSize = new System.Drawing.Size(135, 80);
            this.lblDescription2.Name = "lblDescription2";
            this.lblDescription2.Size = new System.Drawing.Size(0, 16);
            this.lblDescription2.TabIndex = 6;
            // 
            // btnGetMapsTutorial
            // 
            this.btnGetMapsTutorial.Location = new System.Drawing.Point(15, 305);
            this.btnGetMapsTutorial.Name = "btnGetMapsTutorial";
            this.btnGetMapsTutorial.Size = new System.Drawing.Size(142, 32);
            this.btnGetMapsTutorial.TabIndex = 5;
            this.btnGetMapsTutorial.UseVisualStyleBackColor = true;
            // 
            // btnDonate
            // 
            this.btnDonate.Location = new System.Drawing.Point(15, 267);
            this.btnDonate.Name = "btnDonate";
            this.btnDonate.Size = new System.Drawing.Size(142, 32);
            this.btnDonate.TabIndex = 4;
            this.btnDonate.UseVisualStyleBackColor = true;
            // 
            // btnExportFavorites
            // 
            this.btnExportFavorites.Location = new System.Drawing.Point(15, 229);
            this.btnExportFavorites.Name = "btnExportFavorites";
            this.btnExportFavorites.Size = new System.Drawing.Size(142, 32);
            this.btnExportFavorites.TabIndex = 3;
            this.btnExportFavorites.UseVisualStyleBackColor = true;
            // 
            // btnExportPlayLists
            // 
            this.btnExportPlayLists.Location = new System.Drawing.Point(15, 191);
            this.btnExportPlayLists.Name = "btnExportPlayLists";
            this.btnExportPlayLists.Size = new System.Drawing.Size(142, 32);
            this.btnExportPlayLists.TabIndex = 2;
            this.btnExportPlayLists.UseVisualStyleBackColor = true;
            // 
            // btnSaveList
            // 
            this.btnSaveList.Location = new System.Drawing.Point(15, 62);
            this.btnSaveList.Name = "btnSaveList";
            this.btnSaveList.Size = new System.Drawing.Size(142, 32);
            this.btnSaveList.TabIndex = 1;
            this.btnSaveList.UseVisualStyleBackColor = true;
            // 
            // btnAddFolder
            // 
            this.btnAddFolder.Location = new System.Drawing.Point(15, 25);
            this.btnAddFolder.Name = "btnAddFolder";
            this.btnAddFolder.Size = new System.Drawing.Size(142, 32);
            this.btnAddFolder.TabIndex = 0;
            this.btnAddFolder.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(704, 501);
            this.Controls.Add(this.groupBoxControl);
            this.Controls.Add(this.groupBoxInfo);
            this.Controls.Add(this.BSIMMStats);
            this.Controls.Add(this.tabPlatform);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BSIMM-歌曲路径管理器@万毒不侵";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.tabPlatform.ResumeLayout(false);
            this.tabPC.ResumeLayout(false);
            this.tabPC.PerformLayout();
            this.tabQuest.ResumeLayout(false);
            this.tabQuest.PerformLayout();
            this.BSIMMStats.ResumeLayout(false);
            this.BSIMMStats.PerformLayout();
            this.groupBoxInfo.ResumeLayout(false);
            this.groupBoxInfo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picMusicPack)).EndInit();
            this.groupBoxControl.ResumeLayout(false);
            this.groupBoxControl.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabPlatform;
        private System.Windows.Forms.TabPage tabPC;
        private System.Windows.Forms.TabPage tabQuest;
        private System.Windows.Forms.StatusStrip BSIMMStats;
        private System.Windows.Forms.ToolStripStatusLabel BSIMMActionText;
        private System.Windows.Forms.ToolStripStatusLabel BSIMMStatusText;
        private System.Windows.Forms.ToolStripProgressBar BSIMMProgress;
        private System.Windows.Forms.GroupBox groupBoxInfo;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.GroupBox groupBoxControl;
        private System.Windows.Forms.PictureBox picMusicPack;
        private System.Windows.Forms.Label lblAdvancedOptions;
        private System.Windows.Forms.Label lblCurrentFolder;
        private System.Windows.Forms.Label lblSongCount;
        private System.Windows.Forms.Label lblMusicPack;
        private System.Windows.Forms.Button btnGetMapsTutorial;
        private System.Windows.Forms.Button btnDonate;
        private System.Windows.Forms.Button btnExportFavorites;
        private System.Windows.Forms.Button btnExportPlayLists;
        private System.Windows.Forms.Button btnSaveList;
        private System.Windows.Forms.Button btnAddFolder;
        private System.Windows.Forms.Label lblDescription2;
        private System.Windows.Forms.TextBox txtPath_pc;
        private System.Windows.Forms.TextBox txtPath_quest;
    }
}