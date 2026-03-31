namespace BeatSaberIndependentMapsManager
{
    partial class SettingsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            checkBoxHashCache = new System.Windows.Forms.CheckBox();
            checkBoxMemFolder = new System.Windows.Forms.CheckBox();
            checkBoxProxyDownload = new System.Windows.Forms.CheckBox();
            checkBoxSaverCache = new System.Windows.Forms.CheckBox();
            checkBoxSystemProxy = new System.Windows.Forms.CheckBox();
            lblCacheStatus = new System.Windows.Forms.Label();
            btnDownloadCache = new System.Windows.Forms.Button();
            progressBarCache = new System.Windows.Forms.ProgressBar();
            SuspendLayout();
            //
            // checkBoxHashCache
            //
            checkBoxHashCache.AutoSize = true;
            checkBoxHashCache.Location = new System.Drawing.Point(43, 34);
            checkBoxHashCache.Name = "checkBoxHashCache";
            checkBoxHashCache.Size = new System.Drawing.Size(104, 21);
            checkBoxHashCache.TabIndex = 0;
            checkBoxHashCache.Text = "缓存歌曲Hash";
            checkBoxHashCache.UseVisualStyleBackColor = true;
            //
            // checkBoxMemFolder
            //
            checkBoxMemFolder.AutoSize = true;
            checkBoxMemFolder.Location = new System.Drawing.Point(43, 61);
            checkBoxMemFolder.Name = "checkBoxMemFolder";
            checkBoxMemFolder.Size = new System.Drawing.Size(151, 21);
            checkBoxMemFolder.TabIndex = 1;
            checkBoxMemFolder.Text = "歌曲目录记忆(上一次）";
            checkBoxMemFolder.UseVisualStyleBackColor = true;
            //
            // checkBoxProxyDownload
            //
            checkBoxProxyDownload.AutoSize = true;
            checkBoxProxyDownload.Location = new System.Drawing.Point(43, 88);
            checkBoxProxyDownload.Name = "checkBoxProxyDownload";
            checkBoxProxyDownload.Size = new System.Drawing.Size(155, 21);
            checkBoxProxyDownload.TabIndex = 2;
            checkBoxProxyDownload.Text = "歌曲下载中转加速(测试)";
            checkBoxProxyDownload.UseVisualStyleBackColor = true;
            //
            // checkBoxSystemProxy
            //
            checkBoxSystemProxy.AutoSize = true;
            checkBoxSystemProxy.Checked = true;
            checkBoxSystemProxy.CheckState = System.Windows.Forms.CheckState.Checked;
            checkBoxSystemProxy.Location = new System.Drawing.Point(43, 115);
            checkBoxSystemProxy.Name = "checkBoxSystemProxy";
            checkBoxSystemProxy.Size = new System.Drawing.Size(111, 21);
            checkBoxSystemProxy.TabIndex = 3;
            checkBoxSystemProxy.Text = "使用系统代理";
            checkBoxSystemProxy.UseVisualStyleBackColor = true;
            //
            // checkBoxSaverCache
            //
            checkBoxSaverCache.AutoSize = true;
            checkBoxSaverCache.Location = new System.Drawing.Point(43, 142);
            checkBoxSaverCache.Name = "checkBoxSaverCache";
            checkBoxSaverCache.Size = new System.Drawing.Size(133, 21);
            checkBoxSaverCache.TabIndex = 4;
            checkBoxSaverCache.Text = "BeatSaver本地缓存";
            checkBoxSaverCache.UseVisualStyleBackColor = true;
            //
            // lblCacheStatus
            //
            lblCacheStatus.AutoSize = true;
            lblCacheStatus.ForeColor = System.Drawing.Color.Gray;
            lblCacheStatus.Location = new System.Drawing.Point(60, 166);
            lblCacheStatus.Name = "lblCacheStatus";
            lblCacheStatus.Size = new System.Drawing.Size(56, 17);
            lblCacheStatus.TabIndex = 5;
            lblCacheStatus.Text = "检查中...";
            //
            // btnDownloadCache
            //
            btnDownloadCache.Enabled = false;
            btnDownloadCache.Location = new System.Drawing.Point(46, 186);
            btnDownloadCache.Name = "btnDownloadCache";
            btnDownloadCache.Size = new System.Drawing.Size(203, 28);
            btnDownloadCache.TabIndex = 6;
            btnDownloadCache.Text = "检查并更新本地缓存";
            btnDownloadCache.UseVisualStyleBackColor = true;
            //
            // progressBarCache
            //
            progressBarCache.Location = new System.Drawing.Point(46, 220);
            progressBarCache.Name = "progressBarCache";
            progressBarCache.Size = new System.Drawing.Size(203, 23);
            progressBarCache.TabIndex = 7;
            progressBarCache.Visible = false;
            //
            // SettingsForm
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(295, 260);
            Controls.Add(progressBarCache);
            Controls.Add(btnDownloadCache);
            Controls.Add(lblCacheStatus);
            Controls.Add(checkBoxSaverCache);
            Controls.Add(checkBoxSystemProxy);
            Controls.Add(checkBoxProxyDownload);
            Controls.Add(checkBoxMemFolder);
            Controls.Add(checkBoxHashCache);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "程序设置";
            FormClosing += SettingsForm_FormClosing;
            Load += SettingsForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxHashCache;
        private System.Windows.Forms.CheckBox checkBoxMemFolder;
        private System.Windows.Forms.CheckBox checkBoxProxyDownload;
        private System.Windows.Forms.CheckBox checkBoxSystemProxy;
        private System.Windows.Forms.CheckBox checkBoxSaverCache;
        private System.Windows.Forms.Label lblCacheStatus;
        private System.Windows.Forms.Button btnDownloadCache;
        private System.Windows.Forms.ProgressBar progressBarCache;
    }
}