using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Markup.Xaml;
using BeatSaberIndependentMapsManager.Services;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace BSIMM.Avalonia.Views
{
    public partial class MapPreviewWindow : Window
    {
        private Panel _webViewContainer = null!;
        private TextBlock _lblStatus = null!;
        private ProgressBar _loadProgress = null!;
        private NativeWebView? _webView;
        private SimpleHttpServer? _httpServer;
        private string? _tempZipPath;

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

            // Online map with BeatSaver ID — ArcViewer fetches directly (HTTPS→HTTPS)
            if (!string.IsNullOrEmpty(mapId))
            {
                _lblStatus.Text = $"正在加载 ArcViewer (谱面ID: {mapId})...";
                InitializeWebView($"{ArcViewerUrl}?id={Uri.EscapeDataString(mapId)}", allowMixedContent: false);
                return;
            }

            // Download zip, serve via local HTTP
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

            string? zipUrl = StartHttpServer(_tempZipPath);
            if (zipUrl == null)
            {
                _lblStatus.Text = "无法启动本地 HTTP 服务";
                _loadProgress.IsVisible = false;
                return;
            }

            // ArcViewer (HTTPS) fetches zip from local HTTP (mixed content).
            // WebView2 is configured to allow insecure content via AdditionalBrowserArguments.
            InitializeWebView($"{ArcViewerUrl}?url={Uri.EscapeDataString(zipUrl)}", allowMixedContent: true);
        }

        public async Task LoadLocalMapAsync(string mapDir, string mapName)
        {
            this.Title = $"谱面预览 - {mapName}";
            _lblStatus.Text = "正在准备本地谱面...";

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

            string? zipUrl = StartHttpServer(_tempZipPath);
            if (zipUrl == null)
            {
                _lblStatus.Text = "无法启动本地 HTTP 服务";
                _loadProgress.IsVisible = false;
                return;
            }

            InitializeWebView($"{ArcViewerUrl}?url={Uri.EscapeDataString(zipUrl)}", allowMixedContent: true);
            await Task.CompletedTask;
        }

        private string? StartHttpServer(string zipPath)
        {
            try
            {
                _httpServer = new SimpleHttpServer(zipPath);
                _httpServer.Start();
                return _httpServer.ZipUrl;
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"HTTP 服务启动失败: {ex.Message}";
                return null;
            }
        }

        private void InitializeWebView(string url, bool allowMixedContent = false)
        {
            try
            {
                _webView = new NativeWebView
                {
                    Source = new Uri(url)
                };

                if (allowMixedContent)
                {
                    // Configure WebView2 to allow mixed content (HTTPS page loading HTTP resources)
                    // via Chromium command-line flag passed through AdditionalBrowserArguments
                    _webView.EnvironmentRequested += (sender, args) =>
                    {
                        // The event args type is platform-specific; use dynamic to access properties
                        dynamic dynArgs = args;
                        try
                        {
                            // --allow-running-insecure-content allows HTTPS pages to fetch HTTP resources
                            // --disable-web-security disables same-origin policy (needed for CORS)
                            dynArgs.AdditionalBrowserArguments =
                                "--allow-running-insecure-content --disable-web-security";
                        }
                        catch { }
                    };
                }

                _webView.NavigationCompleted += (s, e) =>
                {
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (e != null && e.IsSuccess)
                        {
                            _lblStatus.Text = "ArcViewer 已加载";
                            _loadProgress.IsVisible = false;
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
            try { _httpServer?.Dispose(); } catch { }

            try
            {
                if (_tempZipPath != null && File.Exists(_tempZipPath))
                    File.Delete(_tempZipPath);
            }
            catch { }
        }
    }

    /// <summary>
    /// Simple HTTP server using HttpListener — serves a single zip file.
    /// </summary>
    internal sealed class SimpleHttpServer : IDisposable
    {
        private HttpListener? _listener;
        private CancellationTokenSource _cts = new();
        private readonly string _zipPath;
        private readonly int _port;

        public string ZipUrl => $"http://localhost:{_port}/map.zip";

        public SimpleHttpServer(string zipPath)
        {
            _zipPath = zipPath;
            _port = FindFreePort();
        }

        public void Start()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Start();
            _ = Task.Run(() => ServerLoop());
        }

        private async Task ServerLoop()
        {
            while (!_cts.IsCancellationRequested && _listener?.IsListening == true)
            {
                HttpListenerContext? ctx = null;
                try
                {
                    ctx = await _listener.GetContextAsync();
                    _ = HandleRequest(ctx);
                }
                catch { }
            }
        }

        private async Task HandleRequest(HttpListenerContext ctx)
        {
            try
            {
                var req = ctx.Request;
                var resp = ctx.Response;

                // CORS headers — critical for ArcViewer (cross-origin) to fetch the zip
                resp.Headers.Add("Access-Control-Allow-Origin", "*");
                resp.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS, HEAD");
                resp.Headers.Add("Access-Control-Allow-Headers", "*");

                string path = req.Url?.LocalPath ?? "/";

                if (req.HttpMethod == "OPTIONS" || req.HttpMethod == "HEAD")
                {
                    resp.StatusCode = 200;
                    resp.ContentLength64 = 0;
                    resp.Close();
                    return;
                }

                if (path == "/map.zip" || path == "/download")
                {
                    if (File.Exists(_zipPath))
                    {
                        using var fs = File.OpenRead(_zipPath);
                        resp.StatusCode = 200;
                        resp.ContentType = "application/zip";
                        resp.ContentLength64 = fs.Length;
                        await fs.CopyToAsync(resp.OutputStream);
                        resp.OutputStream.Flush();
                    }
                    else
                    {
                        resp.StatusCode = 404;
                        var msg = System.Text.Encoding.UTF8.GetBytes("File not found");
                        resp.ContentLength64 = msg.Length;
                        resp.OutputStream.Write(msg, 0, msg.Length);
                    }
                }
                else
                {
                    resp.StatusCode = 404;
                    var msg = System.Text.Encoding.UTF8.GetBytes("Not found");
                    resp.ContentLength64 = msg.Length;
                    resp.OutputStream.Write(msg, 0, msg.Length);
                }
            }
            catch { }
            finally
            {
                try { ctx.Response.Close(); } catch { }
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

        public void Dispose()
        {
            try { _cts.Cancel(); } catch { }
            try { _listener?.Stop(); } catch { }
            try { _listener?.Close(); } catch { }
            try { _cts.Dispose(); } catch { }
        }
    }
}
