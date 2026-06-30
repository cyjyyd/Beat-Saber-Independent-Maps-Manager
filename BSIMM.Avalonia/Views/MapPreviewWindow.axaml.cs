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
using System.Net.Sockets;
using System.Text;
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
        private LocalHttpServer? _localServer;
        private string? _tempZipPath;

        private const string ArcViewerRemote = "https://allpoland.github.io/ArcViewer/";
        private static readonly string[] ArcViewerFiles = {
            "index.html", "TemplateData/favicon.ico", "TemplateData/style.css",
            "TemplateData/Scripts/oggdecode.js", "Build/ArcViewer.loader.js",
            "Build/ArcViewer.framework.js", "Build/ArcViewer.wasm", "Build/ArcViewer.data",
        };
        private static readonly string ArcViewerCacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BSIMM", "ArcViewer");
        private static readonly HttpClient _http = new(new HttpClientHandler
        { ServerCertificateCustomValidationCallback = (s, c, ch, e) => true })
        { Timeout = TimeSpan.FromMinutes(5) };
        static MapPreviewWindow() => _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

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
            Title = $"谱面预览 - {mapName}";
            if (!string.IsNullOrEmpty(mapId))
            {
                _lblStatus.Text = $"正在加载 ArcViewer (ID: {mapId})...";
                InitWebView($"{ArcViewerRemote}?id={Uri.EscapeDataString(mapId)}");
                return;
            }

            _lblStatus.Text = $"正在下载谱面: {mapName}...";
            _tempZipPath = await DownloadZipAsync(downloadUrl);
            if (_tempZipPath == null) { OnFail("下载失败，请检查网络连接"); return; }

            await StartServerAndLoad(_tempZipPath, mapName);
        }

        public async Task LoadLocalMapAsync(string mapDir, string mapName)
        {
            Title = $"谱面预览 - {mapName}";
            _lblStatus.Text = "正在准备本地谱面...";
            _tempZipPath = Path.Combine(Path.GetTempPath(), "bsim_preview", $"local_{Guid.NewGuid():N}.zip");
            try
            {
                var d = Path.GetDirectoryName(_tempZipPath)!;
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                System.IO.Compression.ZipFile.CreateFromDirectory(mapDir, _tempZipPath);
            }
            catch (Exception ex) { OnFail($"无法打包: {ex.Message}"); return; }

            await StartServerAndLoad(_tempZipPath, mapName);
            await Task.CompletedTask;
        }

        private async Task StartServerAndLoad(string zipPath, string mapName)
        {
            try
            {
                _lblStatus.Text = "正在准备 ArcViewer 引擎...";
                await EnsureArcViewerCachedAsync();
                _localServer = new LocalHttpServer(zipPath, ArcViewerCacheDir);
                _localServer.Start();
            }
            catch (Exception ex) { OnFail(ex.Message); return; }

            int port = _localServer.Port;
            string zipUrl = $"http://localhost:{port}/map.zip";
            // ArcViewer reads ?url= from Application.absoluteURL
            string avUrl = $"http://localhost:{port}/index.html?url={Uri.EscapeDataString(zipUrl)}";
            InitWebView(avUrl);
        }

        private async Task<string?> DownloadZipAsync(string url)
        {
            try
            {
                var dir = Path.Combine(Path.GetTempPath(), "bsim_preview");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, $"map_{Guid.NewGuid():N}.zip");
                var bytes = await _http.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(path, bytes);
                return path;
            }
            catch { return null; }
        }

        private async Task EnsureArcViewerCachedAsync()
        {
            if (!Directory.Exists(ArcViewerCacheDir)) Directory.CreateDirectory(ArcViewerCacheDir);
            var idx = Path.Combine(ArcViewerCacheDir, "index.html");
            var wasm = Path.Combine(ArcViewerCacheDir, "Build", "ArcViewer.wasm");
            if (File.Exists(idx) && File.Exists(wasm) && new FileInfo(wasm).Length > 0) return;

            foreach (var f in ArcViewerFiles)
            {
                var local = Path.Combine(ArcViewerCacheDir, f.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(local) && new FileInfo(local).Length > 0) continue;
                try
                {
                    var d = Path.GetDirectoryName(local)!;
                    if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                    var data = await _http.GetByteArrayAsync(ArcViewerRemote + f);
                    await File.WriteAllBytesAsync(local, data);
                }
                catch { }
            }
            if (!File.Exists(idx) || !File.Exists(wasm))
                throw new Exception("无法下载 ArcViewer 引擎文件，请检查网络");
        }

        private void InitWebView(string url)
        {
            try
            {
                _webView = new NativeWebView();

                // Subscribe BEFORE setting Source
                _webView.EnvironmentRequested += (s, args) =>
                {
                    try
                    {
                        var prop = args.GetType().GetProperty("AdditionalBrowserArguments");
                        if (prop != null)
                            prop.SetValue(args, "--allow-insecure-localhost");
                    }
                    catch { }
                };

                _webView.Source = new Uri(url);

                _webView.NavigationCompleted += (s, e) =>
                {
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (e != null && e.IsSuccess) { _lblStatus.Text = "ArcViewer 已加载"; _loadProgress.IsVisible = false; }
                    });
                };
                _webViewContainer.Children.Add(_webView);
                _lblStatus.Text = "正在加载 ArcViewer...";
            }
            catch (Exception ex) { OnFail($"WebView 不可用: {ex.Message}"); }
        }

        private void OnFail(string msg) { _lblStatus.Text = msg; _loadProgress.IsVisible = false; }

        private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

        private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            try { _localServer?.Dispose(); } catch { }
            try { if (_tempZipPath != null && File.Exists(_tempZipPath)) File.Delete(_tempZipPath); } catch { }
        }
    }

    /// <summary>Serves ArcViewer files and a zip via raw TcpListener with proper HTTP parsing.</summary>
    internal sealed class LocalHttpServer : IDisposable
    {
        private readonly string _zipPath, _cacheDir;
        private TcpListener? _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentDictionary<string, byte[]> _fileCache = new();
        public int Port { get; }

        public LocalHttpServer(string zipPath, string cacheDir)
        {
            _zipPath = zipPath;
            _cacheDir = cacheDir;
            using var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            Port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
        }

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Loopback, Port);
            _listener.Start();
            _ = Task.Run(RunLoop);
        }

        private async Task RunLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener!.AcceptTcpClientAsync(_cts.Token);
                    _ = HandleClient(client);
                }
                catch (OperationCanceledException) { break; }
                catch { }
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            NetworkStream? stream = null;
            try
            {
                stream = client.GetStream();
                // Use StreamReader for reliable HTTP request parsing (reads until \r\n\r\n)
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 4096, true);
                string? requestLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(requestLine)) return;

                var parts = requestLine.Split(' ');
                if (parts.Length < 2) return;
                var method = parts[0].ToUpperInvariant();
                var rawPath = parts[1];

                // Strip query string to get the clean path
                var idx = rawPath.IndexOf('?');
                var path = idx >= 0 ? rawPath.Substring(0, idx) : rawPath;

                // Read all headers (must consume before writing response)
                int contentLength = 0;
                string? header;
                while (!string.IsNullOrEmpty(header = await reader.ReadLineAsync()))
                {
                    if (header.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(header.AsSpan(15).Trim(), out contentLength);
                }
                // Skip body
                if (contentLength > 0)
                {
                    var bodyBuf = new byte[contentLength];
                    int pos = 0;
                    while (pos < contentLength)
                        pos += await stream.ReadAsync(bodyBuf, pos, contentLength - pos);
                }

                if (method == "OPTIONS" || method == "HEAD")
                {
                    await WriteResponse(stream, 200, "", Array.Empty<byte>(), true);
                    return;
                }

                if (path == "/map.zip" || path == "/download")
                {
                    if (File.Exists(_zipPath))
                    {
                        await WriteResponse(stream, 200, "application/zip",
                            await File.ReadAllBytesAsync(_zipPath), true);
                    }
                    else await WriteResponse(stream, 404, "text/plain", "File not found"u8.ToArray(), true);
                }
                else
                {
                    var rel = path == "/" ? "index.html" : path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var filePath = Path.Combine(_cacheDir, rel);
                    if (File.Exists(filePath))
                    {
                        if (!_fileCache.TryGetValue(filePath, out var data) || data == null)
                        {
                            data = File.ReadAllBytes(filePath);
                            if (data.Length < 5_000_000) _fileCache[filePath] = data;
                        }
                        await WriteResponse(stream, 200, GetMime(filePath), data!, true);
                    }
                    else await WriteResponse(stream, 404, "text/plain", "Not found"u8.ToArray(), true);
                }
            }
            catch { }
            finally
            {
                try { stream?.Close(); } catch { }
                try { client.Close(); } catch { }
            }
        }

        private static async Task WriteResponse(NetworkStream stream, int code, string contentType, byte[] body, bool cors)
        {
            var sb = new StringBuilder();
            sb.Append($"HTTP/1.1 {code} {StatusText(code)}\r\n");
            if (!string.IsNullOrEmpty(contentType))
                sb.Append($"Content-Type: {contentType}\r\n");
            sb.Append($"Content-Length: {body.Length}\r\n");
            sb.Append("Connection: close\r\n");
            if (cors)
            {
                sb.Append("Access-Control-Allow-Origin: *\r\n");
                sb.Append("Access-Control-Allow-Methods: GET, OPTIONS, HEAD\r\n");
                sb.Append("Access-Control-Allow-Headers: Content-Type, Range\r\n");
            }
            sb.Append("\r\n");
            await stream.WriteAsync(Encoding.ASCII.GetBytes(sb.ToString()));
            await stream.WriteAsync(body);
            await stream.FlushAsync();
        }

        private static string StatusText(int code) => code == 200 ? "OK" : "Not Found";
        private static string GetMime(string p)
        {
            if (p.EndsWith(".js")) return "application/javascript";
            if (p.EndsWith(".wasm")) return "application/wasm";
            if (p.EndsWith(".html")) return "text/html; charset=utf-8";
            if (p.EndsWith(".css")) return "text/css";
            if (p.EndsWith(".ico")) return "image/x-icon";
            if (p.EndsWith(".data")) return "application/octet-stream";
            return "application/octet-stream";
        }

        public void Dispose()
        {
            try { _cts.Cancel(); } catch { }
            try { _listener?.Stop(); } catch { }
            try { _cts.Dispose(); } catch { }
        }
    }
}
