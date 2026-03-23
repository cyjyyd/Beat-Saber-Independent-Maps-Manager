using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// 更新管理器，负责检查、下载和应用更新
    /// </summary>
    internal class UpdateManager : IDisposable
    {
        private readonly GitHubReleaseClient _client;
        private readonly Config _config;
        private readonly string _tempPath;
        private readonly string _backupPath;
        private bool _disposed;

        /// <summary>
        /// 当前是否有更新正在进行
        /// </summary>
        public bool IsUpdating { get; private set; }

        /// <summary>
        /// 创建更新管理器
        /// </summary>
        public UpdateManager(Config config)
        {
            _config = config;
            _client = new GitHubReleaseClient();
            _tempPath = Path.Combine(Path.GetTempPath(), "BSIMM_Update");
            _backupPath = Path.Combine(Application.StartupPath, "BSIMM_backup");
        }

        /// <summary>
        /// 检查是否有可用更新
        /// </summary>
        /// <returns>更新信息，如果没有更新返回 null</returns>
        public async Task<UpdateInfo> CheckForUpdateAsync()
        {
            try
            {
                var release = await _client.GetLatestReleaseAsync();
                if (release == null)
                    return null;

                // 检查是否跳过此版本
                if (_config.SkipVersion == release.TagName)
                    return null;

                // 检查是否有新版本
                if (!AppVersion.IsUpdateAvailable(release.TagName))
                    return null;

                // 检查是否有 ZIP 资源
                var zipAsset = _client.GetZipAsset(release);
                if (zipAsset == null)
                    return null;

                return new UpdateInfo
                {
                    Release = release,
                    ZipAsset = zipAsset,
                    CurrentVersion = AppVersion.VersionString,
                    NewVersion = release.TagName,
                    ReleaseNotes = release.Body,
                    DownloadUrl = zipAsset.BrowserDownloadUrl,
                    DownloadSize = zipAsset.Size
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 下载并应用更新
        /// </summary>
        /// <param name="updateInfo">更新信息</param>
        /// <param name="progress">下载进度回调</param>
        /// <returns>更新是否成功</returns>
        public async Task<bool> DownloadAndApplyUpdateAsync(UpdateInfo updateInfo, IProgress<int> progress = null)
        {
            if (IsUpdating)
                return false;

            IsUpdating = true;

            try
            {
                // 创建临时目录
                if (!Directory.Exists(_tempPath))
                    Directory.CreateDirectory(_tempPath);

                // 下载 ZIP 文件
                string zipPath = Path.Combine(_tempPath, updateInfo.ZipAsset.Name);

                bool downloaded = await _client.DownloadAssetAsync(updateInfo.ZipAsset, zipPath, progress);
                if (!downloaded)
                {
                    CleanupTempFiles();
                    return false;
                }

                // 创建备份
                CreateBackup();

                // 解压更新
                bool extracted = ExtractUpdate(zipPath);
                if (!extracted)
                {
                    RestoreBackup();
                    return false;
                }

                // 清理备份和临时文件
                CleanupBackup();
                CleanupTempFiles();

                return true;
            }
            catch (Exception)
            {
                RestoreBackup();
                return false;
            }
            finally
            {
                IsUpdating = false;
            }
        }

        /// <summary>
        /// 创建当前版本的备份
        /// </summary>
        private void CreateBackup()
        {
            try
            {
                // 删除旧备份
                if (Directory.Exists(_backupPath))
                    Directory.Delete(_backupPath, true);

                Directory.CreateDirectory(_backupPath);

                // 备份关键文件
                string[] filesToBackup = {
                    "BeatSaberIndependentMapsManager.exe",
                    "BeatSaberIndependentMapsManager.dll",
                    "BeatSaberIndependentMapsManager.runtimeconfig.json",
                    "BeatSaberIndependentMapsManager.deps.json"
                };

                foreach (var file in filesToBackup)
                {
                    string sourcePath = Path.Combine(Application.StartupPath, file);
                    if (File.Exists(sourcePath))
                    {
                        string destPath = Path.Combine(_backupPath, file);
                        File.Copy(sourcePath, destPath, true);
                    }
                }
            }
            catch (Exception)
            {
                // 备份失败不阻止更新
            }
        }

        /// <summary>
        /// 解压更新文件
        /// </summary>
        private bool ExtractUpdate(string zipPath)
        {
            try
            {
                // 先解压到临时子目录
                string extractPath = Path.Combine(_tempPath, "extracted");
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);

                ZipFile.ExtractToDirectory(zipPath, extractPath);

                // 复制文件到程序目录
                CopyDirectory(extractPath, Application.StartupPath);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 复制目录内容
        /// </summary>
        private void CopyDirectory(string sourceDir, string destDir)
        {
            // 复制文件
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, true);
            }

            // 递归复制子目录
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dir);
                string destSubDir = Path.Combine(destDir, dirName);

                if (!Directory.Exists(destSubDir))
                    Directory.CreateDirectory(destSubDir);

                CopyDirectory(dir, destSubDir);
            }
        }

        /// <summary>
        /// 从备份恢复
        /// </summary>
        private void RestoreBackup()
        {
            try
            {
                if (Directory.Exists(_backupPath))
                {
                    CopyDirectory(_backupPath, Application.StartupPath);
                }
            }
            catch (Exception)
            {
                // 恢复失败
            }
        }

        /// <summary>
        /// 清理备份目录
        /// </summary>
        private void CleanupBackup()
        {
            try
            {
                if (Directory.Exists(_backupPath))
                    Directory.Delete(_backupPath, true);
            }
            catch (Exception)
            {
                // 清理失败不阻止更新
            }
        }

        /// <summary>
        /// 清理临时文件
        /// </summary>
        private void CleanupTempFiles()
        {
            try
            {
                if (Directory.Exists(_tempPath))
                    Directory.Delete(_tempPath, true);
            }
            catch (Exception)
            {
                // 清理失败不阻止更新
            }
        }

        /// <summary>
        /// 重启应用程序
        /// </summary>
        public void RestartApplication()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                Process.Start(exePath);
                Application.Exit();
            }
            catch (Exception)
            {
                // 重启失败
            }
        }

        /// <summary>
        /// 跳过指定版本
        /// </summary>
        public void SkipVersion(string version)
        {
            _config.SkipVersion = version;
            _config.configUpdate();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _client?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 更新信息
    /// </summary>
    internal class UpdateInfo
    {
        public GitHubRelease Release { get; set; }
        public GitHubAsset ZipAsset { get; set; }
        public string CurrentVersion { get; set; }
        public string NewVersion { get; set; }
        public string ReleaseNotes { get; set; }
        public string DownloadUrl { get; set; }
        public long DownloadSize { get; set; }

        /// <summary>
        /// 获取格式化的下载大小
        /// </summary>
        public string GetFormattedSize()
        {
            const long KB = 1024;
            const long MB = KB * 1024;

            if (DownloadSize >= MB)
                return $"{DownloadSize / (double)MB:F2} MB";
            else if (DownloadSize >= KB)
                return $"{DownloadSize / (double)KB:F2} KB";
            else
                return $"{DownloadSize} B";
        }
    }
}