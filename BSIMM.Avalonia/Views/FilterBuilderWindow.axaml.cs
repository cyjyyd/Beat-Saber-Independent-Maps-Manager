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
using System.Linq;

namespace BSIMM.Avalonia.Views
{
    public partial class FilterBuilderWindow : Window
    {
        private StackPanel _groupsPanel = null!;
        private Border _emptyPlaceholder = null!;
        public FilterPreset? CurrentPreset { get; private set; }
        public event EventHandler<FilterPreset>? SearchRequested;

        public FilterBuilderWindow() : this(null) { }

        public FilterBuilderWindow(FilterPreset? preset)
        {
            AvaloniaXamlLoader.Load(this);
            _groupsPanel = this.FindControl<StackPanel>("GroupsPanel")!;
            _emptyPlaceholder = this.FindControl<Border>("EmptyPlaceholder")!;
            CurrentPreset = preset ?? new FilterPreset("新预设");
            RebuildUI();
        }

        private IBrush? GetBrush(string key)
        {
            if (global::Avalonia.Application.Current?.TryFindResource(key, out var value) == true)
                return value as IBrush;
            return null;
        }

        private void RebuildUI()
        {
            _groupsPanel.Children.Clear();

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

            // === Group Header ===
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            var chkEnabled = new CheckBox { Content = "启用", IsChecked = group.IsEnabled, VerticalAlignment = VerticalAlignment.Center };
            chkEnabled.IsCheckedChanged += (s, e) => group.IsEnabled = chkEnabled.IsChecked ?? true;

            var lblName = new TextBlock
            {
                Text = string.IsNullOrEmpty(group.Name) ? "条件组" : group.Name,
                FontWeight = FontWeight.Bold,
                FontSize = 15,
                Foreground = GetBrush("HighlightBlueBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };

            var cmbOperator = new ComboBox
            {
                ItemsSource = new[] { "AND (全部满足)", "OR (满足任一)" },
                SelectedIndex = group.GroupOperator == LogicOperator.And ? 0 : 1,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 140
            };
            cmbOperator.SelectionChanged += (s, e) => group.GroupOperator = cmbOperator.SelectedIndex == 0 ? LogicOperator.And : LogicOperator.Or;

            var chkCache = new CheckBox { Content = "本地缓存", IsChecked = group.UseLocalCache, VerticalAlignment = VerticalAlignment.Center };
            chkCache.IsCheckedChanged += (s, e) => group.UseLocalCache = chkCache.IsChecked ?? false;

            var btnRemoveGroup = new Button { Content = "删除组", VerticalAlignment = VerticalAlignment.Center };
            btnRemoveGroup.Click += (s, e) => { CurrentPreset?.Groups.Remove(group); RebuildUI(); };

            headerPanel.Children.Add(chkEnabled);
            headerPanel.Children.Add(lblName);
            headerPanel.Children.Add(cmbOperator);
            headerPanel.Children.Add(chkCache);
            headerPanel.Children.Add(btnRemoveGroup);
            outerStack.Children.Add(headerPanel);

            // === Separator ===
            var sep = new Border { Height = 1, Background = GetBrush("BorderBrush"), Margin = new Thickness(0, 2, 0, 2) };
            outerStack.Children.Add(sep);

            // === Conditions List ===
            var conditionsPanel = new StackPanel { Spacing = 6, Margin = new Thickness(0, 0, 0, 0) };
            foreach (var condition in group.Conditions)
            {
                conditionsPanel.Children.Add(CreateConditionControl(condition, group));
            }
            outerStack.Children.Add(conditionsPanel);

            // === Add Condition Button ===
            var btnAddCondition = new Button
            {
                Content = "+ 添加条件",
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Classes = { "bs-blue" }
            };
            btnAddCondition.Click += (s, e) =>
            {
                var newCondition = new FilterCondition(FilterConditionType.Query);
                group.Conditions.Add(newCondition);
                conditionsPanel.Children.Add(CreateConditionControl(newCondition, group));
            };
            outerStack.Children.Add(btnAddCondition);

            groupBorder.Child = outerStack;
            return groupBorder;
        }

        private StackPanel CreateConditionControl(FilterCondition condition, FilterGroup group)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Tag = condition, Margin = new Thickness(0, 2, 0, 2) };

            // Enable checkbox
            var chkEnabled = new CheckBox { IsChecked = condition.IsEnabled, VerticalAlignment = VerticalAlignment.Center };
            chkEnabled.IsCheckedChanged += (s, e) => condition.IsEnabled = chkEnabled.IsChecked ?? true;
            panel.Children.Add(chkEnabled);

            // Condition type dropdown - build a flat list with category labels
            var groupedConditions = FilterConditionMetadata.GetGroupedConditions();
            var typeItems = new List<string>();
            var typeMap = new List<FilterConditionType>();
            foreach (var category in groupedConditions)
            {
                typeItems.Add($"── {category.Key} ──");
                typeMap.Add(FilterConditionType.None); // separator marker
                foreach (var type in category.Value)
                {
                    typeItems.Add("  " + FilterConditionMetadata.GetDisplayName(type));
                    typeMap.Add(type);
                }
            }
            var cmbType = new ComboBox { ItemsSource = typeItems, MinWidth = 180, VerticalAlignment = VerticalAlignment.Center };
            var currentDisplayName = "  " + FilterConditionMetadata.GetDisplayName(condition.Type);
            cmbType.SelectedIndex = typeItems.IndexOf(currentDisplayName);
            if (cmbType.SelectedIndex < 0) cmbType.SelectedIndex = typeItems.IndexOf("  搜索关键词");
            panel.Children.Add(cmbType);

            // Value input (dynamic based on type)
            var valuePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, VerticalAlignment = VerticalAlignment.Center };
            UpdateValueInput(valuePanel, condition);
            panel.Children.Add(valuePanel);

            // Operator dropdown
            var cmbOperator = new ComboBox { ItemsSource = new[] { "AND", "OR" }, SelectedIndex = condition.Operator == LogicOperator.And ? 0 : 1, VerticalAlignment = VerticalAlignment.Center, MinWidth = 60 };
            cmbOperator.SelectionChanged += (s, e) => condition.Operator = cmbOperator.SelectedIndex == 0 ? LogicOperator.And : LogicOperator.Or;
            panel.Children.Add(cmbOperator);

            // Remove button
            var btnRemove = new Button { Content = "✕", VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(10, 4), FontSize = 12 };
            btnRemove.Click += (s, e) => { group.Conditions.Remove(condition); RebuildUI(); };
            panel.Children.Add(btnRemove);

            // Handle type change
            cmbType.SelectionChanged += (s, e) =>
            {
                if (cmbType.SelectedIndex < 0) return;
                var selectedType = typeMap[cmbType.SelectedIndex];
                if (selectedType == FilterConditionType.None) return; // category separator
                condition.Type = selectedType;
                condition.SetDefaultValue();
                UpdateValueInput(valuePanel, condition);
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
                    var minNum = new NumericUpDown { Value = (decimal?)(rangeVal.Min ?? 0), MinWidth = 80, PlaceholderText = "最小" };
                    var maxNum = new NumericUpDown { Value = (decimal?)(rangeVal.Max ?? 0), MinWidth = 80, PlaceholderText = "最大" };
                    minNum.ValueChanged += (s, e) => { var r = condition.Value as RangeValue ?? new RangeValue(); r.Min = (double?)(minNum.Value); condition.Value = r; };
                    maxNum.ValueChanged += (s, e) => { var r = condition.Value as RangeValue ?? new RangeValue(); r.Max = (double?)(maxNum.Value); condition.Value = r; };
                    panel.Children.Add(minNum);
                    panel.Children.Add(new TextBlock { Text = "~", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0) });
                    panel.Children.Add(maxNum);
                    break;

                case FilterValueType.SearchQuery:
                    var queryVal = condition.Value as SearchQueryValue ?? new SearchQueryValue();
                    var queryTxt = new TextBox { Text = queryVal.Query, MinWidth = 150, PlaceholderText = "搜索关键词" };
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
                    var dateTxt = new TextBox { Text = dateStr, MinWidth = 120, PlaceholderText = "yyyy-MM-dd" };
                    dateTxt.TextChanged += (s, e) => { if (DateTime.TryParse(dateTxt.Text, out var parsed)) condition.Value = parsed; };
                    panel.Children.Add(dateTxt);
                    break;

                case FilterValueType.NumberWithSort:
                    var limitVal = condition.Value as ResultLimitValue ?? new ResultLimitValue(100);
                    var limitNum = new NumericUpDown { Value = limitVal.Count, MinWidth = 80, PlaceholderText = "数量" };
                    var sortCmb = new ComboBox { ItemsSource = new[] { "最新上传", "最早上传", "随机" }, SelectedIndex = (int)limitVal.SortOption, MinWidth = 90 };
                    limitNum.ValueChanged += (s, e) => { var r = condition.Value as ResultLimitValue ?? new ResultLimitValue(); r.Count = (int)(limitNum.Value ?? 100); condition.Value = r; };
                    sortCmb.SelectionChanged += (s, e) => { var r = condition.Value as ResultLimitValue ?? new ResultLimitValue(); r.SortOption = (ResultSortOption)sortCmb.SelectedIndex; condition.Value = r; };
                    panel.Children.Add(limitNum);
                    panel.Children.Add(sortCmb);
                    break;

                case FilterValueType.ExcludeMod:
                    var excludeVal = condition.Value as ExcludeModValue ?? new ExcludeModValue();
                    var modTxt = new TextBox { Text = excludeVal.ModName, MinWidth = 120, PlaceholderText = "Mod名称" };
                    var strictChk = new CheckBox { Content = "严格", IsChecked = excludeVal.Strict };
                    modTxt.TextChanged += (s, e) => { var v = condition.Value as ExcludeModValue ?? new ExcludeModValue(); v.ModName = modTxt.Text; condition.Value = v; };
                    strictChk.IsCheckedChanged += (s, e) => { var v = condition.Value as ExcludeModValue ?? new ExcludeModValue(); v.Strict = strictChk.IsChecked ?? false; condition.Value = v; };
                    panel.Children.Add(modTxt);
                    panel.Children.Add(strictChk);
                    break;
            }
        }

        private void OnNewPresetClick(object? sender, RoutedEventArgs e)
        {
            CurrentPreset = new FilterPreset("新预设");
            RebuildUI();
        }

        private async void OnLoadPresetClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "加载筛选预设",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("筛选预设文件 (*.bsf)") { Patterns = new[] { "*.bsf" } },
                    new FilePickerFileType("JSON文件 (*.json)") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("所有文件") { Patterns = new[] { "*.*" } }
                }
            });
            if (files != null && files.Count > 0)
            {
                string path = files[0].Path.LocalPath;
                var preset = FilterPreset.LoadFromFile(path);
                if (preset != null)
                {
                    CurrentPreset = preset;
                    RebuildUI();
                }
            }
        }

        private async void OnSavePresetClick(object? sender, RoutedEventArgs e)
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null) return;
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "保存筛选预设",
                DefaultExtension = "bsf",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("筛选预设文件 (*.bsf)") { Patterns = new[] { "*.bsf" } },
                    new FilePickerFileType("JSON文件 (*.json)") { Patterns = new[] { "*.json" } }
                }
            });
            if (file != null && CurrentPreset != null)
            {
                string path = file.Path.LocalPath;
                CurrentPreset.SaveToFile(path);
            }
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
            if (CurrentPreset != null)
                SearchRequested?.Invoke(this, CurrentPreset);
            Close();
        }
    }
}
