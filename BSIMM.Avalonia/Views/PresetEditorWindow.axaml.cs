using Avalonia;
using Avalonia.Controls;
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
        private StackPanel _conditionsPanel = null!;
        private Border _emptyPlaceholder = null!;
        private ComboBox _cboPreset = null!;
        private ComboBox _cboAddCondition = null!;
        private TextBox _txtPresetName = null!;
        private TextBlock _lblFilterSummary = null!;
        private TextBlock _lblMatchCount = null!;
        private CheckBox _chkLocalCache = null!;
        private CheckBox _chkLimit = null!;
        private NumericUpDown _numLimit = null!;
        private CheckBox _chkPlaylist = null!;
        private TextBox _txtPlaylistDir = null!;
        private CheckBox _chkDownload = null!;
        private TextBox _txtDownloadDir = null!;

        private string _presetsDir;
        private List<PresetInfo> _savedPresets = new();
        private List<FilterCondition> _conditions = new();

        private string PlaylistDir { get; set; } = "";
        private string DownloadDir { get; set; } = "";

        private struct PresetInfo
        {
            public BeatSpiderSharp.Models.Preset.Preset Preset { get; set; }
            public string FilePath { get; set; }
        }

        public delegate void ExecuteHandler(BeatSpiderSharp.Models.Preset.Preset preset, string playlistDir, string downloadDir);
        public ExecuteHandler? OnExecute;

        public PresetEditorWindow()
        {
            AvaloniaXamlLoader.Load(this);
            _conditionsPanel = this.FindControl<StackPanel>("ConditionsPanel")!;
            _emptyPlaceholder = this.FindControl<Border>("EmptyPlaceholder")!;
            _cboPreset = this.FindControl<ComboBox>("CboPreset")!;
            _cboAddCondition = this.FindControl<ComboBox>("CboAddCondition")!;
            _txtPresetName = this.FindControl<TextBox>("TxtPresetName")!;
            _lblFilterSummary = this.FindControl<TextBlock>("LblFilterSummary")!;
            _lblMatchCount = this.FindControl<TextBlock>("LblMatchCount")!;
            _chkLocalCache = this.FindControl<CheckBox>("ChkLocalCache")!;
            _chkLimit = this.FindControl<CheckBox>("ChkLimit")!;
            _numLimit = this.FindControl<NumericUpDown>("NumLimit")!;
            _chkPlaylist = this.FindControl<CheckBox>("ChkPlaylist")!;
            _txtPlaylistDir = this.FindControl<TextBox>("TxtPlaylistDir")!;
            _chkDownload = this.FindControl<CheckBox>("ChkDownload")!;
            _txtDownloadDir = this.FindControl<TextBox>("TxtDownloadDir")!;

            _presetsDir = Path.Combine(AppContext.BaseDirectory, "presets.bss");
            _chkLocalCache.Click += (s, e) => PopulateAddConditionCombo();

            PopulateAddConditionCombo();
            _cboAddCondition.SelectionChanged += OnAddConditionSelected;
            _cboPreset.SelectionChanged += OnPresetComboSelectionChanged;
            _chkLimit.Click += (s, e) => _numLimit.IsEnabled = _chkLimit.IsChecked ?? false;

            LoadSavedPresets();
            if (_savedPresets.Count == 0)
                CreateNewPreset();
        }

        private IBrush? GetBrush(string key)
        {
            if (global::Avalonia.Application.Current?.TryFindResource(key, out var value) == true)
                return value as IBrush;
            return null;
        }

        // === Preset Management ===

        private void LoadSavedPresets()
        {
            _savedPresets.Clear();
            try
            {
                if (!Directory.Exists(_presetsDir)) return;
                foreach (var file in Directory.GetFiles(_presetsDir, "*.json"))
                {
                    var preset = BeatSpiderSharp.Core.Utilities.PresetLoader.LoadPreset(file);
                    if (preset != null)
                        _savedPresets.Add(new PresetInfo { Preset = preset, FilePath = file });
                }
            }
            catch { }
            UpdatePresetCombo();
        }

        private void UpdatePresetCombo()
        {
            _cboPreset.ItemsSource = _savedPresets.Select(p => p.Preset.Name ?? "(未命名)").ToList();
            _cboPreset.SelectedIndex = -1;
        }

        private void CreateNewPreset()
        {
            _conditions.Clear();
            _txtPresetName.Text = "新预设";
            _cboPreset.SelectedIndex = -1;
            PlaylistDir = "";
            DownloadDir = "";
            _chkLimit.IsChecked = false;
            _chkPlaylist.IsChecked = false;
            _chkDownload.IsChecked = false;
            _numLimit.Value = 100;
            _txtPlaylistDir.Text = "";
            _txtDownloadDir.Text = "";
            RebuildUI();
        }

        private void LoadFromBssPreset(BeatSpiderSharp.Models.Preset.Preset? bssPreset)
        {
            if (bssPreset == null)
            {
                CreateNewPreset();
                return;
            }

            _conditions.Clear();
            _txtPresetName.Text = bssPreset.Name ?? "导入的预设";
            PlaylistDir = bssPreset.Output.Playlist.PlaylistDirectory ?? "";
            DownloadDir = bssPreset.Output.SongDownload.DownloadPath ?? "";
            _txtPlaylistDir.Text = PlaylistDir;
            _txtDownloadDir.Text = DownloadDir;
            _chkPlaylist.IsChecked = bssPreset.Output.Playlist.SavePlaylist;
            _chkDownload.IsChecked = bssPreset.Output.SongDownload.DownloadSongs;
            _chkLimit.IsChecked = bssPreset.Output.LimitSongs;
            _numLimit.Value = bssPreset.Output.MaxSongs ?? 100;
            _numLimit.IsEnabled = bssPreset.Output.LimitSongs;

            // Extract conditions from all FilterConfigs (merge into flat list)
            foreach (var fc in bssPreset.FilterOptions)
            {
                ExtractConditionsFromFilterConfig(fc);
            }
            RebuildUI();
        }

        private void ExtractConditionsFromFilterConfig(BeatSpiderSharp.Models.Preset.FilterConfig fc)
        {
            var s = fc.SongDetailFilter;
            var l = fc.LevelDetailOptions;
            var q = fc.SearchOptions;

            if (s.Bpm.Enable && (s.Bpm.Min.HasValue || s.Bpm.Max.HasValue))
                AddRangeCondition(FilterConditionType.BpmRange, s.Bpm.Min, s.Bpm.Max);
            if (s.Duration.Enable && (s.Duration.Min.HasValue || s.Duration.Max.HasValue))
                AddRangeCondition(FilterConditionType.DurationRange, s.Duration.Min, s.Duration.Max);
            if (s.Rating.Enable && (s.Rating.Min.HasValue || s.Rating.Max.HasValue))
                AddRangeCondition(FilterConditionType.ScoreRange, s.Rating.Min, s.Rating.Max);
            if (s.UpVotes.Enable && (s.UpVotes.Min.HasValue || s.UpVotes.Max.HasValue))
                AddRangeCondition(FilterConditionType.UpvotesRange, s.UpVotes.Min, s.UpVotes.Max);
            if (s.DownVotes.Enable && (s.DownVotes.Min.HasValue || s.DownVotes.Max.HasValue))
                AddRangeCondition(FilterConditionType.DownvotesRange, s.DownVotes.Min, s.DownVotes.Max);
            if (s.UpVotePercentage.Enable && (s.UpVotePercentage.Min.HasValue || s.UpVotePercentage.Max.HasValue))
                AddRangeCondition(FilterConditionType.UpvoteRatioRange, s.UpVotePercentage.Min, s.UpVotePercentage.Max);
            if (s.DownVotePercentage.Enable && (s.DownVotePercentage.Min.HasValue || s.DownVotePercentage.Max.HasValue))
                AddRangeCondition(FilterConditionType.DownvoteRatioRange, s.DownVotePercentage.Min, s.DownVotePercentage.Max);
            if (s.SageScore.Enable && (s.SageScore.Min.HasValue || s.SageScore.Max.HasValue))
                AddRangeCondition(FilterConditionType.SageScoreRange, s.SageScore.Min, s.SageScore.Max);
            if (s.AutoMapper.Enable)
                _conditions.Add(new FilterCondition(FilterConditionType.Automapper, s.AutoMapper.Filter));
            if (s.UploaderName.Enable && s.UploaderName.Filter.Count > 0)
                _conditions.Add(new FilterCondition(FilterConditionType.UploaderName, s.UploaderName.Filter.First()));
            if (s.IncludeTags.Enable && s.IncludeTags.Filter.Count > 0)
                _conditions.Add(new FilterCondition(FilterConditionType.Tags, string.Join(",", s.IncludeTags.Filter)));
            if (s.ExcludeTags.Enable && s.ExcludeTags.Filter.Count > 0)
                _conditions.Add(new FilterCondition(FilterConditionType.ExcludeTags, string.Join(",", s.ExcludeTags.Filter)));
            if (s.ScoreSaberRanking.Enable && s.ScoreSaberRanking.Filter.Contains(RankingStatus.Ranked))
                _conditions.Add(new FilterCondition(FilterConditionType.Ranked, true));
            if (s.ScoreSaberRanking.Enable && s.ScoreSaberRanking.Filter.Contains(RankingStatus.Qualified))
                _conditions.Add(new FilterCondition(FilterConditionType.Qualified, true));
            if (s.BeatLeaderRanking.Enable && s.BeatLeaderRanking.Filter.Contains(RankingStatus.Ranked))
                _conditions.Add(new FilterCondition(FilterConditionType.BlRanked, true));
            if (s.BeatLeaderRanking.Enable && s.BeatLeaderRanking.Filter.Contains(RankingStatus.Qualified))
                _conditions.Add(new FilterCondition(FilterConditionType.BlQualified, true));
            if (s.UploadTime.Enable && s.UploadTime.Min.HasValue)
                _conditions.Add(new FilterCondition(FilterConditionType.MinUploadedDate, s.UploadTime.Min.Value.DateTime));
            if (s.UploadTime.Enable && s.UploadTime.Max.HasValue)
                _conditions.Add(new FilterCondition(FilterConditionType.MaxUploadedDate, s.UploadTime.Max.Value.DateTime));
            if (l.Nps.Enable && (l.Nps.Min.HasValue || l.Nps.Max.HasValue))
                AddRangeCondition(FilterConditionType.NpsRange, l.Nps.Min, l.Nps.Max);
            if (l.Njs.Enable && (l.Njs.Min.HasValue || l.Njs.Max.HasValue))
                AddRangeCondition(FilterConditionType.NjsRange, l.Njs.Min, l.Njs.Max);
            if (l.ScoreSaberStars.Enable && (l.ScoreSaberStars.Min.HasValue || l.ScoreSaberStars.Max.HasValue))
                AddRangeCondition(FilterConditionType.SsStarsRange, l.ScoreSaberStars.Min, l.ScoreSaberStars.Max);
            if (l.BeatLeaderStars.Enable && (l.BeatLeaderStars.Min.HasValue || l.BeatLeaderStars.Max.HasValue))
                AddRangeCondition(FilterConditionType.BlStarsRange, l.BeatLeaderStars.Min, l.BeatLeaderStars.Max);
            if (l.Notes.Enable && (l.Notes.Min.HasValue || l.Notes.Max.HasValue))
                AddRangeCondition(FilterConditionType.NotesRange, l.Notes.Min, l.Notes.Max);
            if (l.Bombs.Enable && (l.Bombs.Min.HasValue || l.Bombs.Max.HasValue))
                AddRangeCondition(FilterConditionType.BombsRange, l.Bombs.Min, l.Bombs.Max);
            if (l.Events.Enable && (l.Events.Min.HasValue || l.Events.Max.HasValue))
                AddRangeCondition(FilterConditionType.EventsRange, l.Events.Min, l.Events.Max);
            if (l.Walls.Enable && (l.Walls.Min.HasValue || l.Walls.Max.HasValue))
                AddRangeCondition(FilterConditionType.ObstaclesRange, l.Walls.Min, l.Walls.Max);
            if (l.Seconds.Enable && (l.Seconds.Min.HasValue || l.Seconds.Max.HasValue))
                AddRangeCondition(FilterConditionType.SecondsRange, l.Seconds.Min, l.Seconds.Max);
            if (l.Beats.Enable && (l.Beats.Min.HasValue || l.Beats.Max.HasValue))
                AddRangeCondition(FilterConditionType.LengthRange, l.Beats.Min, l.Beats.Max);
            if (l.Offset.Enable && (l.Offset.Min.HasValue || l.Offset.Max.HasValue))
                AddRangeCondition(FilterConditionType.OffsetRange, l.Offset.Min, l.Offset.Max);
            if (l.ParityErrors.Enable && (l.ParityErrors.Min.HasValue || l.ParityErrors.Max.HasValue))
                AddRangeCondition(FilterConditionType.ParityErrorsRange, l.ParityErrors.Min, l.ParityErrors.Max);
            if (l.ParityWarns.Enable && (l.ParityWarns.Min.HasValue || l.ParityWarns.Max.HasValue))
                AddRangeCondition(FilterConditionType.ParityWarnsRange, l.ParityWarns.Min, l.ParityWarns.Max);
            if (l.ParityResets.Enable && (l.ParityResets.Min.HasValue || l.ParityResets.Max.HasValue))
                AddRangeCondition(FilterConditionType.ParityResetsRange, l.ParityResets.Min, l.ParityResets.Max);
            if (l.MaxScore.Enable && (l.MaxScore.Min.HasValue || l.MaxScore.Max.HasValue))
                AddRangeCondition(FilterConditionType.MaxScoreRange, l.MaxScore.Min, l.MaxScore.Max);
            foreach (var mod in l.RequireMods.Filter)
            {
                var ct = mod switch
                {
                    MMod.Chroma => FilterConditionType.Chroma,
                    MMod.NoodleExtensions => FilterConditionType.Noodle,
                    MMod.MappingExtensions => FilterConditionType.Me,
                    MMod.Cinema => FilterConditionType.Cinema,
                    MMod.Vivify => FilterConditionType.Vivify,
                    _ => FilterConditionType.Chroma
                };
                _conditions.Add(new FilterCondition(ct, true));
            }
            foreach (var c in l.IncludeCharacteristics.Filter)
                _conditions.Add(new FilterCondition(FilterConditionType.Characteristic, c.ToString()));
            foreach (var d in l.IncludeDifficulties.Filter)
                _conditions.Add(new FilterCondition(FilterConditionType.Difficulty, d.ToString()));
            if (q.Enable)
            {
                foreach (var term in q.AdvanceTerms)
                    _conditions.Add(new FilterCondition(FilterConditionType.Query, term.Content));
            }
        }

        private void AddRangeCondition(FilterConditionType type, double? min, double? max)
        {
            _conditions.Add(new FilterCondition(type, new RangeValue
            {
                MinRaw = min ?? double.NaN,
                MaxRaw = max ?? double.NaN
            }));
        }

        // === Build BSS Preset ===

        private BeatSpiderSharp.Models.Preset.Preset BuildBssPreset()
        {
            var bsfPreset = new FilterPreset(_txtPresetName.Text?.Trim() ?? "未命名");
            var group = new FilterGroup("筛选组");
            foreach (var c in _conditions)
                group.AddCondition(c.Clone());
            bsfPreset.AddGroup(group);

            if (_chkLimit.IsChecked == true)
            {
                var limitCond = new FilterCondition(FilterConditionType.ResultLimit, new ResultLimitValue((int)(_numLimit.Value ?? 100)));
                group.AddCondition(limitCond);
            }

            var bssPreset = BsfToPresetConverter.Convert(bsfPreset);
            bssPreset.Output.Playlist.SavePlaylist = _chkPlaylist.IsChecked ?? false;
            bssPreset.Output.Playlist.PlaylistDirectory = PlaylistDir;
            bssPreset.Output.SongDownload.DownloadSongs = _chkDownload.IsChecked ?? false;
            bssPreset.Output.SongDownload.DownloadPath = DownloadDir;
            return bssPreset;
        }

        // === UI Rebuild ===

        private void RebuildUI()
        {
            _conditionsPanel.Children.Clear();
            UpdateFilterSummary();

            if (_conditions.Count == 0)
            {
                _emptyPlaceholder.IsVisible = true;
                return;
            }

            _emptyPlaceholder.IsVisible = false;
            foreach (var condition in _conditions)
                _conditionsPanel.Children.Add(CreateConditionControl(condition));
        }

        private void UpdateFilterSummary()
        {
            if (_conditions.Count == 0)
            {
                _lblFilterSummary.Text = "当前筛选：无";
                return;
            }
            var names = _conditions.Where(c => c.IsEnabled)
                .Select(c => FilterConditionMetadata.GetDisplayName(c.Type));
            _lblFilterSummary.Text = "当前筛选：" + string.Join(", ", names);
        }

        private void PopulateAddConditionCombo()
        {
            var items = new List<ConditionComboItem>
            {
                new() { Type = FilterConditionType.None, Display = "-- 添加条件 --" }
            };

            var useLocalCache = _chkLocalCache.IsChecked ?? false;
            var grouped = FilterConditionMetadata.GetGroupedConditions();
            foreach (var cat in grouped)
            {
                if (!useLocalCache && cat.Key == "本地缓存专属") continue;

                items.Add(new ConditionComboItem { Type = FilterConditionType.None, Display = $"-- {cat.Key} --", IsSeparator = true });
                foreach (var type in cat.Value)
                {
                    if (!useLocalCache && FilterConditionMetadata.RequiresLocalCache(type)) continue;
                    items.Add(new ConditionComboItem { Type = type, Display = "  " + FilterConditionMetadata.GetDisplayName(type) });
                }
            }
            _cboAddCondition.ItemsSource = items;
            _cboAddCondition.SelectedIndex = 0;
        }

        private void OnAddConditionSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (_cboAddCondition.SelectedIndex <= 0) return;
            var item = _cboAddCondition.SelectedItem as ConditionComboItem;
            if (item == null || item.Type == FilterConditionType.None || item.IsSeparator) return;
            _conditions.Add(new FilterCondition(item.Type));
            RebuildUI();
            _cboAddCondition.SelectedIndex = 0;
        }

        // === Condition Control ===

        private class ConditionComboItem
        {
            public FilterConditionType Type { get; set; }
            public string Display { get; set; } = "";
            public bool IsSeparator { get; set; }
            public override string ToString() => Display;
        }

        private StackPanel CreateConditionControl(FilterCondition condition)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 2, 0, 2) };

            // Enable checkbox
            var chkEnabled = new CheckBox { IsChecked = condition.IsEnabled, VerticalAlignment = VerticalAlignment.Center };
            chkEnabled.IsCheckedChanged += (s, e) => { condition.IsEnabled = chkEnabled.IsChecked ?? true; UpdateFilterSummary(); };
            row.Children.Add(chkEnabled);

            // Type dropdown
            var typeItems = new List<ConditionComboItem>();
            var useLocalCache = _chkLocalCache.IsChecked ?? false;
            var grouped = FilterConditionMetadata.GetGroupedConditions();
            foreach (var cat in grouped)
            {
                if (!useLocalCache && cat.Key == "本地缓存专属") continue;
                typeItems.Add(new ConditionComboItem { Type = FilterConditionType.None, Display = $"-- {cat.Key} --", IsSeparator = true });
                foreach (var t in cat.Value)
                {
                    if (!useLocalCache && FilterConditionMetadata.RequiresLocalCache(t)) continue;
                    typeItems.Add(new ConditionComboItem { Type = t, Display = "  " + FilterConditionMetadata.GetDisplayName(t) });
                }
            }
            var cmbType = new ComboBox { MinWidth = 180, VerticalAlignment = VerticalAlignment.Center, ItemsSource = typeItems };
            var matchIdx = typeItems.FindIndex(t => t.Type == condition.Type);
            cmbType.SelectedIndex = matchIdx >= 0 ? matchIdx : 0;
            row.Children.Add(cmbType);

            // Value control wrapper
            var valueWrapper = new StackPanel
            {
                Orientation = Orientation.Horizontal, Spacing = 5, VerticalAlignment = VerticalAlignment.Center
            };
            BuildValueControl(valueWrapper, condition);
            row.Children.Add(valueWrapper);

            // Handle type change: update condition type + rebuild value controls
            cmbType.SelectionChanged += (s, e) =>
            {
                if (cmbType.SelectedIndex < 0) return;
                var item = cmbType.SelectedItem as ConditionComboItem;
                if (item == null || item.Type == FilterConditionType.None || item.IsSeparator) return;
                condition.Type = item.Type;
                condition.SetDefaultValue();
                BuildValueControl(valueWrapper, condition);
                UpdateFilterSummary();
            };

            // Remove button
            var btnRemove = new Button
            {
                Content = "✕", VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(10, 4), FontSize = 12
            };
            btnRemove.Click += (s, e) => { _conditions.Remove(condition); RebuildUI(); };
            row.Children.Add(btnRemove);

            return row;
        }

        private void BuildValueControl(StackPanel panel, FilterCondition condition)
        {
            panel.Children.Clear();
            var vt = condition.ValueType;
            var type = condition.Type;

            switch (vt)
            {
                case FilterValueType.Text:
                {
                    var txt = new TextBox { Text = condition.Value?.ToString() ?? "", MinWidth = 200 };
                    txt.TextChanged += (s, e) => condition.Value = txt.Text;
                    panel.Children.Add(txt);
                    break;
                }
                case FilterValueType.Number:
                {
                    var num = new NumericUpDown { Value = (decimal)Convert.ToDouble(condition.Value ?? 0), MinWidth = 100 };
                    num.ValueChanged += (s, e) => condition.Value = (double)(num.Value ?? 0);
                    panel.Children.Add(num);
                    break;
                }
                case FilterValueType.Boolean:
                {
                    var cmb = new ComboBox { ItemsSource = new[] { "不限", "是", "否" }, MinWidth = 80 };
                    cmb.SelectedIndex = condition.Value == null ? 0 : (Convert.ToBoolean(condition.Value) ? 1 : 2);
                    cmb.SelectionChanged += (s, e) => condition.Value = cmb.SelectedIndex == 0 ? null : (object)(cmb.SelectedIndex == 1);
                    panel.Children.Add(cmb);
                    break;
                }
                case FilterValueType.Selection:
                {
                    var options = condition.Options ?? new List<string>();
                    var cmb = new ComboBox { ItemsSource = options, MinWidth = 130 };
                    var cur = condition.Value?.ToString();
                    if (cur != null && options.Contains(cur)) cmb.SelectedIndex = options.IndexOf(cur);
                    cmb.SelectionChanged += (s, e) => condition.Value = cmb.SelectedIndex >= 0 ? options[cmb.SelectedIndex] : "";
                    panel.Children.Add(cmb);
                    break;
                }
                case FilterValueType.Range:
                {
                    var r = condition.Value as RangeValue ?? new RangeValue();
                    var numMin = new NumericUpDown
                    {
                        Value = decimal.TryParse(r.MinRaw.ToString(), out var min) ? min : 0,
                        MinWidth = 80,
                        PlaceholderText = "最小"
                    };
                    var numMax = new NumericUpDown
                    {
                        Value = decimal.TryParse(r.MaxRaw.ToString(), out var max) ? max : 0,
                        MinWidth = 80,
                        PlaceholderText = "最大"
                    };
                    numMin.ValueChanged += (s, e) => SetRange(condition, (double)(numMin.Value ?? 0), null);
                    numMax.ValueChanged += (s, e) => SetRange(condition, null, (double)(numMax.Value ?? 0));
                    panel.Children.Add(numMin);
                    panel.Children.Add(new TextBlock { Text = "~", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0) });
                    panel.Children.Add(numMax);
                    break;
                }
                case FilterValueType.SearchQuery:
                {
                    var qv = condition.Value as SearchQueryValue ?? new SearchQueryValue();
                    var txt = new TextBox { Text = qv.Query, MinWidth = 200, PlaceholderText = "搜索关键词" };
                    txt.TextChanged += (s, e) => { var q = condition.Value as SearchQueryValue ?? new SearchQueryValue(); q.Query = txt.Text; condition.Value = q; };
                    panel.Children.Add(txt);
                    break;
                }
                case FilterValueType.Date:
                {
                    var dateStr = condition.Value is DateTime dt ? dt.ToString("yyyy-MM-dd") : "";
                    var txt = new TextBox { Text = dateStr, MinWidth = 120, PlaceholderText = "yyyy-MM-dd" };
                    txt.TextChanged += (s, e) => { if (DateTime.TryParse(txt.Text, out var d)) condition.Value = d; };
                    panel.Children.Add(txt);
                    break;
                }
                case FilterValueType.NumberWithSort:
                {
                    var lv = condition.Value as ResultLimitValue ?? new ResultLimitValue(100);
                    var num = new NumericUpDown { Value = lv.Count, MinWidth = 80, PlaceholderText = "数量" };
                    var cmb = new ComboBox { ItemsSource = new[] { "最新", "最早", "随机" }, SelectedIndex = (int)lv.SortOption, MinWidth = 80 };
                    num.ValueChanged += (s, e) => { var r = condition.Value as ResultLimitValue ?? new ResultLimitValue(); r.Count = (int)(num.Value ?? 0); condition.Value = r; };
                    cmb.SelectionChanged += (s, e) => { var r = condition.Value as ResultLimitValue ?? new ResultLimitValue(); r.SortOption = (ResultSortOption)cmb.SelectedIndex; condition.Value = r; };
                    panel.Children.Add(num);
                    panel.Children.Add(cmb);
                    break;
                }
                case FilterValueType.ExcludeMod:
                {
                    var ev = condition.Value as ExcludeModValue ?? new ExcludeModValue();
                    var txt = new TextBox { Text = ev.ModName, MinWidth = 120, PlaceholderText = "Mod名称" };
                    var chk = new CheckBox { Content = "严格", IsChecked = ev.Strict };
                    txt.TextChanged += (s, e) => { var v = condition.Value as ExcludeModValue ?? new ExcludeModValue(); v.ModName = txt.Text; condition.Value = v; };
                    chk.IsCheckedChanged += (s, e) => { var v = condition.Value as ExcludeModValue ?? new ExcludeModValue(); v.Strict = chk.IsChecked ?? false; condition.Value = v; };
                    panel.Children.Add(txt);
                    panel.Children.Add(chk);
                    break;
                }
            }
        }

        private static void SetRange(FilterCondition cond, double? min, double? max)
        {
            var r = cond.Value as RangeValue ?? new RangeValue();
            if (min.HasValue) r.MinRaw = min.Value;
            if (max.HasValue) r.MaxRaw = max.Value;
            cond.Value = r;
        }

        // === Button Handlers ===

        private void OnNewPresetClick(object? sender, RoutedEventArgs e) => CreateNewPreset();

        private void OnSavePresetClick(object? sender, RoutedEventArgs e)
        {
            var bssPreset = BuildBssPreset();
            if (!Directory.Exists(_presetsDir)) Directory.CreateDirectory(_presetsDir);
            var path = Path.Combine(_presetsDir, SanitizeFileName(bssPreset.Name) + ".json");
            BeatSpiderSharp.Core.Utilities.PresetLoader.SavePreset(bssPreset, path);

            var existing = _savedPresets.FindIndex(p => p.Preset.Name == bssPreset.Name);
            if (existing >= 0) _savedPresets[existing] = new PresetInfo { Preset = bssPreset, FilePath = path };
            else _savedPresets.Add(new PresetInfo { Preset = bssPreset, FilePath = path });
            UpdatePresetCombo();
        }

        private void OnDeletePresetClick(object? sender, RoutedEventArgs e)
        {
            if (_cboPreset.SelectedIndex < 0 || _cboPreset.SelectedIndex >= _savedPresets.Count) return;
            var item = _savedPresets[_cboPreset.SelectedIndex];
            if (File.Exists(item.FilePath)) File.Delete(item.FilePath);
            _savedPresets.RemoveAt(_cboPreset.SelectedIndex);
            UpdatePresetCombo();
            CreateNewPreset();
        }

        private void OnPresetComboSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_cboPreset.SelectedIndex >= 0 && _cboPreset.SelectedIndex < _savedPresets.Count)
            {
                var item = _savedPresets[_cboPreset.SelectedIndex];
                LoadFromBssPreset(item.Preset);
            }
        }

        private async void OnImportLegacyClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "导入预设",
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
                    var bssPreset = BeatSpiderSharp.Core.Utilities.PresetLoader.LoadPreset(path);
                    if (bssPreset != null)
                    {
                        var name = bssPreset.Name ?? "导入的预设";
                        int n = 2;
                        var baseName = name;
                        while (_savedPresets.Any(p => p.Preset.Name == name)) name = $"{baseName} ({n++})";
                        bssPreset.Name = name;
                        var outPath = Path.Combine(_presetsDir, name + ".json");
                        BeatSpiderSharp.Core.Utilities.PresetLoader.SavePreset(bssPreset, outPath);
                        _savedPresets.Add(new PresetInfo { Preset = bssPreset, FilePath = outPath });
                        continue;
                    }

                    var bsf = FilterPreset.LoadFromFile(path);
                    if (bsf != null)
                    {
                        var converted = BsfToPresetConverter.Convert(bsf);
                        converted.Name = bsf.Name ?? "旧预设";
                        int n = 2;
                        var baseName = converted.Name;
                        while (_savedPresets.Any(p => p.Preset.Name == converted.Name)) converted.Name = $"{baseName} ({n++})";
                        var outPath = Path.Combine(_presetsDir, converted.Name + ".json");
                        BeatSpiderSharp.Core.Utilities.PresetLoader.SavePreset(converted, outPath);
                        _savedPresets.Add(new PresetInfo { Preset = converted, FilePath = outPath });
                    }
                }
                catch { }
            }
            UpdatePresetCombo();
            if (_savedPresets.Count > 0)
            {
                var last = _savedPresets.Last();
                LoadFromBssPreset(last.Preset);
            }
        }

        private async void OnBrowsePlaylistClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "选择歌单导出目录" });
            if (folders.Count > 0)
            {
                PlaylistDir = folders[0].Path.LocalPath;
                _txtPlaylistDir.Text = PlaylistDir;
            }
        }

        private async void OnBrowseDownloadClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "选择歌曲下载目录" });
            if (folders.Count > 0)
            {
                DownloadDir = folders[0].Path.LocalPath;
                _txtDownloadDir.Text = DownloadDir;
            }
        }

        private async void OnPreviewClick(object? sender, RoutedEventArgs e)
        {
            _lblMatchCount.Text = "预览中...";
            try
            {
                var bsfPreset = new FilterPreset("preview");
                var group = new FilterGroup("g");
                foreach (var c in _conditions) group.AddCondition(c.Clone());
                if (_chkLimit.IsChecked == true)
                    group.AddCondition(new FilterCondition(FilterConditionType.ResultLimit, new ResultLimitValue((int)_numLimit.Value)));
                bsfPreset.AddGroup(group);

                var cacheMgr = new LocalCacheManager();
                if (!cacheMgr.IsCacheAvailable)
                {
                    _lblMatchCount.Text = "缓存不可用";
                    return;
                }

                var token = System.Threading.CancellationToken.None;
                var results = await System.Threading.Tasks.Task.Run(
                    () => cacheMgr.ParallelFilterMaps(bsfPreset, null, token));
                _lblMatchCount.Text = $"匹配: {results.Count} 首";
            }
            catch (Exception ex)
            {
                _lblMatchCount.Text = $"预览失败: {ex.Message}";
            }
        }

        private void OnExecuteClick(object? sender, RoutedEventArgs e)
        {
            var bssPreset = BuildBssPreset();
            OnExecute?.Invoke(bssPreset, PlaylistDir, DownloadDir);
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();

        private static string SanitizeFileName(string name)
        {
            var invalids = System.IO.Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
    }
}
