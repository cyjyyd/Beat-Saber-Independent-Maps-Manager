using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Layout;
using global::Avalonia.Markup.Xaml;
using global::Avalonia.Platform.Storage;
using global::Avalonia.Media;
using BeatSaberIndependentMapsManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BSIMM.Avalonia.Views
{
    public partial class BatchExportWindow : Window
    {
        private StackPanel _presetsPanel = null!;
        private TextBox _txtOutputDir = null!;
        private TextBlock _lblStatus = null!;

        private List<FilterPreset> _availablePresets = new();
        private List<FilterPreset> _importedPresets = new();
        private List<CheckBox> _presetCheckboxes = new();
        private string _presetsDir = Path.Combine(AppContext.BaseDirectory, "presets");

        public List<FilterPreset> SelectedPresets { get; private set; } = new();
        public string OutputDirectory { get; private set; } = Path.Combine(AppContext.BaseDirectory, "playlists");

        public BatchExportWindow()
        {
            AvaloniaXamlLoader.Load(this);
            _presetsPanel = this.FindControl<StackPanel>("PresetsPanel")!;
            _txtOutputDir = this.FindControl<TextBox>("TxtOutputDir")!;
            _lblStatus = this.FindControl<TextBlock>("LblStatus")!;

            _txtOutputDir.Text = OutputDirectory;
            LoadAvailablePresets();
            RebuildPresetsList();
        }

        private void LoadAvailablePresets()
        {
            _availablePresets.Clear();
            try
            {
                if (Directory.Exists(_presetsDir))
                {
                    foreach (var file in Directory.GetFiles(_presetsDir, "*.bsf"))
                    {
                        var p = FilterPreset.LoadFromFile(file);
                        if (p != null) _availablePresets.Add(p);
                    }
                }
            }
            catch { }
        }

        private void RebuildPresetsList()
        {
            _presetsPanel.Children.Clear();
            _presetCheckboxes.Clear();

            foreach (var preset in _availablePresets)
            {
                var chk = new CheckBox
                {
                    Content = preset.Name,
                    IsChecked = false,
                    Margin = new Thickness(4, 2),
                    Tag = preset
                };
                _presetCheckboxes.Add(chk);
                _presetsPanel.Children.Add(chk);
            }

            foreach (var preset in _importedPresets)
            {
                var chk = new CheckBox
                {
                    Content = "[导入] " + preset.Name,
                    IsChecked = true,
                    Margin = new Thickness(4, 2),
                    Tag = preset
                };
                _presetCheckboxes.Add(chk);
                _presetsPanel.Children.Add(chk);
            }

            if (_availablePresets.Count == 0 && _importedPresets.Count == 0)
            {
                _presetsPanel.Children.Add(new TextBlock
                {
                    Text = "没有可用的预设，请先在筛选构建器中保存预设或点击「导入预设文件」",
                    Foreground = this.TryFindResource("TextSecondaryBrush", out var b) ? b as IBrush : null,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(8, 16),
                    FontSize = 13
                });
            }
        }

        private void OnImportClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;
            _ = DoImportAsync(topLevel);
        }

        private async System.Threading.Tasks.Task DoImportAsync(TopLevel topLevel)
        {
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "导入筛选预设（支持多选）",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("筛选预设文件 (*.bsf)") { Patterns = new[] { "*.bsf" } },
                    new FilePickerFileType("JSON文件 (*.json)") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("所有文件") { Patterns = new[] { "*.*" } }
                }
            });
            if (files == null || files.Count == 0) return;

            foreach (var f in files)
            {
                var preset = FilterPreset.LoadFromFile(f.Path.LocalPath);
                if (preset == null) continue;
                string name = preset.Name;
                int n = 2;
                while (_importedPresets.Any(p => p.Name == name) || _availablePresets.Any(p => p.Name == name))
                {
                    name = $"{preset.Name} ({n})";
                    n++;
                }
                preset.Name = name;
                _importedPresets.Add(preset);
            }
            RebuildPresetsList();
        }

        private void OnSelectAllClick(object? sender, RoutedEventArgs e)
        {
            foreach (var chk in _presetCheckboxes) chk.IsChecked = true;
        }

        private void OnSelectInverseClick(object? sender, RoutedEventArgs e)
        {
            foreach (var chk in _presetCheckboxes) chk.IsChecked = !chk.IsChecked;
        }

        private async void OnBrowseClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;
            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择歌单输出目录",
                AllowMultiple = false
            });
            if (folder != null && folder.Count > 0)
            {
                OutputDirectory = folder[0].Path.LocalPath;
                _txtOutputDir.Text = OutputDirectory;
            }
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();

        private void OnStartClick(object? sender, RoutedEventArgs e)
        {
            SelectedPresets.Clear();
            foreach (var chk in _presetCheckboxes)
            {
                if (chk.IsChecked == true && chk.Tag is FilterPreset p)
                    SelectedPresets.Add(p);
            }

            if (SelectedPresets.Count == 0)
            {
                _lblStatus.Text = "请至少选择一个预设！";
                _lblStatus.Foreground = this.TryFindResource("HighlightBlueBrush", out var b) ? b as IBrush : Brushes.Red;
                return;
            }

            OutputDirectory = _txtOutputDir.Text;
            Close();
        }
    }
}
