using Avalonia.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace BSIMM.Avalonia.Services
{
    public class CoverImageCacheService
    {
        // Reuse a single HttpClient instance across the entire app lifetime (same as WinForms imageHttpClient)
        private static readonly HttpClient _httpClient;
        private static readonly ConcurrentDictionary<string, byte[]> _cache = new();
        private const int MaxCacheSize = 500;

        static CoverImageCacheService()
        {
            // Use HttpClientHandler (same pattern as WinForms version) for maximum compatibility
            var handler = new HttpClientHandler
            {
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true,
                MaxConnectionsPerServer = 20,
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BSIMM/1.1");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        private static async Task<byte[]?> DownloadWithRetryAsync(string url, int maxRetries = 3, CancellationToken ct = default)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                if (ct.IsCancellationRequested) return null;
                try
                {
                    // Use GetByteArrayAsync - same as WinForms version, simple and reliable
                    var bytes = await _httpClient.GetByteArrayAsync(url, ct);
                    if (bytes != null && bytes.Length > 0)
                        return bytes;
                }
                catch (OperationCanceledException) { return null; }
                catch (HttpRequestException)
                {
                    // Retry on network errors with incremental delay
                    if (attempt < maxRetries - 1)
                        await Task.Delay(500 * (attempt + 1), ct);
                }
                catch (IOException)
                {
                    if (attempt < maxRetries - 1)
                        await Task.Delay(500 * (attempt + 1), ct);
                }
                catch { return null; }
            }
            return null;
        }

        public async Task<Bitmap?> GetCoverAsync(string? url, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(url)) return null;

            // Check cache
            if (_cache.TryGetValue(url, out var cached))
                return BytesToBitmap(cached);

            var bytes = await DownloadWithRetryAsync(url, 3, ct);
            if (bytes == null || bytes.Length == 0) return null;

            if (_cache.Count < MaxCacheSize)
                _cache[url] = bytes;

            return BytesToBitmap(bytes);
        }

        public async Task<string?> DownloadToTempFileAsync(string url, string extension, CancellationToken ct = default)
        {
            var bytes = await DownloadWithRetryAsync(url, 3, ct);
            if (bytes == null || bytes.Length == 0) return null;

            string tempFile = Path.Combine(Path.GetTempPath(), $"bsim_preview_{Guid.NewGuid():N}{extension}");
            try
            {
                await File.WriteAllBytesAsync(tempFile, bytes, ct);
                return tempFile;
            }
            catch { return null; }
        }

        private static Bitmap? BytesToBitmap(byte[] bytes)
        {
            try
            {
                using var ms = new MemoryStream(bytes);
                return new Bitmap(ms);
            }
            catch { return null; }
        }

        public void ClearCache() => _cache.Clear();
    }
}
