using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// Main panel for building filter conditions with groups and presets
    /// </summary>
    public class FilterBuilderPanel : UserControl
    {
        // Preset controls
        private Panel presetPanel;
        private ComboBox cboPreset;
        private Button btnSavePreset;
        private Button btnNewPreset;
        private Button btnDeletePreset;
        private Button btnImportPreset;
        private Button btnExportPreset;

        // Filter builder controls
        private Panel groupsPanel;
        private Button btnAddGroup;
        private Panel buttonsPanel;
        private Button btnReset;
        private Label lblResultCount;

        // Data
        private List<FilterPreset> presets = new List<FilterPreset>();
        private List<FilterGroupControl> groupControls = new List<FilterGroupControl>();
        private FilterPreset currentPreset;

        public event EventHandler<FilterPreset> SearchRequested;
        public event EventHandler ResetRequested;

        public FilterPreset CurrentPreset => currentPreset;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ResultCount
        {
            set
            {
                lblResultCount.Text = $"结果: {value} 首";
            }
        }

        public FilterBuilderPanel()
        {
            InitializeUI();
            LoadPresets();
            CreateDefaultPreset();
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(5);

            // Preset panel
            presetPanel = new Panel
            {
                Height = 45,
                Dock = DockStyle.Top,
                Padding = new Padding(2)
            };

            Label lblPreset = new Label
            {
                Text = "预设：",
                Width = 50,
                Dock = DockStyle.Left,
                Margin = new Padding(5, 8, 0, 0)
            };

            cboPreset = new ComboBox
            {
                Width = 200,
                Dock = DockStyle.Left,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(2)
            };
            cboPreset.SelectedIndexChanged += (s, e) =>
            {
                if (cboPreset.SelectedItem is FilterPreset preset)
                {
                    LoadPreset(preset);
                }
            };

            btnSavePreset = new Button
            {
                Text = "保存",
                Width = 60,
                Dock = DockStyle.Left,
                Margin = new Padding(2)
            };
            btnSavePreset.Click += (s, e) => SaveCurrentPreset();

            btnNewPreset = new Button
            {
                Text = "新建",
                Width = 60,
                Dock = DockStyle.Left,
                Margin = new Padding(2)
            };
            btnNewPreset.Click += (s, e) => CreateNewPreset();

            btnDeletePreset = new Button
            {
                Text = "删除",
                Width = 60,
                Dock = DockStyle.Left,
                Margin = new Padding(2)
            };
            btnDeletePreset.Click += (s, e) => DeleteSelectedPreset();

            btnImportPreset = new Button
            {
                Text = "导入",
                Width = 60,
                Dock = DockStyle.Left,
                Margin = new Padding(2)
            };
            btnImportPreset.Click += (s, e) => ImportPreset();

            btnExportPreset = new Button
            {
                Text = "导出",
                Width = 60,
                Dock = DockStyle.Left,
                Margin = new Padding(2)
            };
            btnExportPreset.Click += (s, e) => ExportCurrentPreset();

            presetPanel.Controls.Add(btnExportPreset);
            presetPanel.Controls.Add(btnImportPreset);
            presetPanel.Controls.Add(btnDeletePreset);
            presetPanel.Controls.Add(btnNewPreset);
            presetPanel.Controls.Add(btnSavePreset);
            presetPanel.Controls.Add(cboPreset);
            presetPanel.Controls.Add(lblPreset);

            // Groups panel (scrollable)
            groupsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = SystemColors.Control
            };

            // Buttons panel
            buttonsPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom,
                Padding = new Padding(5)
            };

            btnAddGroup = new Button
            {
                Text = "+ 添加条件组",
                Width = 120,
                Dock = DockStyle.Left,
                Margin = new Padding(2)
            };
            btnAddGroup.Click += (s, e) => AddGroup();

            btnReset = new Button
            {
                Text = "重置",
                Width = 80,
                Dock = DockStyle.Right,
                Margin = new Padding(2)
            };
            btnReset.Click += (s, e) => OnResetRequested();

            lblResultCount = new Label
            {
                Text = "结果: 0 首",
                Width = 100,
                Dock = DockStyle.Right,
                Margin = new Padding(10, 8, 10, 0)
            };

            buttonsPanel.Controls.Add(lblResultCount);
            buttonsPanel.Controls.Add(btnReset);
            buttonsPanel.Controls.Add(btnAddGroup);

            // Add main controls
            this.Controls.Add(groupsPanel);
            this.Controls.Add(buttonsPanel);
            this.Controls.Add(presetPanel);
        }

        private void CreateDefaultPreset()
        {
            currentPreset = new FilterPreset("新建筛选");
            currentPreset.AddGroup(new FilterGroup("条件组1"));
            UpdateGroupControls();
            UpdatePresetCombo();
        }

        private void AddGroup()
        {
            if (currentPreset == null)
                currentPreset = new FilterPreset("新建筛选");

            int groupNum = currentPreset.Groups.Count + 1;
            var newGroup = new FilterGroup($"条件组{groupNum}");
            currentPreset.AddGroup(newGroup);

            AddGroupControl(newGroup);
        }

        private void AddGroupControl(FilterGroup group)
        {
            var ctrl = new FilterGroupControl(group);
            ctrl.GroupChanged += OnGroupChanged;
            ctrl.RemoveRequested += OnRemoveGroup;
            groupControls.Add(ctrl);
            groupsPanel.Controls.Add(ctrl);
            UpdateGroupOperatorVisibility();
        }

        private void RemoveGroupControl(FilterGroupControl control)
        {
            if (currentPreset != null)
            {
                currentPreset.RemoveGroup(control.Group);
            }
            control.GroupChanged -= OnGroupChanged;
            control.RemoveRequested -= OnRemoveGroup;
            groupControls.Remove(control);
            groupsPanel.Controls.Remove(control);
            control.Dispose();
            UpdateGroupOperatorVisibility();
        }

        private void OnGroupChanged(object sender, FilterGroup group)
        {
            // Group changed, could trigger auto-search if desired
        }

        private void OnRemoveGroup(object sender, EventArgs e)
        {
            if (sender is FilterGroupControl ctrl)
            {
                RemoveGroupControl(ctrl);
            }
        }

        private void UpdateGroupControls()
        {
            // Clear existing controls
            foreach (var ctrl in groupControls)
            {
                ctrl.GroupChanged -= OnGroupChanged;
                ctrl.RemoveRequested -= OnRemoveGroup;
                groupsPanel.Controls.Remove(ctrl);
                ctrl.Dispose();
            }
            groupControls.Clear();

            // Add controls for each group
            if (currentPreset != null)
            {
                foreach (var group in currentPreset.Groups)
                {
                    AddGroupControl(group);
                }
            }

            UpdateGroupOperatorVisibility();
        }

        private void UpdateGroupOperatorVisibility()
        {
            for (int i = 0; i < groupControls.Count; i++)
            {
                groupControls[i].SetGroupOperatorVisibility(i < groupControls.Count - 1);
            }
        }

        private void OnSearchRequested()
        {
            if (currentPreset != null)
            {
                SearchRequested?.Invoke(this, currentPreset);
            }
        }

        private void OnResetRequested()
        {
            CreateDefaultPreset();
            ResetRequested?.Invoke(this, EventArgs.Empty);
        }

        #region Preset Management

        private void LoadPresets()
        {
            presets.Clear();
            string presetDir = Path.Combine(Application.StartupPath, "presets");
            if (!Directory.Exists(presetDir))
            {
                Directory.CreateDirectory(presetDir);
            }

            foreach (var file in Directory.GetFiles(presetDir, "*.bsf"))
            {
                var preset = FilterPreset.LoadFromFile(file);
                if (preset != null)
                {
                    presets.Add(preset);
                }
            }
        }

        private void UpdatePresetCombo()
        {
            cboPreset.Items.Clear();
            cboPreset.Items.Add(currentPreset);
            foreach (var preset in presets)
            {
                cboPreset.Items.Add(preset);
            }
            if (cboPreset.Items.Count > 0)
                cboPreset.SelectedIndex = 0;
        }

        private void LoadPreset(FilterPreset preset)
        {
            currentPreset = preset.Clone();
            UpdateGroupControls();
        }

        private void SaveCurrentPreset()
        {
            if (currentPreset == null) return;

            // Ask for name
            string name = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入预设名称：",
                "保存预设",
                currentPreset.Name,
                -1, -1);

            if (string.IsNullOrWhiteSpace(name)) return;

            currentPreset.Name = name;
            string presetDir = Path.Combine(Application.StartupPath, "presets");
            if (!Directory.Exists(presetDir))
                Directory.CreateDirectory(presetDir);

            string filePath = Path.Combine(presetDir, $"{name}.bsf");
            currentPreset.SaveToFile(filePath);

            // Update presets list
            var existing = presets.FirstOrDefault(p => p.Name == name);
            if (existing != null)
                presets.Remove(existing);
            presets.Add(currentPreset.Clone());

            UpdatePresetCombo();
            MessageBox.Show("预设保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CreateNewPreset()
        {
            currentPreset = new FilterPreset("新建筛选");
            currentPreset.AddGroup(new FilterGroup("条件组1"));
            UpdateGroupControls();
            UpdatePresetCombo();
        }

        private void DeleteSelectedPreset()
        {
            if (!(cboPreset.SelectedItem is FilterPreset selectedPreset))
                return;

            // 查找 presets 列表中对应的预设
            var presetToDelete = presets.FirstOrDefault(p => p.Name == selectedPreset.Name);
            if (presetToDelete == null)
            {
                MessageBox.Show("无法删除当前正在编辑的预设。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"确定删除预设 '{presetToDelete.Name}' 吗？", "确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // 删除文件
                string presetDir = Path.Combine(Application.StartupPath, "presets");
                string filePath = Path.Combine(presetDir, $"{presetToDelete.Name}.bsf");
                if (File.Exists(filePath))
                    File.Delete(filePath);

                // 从列表中移除
                presets.Remove(presetToDelete);

                // 如果当前编辑的就是被删除的预设，创建新预设
                if (currentPreset.Name == presetToDelete.Name)
                {
                    CreateDefaultPreset();
                }
                else
                {
                    UpdatePresetCombo();
                }

                MessageBox.Show("预设已删除。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ImportPreset()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "导入筛选预设（支持多选）";
                ofd.Filter = "筛选预设文件 (*.bsf)|*.bsf|JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*";
                ofd.DefaultExt = ".bsf";
                ofd.RestoreDirectory = true;
                ofd.Multiselect = true;  // 启用多选

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string presetDir = Path.Combine(Application.StartupPath, "presets");
                    if (!Directory.Exists(presetDir))
                        Directory.CreateDirectory(presetDir);

                    int successCount = 0;
                    int failCount = 0;
                    List<string> failedFiles = new List<string>();
                    FilterPreset lastImportedPreset = null;

                    foreach (string filePath in ofd.FileNames)
                    {
                        try
                        {
                            var preset = FilterPreset.LoadFromFile(filePath);
                            if (preset != null && preset.Groups != null)
                            {
                                // 检查是否已存在同名预设
                                string presetName = preset.Name;
                                int counter = 1;
                                while (presets.Any(p => p.Name == presetName))
                                {
                                    presetName = $"{preset.Name} ({counter})";
                                    counter++;
                                }
                                preset.Name = presetName;

                                // 保存到本地预设目录
                                string destPath = Path.Combine(presetDir, $"{presetName}.bsf");
                                preset.SaveToFile(destPath);

                                // 添加到列表
                                var clonedPreset = preset.Clone();
                                presets.Add(clonedPreset);
                                cboPreset.Items.Add(clonedPreset);

                                lastImportedPreset = preset;
                                successCount++;
                            }
                            else
                            {
                                failCount++;
                                failedFiles.Add(Path.GetFileName(filePath));
                            }
                        }
                        catch (Exception ex)
                        {
                            failCount++;
                            failedFiles.Add($"{Path.GetFileName(filePath)} ({ex.Message})");
                        }
                    }

                    // 选中最后导入的预设
                    if (lastImportedPreset != null)
                    {
                        currentPreset = lastImportedPreset.Clone();
                        cboPreset.SelectedItem = presets.LastOrDefault(p => p.Name == lastImportedPreset.Name);
                        UpdateGroupControls();
                    }

                    // 显示结果
                    string message = $"成功导入 {successCount} 个预设";
                    if (failCount > 0)
                    {
                        message += $"\n失败 {failCount} 个";
                        if (failedFiles.Count <= 5)
                        {
                            message += ":\n" + string.Join("\n", failedFiles);
                        }
                    }
                    MessageBox.Show(message, "导入完成",
                        MessageBoxButtons.OK, successCount > 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                }
            }
        }

        private void ExportCurrentPreset()
        {
            if (presets.Count == 0 && currentPreset == null)
            {
                MessageBox.Show("没有可导出的预设。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 如果只有一个预设或当前预设不为空，直接导出
            if (currentPreset != null && presets.Count == 1)
            {
                ExportSinglePreset(currentPreset);
                return;
            }

            // 多个预设时显示选择菜单
            var menu = new ContextMenuStrip();
            menu.Items.Add("导出当前预设", null, (s, e) => ExportSinglePreset(currentPreset));
            menu.Items.Add("批量导出全部...", null, (s, e) => ExportAllPresets());
            menu.Show(btnExportPreset, 0, btnExportPreset.Height);
        }

        private void ExportSinglePreset(FilterPreset preset)
        {
            if (preset == null)
            {
                MessageBox.Show("没有可导出的预设。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string fileName = preset.Name;
            string filter = "筛选预设文件 (*.bsf)|*.bsf|JSON文件 (*.json)|*.json";

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "导出筛选预设";
                sfd.Filter = filter;
                sfd.DefaultExt = ".bsf";
                sfd.FileName = fileName;
                sfd.RestoreDirectory = true;
                sfd.OverwritePrompt = true;

                if (sfd.ShowDialog(this.FindForm()) == DialogResult.OK)
                {
                    try
                    {
                        // 异步执行导出操作避免UI卡顿
                        var filePath = sfd.FileName;
                        var exportPreset = preset.Clone();

                        System.Threading.Tasks.Task.Run(() =>
                        {
                            try
                            {
                                exportPreset.SaveToFile(filePath);
                                if (!this.IsDisposed && this.IsHandleCreated)
                                {
                                    this.BeginInvoke(new Action(() =>
                                    {
                                        if (!this.IsDisposed)
                                        {
                                            MessageBox.Show($"预设已导出到：{filePath}", "成功",
                                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }
                                    }));
                                }
                            }
                            catch (Exception ex)
                            {
                                if (!this.IsDisposed && this.IsHandleCreated)
                                {
                                    this.BeginInvoke(new Action(() =>
                                    {
                                        if (!this.IsDisposed)
                                        {
                                            MessageBox.Show($"导出预设失败：{ex.Message}", "错误",
                                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }
                                    }));
                                }
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导出预设失败：{ex.Message}", "错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportAllPresets()
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "选择导出目录";
                fbd.ShowNewFolderButton = true;

                if (fbd.ShowDialog(this.FindForm()) == DialogResult.OK)
                {
                    var selectedPath = fbd.SelectedPath;
                    var presetsCopy = presets.ToList();

                    // 异步执行导出操作
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        int successCount = 0;
                        int failCount = 0;

                        foreach (var preset in presetsCopy)
                        {
                            try
                            {
                                string destPath = Path.Combine(selectedPath, $"{preset.Name}.bsf");
                                preset.Clone().SaveToFile(destPath);
                                successCount++;
                            }
                            catch
                            {
                                failCount++;
                            }
                        }

                        if (!this.IsDisposed && this.IsHandleCreated)
                        {
                            this.BeginInvoke(new Action(() =>
                            {
                                if (!this.IsDisposed)
                                {
                                    string message = $"成功导出 {successCount} 个预设到：\n{selectedPath}";
                                    if (failCount > 0)
                                        message += $"\n失败 {failCount} 个";

                                    MessageBox.Show(message, "导出完成",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }));
                        }
                    });
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets all active filter groups
        /// </summary>
        public List<FilterGroup> GetActiveGroups()
        {
            return currentPreset?.GetActiveGroups() ?? new List<FilterGroup>();
        }

        /// <summary>
        /// Checks if the current preset has any OR logic
        /// </summary>
        public bool HasOrLogic()
        {
            if (currentPreset == null) return false;

            foreach (var group in currentPreset.GetActiveGroups())
            {
                // Check for OR between groups
                if (group.GroupOperator == LogicOperator.Or)
                    return true;

                // Check for OR within conditions
                foreach (var condition in group.GetActiveConditions())
                {
                    if (condition.Operator == LogicOperator.Or)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Converts the current filter configuration to BeatSaverSearchFilter
        /// Note: This method only handles AND logic. For OR logic, use MainForm's BuildSearchFiltersWithOrLogic instead.
        /// </summary>
        public BeatSaverSearchFilter ToSearchFilter()
        {
            var filter = new BeatSaverSearchFilter();

            if (currentPreset == null) return filter;

            // Collect all conditions from all groups
            // Note: This ignores OR operators - for OR logic support,
            // use MainForm.BuildSearchFiltersWithOrLogic instead
            foreach (var group in currentPreset.GetActiveGroups())
            {
                foreach (var condition in group.GetActiveConditions())
                {
                    ApplyConditionToFilter(filter, condition);
                }
            }

            return filter;
        }

        private void ApplyConditionToFilter(BeatSaverSearchFilter filter, FilterCondition condition)
        {
            if (condition.Value == null) return;

            // Handle range-type conditions for API
            if (FilterConditionMetadata.IsRangeType(condition.Type))
            {
                ApplyRangeConditionToFilter(filter, condition);
                return;
            }

            switch (condition.Type)
            {
                case FilterConditionType.Query:
                    // Handle SearchQueryValue with field type
                    if (condition.Value is SearchQueryValue queryValue)
                        filter.Query = queryValue.ToApiQuery();
                    else
                        filter.Query = condition.Value.ToString();
                    break;
                case FilterConditionType.Order:
                    filter.Order = condition.Value.ToString();
                    break;
                case FilterConditionType.MinBpm:
                    if (double.TryParse(condition.Value.ToString(), out double minBpm))
                        filter.MinBpm = minBpm;
                    break;
                case FilterConditionType.MaxBpm:
                    if (double.TryParse(condition.Value.ToString(), out double maxBpm))
                        filter.MaxBpm = maxBpm;
                    break;
                case FilterConditionType.MinNps:
                    if (double.TryParse(condition.Value.ToString(), out double minNps))
                        filter.MinNps = minNps;
                    break;
                case FilterConditionType.MaxNps:
                    if (double.TryParse(condition.Value.ToString(), out double maxNps))
                        filter.MaxNps = maxNps;
                    break;
                case FilterConditionType.MinDuration:
                    if (int.TryParse(condition.Value.ToString(), out int minDur))
                        filter.MinDuration = minDur;
                    break;
                case FilterConditionType.MaxDuration:
                    if (int.TryParse(condition.Value.ToString(), out int maxDur))
                        filter.MaxDuration = maxDur;
                    break;
                case FilterConditionType.MinSsStars:
                    if (double.TryParse(condition.Value.ToString(), out double minSs))
                        filter.MinSsStars = minSs;
                    break;
                case FilterConditionType.MaxSsStars:
                    if (double.TryParse(condition.Value.ToString(), out double maxSs))
                        filter.MaxSsStars = maxSs;
                    break;
                case FilterConditionType.MinBlStars:
                    if (double.TryParse(condition.Value.ToString(), out double minBl))
                        filter.MinBlStars = minBl;
                    break;
                case FilterConditionType.MaxBlStars:
                    if (double.TryParse(condition.Value.ToString(), out double maxBl))
                        filter.MaxBlStars = maxBl;
                    break;
                case FilterConditionType.Chroma:
                    filter.Chroma = Convert.ToBoolean(condition.Value);
                    break;
                case FilterConditionType.Noodle:
                    filter.Noodle = Convert.ToBoolean(condition.Value);
                    break;
                case FilterConditionType.Me:
                    filter.Me = Convert.ToBoolean(condition.Value);
                    break;
                case FilterConditionType.Cinema:
                    filter.Cinema = Convert.ToBoolean(condition.Value);
                    break;
                case FilterConditionType.Vivify:
                    filter.Vivify = Convert.ToBoolean(condition.Value);
                    break;
                case FilterConditionType.Automapper:
                    var autoVal = condition.Value.ToString();
                    if (autoVal == "仅AI谱")
                        filter.Automapper = true;
                    else if (autoVal == "排除AI谱")
                        filter.Automapper = false;
                    break;
                case FilterConditionType.Leaderboard:
                    filter.Leaderboard = condition.Value.ToString();
                    break;
                case FilterConditionType.Curated:
                    filter.Curated = Convert.ToBoolean(condition.Value);
                    break;
                case FilterConditionType.Verified:
                    filter.Verified = Convert.ToBoolean(condition.Value);
                    break;
                case FilterConditionType.MinScore:
                    if (double.TryParse(condition.Value.ToString(), out double minScore))
                        filter.MinRating = minScore;
                    break;
                case FilterConditionType.MaxScore:
                    if (double.TryParse(condition.Value.ToString(), out double maxScore))
                        filter.MaxRating = maxScore;
                    break;
            }
        }

        /// <summary>
        /// Applies range-type conditions to the API filter
        /// </summary>
        private void ApplyRangeConditionToFilter(BeatSaverSearchFilter filter, FilterCondition condition)
        {
            if (!(condition.Value is RangeValue rangeVal) || !rangeVal.HasValue)
                return;

            switch (condition.Type)
            {
                case FilterConditionType.BpmRange:
                    if (rangeVal.Min.HasValue) filter.MinBpm = rangeVal.Min.Value;
                    if (rangeVal.Max.HasValue) filter.MaxBpm = rangeVal.Max.Value;
                    break;

                case FilterConditionType.NpsRange:
                    if (rangeVal.Min.HasValue) filter.MinNps = rangeVal.Min.Value;
                    if (rangeVal.Max.HasValue) filter.MaxNps = rangeVal.Max.Value;
                    break;

                case FilterConditionType.DurationRange:
                    if (rangeVal.Min.HasValue) filter.MinDuration = (int)rangeVal.Min.Value;
                    if (rangeVal.Max.HasValue) filter.MaxDuration = (int)rangeVal.Max.Value;
                    break;

                case FilterConditionType.SsStarsRange:
                    if (rangeVal.Min.HasValue) filter.MinSsStars = rangeVal.Min.Value;
                    if (rangeVal.Max.HasValue) filter.MaxSsStars = rangeVal.Max.Value;
                    break;

                case FilterConditionType.BlStarsRange:
                    if (rangeVal.Min.HasValue) filter.MinBlStars = rangeVal.Min.Value;
                    if (rangeVal.Max.HasValue) filter.MaxBlStars = rangeVal.Max.Value;
                    break;

                case FilterConditionType.ScoreRange:
                    // User inputs 0-100, API needs 0-1
                    if (rangeVal.Min.HasValue) filter.MinRating = rangeVal.Min.Value;
                    if (rangeVal.Max.HasValue) filter.MaxRating = rangeVal.Max.Value;
                    break;
            }
        }

        /// <summary>
        /// Gets all saved presets
        /// </summary>
        public List<FilterPreset> GetSavedPresets()
        {
            return presets.ToList();
        }
    }
}