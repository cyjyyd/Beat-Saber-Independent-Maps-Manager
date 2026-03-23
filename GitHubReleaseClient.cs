using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// GitHub Release 信息模型
    /// </summary>
    internal class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("prerelease")]
        public bool Prerelease { get; set; }

        [JsonProperty("draft")]
        public bool Draft { get; set; }

        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }

        [JsonProperty("assets")]
        public List<GitHubAsset> Assets { get; set; } = new List<GitHubAsset>();
    }

    /// <summary>
    /// GitHub Release 资源文件模型
    /// </summary>
    internal class GitHubAsset
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("content_type")]
        public string ContentType { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// GitHub API 客户端，用于检查更新
    /// </summary>
    internal class GitHubReleaseClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _repoOwner;
        private readonly string _repoName;
        private bool _disposed;

        /// <summary>
        /// GitHub API 基础 URL
        /// </summary>
        private const string GitHubApiBase = "https://api.github.com/repos";

        /// <summary>
        /// 创建 GitHub Release 客户端
        /// </summary>
        /// <param name="repoOwner">仓库所有者</param>
        /// <param name="repoName">仓库名称</param>
        public GitHubReleaseClient(string repoOwner = AppVersion.RepoOwner, string repoName = AppVersion.RepoName)
        {
            _repoOwner = repoOwner;
            _repoName = repoName;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", AppVersion.UserAgent);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// 获取最新 Release 信息
        /// </summary>
        /// <returns>最新 Release 信息，如果获取失败返回 null</returns>
        public async Task<GitHubRelease> GetLatestReleaseAsync()
        {
            try
            {
                string url = $"{GitHubApiBase}/{_repoOwner}/{_repoName}/releases/latest";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var release = JsonConvert.DeserializeObject<GitHubRelease>(json);

                // 跳过预发布和草稿版本
                if (release?.Prerelease == true || release?.Draft == true)
                {
                    return null;
                }

                return release;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取 ZIP 格式的资源文件
        /// </summary>
        /// <param name="release">Release 信息</param>
        /// <returns>ZIP 资源文件，如果没有找到返回 null</returns>
        public GitHubAsset GetZipAsset(GitHubRelease release)
        {
            if (release?.Assets == null)
                return null;

            foreach (var asset in release.Assets)
            {
                // 查找 ZIP 文件
                if (asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                    asset.ContentType?.Equals("application/zip", StringComparison.OrdinalIgnoreCase) == true ||
                    asset.ContentType?.Equals("application/x-zip-compressed", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return asset;
                }
            }

            return null;
        }

        /// <summary>
        /// 下载资源文件
        /// </summary>
        /// <param name="asset">资源文件信息</param>
        /// <param name="destinationPath">目标文件路径</param>
        /// <param name="progress">进度回调 (0-100)</param>
        /// <returns>下载是否成功</returns>
        public async Task<bool> DownloadAssetAsync(GitHubAsset asset, string destinationPath, IProgress<int> progress = null)
        {
            try
            {
                var response = await _httpClient.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                    return false;

                var totalBytes = response.Content.Headers.ContentLength ?? asset.Size;
                var downloadedBytes = 0L;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new System.IO.FileStream(destinationPath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
                {
                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        downloadedBytes += bytesRead;

                        if (totalBytes > 0)
                        {
                            int percent = (int)((downloadedBytes * 100) / totalBytes);
                            progress?.Report(percent);
                        }
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}