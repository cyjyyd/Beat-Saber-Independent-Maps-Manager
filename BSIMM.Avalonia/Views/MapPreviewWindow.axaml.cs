using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Markup.Xaml;
using BeatSaberIndependentMapsManager.Services;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace BSIMM.Avalonia.Views
{
    public partial class MapPreviewWindow : Window
    {
        private Panel _webViewContainer = null!;
        private TextBlock _lblStatus = null!;
        private ProgressBar _loadProgress = null!;
        private NativeWebView? _webView;
        private HttpListener? _httpServer;
        private string? _tempZipPath;

        // ArcViewer hosted on GitHub Pages — the same tool beatsaver.com embeds
        private const string ArcViewerUrl = "https://allpoland.github.io/ArcViewer/";

        public MapPreviewWindow()
        {
            AvaloniaXamlLoader.Load(this);
            _webViewContainer = this.FindControl<Panel>("WebViewContainer")!;
            _lblStatus = this.FindControl<TextBlock>("LblStatus")!;
            _loadProgress = this.FindControl<ProgressBar>("LoadProgress")!;
            this.Closing += OnWindowClosing;
        }

        public async Task LoadMapAsync(string downloadUrl, string mapName, string? mapId = null)
        {
            this.Title = $"谱面预览 - {mapName}";

            // For online maps with a BeatSaver ID, ArcViewer can fetch the map directly
            if (!string.IsNullOrEmpty(mapId))
            {
                _lblStatus.Text = $"正在加载 ArcViewer (谱面ID: {mapId})...";
                var url = $"{ArcViewerUrl}?id={Uri.EscapeDataString(mapId)}";
                InitializeWebView(url);
                return;
            }

            // Fallback: download the zip and serve it via local HTTP
            _lblStatus.Text = $"正在下载谱面: {mapName}...";
            var tempDir = Path.Combine(Path.GetTempPath(), "bsim_preview");
            var previewService = new MapPreviewService();
            try
            {
                _tempZipPath = await previewService.DownloadMapZipAsync(downloadUrl, tempDir);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"下载失败: {ex.Message}";
                _loadProgress.IsVisible = false;
                return;
            }

            if (_tempZipPath == null)
            {
                _lblStatus.Text = "下载失败，请检查网络连接";
                _loadProgress.IsVisible = false;
                return;
            }

            _lblStatus.Text = "正在启动本地预览服务...";
            string? servedUrl = StartLocalZipServer(_tempZipPath, mapName);
            if (servedUrl == null)
            {
                _lblStatus.Text = "无法启动本地预览服务";
                _loadProgress.IsVisible = false;
                return;
            }

            // Load ArcViewer directly with ?url= pointing to local zip server
            InitializeWebView($"{ArcViewerUrl}?url={servedUrl}map.zip");
        }

        public async Task LoadLocalMapAsync(string mapDir, string mapName)
        {
            this.Title = $"谱面预览 - {mapName}";
            _lblStatus.Text = "正在准备本地谱面...";

            // Create a zip from the local map directory and serve it
            _tempZipPath = Path.Combine(Path.GetTempPath(), "bsim_preview", $"local_{Guid.NewGuid():N}.zip");
            try
            {
                var dir = Path.GetDirectoryName(_tempZipPath);
                if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                System.IO.Compression.ZipFile.CreateFromDirectory(mapDir, _tempZipPath);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"无法打包本地谱面: {ex.Message}";
                _loadProgress.IsVisible = false;
                return;
            }

            string? servedUrl = StartLocalZipServer(_tempZipPath, mapName);
            if (servedUrl == null)
            {
                _lblStatus.Text = "无法启动本地预览服务";
                _loadProgress.IsVisible = false;
                return;
            }

            // Load ArcViewer directly with ?url= pointing to local zip server
            // Note: WebView2 on localhost may allow mixed content (HTTPS→HTTP localhost)
            InitializeWebView($"{ArcViewerUrl}?url={servedUrl}map.zip");
            await Task.CompletedTask;
        }

        private string? StartLocalZipServer(string zipPath, string mapName)
        {
            try
            {
                int port = FindFreePort();
                string prefix = $"http://localhost:{port}/";

                _httpServer = new HttpListener();
                _httpServer.Prefixes.Add(prefix);
                _httpServer.Start();

                _ = Task.Run(() =>
                {
                    while (_httpServer.IsListening)
                    {
                        HttpListenerContext? context = null;
                        try
                        {
                            context = _httpServer.GetContext();
                            var request = context.Request;
                            var response = context.Response;

                            string path = request.Url?.LocalPath ?? "/";

                            if (path == "/map.zip" || path == "/download")
                            {
                                byte[] zipBytes = File.ReadAllBytes(zipPath);
                                response.StatusCode = 200;
                                response.ContentType = "application/zip";
                                response.ContentLength64 = zipBytes.Length;
                                // Allow cross-origin access so ArcViewer (HTTPS) can fetch from this HTTP server
                                response.Headers.Add("Access-Control-Allow-Origin", "*");
                                response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                                response.Headers.Add("Access-Control-Allow-Headers", "*");
                                response.Headers.Add("Content-Disposition", $"attachment; filename=\"{Uri.EscapeDataString(mapName)}.zip\"");
                                response.OutputStream.Write(zipBytes, 0, zipBytes.Length);
                                response.OutputStream.Flush();
                            }
                            else if (request.HttpMethod == "OPTIONS")
                            {
                                // Handle CORS preflight
                                response.StatusCode = 200;
                                response.Headers.Add("Access-Control-Allow-Origin", "*");
                                response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                                response.Headers.Add("Access-Control-Allow-Headers", "*");
                            }
                            else
                            {
                                // Redirect to ArcViewer with local zip URL
                                string redirectUrl = $"{ArcViewerUrl}?url=http://localhost:{port}/map.zip";
                                response.Redirect(redirectUrl);
                            }
                        }
                        catch
                        {
                        }
                        finally
                        {
                            try { context?.Response.Close(); } catch { }
                        }
                    }
                });

                return $"http://localhost:{port}/";
            }
            catch
            {
                return null;
            }
        }

        private static int FindFreePort()
        {
            using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private void InitializeWebView(string url)
        {
            try
            {
                _webView = new NativeWebView
                {
                    Source = new Uri(url)
                };
                _webView.NavigationCompleted += (s, e) =>
                {
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (e != null && e.IsSuccess)
                        {
                            _lblStatus.Text = "ArcViewer 已加载";
                            _loadProgress.IsVisible = false;
                        }
                        else if (e != null)
                        {
                            _lblStatus.Text = $"加载失败，正在重试...";
                        }
                    });
                };

                _webViewContainer.Children.Add(_webView);
                _lblStatus.Text = "正在加载 ArcViewer...";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"WebView 不可用: {ex.Message}，正在在浏览器中打开...";
                _loadProgress.IsVisible = false;
                OpenInExternalBrowser(url);
            }
        }

        private void OpenInExternalBrowser(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                _lblStatus.Text = $"请在浏览器中打开: {url}";
            }
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

        private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            try
            {
                _httpServer?.Stop();
                _httpServer?.Close();
                _httpServer = null;
            }
            catch { }

            try
            {
                if (_tempZipPath != null && File.Exists(_tempZipPath))
                    File.Delete(_tempZipPath);
            }
            catch { }
        }
    }
}
