using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.Interactivity;
using global::Avalonia.Markup.Xaml;
using global::Avalonia.Media;
using global::Avalonia.Threading;
using BeatSaberIndependentMapsManager.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BSIMM.Avalonia.Views
{
    public partial class MapPreviewWindow : Window
    {
        private MapPreviewControl _previewCanvas = null!;
        private ComboBox _cboDifficulty = null!;
        private TextBlock _lblNoteCount = null!;
        private TextBlock _lblCurrentTime = null!;
        private TextBlock _lblTotalTime = null!;
        private Slider _progressSlider = null!;
        private Button _btnPlayPause = null!;

        private readonly MapPreviewService _previewService = new();
        private MapPreviewData? _previewData;
        private string? _zipPath;
        private bool _isLoadingData = false;

        public MapPreviewWindow()
        {
            AvaloniaXamlLoader.Load(this);
            _previewCanvas = this.FindControl<MapPreviewControl>("PreviewCanvas")!;
            _cboDifficulty = this.FindControl<ComboBox>("CboDifficulty")!;
            _lblNoteCount = this.FindControl<TextBlock>("LblNoteCount")!;
            _lblCurrentTime = this.FindControl<TextBlock>("LblCurrentTime")!;
            _lblTotalTime = this.FindControl<TextBlock>("LblTotalTime")!;
            _progressSlider = this.FindControl<Slider>("ProgressSlider")!;
            _btnPlayPause = this.FindControl<Button>("BtnPlayPause")!;

            _previewCanvas.ProgressChanged += OnProgressChanged;
            _cboDifficulty.SelectionChanged += (s, e) => OnDifficultyChanged();
        }

        public async Task LoadMapAsync(string downloadUrl, string mapName)
        {
            this.Title = $"谱面预览 - {mapName}";

            var tempDir = Path.Combine(Path.GetTempPath(), "bsim_preview");
            _zipPath = await _previewService.DownloadMapZipAsync(downloadUrl, tempDir);

            if (_zipPath == null)
            {
                _lblNoteCount.Text = "下载失败，请检查网络连接";
                _btnPlayPause.IsEnabled = false;
                return;
            }

            _previewData = _previewService.ParseMapFromZip(_zipPath);

            if (_previewData?.Difficulties.Count > 0)
            {
                var diffNames = _previewData.Difficulties
                    .Select(d => $"{d.Characteristic}/{d.Difficulty}")
                    .ToList();
                _cboDifficulty.ItemsSource = diffNames;

                int selectedIdx = diffNames.FindIndex(n =>
                    n.Contains(_previewData.SelectedCharacteristic) &&
                    n.Contains(_previewData.SelectedDifficulty));
                _cboDifficulty.SelectedIndex = selectedIdx >= 0 ? selectedIdx : 0;

                LoadPreviewData();

                _lblTotalTime.Text = FormatTime(_previewCanvas.Duration);
                _lblNoteCount.Text = $"音符数: {_previewData.Notes.Count}";
            }
            else
            {
                _lblNoteCount.Text = "未找到难度数据";
                _btnPlayPause.IsEnabled = false;
            }
        }

        private void OnDifficultyChanged()
        {
            if (_previewData == null || _cboDifficulty.SelectedIndex < 0 || _zipPath == null) return;

            var selected = _previewData.Difficulties[_cboDifficulty.SelectedIndex];
            _previewData = _previewService.ParseDifficultyFromZip(_zipPath, _previewData, selected.Characteristic, selected.Difficulty);
            LoadPreviewData();
        }

        private void LoadPreviewData()
        {
            if (_previewData == null) return;
            _previewCanvas.LoadData(_previewData);
            _lblTotalTime.Text = FormatTime(_previewCanvas.Duration);
            _lblNoteCount.Text = $"音符数: {_previewData.Notes.Count}";
        }

        private void OnProgressChanged(object? sender, double progress)
        {
            if (_isLoadingData) return;
            _isLoadingData = true;
            Dispatcher.UIThread.Post(() =>
            {
                _progressSlider.Value = progress;
                _lblCurrentTime.Text = FormatTime(_previewCanvas.CurrentTime);
                _isLoadingData = false;
            });
        }

        private void OnPlayPauseClick(object? sender, RoutedEventArgs e)
        {
            if (_previewCanvas.IsPlaying)
            {
                _previewCanvas.Pause();
                _btnPlayPause.Content = "▶ 播放";
            }
            else
            {
                if (_previewCanvas.CurrentTime >= _previewCanvas.Duration && _previewCanvas.Duration > 0)
                    _previewCanvas.Stop();
                _previewCanvas.Play();
                _btnPlayPause.Content = "⏸ 暂停";
            }
        }

        private void OnStopClick(object? sender, RoutedEventArgs e)
        {
            _previewCanvas.Stop();
            _btnPlayPause.Content = "▶ 播放";
            _lblCurrentTime.Text = "0:00";
            _progressSlider.Value = 0;
        }

        private void OnProgressSliderReleased(object? sender, PointerReleasedEventArgs e)
        {
            double pct = _progressSlider.Value;
            double time = _previewCanvas.Duration * pct / 100.0;
            _previewCanvas.Seek(time);
            _lblCurrentTime.Text = FormatTime(time);
        }

        private void OnCloseClick(object? sender, RoutedEventArgs e)
        {
            _previewCanvas.Stop();
            try { if (_zipPath != null && File.Exists(_zipPath)) File.Delete(_zipPath); } catch { }
            Close();
        }

        private static string FormatTime(double seconds)
        {
            int m = (int)(seconds / 60);
            int s = (int)(seconds % 60);
            return $"{m}:{s:D2}";
        }
    }
}
