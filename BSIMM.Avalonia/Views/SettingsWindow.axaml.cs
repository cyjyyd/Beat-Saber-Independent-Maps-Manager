using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Markup.Xaml;
using global::Avalonia.Media;
using global::Avalonia.Threading;
using BeatSaberIndependentMapsManager;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BSIMM.Avalonia.Views
{
    internal partial class SettingsWindow : Window
    {
        private CheckBox _chkLocalCache = null!;
        private CheckBox _chkHashCache = null!;
        private CheckBox _chkSystemProxy = null!;
        private TextBlock _lblCacheStatus = null!;
        private ProgressBar _progressCache = null!;
        private Button _btnDownloadCache = null!;

        private Config _config;
        private LocalCacheManager _localCache;
        private volatile bool _isClosed;

        public SettingsWindow(Config config, LocalCacheManager localCache)
        {
            AvaloniaXamlLoader.Load(this);
            _chkLocalCache = this.FindControl<CheckBox>("ChkLocalCache")!;
            _chkHashCache = this.FindControl<CheckBox>("ChkHashCache")!;
            _chkSystemProxy = this.FindControl<CheckBox>("ChkSystemProxy")!;
            _lblCacheStatus = this.FindControl<TextBlock>("LblCacheStatus")!;
            _progressCache = this.FindControl<ProgressBar>("ProgressCache")!;
            _btnDownloadCache = this.FindControl<Button>("BtnDownloadCache")!;

            _config = config;
            _localCache = localCache;

            _chkLocalCache.IsChecked = config.LocalCaChe;
            _chkHashCache.IsChecked = config.HashCache;
            _chkSystemProxy.IsChecked = config.UseSystemProxy;

            RefreshCacheStatus();

            // Subscribe to download progress
            _localCache.DownloadProgress += OnDownloadProgress;
            this.Closed += (s, e) => { _isClosed = true; _localCache.DownloadProgress -= OnDownloadProgress; };
        }

        private IBrush? GetBrush(string key)
        {
            if (global::Avalonia.Application.Current?.TryFindResource(key, out var value) == true)
                return value as IBrush;
            return null;
        }

        private void RefreshCacheStatus(bool justDownloaded = false)
        {
            if (_localCache.IsCacheAvailable)
            {
                long sizeBytes = _localCache.GetCacheFileSize();
                double sizeMb = sizeBytes / 1024.0 / 1024.0;
                DateTime cacheDate = DateTimeOffset.FromUnixTimeSeconds(_localCache.CacheDate).LocalDateTime;
                _lblCacheStatus.Text = justDownloaded
                    ? $"下载完成！({cacheDate:yyyy-MM-dd HH:mm}) | {sizeMb:F1} MB"
                    : $"已下载({cacheDate:yyyy-MM-dd HH:mm}) | {sizeMb:F1} MB";
                _lblCacheStatus.Foreground = GetBrush("HighlightGreenBrush");
                _btnDownloadCache.Content = "检查并更新本地缓存";
            }
            else
            {
                _lblCacheStatus.Text = "未下载";
                _lblCacheStatus.Foreground = GetBrush("HighlightRedBrush");
                _btnDownloadCache.Content = "下载本地缓存";
            }
        }

        private void OnDownloadProgress(object? sender, CacheDownloadProgress e)
        {
            if (_isClosed) return;
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_isClosed) return;
                _progressCache.IsVisible = true;
                _progressCache.Value = e.Percentage;
                if (e.Status != null && e.Status.StartsWith("正在下载"))
                    _lblCacheStatus.Text = $"{e.Status} {e.Percentage:F1}%";
                else if (e.Status != null)
                    _lblCacheStatus.Text = e.Status;
                _btnDownloadCache.Content = $"下载中 ({e.SourceIndex + 1}/{e.TotalSources}): {e.CurrentSource}";
            });
        }

        private void OnLocalCacheChanged(object? sender, RoutedEventArgs e)
        {
            _config.LocalCaChe = _chkLocalCache.IsChecked ?? false;
            _config.configUpdate();
        }

        private void OnHashCacheChanged(object? sender, RoutedEventArgs e)
        {
            _config.HashCache = _chkHashCache.IsChecked ?? false;
            _config.configUpdate();
        }

        private void OnSystemProxyChanged(object? sender, RoutedEventArgs e)
        {
            _config.UseSystemProxy = _chkSystemProxy.IsChecked ?? false;
            _config.configUpdate();
            _localCache.UseSystemProxy = _config.UseSystemProxy;
            _localCache.ReinitializeHttpClient();
        }

        private async void OnDownloadCacheClick(object? sender, RoutedEventArgs e)
        {
            _btnDownloadCache.IsEnabled = false;
            _progressCache.IsVisible = true;
            _progressCache.Value = 0;

            bool success;
            if (_localCache.IsCacheAvailable)
            {
                bool outdated = await _localCache.IsCacheOutdatedAsync();
                if (_isClosed) return;
                if (!outdated)
                {
                    _lblCacheStatus.Text = "本地缓存已是最新版本，无需更新。";
                    _lblCacheStatus.Foreground = GetBrush("HighlightGreenBrush");
                    _progressCache.IsVisible = false;
                    _btnDownloadCache.IsEnabled = true;
                    return;
                }
                _lblCacheStatus.Text = "正在更新缓存...";
                success = await _localCache.DownloadCacheAsync();
            }
            else
            {
                _lblCacheStatus.Text = "正在下载缓存...";
                success = await _localCache.DownloadCacheAsync();
            }

            if (_isClosed) return;

            _progressCache.IsVisible = false;
            _btnDownloadCache.IsEnabled = true;

            if (success)
            {
                RefreshCacheStatus(justDownloaded: true);
            }
            else
            {
                _lblCacheStatus.Text = "下载失败，点击重试";
                _lblCacheStatus.Foreground = GetBrush("HighlightRedBrush");
            }
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
