using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Markup.Xaml;
using BeatSaberIndependentMapsManager.Services;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
        private HttpsServer? _httpsServer;
        private string? _tempZipPath;

        private const string ArcViewerUrl = "https://allpoland.github.io/ArcViewer/";
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
                InitWebView($"{ArcViewerUrl}?id={Uri.EscapeDataString(mapId)}", needsHttpsBypass: false);
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
                _httpsServer = new HttpsServer(zipPath);
                _httpsServer.Start();
                string zipUrl = _httpsServer.ZipUrl;
                // ArcViewer (remote HTTPS) loads zip from local HTTPS — no mixed content
                InitWebView($"{ArcViewerUrl}?url={Uri.EscapeDataString(zipUrl)}", needsHttpsBypass: true);
            }
            catch (Exception ex) { OnFail(ex.Message); }
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

        private void InitWebView(string url, bool needsHttpsBypass)
        {
            try
            {
                _webView = new NativeWebView();

                if (needsHttpsBypass)
                {
                    _webView.EnvironmentRequested += (s, args) =>
                    {
                        try
                        {
                            var prop = args.GetType().GetProperty("AdditionalBrowserArguments");
                            prop?.SetValue(args, "--ignore-certificate-errors --allow-insecure-localhost");
                        }
                        catch { }
                    };
                }

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
            try { _httpsServer?.Dispose(); } catch { }
            try { if (_tempZipPath != null && File.Exists(_tempZipPath)) File.Delete(_tempZipPath); } catch { }
        }
    }

    /// <summary>Minimal HTTPS server serving a single zip file with self-signed cert.</summary>
    internal sealed class HttpsServer : IDisposable
    {
        private TcpListener? _listener;
        private X509Certificate2? _cert;
        private readonly CancellationTokenSource _cts = new();
        private readonly string _zipPath;
        private readonly int _port;

        public string ZipUrl => $"https://localhost:{_port}/map.zip";

        public HttpsServer(string zipPath) { _zipPath = zipPath; _port = FreePort(); }

        public void Start()
        {
            _cert = GenCert();
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            _ = Task.Run(() => Loop());
        }

        private async Task Loop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try { var c = await _listener!.AcceptTcpClientAsync(_cts.Token); _ = Handle(c); }
                catch { break; }
            }
        }

        private async Task Handle(TcpClient client)
        {
            SslStream? ssl = null;
            try
            {
                ssl = new SslStream(client.GetStream(), false);
                await ssl.AuthenticateAsServerAsync(_cert!, false, System.Security.Authentication.SslProtocols.None, false);

                using var reader = new StreamReader(ssl, Encoding.ASCII, false, 4096, true);
                var line = await reader.ReadLineAsync(); if (string.IsNullOrEmpty(line)) return;
                var parts = line.Split(' '); if (parts.Length < 2) return;
                var method = parts[0].ToUpperInvariant();
                var rawPath = parts[1];
                // Handle full URLs (proxy-style requests) and query strings
                if (rawPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    try { rawPath = new Uri(rawPath).AbsolutePath; } catch { }
                }
                var path = rawPath.Split('?')[0];

                // Read headers
                string? hdr;
                while (!string.IsNullOrEmpty(hdr = await reader.ReadLineAsync())) { }

                if (method == "OPTIONS" || method == "HEAD")
                {
                    await Write(ssl, 200, "", Array.Empty<byte>()); return;
                }

                if ((path == "/map.zip" || path.EndsWith("/map.zip")) && File.Exists(_zipPath))
                {
                    await Write(ssl, 200, "application/zip", await File.ReadAllBytesAsync(_zipPath));
                }
                else
                {
                    var msg = $"404 path=[{rawPath}] clean=[{path}] zip=[{File.Exists(_zipPath)}]";
                    await Write(ssl, 404, "text/plain", Encoding.UTF8.GetBytes(msg));
                }
            }
            catch { }
            finally
            {
                try { ssl?.Close(); } catch { }
                try { client.Close(); } catch { }
            }
        }

        private static async Task Write(SslStream s, int code, string ct, byte[] body)
        {
            var h = $"HTTP/1.1 {code} {(code == 200 ? "OK" : "Not Found")}\r\n" +
                    $"Content-Length: {body.Length}\r\n" +
                    (ct.Length > 0 ? $"Content-Type: {ct}\r\n" : "") +
                    "Access-Control-Allow-Origin: *\r\n" +
                    "Access-Control-Allow-Methods: GET,OPTIONS,HEAD\r\n" +
                    "Connection: close\r\n\r\n";
            await s.WriteAsync(Encoding.ASCII.GetBytes(h));
            await s.WriteAsync(body);
            await s.FlushAsync();
        }

        private static X509Certificate2 GenCert()
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName("localhost");
            san.AddIpAddress(IPAddress.Loopback);
            req.CertificateExtensions.Add(san.Build());
            req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            using var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
            return new X509Certificate2(cert.Export(X509ContentType.Pfx, "b"), "b");
        }

        private static int FreePort()
        {
            using var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start(); int p = ((IPEndPoint)l.LocalEndpoint).Port; l.Stop();
            return p;
        }

        public void Dispose()
        {
            try { _cts.Cancel(); } catch { }
            try { _listener?.Stop(); } catch { }
            try { _cert?.Dispose(); } catch { }
            _cts.Dispose();
        }
    }
}
