using System;
using System.IO;
using System.Net.Http;
using NAudio.Vorbis;
using NAudio.Wave;

namespace BeatSaberIndependentMapsManager.Services
{
    public enum AudioState
    {
        Stopped,
        Playing,
        Paused
    }

    public class AudioPreviewService : IDisposable
    {
        private WaveOut _waveOut;
        private WaveStream _currentAudioReader;
        private bool _disposed;
        private static readonly HttpClient _httpClient = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, errors) => true
        };
        return new HttpClient(handler);
    }

        public AudioState State => _waveOut == null ? AudioState.Stopped
            : _waveOut.PlaybackState == PlaybackState.Playing ? AudioState.Playing
            : _waveOut.PlaybackState == PlaybackState.Paused ? AudioState.Paused
            : AudioState.Stopped;

        public bool IsPlaying => State == AudioState.Playing;
        public bool IsPaused => State == AudioState.Paused;
        public bool IsStopped => State == AudioState.Stopped;

        public TimeSpan CurrentTime
        {
            get => _currentAudioReader?.CurrentTime ?? TimeSpan.Zero;
            set
            {
                if (_currentAudioReader != null)
                    _currentAudioReader.CurrentTime = value;
            }
        }

        public TimeSpan TotalTime => _currentAudioReader?.TotalTime ?? TimeSpan.Zero;

        public float Volume
        {
            get => _waveOut?.Volume ?? 0;
            set
            {
                if (_waveOut != null)
                    _waveOut.Volume = value;
            }
        }

        /// <summary>
        /// Try to load and play a song file.
        /// Returns true if playback started successfully.
        /// </summary>
        public bool TryPlay(string songFolder, string songFilename, double previewStartTime = 0, double previewDuration = 0)
        {
            try
            {
                string playPath = Path.Combine(songFolder, songFilename);
                if (!File.Exists(playPath))
                    return false;

                StopAndDispose();

                if (songFilename.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    _currentAudioReader = new WaveFileReader(playPath);
                }
                else if (songFilename.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
                         songFilename.EndsWith(".egg", StringComparison.OrdinalIgnoreCase))
                {
                    _currentAudioReader = new VorbisWaveReader(playPath);
                }
                else
                {
                    return false;
                }

                IWaveProvider providerToPlay = _currentAudioReader;

                if (previewStartTime > 0 || previewDuration > 0)
                {
                    var sampleProvider = _currentAudioReader.ToSampleProvider();
                    var offsetProvider = new NAudio.Wave.SampleProviders.OffsetSampleProvider(sampleProvider);

                    if (previewStartTime > 0)
                    {
                        offsetProvider.SkipOver = TimeSpan.FromSeconds(previewStartTime);
                        // Also update the reader's position for accurate CurrentTime reporting if needed,
                        // but actually OffsetSampleProvider skips internally by reading and discarding.
                        // For better performance with large files, setting reader position directly is better:
                        _currentAudioReader.CurrentTime = TimeSpan.FromSeconds(previewStartTime);
                        offsetProvider.SkipOver = TimeSpan.Zero; // Since we seeked the stream directly
                    }
                    if (previewDuration > 0)
                    {
                        offsetProvider.Take = TimeSpan.FromSeconds(previewDuration);
                    }
                    
                    providerToPlay = offsetProvider.ToWaveProvider();
                }

                _waveOut = new WaveOut();
                _waveOut.Init(providerToPlay);
                _waveOut.Play();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Pause()
        {
            if (_waveOut?.PlaybackState == PlaybackState.Playing)
                _waveOut.Pause();
        }

        public void Resume()
        {
            if (_waveOut?.PlaybackState == PlaybackState.Paused)
                _waveOut.Play();
        }

        public void Stop()
        {
            _waveOut?.Stop();
        }

        public int GetPositionSeconds()
        {
            return (int)(_currentAudioReader?.CurrentTime.TotalSeconds ?? 0);
        }

        private void StopAndDispose()
        {
            if (_waveOut != null)
            {
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }
            if (_currentAudioReader != null)
            {
                _currentAudioReader.Dispose();
                _currentAudioReader = null;
            }
        }

        /// <summary>
        /// Play a local audio file with automatic format detection.
        /// Tries MP3 (MediaFoundationReader), then OGG (VorbisWaveReader), then WAV.
        /// Cleans up the file when playback stops.
        /// </summary>
        public void PlayLocalFile(string filePath)
        {
            StopAndDispose();

            WaveStream? reader = null;

            // Try MediaFoundationReader first (handles MP3, AAC, etc.)
            try
            {
                reader = new MediaFoundationReader(filePath);
            }
            catch { }

            // Try VorbisWaveReader for OGG
            if (reader == null)
            {
                try
                {
                    reader = new VorbisWaveReader(filePath);
                }
                catch { }
            }

            // Try WaveFileReader for WAV
            if (reader == null)
            {
                try
                {
                    reader = new WaveFileReader(filePath);
                }
                catch { }
            }

            if (reader == null)
                throw new NotSupportedException("Unsupported audio format");

            _currentAudioReader = reader;
            _waveOut = new WaveOut();
            _waveOut.Init(_currentAudioReader);
            _waveOut.Play();

            // Clean up temp file when playback stops
            _waveOut.PlaybackStopped += (s, e) =>
            {
                try { if (File.Exists(filePath)) File.Delete(filePath); } catch { }
            };
        }

        /// <summary>
        /// Play a pre-loaded WaveStream (used for online preview from downloaded temp file).
        /// The cleanupFile path will be deleted when playback stops.
        /// </summary>
        public void PlayStream(WaveStream reader, string? cleanupFile = null)
        {
            StopAndDispose();
            _currentAudioReader = reader;
            _waveOut = new WaveOut();
            _waveOut.Init(_currentAudioReader);
            _waveOut.Play();

            if (!string.IsNullOrEmpty(cleanupFile))
            {
                _waveOut.PlaybackStopped += (s, e) =>
                {
                    try { if (File.Exists(cleanupFile)) File.Delete(cleanupFile); } catch { }
                };
            }
        }

        /// <summary>
        /// Try to load and play a song from a URL (online preview from BeatSaver).
        /// Downloads to a temp file, then plays with NAudio.
        /// </summary>
        public async System.Threading.Tasks.Task<bool> TryPlayFromUrlAsync(string url)
        {
            try
            {
                StopAndDispose();

                // Download to temp file
                string tempFile = Path.Combine(Path.GetTempPath(), $"bsim_preview_{Guid.NewGuid():N}.ogg");
                byte[] data = await _httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(tempFile, data);

                _currentAudioReader = new VorbisWaveReader(tempFile);
                _waveOut = new WaveOut();
                _waveOut.Init(_currentAudioReader);
                _waveOut.Play();

                // Clean up temp file when playback stops
                _waveOut.PlaybackStopped += (s, e) =>
                {
                    try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
                };

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            StopAndDispose();
            _disposed = true;
        }
    }
}
