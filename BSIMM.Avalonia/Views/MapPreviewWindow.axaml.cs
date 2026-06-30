using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Markup.Xaml;
using global::Avalonia.Platform;
using BeatSaberIndependentMapsManager.Services;
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
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
        private string _zipUrl = "";

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
                _zipUrl = $"http://127.0.0.1:{0}/{zipName}";

                _server = new SimpleFileServer(ArcViewerCacheDir);
                _server.Start();
                int port = _server.Port;
                _zipUrl = $"http://127.0.0.1:{port}/{zipName}";

                string jsonBody = $@"{{""id"":""localmap"",""name"":""{mapName.Replace("\"","'")}"",""versions"":[{{""downloadURL"":""{_zipUrl}"",""coverURL"":""""}}]}}";
                string mockApiFile = Path.Combine(ArcViewerCacheDir, "_api_mock.json");
                File.WriteAllText(mockApiFile, jsonBody);

                // Use prototype-level XHR open patch for redirecting the API call + fetch patch for added safety
                _cachePatchHtml = Path.Combine(ArcViewerCacheDir, "_index_patched.html");
                File.WriteAllText(_cachePatchHtml, GeneratePatchedHtml(port, mapName));

                // Load with ?id= so ArcViewer triggers its map load flow
                InitWebView($"http://127.0.0.1:{port}/_index_patched.html?id=localmap");
            }
            catch (Exception ex) { OnFail(ex.Message); }
        }

        private static string GeneratePatchedHtml(int port, string mapName)
        {
            string origHtml = File.ReadAllText(Path.Combine(ArcViewerCacheDir, "index.html"));
            string patch = $@"<script>
var __open = XMLHttpRequest.prototype.open;
XMLHttpRequest.prototype.open = function(m,d,a,u,p){{if(typeof d==='string'&&d.indexOf('api.beatsaver.com/maps/')>=0)d='http://127.0.0.1:{port}/_api_mock.json';return __open.call(this,m,d,a===undefined?true:a,u,p);}};
var __fetch = window.fetch;
window.fetch = function(url,opts){{if(typeof url==='string'&&url.indexOf('api.beatsaver.com/maps/')>=0)url='http://127.0.0.1:{port}/_api_mock.json';return __fetch(url,opts);}};
</script>";
            int pos = origHtml.IndexOf("<script");
            if (pos < 0) pos = origHtml.IndexOf("</head>");
            if (pos < 0) pos = origHtml.IndexOf("<body");
            if (pos < 0) pos = 0;
            return origHtml.Insert(pos, patch);
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
                    // Also try to set up CoreWebView2-level resource filter
                    TrySetupWebView2Filter();
                };
                _webViewContainer.Children.Add(_webView);
                _lblStatus.Text = "正在加载 ArcViewer...";
            }
            catch (Exception ex) { OnFail($"WebView 不可用: {ex.Message}"); }
        }

        private void TrySetupWebView2Filter()
        {
            try
            {
                var handle = _webView?.TryGetPlatformHandle();
                if (handle == null) return;
                var prop = handle.GetType().GetProperty("CoreWebView2");
                if (prop == null) return;
                var ptr = (IntPtr)prop.GetValue(handle)!;
                if (ptr == IntPtr.Zero) return;

                // Get the CoreWebView2 via known COM interface
                var obj = Marshal.GetObjectForIUnknown(ptr);
                if (obj == null) return;

                // Use reflection to call AddWebResourceRequestedFilter
                var addFilter = obj.GetType().GetMethod("AddWebResourceRequestedFilter");
                if (addFilter != null)
                {
                    addFilter.Invoke(obj, new object[] { "https://api.beatsaver.com/maps/*", 0 });
                }

                // Subscribe to WebResourceRequested event
                var evt = obj.GetType().GetEvent("WebResourceRequested");
                if (evt == null) return;

                // The event handler needs to create a custom response
                // with the mock JSON pointing to local zip
                var handlerType = evt.EventHandlerType;
                Delegate handler = null!;
                if (handlerType != null)
                {
                    // Create event handler dynamically using reflection
                    var responseJson = $@"{{""id"":""localmap"",""name"":""Local"",""versions"":[{{""downloadURL"":""{_zipUrl}"",""coverURL"":""""}}]}}";
                    var jsonBytes = System.Text.Encoding.UTF8.GetBytes(responseJson);
                    var stream = new MemoryStream(jsonBytes);

                    handler = Delegate.CreateDelegate(handlerType, this, nameof(OnWebResourceRequested));
                    evt.AddEventHandler(obj, handler);
                }
            }
            catch { }
        }

        // Placeholder for WebResourceRequested event — actual implementation needs
        // ICoreWebView2WebResourceRequestedEventArgs COM interface
        private void OnWebResourceRequested(object sender, object args) { }

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

    internal sealed class SimpleFileServer : IDisposable
    {
        private readonly string _rootDir;
        private System.Net.Sockets.TcpListener? _listener;
        private readonly System.Threading.CancellationTokenSource _cts = new();
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> _fc = new();
        public int Port { get; }

        public SimpleFileServer(string rootDir)
        {
            _rootDir = rootDir;
            using var p = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            p.Start(); Port = ((System.Net.IPEndPoint)p.LocalEndpoint).Port; p.Stop();
        }
        public void Start() { _listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, Port); _listener.Start(); _ = System.Threading.Tasks.Task.Run(Loop); }
        private async System.Threading.Tasks.Task Loop() { while (!_cts.IsCancellationRequested) { try { var c = await _listener!.AcceptTcpClientAsync(_cts.Token); _ = Handle(c); } catch { } } }
        private async System.Threading.Tasks.Task Handle(System.Net.Sockets.TcpClient c)
        {
            System.Net.Sockets.NetworkStream? s = null;
            try
            {
                s = c.GetStream();
                using var r = new System.IO.StreamReader(s, System.Text.Encoding.ASCII, false, 4096, true);
                var ln = await r.ReadLineAsync(); if (string.IsNullOrEmpty(ln)) return;
                var ps = ln.Split(' '); if (ps.Length < 2) return;
                var m = ps[0].ToUpperInvariant(); var rp = ps[1];
                var q = rp.IndexOf('?'); var path = q >= 0 ? rp[..q] : rp;
                int cl = 0; string? hdr;
                while (!string.IsNullOrEmpty(hdr = await r.ReadLineAsync()))
                    if (hdr.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(hdr.AsSpan(15).Trim(), out cl);
                if (cl > 0) { var b = new byte[cl]; int i = 0; while (i < cl) i += await s.ReadAsync(b, i, cl - i); }
                if (m == "OPTIONS" || m == "HEAD") { await Wr(s, 200, "", Array.Empty<byte>()); return; }
                var rel = path == "/" ? "index.html" : path.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar);
                var fp = System.IO.Path.Combine(_rootDir, rel);
                if (System.IO.File.Exists(fp))
                {
                    if (!_fc.TryGetValue(fp, out var d) || d == null) { d = System.IO.File.ReadAllBytes(fp); if (d.Length < 5_000_000) _fc[fp] = d; }
                    await Wr(s, 200, Mt(fp), d!);
                }
                else await Wr(s, 404, "text/plain", "Not found"u8.ToArray());
            }
            catch { } finally { try { s?.Close(); } catch { } try { c.Close(); } catch { } }
        }
        private static async System.Threading.Tasks.Task Wr(System.Net.Sockets.NetworkStream s, int cd, string ct, byte[] b)
        {
            var h = $"HTTP/1.1 {cd} {(cd == 200 ? "OK" : "Not Found")}\r\nContent-Length: {b.Length}\r\n{(ct.Length > 0 ? $"Content-Type: {ct}\r\n" : "")}Access-Control-Allow-Origin: *\r\nAccess-Control-Allow-Methods: GET,OPTIONS,HEAD\r\nConnection: close\r\n\r\n";
            await s.WriteAsync(System.Text.Encoding.ASCII.GetBytes(h)); await s.WriteAsync(b); await s.FlushAsync();
        }
        private static string Mt(string p) => p.EndsWith(".js") ? "application/javascript" : p.EndsWith(".wasm") ? "application/wasm" : p.EndsWith(".html") ? "text/html; charset=utf-8" : p.EndsWith(".css") ? "text/css" : p.EndsWith(".ico") ? "image/x-icon" : p.EndsWith(".zip") ? "application/zip" : p.EndsWith(".json") ? "application/json" : "application/octet-stream";
        public void Dispose() { try { _cts.Cancel(); } catch { } try { _listener?.Stop(); } catch { } _cts.Dispose(); }
    }
}
