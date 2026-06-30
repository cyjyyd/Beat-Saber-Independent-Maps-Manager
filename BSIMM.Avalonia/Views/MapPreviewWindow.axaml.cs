using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BSIMM.Avalonia.Views
{
    public partial class MapPreviewWindow : Window
    {
        private Panel _webViewContainer = null!;
        private TextBlock _lblStatus = null!;
        private ProgressBar _loadProgress = null!;
        private NativeWebView? _webView;
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
                InitWebView($"{ArcViewerUrl}?id={Uri.EscapeDataString(mapId)}");
                return;
            }
            _tempZipPath = await DownloadZipAsync(downloadUrl);
            if (_tempZipPath == null) { OnFail("下载失败"); return; }
            InitWebViewWithZip(_tempZipPath);
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
            InitWebViewWithZip(_tempZipPath);
            await Task.CompletedTask;
        }

        private void InitWebView(string url)
        {
            try
            {
                _webView = new NativeWebView();
                _webView.Source = new Uri(url);
                _webView.NavigationCompleted += (s, e) =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (e != null && e.IsSuccess) { _lblStatus.Text = "ArcViewer 已加载"; _loadProgress.IsVisible = false; }
                    });
                };
                _webViewContainer.Children.Add(_webView);
                _lblStatus.Text = "正在加载 ArcViewer...";
            }
            catch (Exception ex) { OnFail($"WebView 不可用: {ex.Message}"); }
        }

        private void InitWebViewWithZip(string zipPath)
        {
            try
            {
                var base64 = Convert.ToBase64String(File.ReadAllBytes(zipPath));
                var injectJs = BuildInjectScript(base64);

                _webView = new NativeWebView();
                _webView.NavigationCompleted += async (s, e) =>
                {
                    if (e != null && e.IsSuccess)
                    {
                        Dispatcher.UIThread.Post(() => { _lblStatus.Text = "ArcViewer 已加载"; _loadProgress.IsVisible = false; });
                        try { await ((NativeWebView)s!).InvokeScript(injectJs); }
                        catch { }
                    }
                };
                _webView.Source = new Uri($"{ArcViewerUrl}?url=http://bsimm-local/map.zip");
                _webViewContainer.Children.Add(_webView);
                _lblStatus.Text = "正在加载 ArcViewer...";
            }
            catch (Exception ex) { OnFail($"WebView 初始化失败: {ex.Message}"); }
        }

        private static string BuildInjectScript(string base64)
        {
            var sb = new StringBuilder();
            sb.Append("(function(){");
            sb.Append("var b=atob('").Append(base64).Append("');");
            sb.Append("var a=new Uint8Array(b.length);");
            sb.Append("for(var i=0;i<b.length;i++)a[i]=b.charCodeAt(i);");
            sb.Append("var blob=new Blob([a],{type:'application/zip'});");
            sb.Append("var url=URL.createObjectURL(blob);");
            sb.Append("window.__bsimmUrl=url;");

            sb.Append("var _o=XMLHttpRequest.prototype.open;");
            sb.Append("XMLHttpRequest.prototype.open=function(m,u){");
            sb.Append("if(typeof u==='string'){");
            sb.Append("u=u.replace(/^https:\\/\\/cors\\.bsmg\\.dev\\//i,'');");
            sb.Append("if(u.indexOf('bsimm-local')>=0||u.indexOf('localhost')>=0||u.indexOf('127.0.0.1')>=0)arguments[1]=url;");
            sb.Append("}");
            sb.Append("return _o.apply(this,arguments);};");

            sb.Append("if(window.fetch){var _f=window.fetch;");
            sb.Append("window.fetch=function(u,o){");
            sb.Append("if(typeof u==='string'){");
            sb.Append("u=u.replace(/^https:\\/\\/cors\\.bsmg\\.dev\\//i,'');");
            sb.Append("if(u.indexOf('bsimm-local')>=0||u.indexOf('localhost')>=0||u.indexOf('127.0.0.1')>=0)u=url;");
            sb.Append("}");
            sb.Append("return _f.call(this,u,o);};}");
            sb.Append("})();");
            return sb.ToString();
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

        private void OnFail(string msg) { _lblStatus.Text = msg; _loadProgress.IsVisible = false; }
        private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

        private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            try { if (_tempZipPath != null && File.Exists(_tempZipPath)) File.Delete(_tempZipPath); } catch { }
        }
    }
}
