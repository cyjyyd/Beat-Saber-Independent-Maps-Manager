using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeatSaberIndependentMapsManager
{
    public partial class SettingsForm : Form
    {
        Config config = new Config();
        private LocalCacheManager localCacheManager;
        private bool isDownloading = false;

        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            // 快速加载复选框状态
            checkBoxHashCache.Checked = config.HashCache;
            checkBoxMemFolder.Checked = config.LastFolder;
            checkBoxProxyDownload.Checked = config.DownProxy;
            checkBoxSaverCache.Checked = config.LocalCaChe;
            checkBoxSystemProxy.Checked = config.UseSystemProxy;

            // 绑定复选框事件
            checkBoxSaverCache.CheckedChanged += CheckBoxSaverCache_CheckedChanged;
            checkBoxSystemProxy.CheckedChanged += CheckBoxSystemProxy_CheckedChanged;

            // 显示加载中状态
            lblCacheStatus.Text = "加载中...";
            lblCacheStatus.ForeColor = Color.Gray;
            btnDownloadCache.Enabled = false;

            // 初始化缓存管理器并检查状态（异步执行，不阻塞窗口显示）
            _ = InitializeAndCheckCacheAsync();
        }

        private void CheckBoxSystemProxy_CheckedChanged(object sender, EventArgs e)
        {
            config.UseSystemProxy = checkBoxSystemProxy.Checked;
            if (localCacheManager != null)
            {
                localCacheManager.UseSystemProxy = checkBoxSystemProxy.Checked;
                localCacheManager.ReinitializeHttpClient();
            }
        }

        private async void CheckBoxSaverCache_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxSaverCache.Checked)
            {
                await CheckAndDownloadCacheAsync();
            }
        }

        /// <summary>
        /// 异步初始化缓存管理器并检查状态
        /// </summary>
        private async Task InitializeAndCheckCacheAsync()
        {
            try
            {
                // 初始化本地缓存管理器
                localCacheManager = new LocalCacheManager();
                localCacheManager.UseSystemProxy = config.UseSystemProxy;

                // 读取本地缓存状态
                localCacheManager.RefreshCacheStatus();

                if (!localCacheManager.IsCacheAvailable)
                {
                    lblCacheStatus.Text = "未下载";
                    lblCacheStatus.ForeColor = Color.Red;
                    btnDownloadCache.Enabled = true;
                    btnDownloadCache.Text = "下载本地缓存";
                    return;
                }

                // 显示本地状态
                ShowLocalCacheInfo();

                // 检查远程状态
                await CheckRemoteCacheStatusAsync();
            }
            catch (Exception ex)
            {
                lblCacheStatus.Text = $"初始化失败: {ex.Message}";
                lblCacheStatus.ForeColor = Color.Red;
                btnDownloadCache.Enabled = true;
                btnDownloadCache.Text = "重试";
            }
        }

        /// <summary>
        /// 显示本地缓存信息（不涉及网络请求）
        /// </summary>
        private void ShowLocalCacheInfo()
        {
            long cacheDate = localCacheManager.CacheDate;
            string dateStr = "未知";
            if (cacheDate > 0)
            {
                var date = DateTimeOffset.FromUnixTimeSeconds(cacheDate).LocalDateTime;
                dateStr = date.ToString("yyyy-MM-dd HH:mm");
            }

            long fileSize = localCacheManager.GetCacheFileSize();
            double sizeMB = fileSize / (1024.0 * 1024.0);

            lblCacheStatus.Text = $"已下载 ({dateStr}) | {sizeMB:F1} MB";
            lblCacheStatus.ForeColor = Color.Gray;
            btnDownloadCache.Enabled = true;
            btnDownloadCache.Text = "检查并更新本地缓存";
        }

        /// <summary>
        /// 异步检查远程缓存状态并更新显示
        /// </summary>
        private async Task CheckRemoteCacheStatusAsync()
        {
            if (!localCacheManager.IsCacheAvailable)
                return;

            lblCacheStatus.Text = "检查更新中...";

            try
            {
                bool isOutdated = await localCacheManager.IsCacheOutdatedAsync();

                long cacheDate = localCacheManager.CacheDate;
                string dateStr = "未知";
                if (cacheDate > 0)
                {
                    var date = DateTimeOffset.FromUnixTimeSeconds(cacheDate).LocalDateTime;
                    dateStr = date.ToString("yyyy-MM-dd HH:mm");
                }

                long fileSize = localCacheManager.GetCacheFileSize();
                double sizeMB = fileSize / (1024.0 * 1024.0);

                if (isOutdated)
                {
                    lblCacheStatus.Text = $"已过期 ({dateStr}) | {sizeMB:F1} MB";
                    lblCacheStatus.ForeColor = Color.OrangeRed;
                }
                else
                {
                    lblCacheStatus.Text = $"最新 ({dateStr}) | {sizeMB:F1} MB";
                    lblCacheStatus.ForeColor = Color.Green;
                }
            }
            catch
            {
                // 检查失败，保持灰色状态
                ShowLocalCacheInfo();
            }
        }

        private async void btnDownloadCache_Click(object sender, EventArgs e)
        {
            await CheckAndDownloadCacheAsync();
        }

        private async Task CheckAndDownloadCacheAsync()
        {
            if (isDownloading) return;

            isDownloading = true;
            btnDownloadCache.Enabled = false;
            btnDownloadCache.Text = "检查中...";
            progressBarCache.Visible = true;
            progressBarCache.Value = 0;

            try
            {
                bool needDownload = !localCacheManager.IsCacheAvailable;

                if (!needDownload)
                {
                    btnDownloadCache.Text = "检查更新中...";
                    bool isOutdated = await localCacheManager.IsCacheOutdatedAsync();

                    if (!isOutdated)
                    {
                        lblCacheStatus.Text = "缓存已是最新";
                        lblCacheStatus.ForeColor = Color.Green;
                        btnDownloadCache.Text = "缓存已是最新";
                        progressBarCache.Visible = false;
                        MessageBox.Show("本地缓存已是最新版本，无需更新。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        isDownloading = false;
                        btnDownloadCache.Enabled = true;
                        return;
                    }

                    btnDownloadCache.Text = "发现新版本，准备下载...";
                }

                localCacheManager.DownloadProgress += LocalCacheManager_DownloadProgress;
                bool success = await localCacheManager.DownloadCacheAsync();
                localCacheManager.DownloadProgress -= LocalCacheManager_DownloadProgress;

                if (success)
                {
                    lblCacheStatus.Text = "下载完成";
                    lblCacheStatus.ForeColor = Color.Green;
                    btnDownloadCache.Text = "下载完成";
                    MessageBox.Show("本地缓存下载/更新成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    lblCacheStatus.Text = "下载失败";
                    lblCacheStatus.ForeColor = Color.Red;
                    btnDownloadCache.Text = "下载失败，点击重试";
                    MessageBox.Show("本地缓存下载失败，请检查网络连接后重试。", "下载失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                lblCacheStatus.Text = $"错误: {ex.Message}";
                lblCacheStatus.ForeColor = Color.Red;
                btnDownloadCache.Text = "出错了，点击重试";
                MessageBox.Show($"发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isDownloading = false;
                progressBarCache.Visible = false;
                btnDownloadCache.Enabled = true;

                // 刷新本地状态
                localCacheManager.RefreshCacheStatus();
                if (localCacheManager.IsCacheAvailable)
                {
                    ShowLocalCacheInfo();
                }
            }
        }

        private void LocalCacheManager_DownloadProgress(object sender, CacheDownloadProgress e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(() => LocalCacheManager_DownloadProgress(sender, e));
                return;
            }

            progressBarCache.Visible = true;
            progressBarCache.Value = Math.Min(100, (int)e.Percentage);

            if (!string.IsNullOrEmpty(e.CurrentSource))
            {
                btnDownloadCache.Text = $"下载中 ({e.SourceIndex + 1}/{e.TotalSources}): {e.CurrentSource}";
            }
            else
            {
                btnDownloadCache.Text = e.Status;
            }

            lblCacheStatus.Text = $"{e.Status} {e.Percentage:F1}%";
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            config.HashCache = checkBoxHashCache.Checked;
            config.LocalCaChe = checkBoxSaverCache.Checked;
            config.DownProxy = checkBoxProxyDownload.Checked;
            config.LastFolder = checkBoxMemFolder.Checked;
            config.UseSystemProxy = checkBoxSystemProxy.Checked;
            config.configUpdate();

            localCacheManager?.Dispose();
        }
    }
}