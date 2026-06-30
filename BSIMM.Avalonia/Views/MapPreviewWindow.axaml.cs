using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Markup.Xaml;
using BeatSaberIndependentMapsManager.Services;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
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
        private int _serverPort;

        // ArcViewer remote source for downloading the WebGL build
        private const string ArcViewerRemote = "https://allpoland.github.io/ArcViewer/";
        // ArcViewer build files needed (relative to ArcViewerRemote)
        private static readonly string[] ArcViewerFiles = {
            "index.html",
            "TemplateData/favicon.ico",
            "TemplateData/style.css",
            "TemplateData/Scripts/oggdecode.js",
            "Build/ArcViewer.loader.js",
            "Build/ArcViewer.framework.js",
            "Build/ArcViewer.wasm",
            "Build/ArcViewer.data",
        };

        // Local cache directory for ArcViewer build files
        private static readonly string ArcViewerCacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BSIMM", "ArcViewer");

        private static readonly HttpClient _downloadClient = new(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (s, c, ch, e) => true
        })
        { Timeout = TimeSpan.FromMinutes(5) };

        static MapPreviewWindow()
        {
            _downloadClient.DefaultRequestHeaders.Add("User-Agent", "BSIMM/1.1");
        }

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
                // Use remote ArcViewer directly — no mixed content issue (both HTTPS)
                InitializeWebView($"{ArcViewerRemote}?id={Uri.EscapeDataString(mapId)}");
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
            if (!await StartLocalServerAsync(_tempZipPath, mapName))
            {
                _lblStatus.Text = "无法启动本地预览服务";
                _loadProgress.IsVisible = false;
                return;
            }

            // Load ArcViewer from the same localhost origin (avoids mixed content)
            InitializeWebView($"http://localhost:{_serverPort}/index.html?url=http://localhost:{_serverPort}/map.zip");
        }

        public async Task LoadLocalMapAsync(string mapDir, string mapName)
        {
            this.Title = $"谱面预览 - {mapName}";
            _lblStatus.Text = "正在准备本地谱面...";

            // Create a zip from the local map directory
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

            _lblStatus.Text = "正在启动本地预览服务...";
            if (!await StartLocalServerAsync(_tempZipPath, mapName))
            {
                _lblStatus.Text = "无法启动本地预览服务";
                _loadProgress.IsVisible = false;
                return;
            }

            // Load ArcViewer from the same localhost origin (avoids mixed content)
            InitializeWebView($"http://localhost:{_serverPort}/index.html?url=http://localhost:{_serverPort}/map.zip");
            await Task.CompletedTask;
        }

        private async Task<bool> StartLocalServerAsync(string zipPath, string mapName)
        {
            try
            {
                _serverPort = FindFreePort();

                // Ensure ArcViewer build files are cached locally
                _lblStatus.Text = "正在准备 ArcViewer 预览引擎...";
                await EnsureArcViewerCachedAsync();

                string prefix = $"http://localhost:{_serverPort}/";

                _httpServer = new HttpListener();
                _httpServer.Prefixes.Add(prefix);
                _httpServer.Start();

                _ = Task.Run(() => ServeRequests(zipPath, mapName));

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ServeRequests(string zipPath, string mapName)
        {
            while (_httpServer?.IsListening == true)
            {
                HttpListenerContext? context = null;
                try
                {
                    context = _httpServer.GetContext();
                    var request = context.Request;
                    var response = context.Response;
                    string path = request.Url?.LocalPath ?? "/";

                    // CORS headers for all responses
                    response.Headers.Add("Access-Control-Allow-Origin", "*");
                    response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
                    response.Headers.Add("Access-Control-Allow-Headers", "*");

                    if (request.HttpMethod == "OPTIONS")
                    {
                        response.StatusCode = 200;
                        continue;
                    }

                    if (path == "/map.zip" || path == "/download")
                    {
                        byte[] zipBytes = File.ReadAllBytes(zipPath);
                        response.StatusCode = 200;
                        response.ContentType = "application/zip";
                        response.ContentLength64 = zipBytes.Length;
                        response.OutputStream.Write(zipBytes, 0, zipBytes.Length);
                        response.OutputStream.Flush();
                    }
                    else if (path == "/" || path == "/index.html")
                    {
                        string htmlPath = Path.Combine(ArcViewerCacheDir, "index.html");
                        if (File.Exists(htmlPath))
                        {
                            byte[] html = File.ReadAllBytes(htmlPath);
                            response.StatusCode = 200;
                            response.ContentType = "text/html; charset=utf-8";
                            response.ContentLength64 = html.Length;
                            response.OutputStream.Write(html, 0, html.Length);
                        }
                        else
                        {
                            response.StatusCode = 404;
                        }
                    }
                    else
                    {
                        // Serve other ArcViewer files (TemplateData/*, Build/*)
                        string filePath = Path.Combine(ArcViewerCacheDir, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (File.Exists(filePath))
                        {
                            byte[] fileBytes = File.ReadAllBytes(filePath);
                            response.StatusCode = 200;
                            response.ContentType = GetMimeType(path);
                            response.ContentLength64 = fileBytes.Length;
                            response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
                        }
                        else
                        {
                            response.StatusCode = 404;
                        }
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
        }

        private async Task EnsureArcViewerCachedAsync()
        {
            if (!Directory.Exists(ArcViewerCacheDir))
                Directory.CreateDirectory(ArcViewerCacheDir);

            // Check if the main file exists (indicates cache is complete)
            string indexHtmlPath = Path.Combine(ArcViewerCacheDir, "index.html");
            string wasmPath = Path.Combine(ArcViewerCacheDir, "Build", "ArcViewer.wasm");
            if (File.Exists(indexHtmlPath) && File.Exists(wasmPath))
                return; // Already cached

            // Download all ArcViewer build files
            foreach (var relPath in ArcViewerFiles)
            {
                string localPath = Path.Combine(ArcViewerCacheDir, relPath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(localPath)) continue;

                string dir = Path.GetDirectoryName(localPath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                try
                {
                    var url = ArcViewerRemote + relPath;
                    byte[] data = await _downloadClient.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(localPath, data);
                }
                catch
                {
                    // Continue even if some files fail — the .data and .wasm are critical
                }
            }
        }

        private static string GetMimeType(string path)
        {
            if (path.EndsWith(".js")) return "application/javascript";
            if (path.EndsWith(".wasm")) return "application/wasm";
            if (path.EndsWith(".html")) return "text/html; charset=utf-8";
            if (path.EndsWith(".css")) return "text/css";
            if (path.EndsWith(".ico")) return "image/x-icon";
            if (path.EndsWith(".png")) return "image/png";
            if (path.EndsWith(".json")) return "application/json";
            return "application/octet-stream";
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
