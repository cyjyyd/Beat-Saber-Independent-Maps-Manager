using System;
using System.IO;
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
        public bool TryPlay(string songFolder, string songFilename)
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

                _waveOut = new WaveOut();
                _waveOut.Init(_currentAudioReader);
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

        public void Dispose()
        {
            if (_disposed) return;
            StopAndDispose();
            _disposed = true;
        }
    }
}
