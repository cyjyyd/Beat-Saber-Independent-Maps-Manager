using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// Control for a filter condition group
    /// </summary>
    public class FilterGroupControl : UserControl
    {
        private Panel headerPanel;
        private CheckBox chkEnabled;
        private CheckBox chkUseLocalCache;
        private TextBox txtGroupName;
        private ComboBox cboGroupOperator;
        private Button btnRemoveGroup;
        private Panel conditionsPanel;
        private ComboBox cboAddCondition;

        private FilterGroup group;
        private List<FilterConditionControl> conditionControls = new List<FilterConditionControl>();

        public event EventHandler<FilterGroup> GroupChanged;
        public event EventHandler RemoveRequested;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FilterGroup Group
        {
            get => group;
            set
            {
                group = value;
                UpdateUI();
            }
        }

        public FilterGroupControl()
        {
            InitializeUI();
        }

        public FilterGroupControl(FilterGroup group) : this()
        {
            Group = group;
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Top;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Margin = new Padding(3);
            this.BackColor = SystemColors.ControlLight;

            // Header panel
            headerPanel = new Panel
            {
                Height = 32,
                Dock = DockStyle.Top,
                Padding = new Padding(3)
            };

            // Enable checkbox
            chkEnabled = new CheckBox
            {
                Width = 20,
                Dock = DockStyle.Left,
                Margin = new Padding(2),
                Checked = true
            };
            chkEnabled.CheckedChanged += (s, e) =>
            {
                if (group != null)
                    group.IsEnabled = chkEnabled.Checked;
                GroupChanged?.Invoke(this, group);
            };

            // Use Local Cache checkbox
            chkUseLocalCache = new CheckBox
            {
                Width = 90,
                Dock = DockStyle.Left,
                Margin = new Padding(5, 2, 5, 2),
                Text = "本地缓存",
                Checked = false,
                ForeColor = Color.FromArgb(70, 130, 180)
            };
            chkUseLocalCache.CheckedChanged += (s, e) =>
            {
                if (group != null)
                    group.UseLocalCache = chkUseLocalCache.Checked;
                PopulateConditionTypes();
                GroupChanged?.Invoke(this, group);
            };

            // Group name text box
            txtGroupName = new TextBox
            {
                Width = 120,
                Dock = DockStyle.Left,
                Margin = new Padding(5, 2, 5, 2),
                Text = "条件组"
            };
            txtGroupName.TextChanged += (s, e) =>
            {
                if (group != null)
                    group.Name = txtGroupName.Text;
            };

            // Group operator combo
            cboGroupOperator = new ComboBox
            {
                Width = 70,
                Dock = DockStyle.Left,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(2)
            };
            cboGroupOperator.Items.AddRange(new object[] { "AND", "OR" });
            cboGroupOperator.SelectedIndex = 0;
            cboGroupOperator.SelectedIndexChanged += (s, e) =>
            {
                if (group != null)
                    group.GroupOperator = cboGroupOperator.SelectedIndex == 0 ? LogicOperator.And : LogicOperator.Or;
                GroupChanged?.Invoke(this, group);
            };

            // Remove group button
            btnRemoveGroup = new Button
            {
                Width = 28,
                Height = 24,
                Dock = DockStyle.Right,
                Text = "×",
                Margin = new Padding(2),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnRemoveGroup.Click += (s, e) => RemoveRequested?.Invoke(this, EventArgs.Empty);

            // Add condition combo box (dropdown menu style)
            cboAddCondition = new ComboBox
            {
                Width = 150,
                Dock = DockStyle.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(2)
            };
            PopulateConditionTypes();
            cboAddCondition.SelectedIndexChanged += (s, e) =>
            {
                if (cboAddCondition.SelectedItem is FilterConditionTypeItem item)
                {
                    // 跳过占位符项（Type == None）
                    if (item.Type == FilterConditionType.None)
                        return;

                    AddCondition(new FilterCondition(item.Type));
                    cboAddCondition.SelectedIndex = 0; // Reset to placeholder
                }
            };

            headerPanel.Controls.Add(btnRemoveGroup);
            headerPanel.Controls.Add(cboAddCondition);
            headerPanel.Controls.Add(cboGroupOperator);
            headerPanel.Controls.Add(chkUseLocalCache);
            headerPanel.Controls.Add(txtGroupName);
            headerPanel.Controls.Add(chkEnabled);

            // Conditions panel
            conditionsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Padding = new Padding(10, 5, 5, 5),
                BackColor = SystemColors.Control
            };

            this.Controls.Add(conditionsPanel);
            this.Controls.Add(headerPanel);
        }

        private void PopulateConditionTypes()
        {
            cboAddCondition.Items.Clear();
            cboAddCondition.Items.Add(new FilterConditionTypeItem { DisplayName = "-- 添加条件 --", Type = FilterConditionType.None });
            cboAddCondition.Items.Add(new FilterConditionTypeItem { DisplayName = "[自定义] 自定义条件", Type = FilterConditionType.Custom });

            bool useLocalCache = chkUseLocalCache.Checked;
            var grouped = FilterConditionMetadata.GetGroupedConditions();
            foreach (var kvp in grouped)
            {
                // Skip local cache group if not enabled
                if (kvp.Key == "本地缓存专属" && !useLocalCache)
                    continue;

                foreach (var type in kvp.Value)
                {
                    // Skip local cache-specific conditions if not enabled
                    if (FilterConditionMetadata.RequiresLocalCache(type) && !useLocalCache)
                        continue;

                    cboAddCondition.Items.Add(new FilterConditionTypeItem
                    {
                        DisplayName = $"[{kvp.Key}] {FilterConditionMetadata.GetDisplayName(type)}",
                        Type = type
                    });
                }
            }
            cboAddCondition.SelectedIndex = 0;
        }

        private void UpdateUI()
        {
            if (group == null) return;

            chkEnabled.Checked = group.IsEnabled;
            chkUseLocalCache.Checked = group.UseLocalCache;
            txtGroupName.Text = group.Name;
            cboGroupOperator.SelectedIndex = group.GroupOperator == LogicOperator.And ? 0 : 1;

            // Update condition types dropdown based on UseLocalCache setting
            PopulateConditionTypes();

            // Clear existing condition controls
            foreach (var ctrl in conditionControls)
            {
                ctrl.ConditionChanged -= OnConditionChanged;
                ctrl.RemoveRequested -= OnRemoveCondition;
                conditionsPanel.Controls.Remove(ctrl);
                ctrl.Dispose();
            }
            conditionControls.Clear();

            // Add condition controls
            foreach (var condition in group.Conditions)
            {
                var ctrl = new FilterConditionControl(condition);
                ctrl.ConditionChanged += OnConditionChanged;
                ctrl.RemoveRequested += OnRemoveCondition;
                conditionControls.Add(ctrl);
                conditionsPanel.Controls.Add(ctrl);
            }

            UpdateConditionsLayout();
        }

        private void AddCondition(FilterCondition condition)
        {
            if (group == null) return;

            group.AddCondition(condition);

            var ctrl = new FilterConditionControl(condition);
            ctrl.ConditionChanged += OnConditionChanged;
            ctrl.RemoveRequested += OnRemoveCondition;
            conditionControls.Add(ctrl);
            conditionsPanel.Controls.Add(ctrl);

            UpdateConditionsLayout();
            GroupChanged?.Invoke(this, group);
        }

        private void RemoveCondition(FilterConditionControl control)
        {
            if (group == null) return;

            group.RemoveCondition(control.Condition);
            control.ConditionChanged -= OnConditionChanged;
            control.RemoveRequested -= OnRemoveCondition;
            conditionControls.Remove(control);
            conditionsPanel.Controls.Remove(control);
            control.Dispose();

            UpdateConditionsLayout();
            GroupChanged?.Invoke(this, group);
        }

        private void OnConditionChanged(object sender, FilterCondition condition)
        {
            GroupChanged?.Invoke(this, group);
        }

        private void OnRemoveCondition(object sender, EventArgs e)
        {
            if (sender is FilterConditionControl ctrl)
            {
                RemoveCondition(ctrl);
            }
        }

        private void UpdateConditionsLayout()
        {
            int totalHeight = 0;
            // Calculate total height needed
            foreach (Control ctrl in conditionsPanel.Controls)
            {
                totalHeight += ctrl.Height + 2;
            }

            conditionsPanel.Height = Math.Max(totalHeight + 10, 36);

            // Adjust main control height
            this.Height = headerPanel.Height + conditionsPanel.Height + 10;

            // Update operator visibility for the last condition
            for (int i = 0; i < conditionControls.Count; i++)
            {
                conditionControls[i].SetOperatorVisibility(i < conditionControls.Count - 1);
            }
        }

        /// <summary>
        /// Sets the visibility of the group operator combo box
        /// </summary>
        public void SetGroupOperatorVisibility(bool visible)
        {
            cboGroupOperator.Visible = visible;
        }

        private class FilterConditionTypeItem
        {
            public string DisplayName { get; set; }
            public FilterConditionType Type { get; set; }

            public override string ToString()
            {
                return DisplayName;
            }
        }
    }
}