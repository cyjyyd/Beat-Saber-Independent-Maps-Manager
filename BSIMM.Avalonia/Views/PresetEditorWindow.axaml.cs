using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using BeatSaberIndependentMapsManager;
using BeatSaberIndependentMapsManager.BeatSpiderSharp;
using BeatSpiderSharp.Models.Enums;
using BeatSpiderSharp.Models.Preset;
using BeatSpiderSharp.Models.Preset.FilterOptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BSIMM.Avalonia.Views
{
    public partial class PresetEditorWindow : Window
    {
        private struct PresetItem
        {
            public Preset Preset { get; set; }
            public string FilePath { get; set; }
            public override string ToString() => Preset.Name;
        }

        private StackPanel _editorPanel = null!;
        private Border _emptyHint = null!;
        private ListBox _presetList = null!;
        private TextBox _txtPresetName = null!;
        private TextBox _txtPresetAuthor = null!;
        private TextBlock _lblFilterSummary = null!;
        private TextBlock _lblMatchCount = null!;
        private string _presetsDir;
        private List<PresetItem> _presets = new();

        public event EventHandler<Preset>? ExecuteRequested;
        public Preset? CurrentPreset { get; private set; }

        public PresetEditorWindow()
        {
            AvaloniaXamlLoader.Load(this);
            _editorPanel = this.FindControl<StackPanel>("EditorPanel")!;
            _emptyHint = this.FindControl<Border>("EmptyHint")!;
            _presetList = this.FindControl<ListBox>("PresetList")!;
            _txtPresetName = this.FindControl<TextBox>("TxtPresetName")!;
            _txtPresetAuthor = this.FindControl<TextBox>("TxtPresetAuthor")!;
            _lblFilterSummary = this.FindControl<TextBlock>("LblFilterSummary")!;
            _lblMatchCount = this.FindControl<TextBlock>("LblMatchCount")!;
            _presetsDir = Path.Combine(AppContext.BaseDirectory, "presets.bss");

            LoadPresets();
            if (_presets.Count > 0)
            {
                SelectPreset(0);
            }
            else
            {
                CreateNewPreset();
            }
        }

        private IBrush? GetBrush(string key)
        {
            if (global::Avalonia.Application.Current?.TryFindResource(key, out var value) == true)
                return value as IBrush;
            return null;
        }

        private void LoadPresets()
        {
            _presets.Clear();
            try
            {
                if (!Directory.Exists(_presetsDir))
                    Directory.CreateDirectory(_presetsDir);
                foreach (var file in Directory.GetFiles(_presetsDir, "*.json"))
                {
                    var preset = BeatSpiderSharp.Core.Utilities.PresetLoader.LoadPreset(file);
                    if (preset != null)
                        _presets.Add(new PresetItem { Preset = preset, FilePath = file });
                }
            }
            catch { }
            RefreshList();
        }

        private void RefreshList()
        {
            var names = _presets.Select(p => p.Preset.Name ?? "(未命名)").ToList();
            _presetList.ItemsSource = names;
        }

        private void SelectPreset(int index)
        {
            if (index < 0 || index >= _presets.Count) return;
            CurrentPreset = _presets[index].Preset;
            _presetList.SelectedIndex = index;
            SyncUI();
        }

        private void SyncUI()
        {
            if (CurrentPreset == null) return;
            _txtPresetName.Text = CurrentPreset.Name;
            _txtPresetAuthor.Text = CurrentPreset.Author;
            RebuildFilterUI();
            UpdateFilterSummary();
        }

        private void RebuildFilterUI()
        {
            _editorPanel.Children.Clear();
            if (CurrentPreset == null)
            {
                _emptyHint.IsVisible = true;
                _editorPanel.Children.Add(_emptyHint);
                return;
            }
            _emptyHint.IsVisible = false;

            // Add Group button
            var addGroupBtn = new Button
            {
                Content = "+ 添加筛选组",
                HorizontalAlignment = HorizontalAlignment.Left,
                Classes = { "bs-blue" }
            };
            addGroupBtn.Click += (s, e) =>
            {
                CurrentPreset.FilterOptions.Add(new FilterConfig());
                RebuildFilterUI();
            };
            _editorPanel.Children.Add(addGroupBtn);

            // Render each filter group
            for (int i = 0; i < CurrentPreset.FilterOptions.Count; i++)
            {
                var config = CurrentPreset.FilterOptions[i];
                _editorPanel.Children.Add(BuildFilterConfigPanel(config, i));
            }

            // Output config section
            _editorPanel.Children.Add(BuildOutputConfigPanel());
        }

        private Border BuildFilterConfigPanel(FilterConfig config, int index)
        {
            var border = new Border
            {
                Background = GetBrush("PanelBackgroundBrush"),
                BorderBrush = GetBrush("BorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var stack = new StackPanel { Spacing = 8 };

            var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var lbl = new TextBlock
            {
                Text = $"筛选组 {index + 1}  (OR 逻辑)",
                FontWeight = FontWeight.Bold,
                Foreground = GetBrush("HighlightBlueBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };
            header.Children.Add(lbl);

            var btnDel = new Button
            {
                Content = "删除此组",
                VerticalAlignment = VerticalAlignment.Center,
                Classes = { "bs-red" }
            };
            btnDel.Click += (s, e) =>
            {
                CurrentPreset!.FilterOptions.RemoveAt(index);
                RebuildFilterUI();
            };
            header.Children.Add(btnDel);
            stack.Children.Add(header);

            var tabControl = new TabControl { Margin = new Thickness(0, 4, 0, 0) };
            tabControl.Items.Add(BuildSongDetailTab(config.SongDetailFilter));
            tabControl.Items.Add(BuildLevelDetailTab(config.LevelDetailOptions));
            tabControl.Items.Add(BuildSearchTab(config.SearchOptions));
            stack.Children.Add(tabControl);
            border.Child = stack;
            return border;
        }

        private Border BuildOutputConfigPanel()
        {
            if (CurrentPreset == null) return new Border();
            var output = CurrentPreset.Output;

            var border = new Border
            {
                Background = GetBrush("PanelBackgroundBrush"),
                BorderBrush = GetBrush("BorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 8, 0, 0)
            };
            var stack = new StackPanel { Spacing = 6 };

            var lbl = new TextBlock
            {
                Text = "输出配置",
                FontWeight = FontWeight.Bold,
                Foreground = GetBrush("HighlightGreenBrush")
            };
            stack.Children.Add(lbl);

            var limitPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var chkLimit = new CheckBox
            {
                Content = "限制数量:",
                IsChecked = output.LimitSongs,
                VerticalAlignment = VerticalAlignment.Center
            };
            var numLimit = new NumericUpDown
            {
                Value = output.MaxSongs ?? 100,
                Minimum = 1,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 80
            };
            chkLimit.IsCheckedChanged += (s, e) =>
            {
                output.LimitSongs = chkLimit.IsChecked ?? false;
                output.MaxSongs = (int)(numLimit.Value ?? 100);
            };
            numLimit.ValueChanged += (s, e) => output.MaxSongs = (int)(numLimit.Value ?? 100);
            limitPanel.Children.Add(chkLimit);
            limitPanel.Children.Add(numLimit);
            stack.Children.Add(limitPanel);

            var playlistPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var chkPlaylist = new CheckBox
            {
                Content = "导出歌单:",
                IsChecked = output.Playlist.SavePlaylist,
                VerticalAlignment = VerticalAlignment.Center
            };
            var txtPlaylistDir = new TextBox
            {
                Text = output.Playlist.PlaylistDirectory,
                MinWidth = 250,
                VerticalAlignment = VerticalAlignment.Center,
                IsReadOnly = true
            };
            var btnBrowse = new Button { Content = "浏览...", VerticalAlignment = VerticalAlignment.Center };
            chkPlaylist.IsCheckedChanged += (s, e) => output.Playlist.SavePlaylist = chkPlaylist.IsChecked ?? false;
            btnBrowse.Click += async (s, e) =>
            {
                var topLevel = GetTopLevel(this);
                if (topLevel == null) return;
                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                { Title = "选择歌单导出目录" });
                if (folders.Count > 0)
                {
                    output.Playlist.PlaylistDirectory = folders[0].Path.LocalPath;
                    SyncUI();
                }
            };
            playlistPanel.Children.Add(chkPlaylist);
            playlistPanel.Children.Add(txtPlaylistDir);
            playlistPanel.Children.Add(btnBrowse);
            stack.Children.Add(playlistPanel);

            var downloadPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var chkDownload = new CheckBox
            {
                Content = "下载歌曲:",
                IsChecked = output.SongDownload.DownloadSongs,
                VerticalAlignment = VerticalAlignment.Center
            };
            var txtDownloadDir = new TextBox
            {
                Text = output.SongDownload.DownloadPath,
                MinWidth = 250,
                VerticalAlignment = VerticalAlignment.Center,
                IsReadOnly = true
            };
            var btnBrowse2 = new Button { Content = "浏览...", VerticalAlignment = VerticalAlignment.Center };
            chkDownload.IsCheckedChanged += (s, e) => output.SongDownload.DownloadSongs = chkDownload.IsChecked ?? false;
            btnBrowse2.Click += async (s, e) =>
            {
                var topLevel = GetTopLevel(this);
                if (topLevel == null) return;
                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                { Title = "选择歌曲下载目录" });
                if (folders.Count > 0)
                {
                    output.SongDownload.DownloadPath = folders[0].Path.LocalPath;
                    SyncUI();
                }
            };
            downloadPanel.Children.Add(chkDownload);
            downloadPanel.Children.Add(txtDownloadDir);
            downloadPanel.Children.Add(btnBrowse2);
            stack.Children.Add(downloadPanel);

            border.Child = stack;
            return border;
        }

        private TabItem BuildSongDetailTab(SongDetailOptions s)
        {
            var panel = new StackPanel { Spacing = 11, Margin = new Thickness(8) };

            panel.Children.Add(BuildRangeRow("BPM", s.Bpm));
            panel.Children.Add(BuildRangeRowInt("时长(秒)", s.Duration));
            panel.Children.Add(BuildRangeRowFloat("评分", s.Rating));
            panel.Children.Add(BuildRangeRowInt("点赞数", s.UpVotes));
            panel.Children.Add(BuildRangeRowInt("点踩数", s.DownVotes));
            panel.Children.Add(BuildRangeRowFloat("点赞比例%(0-100)", s.UpVotePercentage));
            panel.Children.Add(BuildRangeRowFloat("点踩比例%(0-100)", s.DownVotePercentage));
            panel.Children.Add(BuildRangeRowInt("Sage分数", s.SageScore));
            panel.Children.Add(BuildTagRow("包含标签(逗号分隔)", s.IncludeTags));
            panel.Children.Add(BuildTagRow("排除标签(逗号分隔)", s.ExcludeTags));
            panel.Children.Add(BuildTextRow("上传者名称", s.UploaderName));
            panel.Children.Add(BuildBooleanRow("仅人类(非AutoMapper)*", s.AutoMapper));
            panel.Children.Add(BuildRankingRow("ScoreSaber排名状态", s.ScoreSaberRanking));
            panel.Children.Add(BuildRankingRow("BeatLeader排名状态", s.BeatLeaderRanking));

            var scroll = new ScrollViewer { Content = panel };
            return new TabItem { Header = "歌曲详情", Content = scroll };
        }

        private TabItem BuildLevelDetailTab(LevelDetailOptions l)
        {
            var panel = new StackPanel { Spacing = 11, Margin = new Thickness(8) };

            panel.Children.Add(BuildRangeRow("NPS", l.Nps));
            panel.Children.Add(BuildRangeRow("NJS", l.Njs));
            panel.Children.Add(BuildRangeRow("SS星级", l.ScoreSaberStars));
            panel.Children.Add(BuildRangeRow("BL星级", l.BeatLeaderStars));
            panel.Children.Add(BuildRangeRow("Offset", l.Offset));
            panel.Children.Add(BuildRangeRow("时长(秒)", l.Seconds));
            panel.Children.Add(BuildRangeRow("节拍数", l.Beats));
            panel.Children.Add(BuildRangeRowInt("方块数", l.Notes));
            panel.Children.Add(BuildRangeRowInt("炸弹数", l.Bombs));
            panel.Children.Add(BuildRangeRowInt("事件数", l.Events));
            panel.Children.Add(BuildRangeRowInt("墙壁数", l.Walls));
            panel.Children.Add(BuildRangeRowInt("Parity错误", l.ParityErrors));
            panel.Children.Add(BuildRangeRowInt("Parity警告", l.ParityWarns));
            panel.Children.Add(BuildRangeRowInt("Parity重置", l.ParityResets));
            panel.Children.Add(BuildRangeRowInt("MaxScore", l.MaxScore));

            panel.Children.Add(BuildModRow("要求Mod(AND 逻辑)", l.RequireMods));
            panel.Children.Add(BuildModExcludeRow("排除Mod", l.ExcludeMods));
            panel.Children.Add(BuildCharRow("包含难度", l.IncludeCharacteristics));
            panel.Children.Add(BuildDiffRow("包含难度", l.IncludeDifficulties));

            var scroll = new ScrollViewer { Content = panel };
            return new TabItem { Header = "铺面详情", Content = scroll };
        }

        private TabItem BuildSearchTab(SearchOptions q)
        {
            var panel = new StackPanel { Spacing = 11, Margin = new Thickness(8) };

            var chkEnable = new CheckBox { Content = "启用文本搜索", IsChecked = q.Enable };
            chkEnable.IsCheckedChanged += (s, e) => { q.Enable = chkEnable.IsChecked ?? false; UpdateFilterSummary(); };
            panel.Children.Add(chkEnable);

            var fieldCheckPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            var chks = new Dictionary<string, Action<bool>>
            {
                { "歌名", v => q.SearchTitle = v },
                { "曲名", v => q.SearchSongName = v },
                { "作者", v => q.SearchAuthor = v },
                { "谱师", v => q.SearchMapper = v },
                { "简介", v => q.SearchDescription = v }
            };
            var initStates = new Dictionary<string, bool>
            {
                { "歌名", q.SearchTitle },
                { "曲名", q.SearchSongName },
                { "作者", q.SearchAuthor },
                { "谱师", q.SearchMapper },
                { "简介", q.SearchDescription }
            };
            foreach (var kv in initStates)
            {
                var cb = new CheckBox { Content = kv.Key, IsChecked = kv.Value };
                cb.IsCheckedChanged += (s, e) => chks[kv.Key](cb.IsChecked ?? false);
                fieldCheckPanel.Children.Add(cb);
            }
            panel.Children.Add(fieldCheckPanel);

            var txtRegex = new TextBox
            {
                PlaceholderText = "正则模式 (每行一个)",
                MinHeight = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true
            };
            panel.Children.Add(txtRegex);

            var txtSearch = new TextBox
            {
                PlaceholderText = "简易搜索关键词 (支持多词)",
                MinHeight = 40,
                TextWrapping = TextWrapping.Wrap
            };
            txtSearch.TextChanged += (s, e) =>
            {
                q.AdvanceTerms.Clear();
                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                    q.AdvanceTerms.Add(new AdvanceSearchTerm { Content = txtSearch.Text.Trim() });
            };
            panel.Children.Add(txtSearch);

            var scroll = new ScrollViewer { Content = panel };
            return new TabItem { Header = "文本搜索", Content = scroll };
        }

        // --- UI helpers ---
        private StackPanel BuildRangeRow(string label, RangeOption<float> opt)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            var lbl = new TextBlock { Text = label + ":", VerticalAlignment = VerticalAlignment.Center, MinWidth = 140, Foreground = GetBrush("TextSecondaryBrush") };
            var min = new NumericUpDown { Value = (decimal)(opt.Min ?? 0), MinWidth = 70 };
            var max = new NumericUpDown { Value = (decimal)(opt.Max ?? 0), MinWidth = 70 };
            min.ValueChanged += (s, e) => { opt.Enable = true; opt.Min = (float)(min.Value ?? 0); };
            max.ValueChanged += (s, e) => { opt.Enable = true; opt.Max = (float)(max.Value ?? 0); };
            row.Children.Add(lbl);
            row.Children.Add(min);
            row.Children.Add(new TextBlock { Text = "~", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0) });
            row.Children.Add(max);
            return row;
        }

        private StackPanel BuildRangeRowInt(string label, RangeOption<int> opt)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            var lbl = new TextBlock { Text = label + ":", VerticalAlignment = VerticalAlignment.Center, MinWidth = 140, Foreground = GetBrush("TextSecondaryBrush") };
            var min = new NumericUpDown { Value = opt.Min ?? 0, MinWidth = 70 };
            var max = new NumericUpDown { Value = opt.Max ?? 0, MinWidth = 70 };
            min.ValueChanged += (s, e) => { opt.Enable = true; opt.Min = (int)(min.Value ?? 0); };
            max.ValueChanged += (s, e) => { opt.Enable = true; opt.Max = (int)(max.Value ?? 0); };
            row.Children.Add(lbl);
            row.Children.Add(min);
            row.Children.Add(new TextBlock { Text = "~", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0) });
            row.Children.Add(max);
            return row;
        }

        private StackPanel BuildRangeRowFloat(string label, RangeOption<float> opt)
        {
            return BuildRangeRow(label, opt);
        }

        private StackPanel BuildTagRow(string label, LogicIncludeOption<string> opt)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            var lbl = new TextBlock { Text = label + ":", VerticalAlignment = VerticalAlignment.Center, MinWidth = 140, Foreground = GetBrush("TextSecondaryBrush") };
            var tagsStr = string.Join(",", opt.Filter);
            var txt = new TextBox { Text = tagsStr, MinWidth = 200 };
            txt.TextChanged += (s, e) =>
            {
                opt.Enable = true;
                opt.Filter.Clear();
                if (!string.IsNullOrWhiteSpace(txt.Text))
                    foreach (var tag in txt.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        opt.Filter.Add(tag);
            };
            row.Children.Add(lbl);
            row.Children.Add(txt);
            return row;
        }

        private StackPanel BuildTagRow(string label, LogicExcludeOption<string> opt)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            var lbl = new TextBlock { Text = label + ":", VerticalAlignment = VerticalAlignment.Center, MinWidth = 140, Foreground = GetBrush("TextSecondaryBrush") };
            var tagsStr = string.Join(",", opt.Filter);
            var txt = new TextBox { Text = tagsStr, MinWidth = 200 };
            txt.TextChanged += (s, e) =>
            {
                opt.Enable = true;
                opt.Filter.Clear();
                if (!string.IsNullOrWhiteSpace(txt.Text))
                    foreach (var tag in txt.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        opt.Filter.Add(tag);
            };
            row.Children.Add(lbl);
            row.Children.Add(txt);
            return row;
        }

        private StackPanel BuildTextRow(string label, IncludeOption<string> opt)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            var lbl = new TextBlock { Text = label + ":", VerticalAlignment = VerticalAlignment.Center, MinWidth = 140, Foreground = GetBrush("TextSecondaryBrush") };
            var name = opt.Filter.Count > 0 ? opt.Filter.First() : "";
            var txt = new TextBox { Text = name, MinWidth = 200 };
            txt.TextChanged += (s, e) =>
            {
                opt.Enable = true;
                opt.Filter.Clear();
                if (!string.IsNullOrWhiteSpace(txt.Text))
                    opt.Filter.Add(txt.Text.Trim());
            };
            row.Children.Add(lbl);
            row.Children.Add(txt);
            return row;
        }

        private StackPanel BuildBooleanRow(string label, Option<bool> opt)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            var chk = new CheckBox { Content = label + " (当前: " + (opt.Filter ? "是" : "否") + ")", IsChecked = opt.Filter, VerticalAlignment = VerticalAlignment.Center };
            chk.IsCheckedChanged += (s, e) =>
            {
                opt.Enable = true;
                opt.Filter = chk.IsChecked ?? false;
                chk.Content = label + " (当前: " + (opt.Filter ? "是" : "否") + ")";
            };
            row.Children.Add(chk);
            return row;
        }

        private StackPanel BuildRankingRow(string label, IncludeOption<RankingStatus> opt)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            var lbl = new TextBlock { Text = label + ":", VerticalAlignment = VerticalAlignment.Center, MinWidth = 140, Foreground = GetBrush("TextSecondaryBrush") };
            var current = opt.Filter.FirstOrDefault();
            var cbo = new ComboBox
            {
                ItemsSource = new[] { "不限", "Ranked", "Qualified", "Unranked" },
                MinWidth = 120
            };
            if (current == RankingStatus.Ranked) cbo.SelectedIndex = 1;
            else if (current == RankingStatus.Qualified) cbo.SelectedIndex = 2;
            else if (current == RankingStatus.Unranked) cbo.SelectedIndex = 3;
            else cbo.SelectedIndex = 0;
            cbo.SelectionChanged += (s, e) =>
            {
                opt.Enable = true;
                opt.Filter.Clear();
                if (cbo.SelectedIndex == 1) opt.Filter.Add(RankingStatus.Ranked);
                else if (cbo.SelectedIndex == 2) opt.Filter.Add(RankingStatus.Qualified);
                else if (cbo.SelectedIndex == 3) opt.Filter.Add(RankingStatus.Unranked);
            };
            row.Children.Add(lbl);
            row.Children.Add(cbo);
            return row;
        }

        private StackPanel BuildModRow(string label, LogicIncludeOption<MMod> opt)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            var lbl = new TextBlock { Text = label + ":", VerticalAlignment = VerticalAlignment.Center, MinWidth = 140, Foreground = GetBrush("TextSecondaryBrush") };
            var modNames = new[] { "Chroma", "NoodleExt", "MappingExt", "Cinema", "Vivify" };
            var modValues = new[] { MMod.Chroma, MMod.NoodleExtensions, MMod.MappingExtensions, MMod.Cinema, MMod.Vivify };
            var modPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            for (int i = 0; i < modNames.Length; i++)
            {
                var m = modValues[i];
                var chk = new CheckBox { Content = modNames[i], IsChecked = opt.Filter.Contains(m) };
                chk.IsCheckedChanged += (s, e) =>
                {
                    opt.Enable = true;
                    if (chk.IsChecked ?? false) opt.Filter.Add(m);
                    else opt.Filter.Remove(m);
                };
                modPanel.Children.Add(chk);
            }
            row.Children.Add(lbl);
            row.Children.Add(modPanel);
            return row;
        }

        private StackPanel BuildModExcludeRow(string label, ExcludeOption<MMod> opt)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            var lbl = new TextBlock { Text = label + ":", VerticalAlignment = VerticalAlignment.Center, MinWidth = 140, Foreground = GetBrush("TextSecondaryBrush") };
            var modNames = new[] { "Chroma", "NoodleExt", "MappingExt", "Cinema", "Vivify" };
            var modValues = new[] { MMod.Chroma, MMod.NoodleExtensions, MMod.MappingExtensions, MMod.Cinema, MMod.Vivify };
            var modPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            for (int i = 0; i < modNames.Length; i++)
            {
                var m = modValues[i];
                var chk = new CheckBox { Content = modNames[i], IsChecked = opt.Filter.Contains(m) };
                chk.IsCheckedChanged += (s, e) =>
                {
                    opt.Enable = true;
                    if (chk.IsChecked ?? false) opt.Filter.Add(m);
                    else opt.Filter.Remove(m);
                };
                modPanel.Children.Add(chk);
            }
            row.Children.Add(lbl);
            row.Children.Add(modPanel);
            return row;
        }

        private StackPanel BuildCharRow(string label, LogicIncludeOption<MCharacteristic> opt)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            var lbl = new TextBlock { Text = label + ":", VerticalAlignment = VerticalAlignment.Center, MinWidth = 140, Foreground = GetBrush("TextSecondaryBrush") };
            var charValues = Enum.GetValues<MCharacteristic>();
            var charPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            foreach (var c in charValues)
            {
                var chk = new CheckBox { Content = c.ToString(), IsChecked = opt.Filter.Contains(c) };
                chk.IsCheckedChanged += (s, e) =>
                {
                    opt.Enable = true;
                    if (chk.IsChecked ?? false) opt.Filter.Add(c);
                    else opt.Filter.Remove(c);
                };
                charPanel.Children.Add(chk);
            }
            row.Children.Add(lbl);
            row.Children.Add(charPanel);
            return row;
        }

        private StackPanel BuildDiffRow(string label, LogicIncludeOption<MDifficulty> opt)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };
            var lbl = new TextBlock { Text = label + ":", VerticalAlignment = VerticalAlignment.Center, MinWidth = 140, Foreground = GetBrush("TextSecondaryBrush") };
            var diffValues = Enum.GetValues<MDifficulty>();
            var diffPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            foreach (var d in diffValues)
            {
                var chk = new CheckBox { Content = d.ToString(), IsChecked = opt.Filter.Contains(d) };
                chk.IsCheckedChanged += (s, e) =>
                {
                    opt.Enable = true;
                    if (chk.IsChecked ?? false) opt.Filter.Add(d);
                    else opt.Filter.Remove(d);
                };
                diffPanel.Children.Add(chk);
            }
            row.Children.Add(lbl);
            row.Children.Add(diffPanel);
            return row;
        }

        private void UpdateFilterSummary()
        {
            if (CurrentPreset == null || CurrentPreset.FilterOptions.Count == 0)
            {
                _lblFilterSummary.Text = "当前筛选：无";
                return;
            }
            var parts = new List<string>();
            for (int i = 0; i < CurrentPreset.FilterOptions.Count; i++)
            {
                var c = CurrentPreset.FilterOptions[i];
                var sub = new List<string>();

                if (HasAnySongFilter(c.SongDetailFilter)) sub.Add("歌曲");
                if (HasAnyLevelFilter(c.LevelDetailOptions)) sub.Add("铺面");
                if (c.SearchOptions.Enable) sub.Add("文本");
                if (sub.Count == 0) sub.Add("无筛选");

                parts.Add($"[{string.Join("+", sub)}]");
            }
            _lblFilterSummary.Text = "当前筛选：" + string.Join(" OR ", parts) +
                $" (共 {CurrentPreset.FilterOptions.Count} 组)";
        }

        private static bool HasAnySongFilter(SongDetailOptions s) =>
            s.Bpm.Enable || s.Duration.Enable || s.UpVotes.Enable || s.DownVotes.Enable ||
            s.UpVotePercentage.Enable || s.DownVotePercentage.Enable || s.Rating.Enable ||
            s.ScoreSaberRanking.Enable || s.BeatLeaderRanking.Enable || s.SageScore.Enable ||
            s.IncludeTags.Enable || s.ExcludeTags.Enable || s.AutoMapper.Enable ||
            s.UploaderName.Enable;

        private static bool HasAnyLevelFilter(LevelDetailOptions l) =>
            l.Nps.Enable || l.Njs.Enable || l.ScoreSaberStars.Enable || l.BeatLeaderStars.Enable ||
            l.Offset.Enable || l.Seconds.Enable || l.Beats.Enable || l.Notes.Enable ||
            l.Bombs.Enable || l.Events.Enable || l.Walls.Enable || l.ParityErrors.Enable ||
            l.ParityWarns.Enable || l.ParityResets.Enable || l.MaxScore.Enable ||
            l.RequireMods.Enable || l.ExcludeMods.Enable || l.IncludeCharacteristics.Enable ||
            l.IncludeDifficulties.Enable;

        // --- Preset Management ---
        private void CreateNewPreset()
        {
            CurrentPreset = new Preset
            {
                Name = "新预设",
                Author = Environment.UserName,
            };
            CurrentPreset.FilterOptions.Add(new FilterConfig());
            _presetList.SelectedIndex = -1;
            SyncUI();
        }

        private void OnNewPresetClick(object? sender, RoutedEventArgs e) => CreateNewPreset();

        private void OnSavePresetClick(object? sender, RoutedEventArgs e)
        {
            if (CurrentPreset == null) return;
            CurrentPreset.Name = string.IsNullOrWhiteSpace(_txtPresetName.Text) ? "未命名" : _txtPresetName.Text.Trim();
            CurrentPreset.Author = _txtPresetAuthor.Text?.Trim() ?? "";
            if (!Directory.Exists(_presetsDir)) Directory.CreateDirectory(_presetsDir);

            var path = Path.Combine(_presetsDir, CurrentPreset.Name + ".json");
            BeatSpiderSharp.Core.Utilities.PresetLoader.SavePreset(CurrentPreset, path);

            var existing = _presets.FindIndex(p => p.Preset.Name == CurrentPreset.Name);
            if (existing >= 0)
                _presets[existing] = new PresetItem { Preset = CurrentPreset, FilePath = path };
            else
            {
                _presets.Add(new PresetItem { Preset = CurrentPreset, FilePath = path });
                _presetList.SelectedIndex = _presets.Count - 1;
            }
            RefreshList();
        }

        private void OnDeletePresetClick(object? sender, RoutedEventArgs e)
        {
            if (_presetList.SelectedIndex < 0 || _presetList.SelectedIndex >= _presets.Count) return;
            var item = _presets[_presetList.SelectedIndex];
            if (File.Exists(item.FilePath)) File.Delete(item.FilePath);
            _presets.RemoveAt(_presetList.SelectedIndex);
            RefreshList();
            if (_presets.Count > 0)
                SelectPreset(0);
            else
                CreateNewPreset();
        }

        private void OnPresetSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (_presetList.SelectedIndex >= 0 && _presetList.SelectedIndex < _presets.Count)
                SelectPreset(_presetList.SelectedIndex);
        }

        private async void OnImportLegacyClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "导入旧预设 (.bsf / .brset / .json)",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("预设文件 (*.bsf;*.json)") { Patterns = new[] { "*.bsf", "*.json" } },
                    new FilePickerFileType("所有文件") { Patterns = new[] { "*.*" } }
                }
            });
            if (files == null || files.Count == 0) return;

            if (!Directory.Exists(_presetsDir)) Directory.CreateDirectory(_presetsDir);

            foreach (var file in files)
            {
                var path = file.Path.LocalPath;
                try
                {
                    // Try loading as new BSS preset first
                    var bssPreset = BeatSpiderSharp.Core.Utilities.PresetLoader.LoadPreset(path);
                    if (bssPreset != null)
                    {
                        // Deduplicate
                        var name = bssPreset.Name ?? "导入的预设";
                        int n = 2;
                        while (_presets.Any(p => p.Preset.Name == name)) name = $"{bssPreset.Name} ({n++})";
                        bssPreset.Name = name;
                        var outPath = Path.Combine(_presetsDir, name + ".json");
                        BeatSpiderSharp.Core.Utilities.PresetLoader.SavePreset(bssPreset, outPath);
                        _presets.Add(new PresetItem { Preset = bssPreset, FilePath = outPath });
                        continue;
                    }

                    // Try loading as legacy BSIMM .bsf preset
                    var bsf = FilterPreset.LoadFromFile(path);
                    if (bsf != null)
                    {
                        var converted = BsfToPresetConverter.Convert(bsf);
                        converted.Name = bsf.Name ?? "旧预设";
                        int n = 2;
                        var baseName = converted.Name;
                        while (_presets.Any(p => p.Preset.Name == converted.Name)) converted.Name = $"{baseName} ({n++})";
                        var outPath = Path.Combine(_presetsDir, converted.Name + ".json");
                        BeatSpiderSharp.Core.Utilities.PresetLoader.SavePreset(converted, outPath);
                        _presets.Add(new PresetItem { Preset = converted, FilePath = outPath });
                    }
                }
                catch { }
            }
            RefreshList();
            if (_presets.Count > 0) SelectPreset(_presets.Count - 1);
        }

        // --- Preview / Execute ---
        private async void OnPreviewClick(object? sender, RoutedEventArgs e)
        {
            if (CurrentPreset == null || CurrentPreset.FilterOptions.Count == 0) return;
            _lblMatchCount.Text = "预览中...";
            try
            {
                var cacheMgr = new LocalCacheManager();
                if (!cacheMgr.IsCacheAvailable)
                {
                    _lblMatchCount.Text = "缓存不可用,请先下载";
                    return;
                }

                var bsfPreset = ConvertBackToBsf(CurrentPreset);
                var token = System.Threading.CancellationToken.None;
                var results = await System.Threading.Tasks.Task.Run(() =>
                    cacheMgr.ParallelFilterMaps(bsfPreset, null, token));
                _lblMatchCount.Text = $"匹配: {results.Count} 首";
            }
            catch { _lblMatchCount.Text = "预览失败"; }
        }

        private static FilterPreset ConvertBackToBsf(Preset preset)
        {
            // Simple bridge: serialize BSS preset, then load as BSIMM via legacy compatibility
            var fpreset = new FilterPreset(preset.Name) { Description = preset.Description };
            foreach (var fc in preset.FilterOptions)
            {
                var group = new FilterGroup("筛选组");
                MapFilterConfigToGroup(fc, group);
                fpreset.AddGroup(group);
            }
            if (preset.Output.LimitSongs && preset.Output.MaxSongs > 0)
            {
                var limitCondition = new FilterCondition(FilterConditionType.ResultLimit,
                    new ResultLimitValue(preset.Output.MaxSongs.Value));
                if (fpreset.Groups.Count > 0)
                    fpreset.Groups[0].AddCondition(limitCondition);
                else
                    fpreset.TopLevelResultLimit = new ResultLimitValue(preset.Output.MaxSongs.Value);
            }
            return fpreset;
        }

        private static void MapFilterConfigToGroup(FilterConfig fc, FilterGroup group)
        {
            var s = fc.SongDetailFilter;
            var l = fc.LevelDetailOptions;
            var q = fc.SearchOptions;

            if (s.Bpm.Enable && (s.Bpm.Min.HasValue || s.Bpm.Max.HasValue))
                group.AddCondition(CreateRangeCond(FilterConditionType.BpmRange, s.Bpm.Min, s.Bpm.Max));
            if (s.Duration.Enable && (s.Duration.Min.HasValue || s.Duration.Max.HasValue))
                group.AddCondition(CreateRangeCondInt(FilterConditionType.DurationRange, s.Duration.Min, s.Duration.Max));
            if (s.Rating.Enable && (s.Rating.Min.HasValue || s.Rating.Max.HasValue))
                group.AddCondition(CreateRangeCond(FilterConditionType.ScoreRange, s.Rating.Min, s.Rating.Max));
            if (s.UpVotes.Enable && (s.UpVotes.Min.HasValue || s.UpVotes.Max.HasValue))
                group.AddCondition(CreateRangeCondInt(FilterConditionType.UpvotesRange, s.UpVotes.Min, s.UpVotes.Max));
            if (s.ScoreSaberRanking.Enable && s.ScoreSaberRanking.Filter.Contains(RankingStatus.Ranked))
                group.AddCondition(new FilterCondition(FilterConditionType.Ranked, true));
            if (s.ScoreSaberRanking.Enable && s.ScoreSaberRanking.Filter.Contains(RankingStatus.Qualified))
                group.AddCondition(new FilterCondition(FilterConditionType.Qualified, true));
            if (s.BeatLeaderRanking.Enable && s.BeatLeaderRanking.Filter.Contains(RankingStatus.Ranked))
                group.AddCondition(new FilterCondition(FilterConditionType.BlRanked, true));
            if (s.IncludeTags.Enable && s.IncludeTags.Filter.Count > 0)
                group.AddCondition(new FilterCondition(FilterConditionType.Tags, string.Join(",", s.IncludeTags.Filter)));
            if (s.ExcludeTags.Enable && s.ExcludeTags.Filter.Count > 0)
                group.AddCondition(new FilterCondition(FilterConditionType.ExcludeTags, string.Join(",", s.ExcludeTags.Filter)));
            if (s.AutoMapper.Enable)
                group.AddCondition(new FilterCondition(FilterConditionType.Automapper, s.AutoMapper.Filter));

            if (l.Nps.Enable && (l.Nps.Min.HasValue || l.Nps.Max.HasValue))
                group.AddCondition(CreateRangeCond(FilterConditionType.NpsRange, l.Nps.Min, l.Nps.Max));
            if (l.Njs.Enable && (l.Njs.Min.HasValue || l.Njs.Max.HasValue))
                group.AddCondition(CreateRangeCond(FilterConditionType.NjsRange, l.Njs.Min, l.Njs.Max));
            if (l.ScoreSaberStars.Enable && (l.ScoreSaberStars.Min.HasValue || l.ScoreSaberStars.Max.HasValue))
                group.AddCondition(CreateRangeCond(FilterConditionType.SsStarsRange, l.ScoreSaberStars.Min, l.ScoreSaberStars.Max));
            if (l.BeatLeaderStars.Enable && (l.BeatLeaderStars.Min.HasValue || l.BeatLeaderStars.Max.HasValue))
                group.AddCondition(CreateRangeCond(FilterConditionType.BlStarsRange, l.BeatLeaderStars.Min, l.BeatLeaderStars.Max));
            if (l.Notes.Enable && (l.Notes.Min.HasValue || l.Notes.Max.HasValue))
                group.AddCondition(CreateRangeCondInt(FilterConditionType.NotesRange, l.Notes.Min, l.Notes.Max));
            if (l.Walls.Enable && (l.Walls.Min.HasValue || l.Walls.Max.HasValue))
                group.AddCondition(CreateRangeCondInt(FilterConditionType.ObstaclesRange, l.Walls.Min, l.Walls.Max));

            foreach (var mod in l.RequireMods.Filter)
            {
                var ct = mod switch
                {
                    MMod.Chroma => FilterConditionType.Chroma,
                    MMod.NoodleExtensions => FilterConditionType.Noodle,
                    MMod.MappingExtensions => FilterConditionType.Me,
                    MMod.Cinema => FilterConditionType.Cinema,
                    MMod.Vivify => FilterConditionType.Vivify,
                    _ => FilterConditionType.None
                };
                if (ct != FilterConditionType.None)
                    group.AddCondition(new FilterCondition(ct, true));
            }

            if (q.Enable)
                group.AddCondition(new FilterCondition(FilterConditionType.Query, q.AdvanceTerms.FirstOrDefault()?.Content ?? ""));
        }

        private static FilterCondition CreateRangeCond(FilterConditionType type, double? min, double? max)
        {
            return new FilterCondition(type, new RangeValue
            {
                MinRaw = min ?? double.NaN,
                MaxRaw = max ?? double.NaN
            });
        }

        private static FilterCondition CreateRangeCondInt(FilterConditionType type, int? min, int? max)
        {
            return new FilterCondition(type, new RangeValue
            {
                MinRaw = min ?? double.NaN,
                MaxRaw = max ?? double.NaN
            });
        }

        private void OnExecuteClick(object? sender, RoutedEventArgs e)
        {
            if (CurrentPreset != null)
            {
                CurrentPreset.Name = string.IsNullOrWhiteSpace(_txtPresetName.Text) ? "未命名" : _txtPresetName.Text.Trim();
                CurrentPreset.Author = _txtPresetAuthor.Text?.Trim() ?? "";
                ExecuteRequested?.Invoke(this, CurrentPreset);
            }
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();
    }
}
