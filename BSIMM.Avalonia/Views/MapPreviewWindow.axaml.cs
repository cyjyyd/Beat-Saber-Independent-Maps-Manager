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
                InitWebView($"{ArcViewerRemote}?id={Uri.EscapeDataString(mapId)}", needsLocalServer: false);
                return;
            }

            _lblStatus.Text = $"正在下载谱面: {mapName}...";
            _tempZipPath = await DownloadZipAsync(downloadUrl);
            if (_tempZipPath == null) { OnFail("下载失败，请检查网络连接"); return; }

            if (!await StartServerAsync(_tempZipPath, mapName)) { OnFail("无法启动本地预览服务"); return; }
            InitWebView(null!, needsLocalServer: true);
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

            if (!await StartServerAsync(_tempZipPath, mapName)) { OnFail("无法启动本地预览服务"); return; }
            InitWebView(null!, needsLocalServer: true);
            await Task.CompletedTask;
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

        private async Task<bool> StartServerAsync(string zipPath, string mapName)
        {
            try
            {
                _lblStatus.Text = "正在准备 ArcViewer 引擎...";
                await EnsureArcViewerCachedAsync();
                _localServer = new LocalHttpServer(zipPath, ArcViewerCacheDir);
                _localServer.Start();
                return true;
            }
            catch (Exception ex) { _lblStatus.Text = ex.Message; return false; }
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
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Download {f}: {ex.Message}"); }
            }
            if (!File.Exists(idx) || !File.Exists(wasm))
                throw new Exception("无法下载 ArcViewer 引擎文件，请检查网络");
        }

        private void InitWebView(string url, bool needsLocalServer)
        {
            try
            {
                _webView = new NativeWebView();

                // Subscribe BEFORE setting Source (EnvironmentRequested fires before environment creation)
                if (needsLocalServer)
                {
                    _webView.EnvironmentRequested += (s, args) =>
                    {
                        try
                        {
                            var prop = args.GetType().GetProperty("AdditionalBrowserArguments");
                            if (prop != null)
                                prop.SetValue(args,
                                    "--allow-insecure-localhost --allow-running-insecure-content");
                        }
                        catch { }
                    };
                }

                _webView.Source = needsLocalServer
                    ? new Uri($"http://localhost:{_localServer!.Port}/index.html?url=http://localhost:{_localServer.Port}/map.zip")
                    : new Uri(url);

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
            catch (Exception ex)
            {
                OnFail($"WebView 不可用: {ex.Message}");
            }
        }

        private void OnFail(string msg) { _lblStatus.Text = msg; _loadProgress.IsVisible = false; }

        private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

        private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            try { _localServer?.Dispose(); } catch { }
            try { if (_tempZipPath != null && File.Exists(_tempZipPath)) File.Delete(_tempZipPath); } catch { }
        }
    }

    /// <summary>Serves ArcViewer files and a single zip via raw TcpListener. No HttpListener — manual HTTP parsing.</summary>
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
            try
            {
                using var stream = client.GetStream();
                var buf = new byte[8192];
                var bytesRead = await stream.ReadAsync(buf);
                var raw = Encoding.ASCII.GetString(buf, 0, bytesRead);
                var lines = raw.Split("\r\n");
                if (lines.Length == 0) return;
                var reqParts = lines[0].Split(' ');
                if (reqParts.Length < 2) return;
                var method = reqParts[0].ToUpperInvariant();
                var path = reqParts[1];

                // Simple path: strip query string
                var qIdx = path.IndexOf('?');
                var cleanPath = qIdx >= 0 ? path.Substring(0, qIdx) : path;

                if (method == "OPTIONS")
                {
                    await WriteResponse(stream, 200, "text/plain", Array.Empty<byte>(), true);
                    return;
                }

                if (cleanPath == "/map.zip" || cleanPath == "/download")
                {
                    if (File.Exists(_zipPath))
                    {
                        var data = await File.ReadAllBytesAsync(_zipPath);
                        await WriteResponse(stream, 200, "application/zip", data, true);
                    }
                    else await WriteResponse(stream, 404, "text/plain", "File not found"u8.ToArray(), true);
                }
                else
                {
                    // Serve ArcViewer file from cache
                    var rel = cleanPath.TrimStart('/');
                    if (rel == "" || rel == "index.html") rel = "index.html";
                    rel = rel.Replace('/', Path.DirectorySeparatorChar);
                    var filePath = Path.Combine(_cacheDir, rel);

                    if (File.Exists(filePath))
                    {
                        byte[] data;
                        if (!_fileCache.TryGetValue(filePath, out data!))
                        {
                            data = File.ReadAllBytes(filePath);
                            if (data.Length < 5_000_000)
                                _fileCache[filePath] = data;
                        }
                        await WriteResponse(stream, 200, GetMime(filePath), data, true);
                    }
                    else
                        await WriteResponse(stream, 404, "text/plain", "Not found"u8.ToArray(), true);
                }
            }
            catch { }
            finally { try { client.Close(); } catch { } }
        }

        private static async Task WriteResponse(NetworkStream stream, int code, string contentType, byte[] body, bool cors)
        {
            var hdr = new StringBuilder()
                .Append($"HTTP/1.1 {code} {StatusText(code)}\r\n")
                .Append($"Content-Type: {contentType}\r\n")
                .Append($"Content-Length: {body.Length}\r\n")
                .Append("Connection: close\r\n");
            if (cors)
                hdr.Append("Access-Control-Allow-Origin: *\r\n" +
                           "Access-Control-Allow-Methods: GET, OPTIONS\r\n" +
                           "Access-Control-Allow-Headers: *\r\n");
            hdr.Append("\r\n");
            await stream.WriteAsync(Encoding.ASCII.GetBytes(hdr.ToString()));
            await stream.WriteAsync(body);
            await stream.FlushAsync();
        }

        private static string StatusText(int code) => code switch
        {
            200 => "OK", 404 => "Not Found", _ => "OK"
        };

        private static string GetMime(string path)
        {
            if (path.EndsWith(".js")) return "application/javascript";
            if (path.EndsWith(".wasm")) return "application/wasm";
            if (path.EndsWith(".html")) return "text/html; charset=utf-8";
            if (path.EndsWith(".css")) return "text/css";
            if (path.EndsWith(".ico")) return "image/x-icon";
            if (path.EndsWith(".data")) return "application/octet-stream";
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
