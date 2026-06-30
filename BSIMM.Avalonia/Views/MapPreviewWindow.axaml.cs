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
        private SimpleFileServer? _server;
        private string? _tempZipPath;
        private string? _cacheZipFile;
        private string? _cachePatchHtml;

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
            _tempZipPath = await DownloadZipAsync(downloadUrl);
            if (_tempZipPath == null) { OnFail("下载失败"); return; }
            await ServeAndLoad(_tempZipPath, mapName);
        }

        public async Task LoadLocalMapAsync(string mapDir, string mapName)
        {
            Title = $"谱面预览 - {mapName}";
            _tempZipPath = Path.Combine(Path.GetTempPath(), "bsim_preview", $"local_{Guid.NewGuid():N}.zip");
            try
            {
                var d = Path.GetDirectoryName(_tempZipPath)!;
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
                System.IO.Compression.ZipFile.CreateFromDirectory(mapDir, _tempZipPath);
            }
            catch (Exception ex) { OnFail(ex.Message); return; }
            await ServeAndLoad(_tempZipPath, mapName);
            await Task.CompletedTask;
        }

        private async Task ServeAndLoad(string zipPath, string mapName)
        {
            try
            {
                await EnsureArcViewerCachedAsync();
                string zipName = $"map_{Guid.NewGuid():N}.zip";
                _cacheZipFile = Path.Combine(ArcViewerCacheDir, zipName);
                File.Copy(zipPath, _cacheZipFile, true);

                _server = new SimpleFileServer(ArcViewerCacheDir);
                _server.Start();
                int port = _server.Port;
                string zipUrl = $"http://127.0.0.1:{port}/{zipName}";

                // Write mock API JSON and service worker
                string mockApiFile = Path.Combine(ArcViewerCacheDir, "_api_mock.json");
                File.WriteAllText(mockApiFile, $@"{{""id"":""localmap"",""name"":""{mapName.Replace("\"","'")}"",""versions"":[{{""downloadURL"":""{zipUrl}"",""coverURL"":""""}}]}}");

                string swFile = Path.Combine(ArcViewerCacheDir, "sw.js");
                File.WriteAllText(swFile, $@"self.addEventListener('fetch',e=>{{if(e.request.url.includes('api.beatsaver.com/maps/')){{e.respondWith((async()=>new Response(await fetch('{zipUrl}'),{{headers:{{'content-type':'application/json'}}}}))());}}}});");

                _cachePatchHtml = Path.Combine(ArcViewerCacheDir, "_index_patched.html");
                File.WriteAllText(_cachePatchHtml, GeneratePatchedHtml(port, zipName, zipUrl, mapName));

                InitWebView($"http://127.0.0.1:{port}/_index_patched.html?id=localmap");
            }
            catch (Exception ex) { OnFail(ex.Message); }
        }

        private static string GeneratePatchedHtml(int port, string zipName, string zipUrl, string mapName)
        {
            string origHtml = File.ReadAllText(Path.Combine(ArcViewerCacheDir, "index.html"));
            string patchScript = $@"<script>
var __bsimm_origOpen = XMLHttpRequest.prototype.open;
XMLHttpRequest.prototype.open = function(method, url, async, user, pwd) {{
    if (typeof url === 'string' && url.indexOf('api.beatsaver.com/maps/') >= 0) {{
        url = 'http://127.0.0.1:{port}/_api_mock.json';
    }}
    return __bsimm_origOpen.call(this, method, url, async===undefined?true:async, user, pwd);
}};
</script>";

            int insertPos = origHtml.IndexOf("<script");
            if (insertPos < 0) insertPos = origHtml.IndexOf("</head>");
            if (insertPos < 0) insertPos = origHtml.IndexOf("<body");
            if (insertPos < 0) insertPos = 0;
            return origHtml.Insert(insertPos, patchScript);
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
                _webView = new NativeWebView { Source = new Uri(url) };
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
            try { _server?.Dispose(); } catch { }
            try { if (_tempZipPath != null && File.Exists(_tempZipPath)) File.Delete(_tempZipPath); } catch { }
            try { if (_cacheZipFile != null && File.Exists(_cacheZipFile)) File.Delete(_cacheZipFile); } catch { }
            try { if (_cachePatchHtml != null && File.Exists(_cachePatchHtml)) File.Delete(_cachePatchHtml); } catch { }
        }
    }

    /// <summary>Simple HTTP/1.1 file server using TcpListener, binding to 127.0.0.1.</summary>
    internal sealed class SimpleFileServer : IDisposable
    {
        private readonly string _rootDir;
        private TcpListener? _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentDictionary<string, byte[]> _fileCache = new();
        public int Port { get; }

        public SimpleFileServer(string rootDir)
        {
            _rootDir = rootDir;
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
                try { var client = await _listener!.AcceptTcpClientAsync(_cts.Token); _ = Handle(client); }
                catch (OperationCanceledException) { break; }
                catch { }
            }
        }

        private async Task Handle(TcpClient client)
        {
            NetworkStream? stream = null;
            try
            {
                stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 4096, true);
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) return;
                var parts = line.Split(' ');
                if (parts.Length < 2) return;
                var method = parts[0].ToUpperInvariant();
                var rawPath = parts[1];

                var q = rawPath.IndexOf('?');
                var path = q >= 0 ? rawPath.Substring(0, q) : rawPath;

                // Read and discard headers + body
                int cl = 0;
                string? hdr;
                while (!string.IsNullOrEmpty(hdr = await reader.ReadLineAsync()))
                    if (hdr.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(hdr.AsSpan(15).Trim(), out cl);
                if (cl > 0) { var b = new byte[cl]; int p = 0; while (p < cl) p += await stream.ReadAsync(b, p, cl - p); }

                if (method == "OPTIONS" || method == "HEAD")
                {
                    await Write(stream, 200, "", Array.Empty<byte>());
                    return;
                }

                var rel = path == "/" ? "index.html" : path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fp = Path.Combine(_rootDir, rel);
                if (File.Exists(fp))
                {
                    if (!_fileCache.TryGetValue(fp, out var data) || data == null)
                        { data = File.ReadAllBytes(fp); if (data.Length < 5_000_000) _fileCache[fp] = data; }
                    var extra = fp.EndsWith(".zip") ? "Content-Disposition: attachment; filename=\"map.zip\"\r\n" : "";
                    await Write(stream, 200, Mime(fp), data!, extra);
                }
                else await Write(stream, 404, "text/plain", "Not found"u8.ToArray());
            }
            catch { }
            finally { try { stream?.Close(); } catch { } try { client.Close(); } catch { } }
        }

        private static async Task Write(NetworkStream s, int code, string ct, byte[] body, string extra = "")
        {
            var h = $"HTTP/1.1 {code} {(code == 200 ? "OK" : "Not Found")}\r\n" +
                    $"Content-Length: {body.Length}\r\n" +
                    (ct.Length > 0 ? $"Content-Type: {ct}\r\n" : "") +
                    extra +
                    "Access-Control-Allow-Origin: *\r\n" +
                    "Access-Control-Allow-Methods: GET, OPTIONS, HEAD\r\n" +
                    "Connection: close\r\n\r\n";
            await s.WriteAsync(Encoding.ASCII.GetBytes(h));
            await s.WriteAsync(body);
            await s.FlushAsync();
        }

        private static string Mime(string p)
        {
            if (p.EndsWith(".js")) return "application/javascript";
            if (p.EndsWith(".wasm")) return "application/wasm";
            if (p.EndsWith(".html")) return "text/html; charset=utf-8";
            if (p.EndsWith(".css")) return "text/css";
            if (p.EndsWith(".ico")) return "image/x-icon";
            if (p.EndsWith(".zip")) return "application/zip";
            if (p.EndsWith(".json")) return "application/json";
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
