using System;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// 应用程序版本管理类
    /// </summary>
    public static class AppVersion
    {
        /// <summary>
        /// 当前版本号 (不含 v 前缀)
        /// </summary>
        public const string CurrentVersion = "1.0.0";

        /// <summary>
        /// 带有 v 前缀的版本字符串，符合 GitHub Release tag 规范
        /// </summary>
        public static string VersionString => $"v{CurrentVersion}";

        /// <summary>
        /// User-Agent 字符串，用于 HTTP 请求
        /// </summary>
        public static string UserAgent => $"BSIMM/{CurrentVersion}";

        /// <summary>
        /// GitHub 仓库所有者
        /// </summary>
        public const string RepoOwner = "cyjyyd";

        /// <summary>
        /// GitHub 仓库名称
        /// </summary>
        public const string RepoName = "Beat-Saber-Independent-Maps-Manager";

        /// <summary>
        /// 比较当前版本与另一个版本
        /// </summary>
        /// <param name="otherVersion">要比较的版本号 (可包含或不包含 v 前缀)</param>
        /// <returns>
        /// 小于 0: 当前版本低于 otherVersion
        /// 等于 0: 版本相同
        /// 大于 0: 当前版本高于 otherVersion
        /// </returns>
        public static int CompareTo(string otherVersion)
        {
            if (string.IsNullOrWhiteSpace(otherVersion))
                return 1;

            // 移除 v 前缀
            string cleanVersion = otherVersion.TrimStart('v', 'V');

            var currentParts = ParseVersionParts(CurrentVersion);
            var otherParts = ParseVersionParts(cleanVersion);

            // 比较各部分
            for (int i = 0; i < Math.Max(currentParts.Length, otherParts.Length); i++)
            {
                int current = i < currentParts.Length ? currentParts[i] : 0;
                int other = i < otherParts.Length ? otherParts[i] : 0;

                if (current != other)
                    return current.CompareTo(other);
            }

            return 0;
        }

        /// <summary>
        /// 检查是否有可用更新
        /// </summary>
        /// <param name="latestVersion">最新版本号</param>
        /// <returns>true 表示有新版本可用</returns>
        public static bool IsUpdateAvailable(string latestVersion)
        {
            return CompareTo(latestVersion) < 0;
        }

        /// <summary>
        /// 解析版本号为整数数组
        /// </summary>
        private static int[] ParseVersionParts(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return new int[] { 0 };

            var parts = version.Split('.');
            var result = new int[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out int value))
                    result[i] = value;
            }

            return result;
        }
    }
}