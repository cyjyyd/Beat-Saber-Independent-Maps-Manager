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
        private Button btnLoadPreset;
        private Button btnSavePreset;
        private Button btnNewPreset;
        private Button btnDeletePreset;

        // Filter builder controls
        private Panel groupsPanel;
        private Button btnAddGroup;
        private Panel buttonsPanel;
        private Button btnSearch;
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
                Height = 40,
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

            btnLoadPreset = new Button
            {
                Text = "加载",
                Width = 60,
                Dock = DockStyle.Left,
                Margin = new Padding(2)
            };
            btnLoadPreset.Click += (s, e) => LoadSelectedPreset();

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

            presetPanel.Controls.Add(btnDeletePreset);
            presetPanel.Controls.Add(btnNewPreset);
            presetPanel.Controls.Add(btnSavePreset);
            presetPanel.Controls.Add(btnLoadPreset);
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

            btnSearch = new Button
            {
                Text = "搜索",
                Width = 80,
                Dock = DockStyle.Right,
                Margin = new Padding(2),
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White
            };
            btnSearch.Click += (s, e) => OnSearchRequested();

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

            buttonsPanel.Controls.Add(btnSearch);
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

        private void LoadSelectedPreset()
        {
            if (cboPreset.SelectedItem is FilterPreset preset)
            {
                currentPreset = preset.Clone();
                UpdateGroupControls();
            }
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
            if (cboPreset.SelectedItem is FilterPreset preset && preset != currentPreset)
            {
                if (MessageBox.Show($"确定删除预设 '{preset.Name}' 吗？", "确认",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    string presetDir = Path.Combine(Application.StartupPath, "presets");
                    string filePath = Path.Combine(presetDir, $"{preset.Name}.bsf");
                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    presets.Remove(preset);
                    UpdatePresetCombo();
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
        /// Converts the current filter configuration to BeatSaverSearchFilter
        /// </summary>
        public BeatSaverSearchFilter ToSearchFilter()
        {
            var filter = new BeatSaverSearchFilter();

            if (currentPreset == null) return filter;

            // Collect all conditions from all groups
            // For simplicity, we'll use AND logic between groups
            // and apply each condition to the filter
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

            switch (condition.Type)
            {
                case FilterConditionType.Query:
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
            }
        }
    }
}