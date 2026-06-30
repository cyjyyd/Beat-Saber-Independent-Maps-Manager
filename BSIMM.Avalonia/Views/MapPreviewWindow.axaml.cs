using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Markup.Xaml;
using BeatSaberIndependentMapsManager.Services;
using System;
using System.IO;
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
        private HttpsZipServer? _httpsServer;
        private string? _tempZipPath;

        // ArcViewer is always loaded from the remote HTTPS source (same as online preview)
        private const string ArcViewerUrl = "https://allpoland.github.io/ArcViewer/";

        private static readonly HttpClient _downloadClient = new(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (s, c, ch, e) => true
        })
        { Timeout = TimeSpan.FromMinutes(5) };

        static MapPreviewWindow()
        {
            _downloadClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
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

            // Online map with BeatSaver ID — ArcViewer fetches directly (HTTPS→HTTPS)
            if (!string.IsNullOrEmpty(mapId))
            {
                _lblStatus.Text = $"正在加载 ArcViewer (谱面ID: {mapId})...";
                InitializeWebView($"{ArcViewerUrl}?id={Uri.EscapeDataString(mapId)}");
                return;
            }

            // Download zip, serve via local HTTPS
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

            _lblStatus.Text = "正在启动本地 HTTPS 服务...";
            string? zipUrl = await StartHttpsServerAsync(_tempZipPath, mapName);
            if (zipUrl == null)
            {
                _lblStatus.Text = "无法启动本地 HTTPS 服务";
                _loadProgress.IsVisible = false;
                return;
            }

            // ArcViewer (remote HTTPS) fetches zip from local HTTPS — no mixed content
            InitializeWebView($"{ArcViewerUrl}?url={Uri.EscapeDataString(zipUrl)}");
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

            _lblStatus.Text = "正在启动本地 HTTPS 服务...";
            string? zipUrl = await StartHttpsServerAsync(_tempZipPath, mapName);
            if (zipUrl == null)
            {
                _lblStatus.Text = "无法启动本地 HTTPS 服务";
                _loadProgress.IsVisible = false;
                return;
            }

            // ArcViewer (remote HTTPS) fetches zip from local HTTPS — no mixed content
            InitializeWebView($"{ArcViewerUrl}?url={Uri.EscapeDataString(zipUrl)}");
            await Task.CompletedTask;
        }

        private async Task<string?> StartHttpsServerAsync(string zipPath, string mapName)
        {
            try
            {
                _httpsServer = new HttpsZipServer();
                await _httpsServer.StartAsync(zipPath, mapName);
                return _httpsServer.ZipUrl;
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"HTTPS 服务启动失败: {ex.Message}";
                return null;
            }
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
            try { _httpsServer?.Dispose(); } catch { }

            try
            {
                if (_tempZipPath != null && File.Exists(_tempZipPath))
                    File.Delete(_tempZipPath);
            }
            catch { }
        }
    }

    /// <summary>
    /// A minimal HTTPS server that serves a single zip file with a self-signed certificate.
    /// Uses raw TcpListener + SslStream (HttpListener doesn't support HTTPS easily).
    /// </summary>
    internal sealed class HttpsZipServer : IDisposable
    {
        private TcpListener? _listener;
        private X509Certificate2? _cert;
        private CancellationTokenSource _cts = new();
        private string _zipPath = "";
        private string _mapName = "";
        private int _port;
        private bool _certInstalled;

        public string ZipUrl => $"https://localhost:{_port}/map.zip";

        public async Task StartAsync(string zipPath, string mapName)
        {
            _zipPath = zipPath;
            _mapName = mapName;

            // Generate and install self-signed certificate to Trusted Root
            _cert = GenerateSelfSignedCert();
            _certInstalled = InstallCertToTrustedRootWithFlag(_cert);

            // Find free port
            _port = FindFreePort();

            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();

            // Run server loop in background
            _ = Task.Run(() => ServerLoop(_cts.Token));
        }

        private static void InstallCertToTrustedRoot(X509Certificate2 cert)
        {
            try
            {
                using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                // Remove any previous BSIMM certs to avoid duplicates
                var existing = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, cert.SubjectName.Name, false);
                foreach (var c in existing)
                {
                    if (c.Thumbprint != cert.Thumbprint)
                        store.Remove(c);
                }
                store.Add(cert);
                store.Close();
            }
            catch { }
        }

        private static bool InstallCertToTrustedRootWithFlag(X509Certificate2 cert)
        {
            try
            {
                using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                var existing = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, cert.SubjectName.Name, false);
                foreach (var c in existing)
                {
                    if (c.Thumbprint != cert.Thumbprint)
                        store.Remove(c);
                }
                store.Add(cert);
                store.Close();
                return true;
            }
            catch { return false; }
        }

        private static void RemoveCertFromTrustedRoot(X509Certificate2 cert)
        {
            try
            {
                using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                store.Remove(cert);
                store.Close();
            }
            catch { }
        }

        private async Task ServerLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                TcpClient? client = null;
                try
                {
                    client = await _listener!.AcceptTcpClientAsync(ct);
                    _ = HandleClientAsync(client, ct);
                }
                catch (OperationCanceledException) { break; }
                catch { }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            try
            {
                using var sslStream = new SslStream(client.GetStream(), false);
                // Authenticate with self-signed cert — accept any client cert
                await sslStream.AuthenticateAsServerAsync(_cert!, false, System.Security.Authentication.SslProtocols.None, false);

                // Read the HTTP request line
                using var reader = new StreamReader(sslStream, Encoding.ASCII);
                string? requestLine = await reader.ReadLineAsync(ct);
                if (requestLine == null) return;

                // Parse: "GET /path HTTP/1.1"
                var parts = requestLine.Split(' ');
                if (parts.Length < 3) return;
                string method = parts[0];
                string path = parts[1];

                // Read and discard headers
                string? header;
                while ((header = await reader.ReadLineAsync(ct)) != null && header.Length > 0) { }

                if (method == "OPTIONS")
                {
                    await SendResponse(sslStream, 200, "", "text/plain", new byte[0]);
                    return;
                }

                if (path == "/map.zip" || path == "/download")
                {
                    if (File.Exists(_zipPath))
                    {
                        byte[] zipBytes = await File.ReadAllBytesAsync(_zipPath, ct);
                        var headers = new StringBuilder()
                            .Append("Access-Control-Allow-Origin: *\r\n")
                            .Append("Access-Control-Allow-Methods: GET, OPTIONS\r\n")
                            .Append("Access-Control-Allow-Headers: *\r\n")
                            .Append($"Content-Disposition: attachment; filename=\"{Uri.EscapeDataString(_mapName)}.zip\"\r\n");
                        await SendRawResponse(sslStream, 200, "application/zip", zipBytes, headers.ToString());
                    }
                    else
                    {
                        await SendResponse(sslStream, 404, "Not Found", "text/plain", Encoding.UTF8.GetBytes("File not found"));
                    }
                }
                else
                {
                    await SendResponse(sslStream, 404, "Not Found", "text/plain", Encoding.UTF8.GetBytes("Not found"));
                }
            }
            catch { }
            finally
            {
                try { client.Close(); } catch { }
            }
        }

        private static async Task SendResponse(SslStream stream, int statusCode, string statusText, string contentType, byte[] body)
        {
            var headerLines = new StringBuilder()
                .Append($"HTTP/1.1 {statusCode} {statusText}\r\n")
                .Append($"Content-Type: {contentType}\r\n")
                .Append($"Content-Length: {body.Length}\r\n")
                .Append("Access-Control-Allow-Origin: *\r\n")
                .Append("Access-Control-Allow-Methods: GET, OPTIONS\r\n")
                .Append("Access-Control-Allow-Headers: *\r\n")
                .Append("Connection: close\r\n")
                .Append("\r\n");

            byte[] headerBytes = Encoding.ASCII.GetBytes(headerLines.ToString());
            await stream.WriteAsync(headerBytes);
            await stream.WriteAsync(body);
            await stream.FlushAsync();
        }

        private static async Task SendRawResponse(SslStream stream, int statusCode, string contentType, byte[] body, string extraHeaders)
        {
            var headerLines = new StringBuilder()
                .Append($"HTTP/1.1 {statusCode} OK\r\n")
                .Append($"Content-Type: {contentType}\r\n")
                .Append($"Content-Length: {body.Length}\r\n")
                .Append(extraHeaders)
                .Append("Connection: close\r\n")
                .Append("\r\n");

            byte[] headerBytes = Encoding.ASCII.GetBytes(headerLines.ToString());
            await stream.WriteAsync(headerBytes);
            await stream.WriteAsync(body);
            await stream.FlushAsync();
        }

        private static X509Certificate2 GenerateSelfSignedCert()
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            // Add SAN for localhost and 127.0.0.1
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName("localhost");
            sanBuilder.AddIpAddress(IPAddress.Loopback);
            req.CertificateExtensions.Add(sanBuilder.Build());

            // Basic constraints
            req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));

            // Key usage: digital signature, key encipherment
            req.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));

            // Enhanced key usage: server auth
            req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, true));

            // Valid for 1 day
            using var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));

            // Export and re-import to get a proper X509Certificate2 with private key
            return new X509Certificate2(cert.Export(X509ContentType.Pfx, "bsimmpassword"), "bsimmpassword");
        }

        private static int FindFreePort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public void Dispose()
        {
            try { _cts.Cancel(); } catch { }
            try { _listener?.Stop(); } catch { }
            try
            {
                if (_cert != null && _certInstalled)
                    RemoveCertFromTrustedRoot(_cert);
            }
            catch { }
            try { _cert?.Dispose(); } catch { }
            try { _cts.Dispose(); } catch { }
        }
    }
}
