using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Layout;
using global::Avalonia.Markup.Xaml;
using global::Avalonia.Platform.Storage;
using global::Avalonia.Media;
using BeatSaberIndependentMapsManager;
using BeatSaberIndependentMapsManager.BeatSpiderSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BSIMM.Avalonia.Views
{
    public partial class PresetEditorWindow : Window
    {
        private StackPanel _groupsPanel = null!;
        private Border _emptyPlaceholder = null!;
        private ComboBox _cboPreset = null!;
        private TextBox _txtPresetName = null!;
        private TextBlock _lblFilterSummary = null!;
        private CheckBox _chkLocalCache = null!;

        private List<FilterPreset> _savedPresets = new();
        private string _presetsDir = Path.Combine(AppContext.BaseDirectory, "presets");

        public FilterPreset? CurrentPreset { get; private set; }
        public event EventHandler<BeatSpiderSharp.Models.Preset.Preset>? SearchRequested;

        public PresetEditorWindow() : this(null) { }

        public PresetEditorWindow(FilterPreset? preset)
        {
            AvaloniaXamlLoader.Load(this);
            _groupsPanel = this.FindControl<StackPanel>("GroupsPanel")!;
            _cboPreset = this.FindControl<ComboBox>("CboPreset")!;
            _txtPresetName = this.FindControl<TextBox>("TxtPresetName")!;
            _lblFilterSummary = this.FindControl<TextBlock>("LblFilterSummary")!;
            _chkLocalCache = this.FindControl<CheckBox>("ChkLocalCache")!;
            _emptyPlaceholder = CreateEmptyPlaceholder();

            LoadSavedPresets();
            CurrentPreset = preset ?? new FilterPreset("新预设");
            if (CurrentPreset.Groups.Count == 0)
            {
                var g = new FilterGroup("条件组1");
                g.AddCondition(new FilterCondition(FilterConditionType.Query));
                CurrentPreset.AddGroup(g);
            }
            RebuildUI();
            UpdatePresetCombo();
            _cboPreset.SelectionChanged += OnPresetComboSelectionChanged;
        }

        private void OnPresetComboSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_cboPreset.SelectedIndex >= 0 && _cboPreset.SelectedIndex < _savedPresets.Count)
            {
                CurrentPreset = _savedPresets[_cboPreset.SelectedIndex].Clone();
                _txtPresetName.Text = CurrentPreset.Name;
                RebuildUI();
            }
        }

        private IBrush? GetBrush(string key)
        {
            if (global::Avalonia.Application.Current?.TryFindResource(key, out var value) == true)
                return value as IBrush;
            return null;
        }

        private Border CreateEmptyPlaceholder()
        {
            var border = new Border
            {
                Background = GetBrush("PanelBackgroundBrush"),
                BorderBrush = GetBrush("BorderBrush"),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(40),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 80, 0, 0)
            };
            var stack = new StackPanel { Spacing = 12, HorizontalAlignment = HorizontalAlignment.Center };
            stack.Children.Add(new TextBlock { Text = "还没有条件组", FontSize = 18, Foreground = GetBrush("TextSecondaryBrush"), HorizontalAlignment = HorizontalAlignment.Center });
            stack.Children.Add(new TextBlock { Text = "点击「添加条件组」开始构建", FontSize = 13, Foreground = GetBrush("TextSecondaryBrush"), HorizontalAlignment = HorizontalAlignment.Center });
            var btn = new Button { Content = "添加条件组", HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) };
            btn.Classes.Add("bs-blue");
            btn.Click += OnAddGroupClick;
            stack.Children.Add(btn);
            border.Child = stack;
            return border;
        }

        private void LoadSavedPresets()
        {
            _savedPresets.Clear();
            try
            {
                if (Directory.Exists(_presetsDir))
                {
                    foreach (var file in Directory.GetFiles(_presetsDir, "*.bsf"))
                    {
                        var p = FilterPreset.LoadFromFile(file);
                        if (p != null) _savedPresets.Add(p);
                    }
                }
            }
            catch { }
        }

        private void UpdatePresetCombo()
        {
            var names = _savedPresets.Select(p => p.Name).ToList();
            _cboPreset.ItemsSource = names;
            if (CurrentPreset != null)
            {
                _txtPresetName.Text = CurrentPreset.Name;
                var idx = _savedPresets.FindIndex(p => p.Name == CurrentPreset.Name);
                _cboPreset.SelectedIndex = idx >= 0 ? idx : -1;
            }
        }

        private void UpdateFilterSummary()
        {
            if (CurrentPreset == null || CurrentPreset.Groups.Count == 0)
            {
                _lblFilterSummary.Text = "当前筛选：无";
                return;
            }
            var sb = new System.Text.StringBuilder("当前筛选：");
            for (int i = 0; i < CurrentPreset.Groups.Count; i++)
            {
                var g = CurrentPreset.Groups[i];
                if (!g.IsEnabled) continue;
                var parts = new List<string>();
                foreach (var c in g.Conditions)
                {
                    if (!c.IsEnabled) continue;
                    parts.Add(FilterConditionMetadata.GetDisplayName(c.Type));
                }
                if (parts.Count > 0)
                    sb.Append($"[{string.Join($" {g.GroupOperator} ", parts)}]");
                if (i < CurrentPreset.Groups.Count - 1)
                    sb.Append($" {CurrentPreset.Groups[i].GroupOperator} ");
            }
            _lblFilterSummary.Text = sb.ToString();
        }

        private void RebuildUI()
        {
            _groupsPanel.Children.Clear();
            UpdateFilterSummary();

            if (CurrentPreset == null || CurrentPreset.Groups.Count == 0)
            {
                _emptyPlaceholder.IsVisible = true;
                _groupsPanel.Children.Add(_emptyPlaceholder);
                return;
            }

            _emptyPlaceholder.IsVisible = false;
            foreach (var group in CurrentPreset.Groups)
            {
                _groupsPanel.Children.Add(CreateGroupControl(group));
            }
        }

        private Border CreateGroupControl(FilterGroup group)
        {
            var groupBorder = new Border
            {
                Background = GetBrush("PanelBackgroundBrush"),
                BorderBrush = GetBrush("BorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Tag = group
            };

            var outerStack = new StackPanel { Spacing = 10 };

            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            var chkEnabled = new CheckBox { Content = "启用", IsChecked = group.IsEnabled, VerticalAlignment = VerticalAlignment.Center };
            chkEnabled.IsCheckedChanged += (s, e) => { group.IsEnabled = chkEnabled.IsChecked ?? true; UpdateFilterSummary(); };

            var lblName = new TextBlock
            {
                Text = string.IsNullOrEmpty(group.Name) ? "条件组" : group.Name,
                FontWeight = FontWeight.Bold, FontSize = 15,
                Foreground = GetBrush("HighlightBlueBrush"), VerticalAlignment = VerticalAlignment.Center
            };

            var cmbOperator = new ComboBox
            {
                ItemsSource = new[] { "AND (全部满足)", "OR (满足任一)" },
                SelectedIndex = group.GroupOperator == LogicOperator.And ? 0 : 1,
                VerticalAlignment = VerticalAlignment.Center, MinWidth = 140
            };
            cmbOperator.SelectionChanged += (s, e) => { group.GroupOperator = cmbOperator.SelectedIndex == 0 ? LogicOperator.And : LogicOperator.Or; UpdateFilterSummary(); };

            var chkCache = new CheckBox { Content = "本地缓存", IsChecked = group.UseLocalCache, VerticalAlignment = VerticalAlignment.Center, Foreground = GetBrush("HighlightBlueBrush") };
            chkCache.IsCheckedChanged += (s, e) => { group.UseLocalCache = chkCache.IsChecked ?? false; RebuildUI(); };

            var btnRemoveGroup = new Button { Content = "删除组", VerticalAlignment = VerticalAlignment.Center };
            btnRemoveGroup.Click += (s, e) => { CurrentPreset?.Groups.Remove(group); RebuildUI(); };

            headerPanel.Children.Add(chkEnabled);
            headerPanel.Children.Add(lblName);
            headerPanel.Children.Add(cmbOperator);
            headerPanel.Children.Add(chkCache);
            headerPanel.Children.Add(btnRemoveGroup);
            outerStack.Children.Add(headerPanel);

            var sep = new Border { Height = 1, Background = GetBrush("BorderBrush"), Margin = new Thickness(0, 2, 0, 2) };
            outerStack.Children.Add(sep);

            var conditionsPanel = new StackPanel { Spacing = 6, Margin = new Thickness(0, 0, 0, 0), Tag = group };
            foreach (var condition in group.Conditions)
            {
                conditionsPanel.Children.Add(CreateConditionControl(condition, group));
            }
            outerStack.Children.Add(conditionsPanel);

            var addCondPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 4, 0, 0) };
            var cboAddCondition = new ComboBox { MinWidth = 220, VerticalAlignment = VerticalAlignment.Center };
            PopulateConditionTypes(cboAddCondition, group.UseLocalCache);
            cboAddCondition.SelectionChanged += (s, e) =>
            {
                if (cboAddCondition.SelectedIndex <= 0) return;
                var tag = cboAddCondition.SelectedItem as ConditionComboItem;
                if (tag == null || tag.Type == FilterConditionType.None) return;
                var newCond = new FilterCondition(tag.Type);
                group.Conditions.Add(newCond);
                conditionsPanel.Children.Add(CreateConditionControl(newCond, group));
                cboAddCondition.SelectedIndex = 0;
            };
            addCondPanel.Children.Add(new TextBlock { Text = "+ 添加条件：", VerticalAlignment = VerticalAlignment.Center, Foreground = GetBrush("TextSecondaryBrush") });
            addCondPanel.Children.Add(cboAddCondition);
            outerStack.Children.Add(addCondPanel);

            groupBorder.Child = outerStack;
            return groupBorder;
        }

        private class ConditionComboItem
        {
            public FilterConditionType Type { get; set; }
            public string Display { get; set; } = "";
            public bool IsSeparator { get; set; }
            public override string ToString() => Display;
        }

        private void PopulateConditionTypes(ComboBox cbo, bool useLocalCache)
        {
            var items = new List<ConditionComboItem>
            {
                new ConditionComboItem { Type = FilterConditionType.None, Display = "-- 添加条件 --" }
            };

            var grouped = FilterConditionMetadata.GetGroupedConditions();
            foreach (var cat in grouped)
            {
                if (!useLocalCache && cat.Key == "本地缓存专属") continue;

                items.Add(new ConditionComboItem { Type = FilterConditionType.None, Display = $"── {cat.Key} ──", IsSeparator = true });
                foreach (var type in cat.Value)
                {
                    if (!useLocalCache && FilterConditionMetadata.RequiresLocalCache(type)) continue;
                    items.Add(new ConditionComboItem { Type = type, Display = "  " + FilterConditionMetadata.GetDisplayName(type) });
                }
            }
            cbo.ItemsSource = items;
            cbo.SelectedIndex = 0;
        }

        private StackPanel CreateConditionControl(FilterCondition condition, FilterGroup group)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Tag = condition, Margin = new Thickness(0, 2, 0, 2) };

            var chkEnabled = new CheckBox { IsChecked = condition.IsEnabled, VerticalAlignment = VerticalAlignment.Center };
            chkEnabled.IsCheckedChanged += (s, e) => { condition.IsEnabled = chkEnabled.IsChecked ?? true; UpdateFilterSummary(); };
            panel.Children.Add(chkEnabled);

            var typeItems = new List<ConditionComboItem>();
            var groupedConditions = FilterConditionMetadata.GetGroupedConditions();
            foreach (var category in groupedConditions)
            {
                if (!group.UseLocalCache && category.Key == "本地缓存专属") continue;
                typeItems.Add(new ConditionComboItem { Type = FilterConditionType.None, Display = $"── {category.Key} ──", IsSeparator = true });
                foreach (var type in category.Value)
                {
                    if (!group.UseLocalCache && FilterConditionMetadata.RequiresLocalCache(type)) continue;
                    typeItems.Add(new ConditionComboItem { Type = type, Display = "  " + FilterConditionMetadata.GetDisplayName(type) });
                }
            }
            var cmbType = new ComboBox { MinWidth = 180, VerticalAlignment = VerticalAlignment.Center, ItemsSource = typeItems };
            var matchIdx = typeItems.FindIndex(t => t.Type == condition.Type);
            cmbType.SelectedIndex = matchIdx >= 0 ? matchIdx : typeItems.FindIndex(t => t.Type == FilterConditionType.Query);
            panel.Children.Add(cmbType);

            var valuePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, VerticalAlignment = VerticalAlignment.Center };
            UpdateValueInput(valuePanel, condition);
            panel.Children.Add(valuePanel);

            var cmbOperator = new ComboBox { ItemsSource = new[] { "AND", "OR" }, SelectedIndex = condition.Operator == LogicOperator.And ? 0 : 1, VerticalAlignment = VerticalAlignment.Center, MinWidth = 60 };
            cmbOperator.SelectionChanged += (s, e) => condition.Operator = cmbOperator.SelectedIndex == 0 ? LogicOperator.And : LogicOperator.Or;
            panel.Children.Add(cmbOperator);

            var btnRemove = new Button { Content = "✕", VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(10, 4), FontSize = 12 };
            btnRemove.Click += (s, e) => { group.Conditions.Remove(condition); RebuildUI(); };
            panel.Children.Add(btnRemove);

            cmbType.SelectionChanged += (s, e) =>
            {
                if (cmbType.SelectedIndex < 0) return;
                var item = cmbType.SelectedItem as ConditionComboItem;
                if (item == null || item.Type == FilterConditionType.None) return;
                condition.Type = item.Type;
                condition.SetDefaultValue();
                UpdateValueInput(valuePanel, condition);
                UpdateFilterSummary();
            };

            return panel;
        }

        private void UpdateValueInput(StackPanel panel, FilterCondition condition)
        {
            panel.Children.Clear();
            var valueType = condition.ValueType;

            switch (valueType)
            {
                case FilterValueType.Text:
                    var txt = new TextBox { Text = condition.Value?.ToString() ?? "", MinWidth = 200 };
                    txt.TextChanged += (s, e) => condition.Value = txt.Text;
                    panel.Children.Add(txt);
                    break;

                case FilterValueType.Number:
                    var num = new NumericUpDown { Value = (decimal)Convert.ToDouble(condition.Value ?? 0), MinWidth = 100 };
                    num.ValueChanged += (s, e) => condition.Value = (double)(num.Value ?? 0);
                    panel.Children.Add(num);
                    break;

                case FilterValueType.Boolean:
                    var boolCmb = new ComboBox { ItemsSource = new[] { "不限", "是", "否" }, SelectedIndex = condition.Value == null ? 0 : (Convert.ToBoolean(condition.Value) ? 1 : 2), MinWidth = 80 };
                    boolCmb.SelectionChanged += (s, e) => condition.Value = boolCmb.SelectedIndex == 0 ? null : (object)(boolCmb.SelectedIndex == 1);
                    panel.Children.Add(boolCmb);
                    break;

                case FilterValueType.Selection:
                    var options = condition.Options ?? new List<string>();
                    var selCmb = new ComboBox { ItemsSource = options, MinWidth = 120 };
                    var currentVal = condition.Value?.ToString();
                    if (currentVal != null && options.Contains(currentVal))
                        selCmb.SelectedIndex = options.IndexOf(currentVal);
                    selCmb.SelectionChanged += (s, e) => condition.Value = selCmb.SelectedIndex >= 0 ? options[selCmb.SelectedIndex] : "";
                    panel.Children.Add(selCmb);
                    break;

                case FilterValueType.Range:
                    var rangeVal = condition.Value as RangeValue ?? new RangeValue();
                    var minNum = new NumericUpDown { Value = (decimal)(double.IsNaN(rangeVal.MinRaw) ? 0 : rangeVal.MinRaw), MinWidth = 80 };
                    var maxNum = new NumericUpDown { Value = (decimal)(double.IsNaN(rangeVal.MaxRaw) ? 0 : rangeVal.MaxRaw), MinWidth = 80 };
                    minNum.ValueChanged += (s, e) => { var r = condition.Value as RangeValue ?? new RangeValue(); r.MinRaw = (double)(minNum.Value ?? 0); condition.Value = r; };
                    maxNum.ValueChanged += (s, e) => { var r = condition.Value as RangeValue ?? new RangeValue(); r.MaxRaw = (double)(maxNum.Value ?? 0); condition.Value = r; };
                    panel.Children.Add(minNum);
                    panel.Children.Add(new TextBlock { Text = "~", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0) });
                    panel.Children.Add(maxNum);
                    break;

                case FilterValueType.SearchQuery:
                    var queryVal = condition.Value as SearchQueryValue ?? new SearchQueryValue();
                    var queryTxt = new TextBox { Text = queryVal.Query, MinWidth = 150 };
                    var fieldCmb = new ComboBox { ItemsSource = new[] { "全部", "歌名", "艺术家", "谱师", "标题", "简介", "上传者" }, SelectedIndex = 0, MinWidth = 80 };
                    queryTxt.TextChanged += (s, e) => { var q = condition.Value as SearchQueryValue ?? new SearchQueryValue(); q.Query = queryTxt.Text; condition.Value = q; };
                    fieldCmb.SelectionChanged += (s, e) =>
                    {
                        var q = condition.Value as SearchQueryValue ?? new SearchQueryValue();
                        q.FieldTypes = fieldCmb.SelectedIndex switch
                        {
                            1 => SearchFieldType.SongName, 2 => SearchFieldType.Artist, 3 => SearchFieldType.Mapper,
                            4 => SearchFieldType.MapName, 5 => SearchFieldType.Description, 6 => SearchFieldType.Uploader,
                            _ => SearchFieldType.All
                        };
                        condition.Value = q;
                    };
                    panel.Children.Add(queryTxt);
                    panel.Children.Add(fieldCmb);
                    break;

                case FilterValueType.Date:
                    var dateStr = condition.Value is DateTime dt ? dt.ToString("yyyy-MM-dd") : "";
                    var dateTxt = new TextBox { Text = dateStr, MinWidth = 120 };
                    dateTxt.TextChanged += (s, e) => { if (DateTime.TryParse(dateTxt.Text, out var parsed)) condition.Value = parsed; };
                    panel.Children.Add(dateTxt);
                    break;

                case FilterValueType.NumberWithSort:
                    var limitVal = condition.Value as ResultLimitValue ?? new ResultLimitValue(100);
                    var limitNum = new NumericUpDown { Value = limitVal.Count, MinWidth = 80 };
                    var sortCmb = new ComboBox { ItemsSource = new[] { "最新上传", "最早上传", "随机" }, SelectedIndex = (int)limitVal.SortOption, MinWidth = 90 };
                    limitNum.ValueChanged += (s, e) => { var r = condition.Value as ResultLimitValue ?? new ResultLimitValue(); r.Count = (int)(limitNum.Value ?? 100); condition.Value = r; };
                    sortCmb.SelectionChanged += (s, e) => { var r = condition.Value as ResultLimitValue ?? new ResultLimitValue(); r.SortOption = (ResultSortOption)sortCmb.SelectedIndex; condition.Value = r; };
                    panel.Children.Add(limitNum);
                    panel.Children.Add(sortCmb);
                    break;

                case FilterValueType.ExcludeMod:
                    var excludeVal = condition.Value as ExcludeModValue ?? new ExcludeModValue();
                    var modTxt = new TextBox { Text = excludeVal.ModName, MinWidth = 120 };
                    var strictChk = new CheckBox { Content = "严格", IsChecked = excludeVal.Strict };
                    modTxt.TextChanged += (s, e) => { var v = condition.Value as ExcludeModValue ?? new ExcludeModValue(); v.ModName = modTxt.Text; condition.Value = v; };
                    strictChk.IsCheckedChanged += (s, e) => { var v = condition.Value as ExcludeModValue ?? new ExcludeModValue(); v.Strict = strictChk.IsChecked ?? false; condition.Value = v; };
                    panel.Children.Add(modTxt);
                    panel.Children.Add(strictChk);
                    break;
            }
        }

        // === Preset Management ===

        private void OnNewPresetClick(object? sender, RoutedEventArgs e)
        {
            CurrentPreset = new FilterPreset("新预设");
            var g = new FilterGroup("条件组1");
            g.AddCondition(new FilterCondition(FilterConditionType.Query));
            CurrentPreset.AddGroup(g);
            _cboPreset.SelectedIndex = -1;
            _txtPresetName.Text = "新预设";
            _txtPresetName.SelectAll();
            _txtPresetName.Focus();
            RebuildUI();
        }

        private void OnSavePresetClick(object? sender, RoutedEventArgs e)
        {
            if (CurrentPreset == null) return;
            CurrentPreset.Name = string.IsNullOrWhiteSpace(_txtPresetName.Text) ? "未命名预设" : _txtPresetName.Text.Trim();
            if (!Directory.Exists(_presetsDir)) Directory.CreateDirectory(_presetsDir);

            var existing = _savedPresets.FirstOrDefault(p => p.Name == CurrentPreset.Name);
            if (existing != null) _savedPresets.Remove(existing);
            _savedPresets.Add(CurrentPreset);
            CurrentPreset.SaveToFile(Path.Combine(_presetsDir, CurrentPreset.Name + ".bsf"));
            UpdatePresetCombo();
        }

        private void OnDeletePresetClick(object? sender, RoutedEventArgs e)
        {
            if (_cboPreset.SelectedIndex < 0 || _cboPreset.SelectedIndex >= _savedPresets.Count) return;
            var preset = _savedPresets[_cboPreset.SelectedIndex];
            var path = Path.Combine(_presetsDir, preset.Name + ".bsf");
            if (File.Exists(path)) File.Delete(path);
            _savedPresets.RemoveAt(_cboPreset.SelectedIndex);
            UpdatePresetCombo();
        }

        private async void OnImportPresetClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;
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

            if (!Directory.Exists(_presetsDir)) Directory.CreateDirectory(_presetsDir);
            foreach (var f in files)
            {
                string path = f.Path.LocalPath;
                var preset = FilterPreset.LoadFromFile(path);
                if (preset == null) continue;

                string name = preset.Name;
                int n = 2;
                while (_savedPresets.Any(p => p.Name == name)) { name = $"{preset.Name} ({n++})"; }
                preset.Name = name;
                preset.SaveToFile(Path.Combine(_presetsDir, name + ".bsf"));
                _savedPresets.Add(preset);
            }
            UpdatePresetCombo();
        }

        private async void OnExportPresetClick(object? sender, RoutedEventArgs e)
        {
            if (CurrentPreset == null) return;
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "导出筛选预设",
                DefaultExtension = "bsf",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("筛选预设文件 (*.bsf)") { Patterns = new[] { "*.bsf" } },
                    new FilePickerFileType("JSON文件 (*.json)") { Patterns = new[] { "*.json" } }
                }
            });
            if (file != null) CurrentPreset.SaveToFile(file.Path.LocalPath);
        }

        private void OnAddGroupClick(object? sender, RoutedEventArgs e)
        {
            if (CurrentPreset == null) return;
            var group = new FilterGroup($"条件组 {CurrentPreset.Groups.Count + 1}");
            group.AddCondition(new FilterCondition(FilterConditionType.Query));
            CurrentPreset.AddGroup(group);
            RebuildUI();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();

        private void OnSearchClick(object? sender, RoutedEventArgs e)
        {
            if (CurrentPreset == null) return;
            // Convert BSIMM FilterPreset → BeatSpiderSharp Preset
            var bssPreset = BsfToPresetConverter.Convert(CurrentPreset);
            SearchRequested?.Invoke(this, bssPreset);
            Close();
        }
    }
}
