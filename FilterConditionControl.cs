using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// Control for a single filter condition row
    /// </summary>
    public class FilterConditionControl : UserControl
    {
        private CheckBox chkEnabled;
        private Label lblConditionName;
        private Control valueControl;
        private ComboBox cboOperator;
        private Button btnRemove;
        private FilterCondition condition;

        public event EventHandler<FilterCondition> ConditionChanged;
        public event EventHandler RemoveRequested;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FilterCondition Condition
        {
            get => condition;
            set
            {
                condition = value;
                UpdateUI();
            }
        }

        public FilterConditionControl()
        {
            InitializeUI();
        }

        public FilterConditionControl(FilterCondition condition) : this()
        {
            Condition = condition;
        }

        private void InitializeUI()
        {
            this.Height = 32;
            this.Dock = DockStyle.Top;
            this.Padding = new Padding(2);

            // Enable checkbox
            chkEnabled = new CheckBox
            {
                Width = 24,
                Dock = DockStyle.Left,
                Margin = new Padding(2),
                Checked = true
            };
            chkEnabled.CheckedChanged += (s, e) =>
            {
                if (condition != null)
                    condition.IsEnabled = chkEnabled.Checked;
                ConditionChanged?.Invoke(this, condition);
            };

            // Condition name label
            lblConditionName = new Label
            {
                Width = 100,
                Dock = DockStyle.Left,
                Margin = new Padding(5, 5, 5, 5),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };

            // Operator combo box
            cboOperator = new ComboBox
            {
                Width = 60,
                Dock = DockStyle.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(2)
            };
            cboOperator.Items.AddRange(new object[] { "AND", "OR" });
            cboOperator.SelectedIndex = 0;
            cboOperator.SelectedIndexChanged += (s, e) =>
            {
                if (condition != null)
                    condition.Operator = cboOperator.SelectedIndex == 0 ? LogicOperator.And : LogicOperator.Or;
                ConditionChanged?.Invoke(this, condition);
            };

            // Remove button
            btnRemove = new Button
            {
                Width = 28,
                Height = 24,
                Dock = DockStyle.Right,
                Text = "×",
                Margin = new Padding(2),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnRemove.Click += (s, e) => RemoveRequested?.Invoke(this, EventArgs.Empty);

            // Add controls
            Controls.Add(cboOperator);
            Controls.Add(btnRemove);
            Controls.Add(lblConditionName);
            Controls.Add(chkEnabled);
        }

        private void UpdateUI()
        {
            if (condition == null) return;

            chkEnabled.Checked = condition.IsEnabled;

            // Remove old value control if exists
            if (valueControl != null)
            {
                Controls.Remove(valueControl);
                valueControl.Dispose();
                valueControl = null;
            }

            // For custom conditions, create an editable name textbox
            if (condition.Type == FilterConditionType.Custom)
            {
                var nameBox = new TextBox
                {
                    Width = 100,
                    Dock = DockStyle.Left,
                    Margin = new Padding(2),
                    Text = condition.CustomName,
                    PlaceholderText = "条件名称"
                };
                nameBox.TextChanged += (s, e) =>
                {
                    condition.CustomName = nameBox.Text;
                    ConditionChanged?.Invoke(this, condition);
                };

                // Replace the label with the editable textbox
                Controls.Remove(lblConditionName);
                lblConditionName.Dispose();
                lblConditionName = null;
                Controls.Add(nameBox);

                // Create value textbox for custom condition
                var txtBox = new TextBox
                {
                    Width = 150,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(2),
                    Text = condition.Value?.ToString() ?? "",
                    PlaceholderText = "条件值"
                };
                txtBox.TextChanged += (s, e) =>
                {
                    condition.Value = txtBox.Text;
                    ConditionChanged?.Invoke(this, condition);
                };
                valueControl = txtBox;
            }
            else
            {
                // Standard condition - use label for display name
                if (lblConditionName == null)
                {
                    lblConditionName = new Label
                    {
                        Width = 100,
                        Dock = DockStyle.Left,
                        Margin = new Padding(5, 5, 5, 5),
                        TextAlign = ContentAlignment.MiddleLeft,
                        AutoSize = true
                    };
                    Controls.Add(lblConditionName);
                }
                lblConditionName.Text = condition.DisplayName + ":";

                // Create appropriate value control based on type
                switch (condition.ValueType)
                {
                    case FilterValueType.Text:
                        var txtBox = new TextBox
                        {
                            Width = 150,
                            Dock = DockStyle.Fill,
                            Margin = new Padding(2),
                            Text = condition.Value?.ToString() ?? ""
                        };
                        txtBox.TextChanged += (s, e) =>
                        {
                            condition.Value = txtBox.Text;
                            ConditionChanged?.Invoke(this, condition);
                        };
                        valueControl = txtBox;
                        break;

                    case FilterValueType.Number:
                        var numBox = new NumericUpDown
                        {
                            Width = 100,
                            Dock = DockStyle.Fill,
                            Margin = new Padding(2),
                            DecimalPlaces = 1,
                            Minimum = 0,
                            Maximum = 2000
                        };
                        if (condition.Value != null && double.TryParse(condition.Value.ToString(), out double numVal))
                            numBox.Value = (decimal)numVal;
                        numBox.ValueChanged += (s, e) =>
                        {
                            condition.Value = (double)numBox.Value;
                            ConditionChanged?.Invoke(this, condition);
                        };
                        valueControl = numBox;
                        break;

                    case FilterValueType.Boolean:
                        var cboBool = new ComboBox
                        {
                            Width = 80,
                            Dock = DockStyle.Fill,
                            DropDownStyle = ComboBoxStyle.DropDownList,
                            Margin = new Padding(2)
                        };
                        cboBool.Items.AddRange(new object[] { "不限", "是", "否" });
                        // Set initial value based on condition
                        if (condition.Value == null)
                            cboBool.SelectedIndex = 0; // 不限
                        else if (Convert.ToBoolean(condition.Value))
                            cboBool.SelectedIndex = 1; // 是
                        else
                            cboBool.SelectedIndex = 2; // 否
                        cboBool.SelectedIndexChanged += (s, e) =>
                        {
                            // 0 = 不限 (null), 1 = 是 (true), 2 = 否 (false)
                            condition.Value = cboBool.SelectedIndex switch
                            {
                                1 => true,
                                2 => false,
                                _ => null
                            };
                            ConditionChanged?.Invoke(this, condition);
                        };
                        valueControl = cboBool;
                        break;

                    case FilterValueType.Selection:
                        var cbo = new ComboBox
                        {
                            Width = 120,
                            Dock = DockStyle.Fill,
                            DropDownStyle = ComboBoxStyle.DropDownList,
                            Margin = new Padding(2)
                        };
                        if (condition.Options != null)
                        {
                            cbo.Items.AddRange(condition.Options.ToArray());
                            if (!string.IsNullOrEmpty(condition.Value?.ToString()))
                            {
                                int idx = condition.Options.IndexOf(condition.Value.ToString());
                                if (idx >= 0) cbo.SelectedIndex = idx;
                            }
                            else if (cbo.Items.Count > 0)
                            {
                                cbo.SelectedIndex = 0;
                            }
                        }
                        cbo.SelectedIndexChanged += (s, e) =>
                        {
                            condition.Value = cbo.SelectedItem?.ToString();
                            ConditionChanged?.Invoke(this, condition);
                        };
                        valueControl = cbo;
                        break;

                    case FilterValueType.Date:
                        var datePicker = new DateTimePicker
                        {
                            Width = 180,
                            Dock = DockStyle.Fill,
                            Margin = new Padding(2),
                            Format = DateTimePickerFormat.Short,
                            MaxDate = DateTime.Now
                        };
                        if (condition.Value != null && condition.Value is DateTime dt)
                            datePicker.Value = dt;
                        else
                            datePicker.Value = DateTime.Now.AddDays(-30); // Default to 30 days ago
                        datePicker.ValueChanged += (s, e) =>
                        {
                            condition.Value = datePicker.Value;
                            ConditionChanged?.Invoke(this, condition);
                        };
                        valueControl = datePicker;
                        break;

                    case FilterValueType.NumberWithSort:
                        // Create a panel with number input and sort dropdown
                        var panel = new Panel
                        {
                            Width = 220,
                            Height = 28,
                            Dock = DockStyle.Fill,
                            Margin = new Padding(2)
                        };

                        var numInput = new NumericUpDown
                        {
                            Width = 70,
                            Dock = DockStyle.Left,
                            Minimum = 1,
                            Maximum = 1000,
                            Value = 100
                        };

                        var sortCombo = new ComboBox
                        {
                            Width = 100,
                            Dock = DockStyle.Right,
                            DropDownStyle = ComboBoxStyle.DropDownList
                        };
                        sortCombo.Items.AddRange(new object[] { "最新上传", "最早上传", "随机" });
                        sortCombo.SelectedIndex = 0;

                        // Initialize from existing value
                        if (condition.Value is ResultLimitValue existingLimit)
                        {
                            numInput.Value = existingLimit.Count;
                            sortCombo.SelectedIndex = (int)existingLimit.SortOption;
                        }
                        else if (condition.Value != null)
                        {
                            // Try to parse from int (backward compatibility)
                            if (int.TryParse(condition.Value.ToString(), out int count))
                                numInput.Value = count;
                        }

                        numInput.ValueChanged += (s, e) =>
                        {
                            condition.Value = new ResultLimitValue((int)numInput.Value, (ResultSortOption)sortCombo.SelectedIndex);
                            ConditionChanged?.Invoke(this, condition);
                        };

                        sortCombo.SelectedIndexChanged += (s, e) =>
                        {
                            condition.Value = new ResultLimitValue((int)numInput.Value, (ResultSortOption)sortCombo.SelectedIndex);
                            ConditionChanged?.Invoke(this, condition);
                        };

                        panel.Controls.Add(sortCombo);
                        panel.Controls.Add(numInput);
                        valueControl = panel;
                        break;

                    case FilterValueType.Range:
                        // Create a panel with two numeric inputs for min-max range
                        var rangePanel = new Panel
                        {
                            Width = 200,
                            Height = 28,
                            Dock = DockStyle.Fill,
                            Margin = new Padding(2)
                        };

                        // Use TextBox instead of NumericUpDown to allow empty/placeholder display
                        var minTextBox = new TextBox
                        {
                            Width = 70,
                            Dock = DockStyle.Left,
                            Text = "",
                            TextAlign = HorizontalAlignment.Center,
                            PlaceholderText = "最小"
                        };

                        var separatorLabel = new Label
                        {
                            Width = 20,
                            Dock = DockStyle.Left,
                            Text = "—",
                            TextAlign = ContentAlignment.MiddleCenter
                        };

                        var maxTextBox = new TextBox
                        {
                            Width = 70,
                            Dock = DockStyle.Left,
                            Text = "",
                            TextAlign = HorizontalAlignment.Center,
                            PlaceholderText = "最大"
                        };

                        // Initialize from existing value
                        if (condition.Value is RangeValue existingRange)
                        {
                            if (existingRange.HasMin)
                                minTextBox.Text = existingRange.MinRaw.ToString();
                            if (existingRange.HasMax)
                                maxTextBox.Text = existingRange.MaxRaw.ToString();
                        }

                        void UpdateRangeValueFromText()
                        {
                            double? minVal = null;
                            double? maxVal = null;

                            if (!string.IsNullOrWhiteSpace(minTextBox.Text) && double.TryParse(minTextBox.Text, out double minParsed))
                                minVal = minParsed;

                            if (!string.IsNullOrWhiteSpace(maxTextBox.Text) && double.TryParse(maxTextBox.Text, out double maxParsed))
                                maxVal = maxParsed;

                            condition.Value = new RangeValue(minVal, maxVal);
                            ConditionChanged?.Invoke(this, condition);
                        }

                        minTextBox.TextChanged += (s, e) => UpdateRangeValueFromText();
                        maxTextBox.TextChanged += (s, e) => UpdateRangeValueFromText();

                        // Allow only numeric input
                        void ValidateNumericInput(TextBox textBox, KeyPressEventArgs e)
                        {
                            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != '-')
                                e.Handled = true;
                        }

                        minTextBox.KeyPress += (s, e) => ValidateNumericInput(minTextBox, e);
                        maxTextBox.KeyPress += (s, e) => ValidateNumericInput(maxTextBox, e);

                        rangePanel.Controls.Add(maxTextBox);
                        rangePanel.Controls.Add(separatorLabel);
                        rangePanel.Controls.Add(minTextBox);
                        valueControl = rangePanel;
                        break;

                    case FilterValueType.SearchQuery:
                        // Create a panel with field type checked list and text input
                        var searchPanel = new Panel
                        {
                            Width = 400,
                            Height = 28,
                            Dock = DockStyle.Fill,
                            Margin = new Padding(2)
                        };

                        // Use a dropdown button with checkboxes for multi-select
                        var fieldButton = new Button
                        {
                            Width = 90,
                            Dock = DockStyle.Left,
                            Text = "全部字段",
                            TextAlign = ContentAlignment.MiddleLeft,
                            Margin = new Padding(0, 0, 2, 0)
                        };

                        var searchTextBox = new TextBox
                        {
                            Width = 300,
                            Dock = DockStyle.Right,
                            Text = "",
                            PlaceholderText = "输入关键词..."
                        };

                        // Track selected field types
                        var selectedFields = new Dictionary<string, SearchFieldType>
                        {
                            { "歌名", SearchFieldType.SongName },
                            { "艺术家", SearchFieldType.Artist },
                            { "谱师", SearchFieldType.Mapper },
                            { "标题", SearchFieldType.MapName },
                            { "简介", SearchFieldType.Description },
                            { "上传者", SearchFieldType.Uploader }
                        };

                        // Initialize from existing value
                        SearchFieldType currentFieldTypes = SearchFieldType.All;
                        if (condition.Value is SearchQueryValue existingQuery)
                        {
                            searchTextBox.Text = existingQuery.Query;
                            currentFieldTypes = existingQuery.FieldTypes;
                        }
                        else if (condition.Value != null)
                        {
                            // Backward compatibility: old string value
                            searchTextBox.Text = condition.Value.ToString();
                        }

                        // Update button text based on selection
                        void UpdateFieldButtonText()
                        {
                            if (currentFieldTypes == SearchFieldType.All || currentFieldTypes == (SearchFieldType.SongName | SearchFieldType.Artist | SearchFieldType.Mapper | SearchFieldType.MapName | SearchFieldType.Description | SearchFieldType.Uploader))
                            {
                                fieldButton.Text = "全部字段";
                            }
                            else if (currentFieldTypes == SearchFieldType.None)
                            {
                                fieldButton.Text = "选择字段";
                            }
                            else
                            {
                                var names = new List<string>();
                                if (currentFieldTypes.HasFlag(SearchFieldType.SongName)) names.Add("歌名");
                                if (currentFieldTypes.HasFlag(SearchFieldType.Artist)) names.Add("艺术家");
                                if (currentFieldTypes.HasFlag(SearchFieldType.Mapper)) names.Add("谱师");
                                if (currentFieldTypes.HasFlag(SearchFieldType.MapName)) names.Add("标题");
                                if (currentFieldTypes.HasFlag(SearchFieldType.Description)) names.Add("简介");
                                if (currentFieldTypes.HasFlag(SearchFieldType.Uploader)) names.Add("上传者");
                                fieldButton.Text = string.Join("/", names.Take(3)) + (names.Count > 3 ? "..." : "");
                            }
                        }

                        UpdateFieldButtonText();

                        // Create context menu for field selection
                        var fieldMenu = new ContextMenuStrip();
                        var menuItems = new List<ToolStripMenuItem>();

                        foreach (var kvp in selectedFields)
                        {
                            var item = new ToolStripMenuItem(kvp.Key);
                            item.Checked = currentFieldTypes.HasFlag(kvp.Value);
                            item.CheckOnClick = true;
                            item.CheckedChanged += (s, e) =>
                            {
                                if (item.Checked)
                                    currentFieldTypes |= kvp.Value;
                                else
                                    currentFieldTypes &= ~kvp.Value;

                                UpdateFieldButtonText();
                                UpdateSearchQueryValue();
                            };
                            menuItems.Add(item);
                            fieldMenu.Items.Add(item);
                        }

                        // Add "全选" and "清除" buttons
                        fieldMenu.Items.Add(new ToolStripSeparator());
                        var selectAllItem = new ToolStripMenuItem("全选");
                        selectAllItem.Click += (s, e) =>
                        {
                            currentFieldTypes = SearchFieldType.All;
                            foreach (ToolStripMenuItem mi in menuItems) mi.Checked = true;
                            UpdateFieldButtonText();
                            UpdateSearchQueryValue();
                        };
                        fieldMenu.Items.Add(selectAllItem);

                        var clearItem = new ToolStripMenuItem("清除");
                        clearItem.Click += (s, e) =>
                        {
                            currentFieldTypes = SearchFieldType.None;
                            foreach (ToolStripMenuItem mi in menuItems) mi.Checked = false;
                            UpdateFieldButtonText();
                            UpdateSearchQueryValue();
                        };
                        fieldMenu.Items.Add(clearItem);

                        fieldButton.Click += (s, e) => fieldMenu.Show(fieldButton, 0, fieldButton.Height);

                        void UpdateSearchQueryValue()
                        {
                            condition.Value = new SearchQueryValue(searchTextBox.Text, currentFieldTypes);
                            ConditionChanged?.Invoke(this, condition);
                        }

                        searchTextBox.TextChanged += (s, e) => UpdateSearchQueryValue();

                        searchPanel.Controls.Add(searchTextBox);
                        searchPanel.Controls.Add(fieldButton);
                        valueControl = searchPanel;
                        break;
                }
            }

            if (valueControl != null)
            {
                Controls.Add(valueControl);
                // Move operator and remove button to end
                Controls.SetChildIndex(valueControl, 0);
            }

            // Update operator combo
            cboOperator.SelectedIndex = condition.Operator == LogicOperator.And ? 0 : 1;
        }

        /// <summary>
        /// Sets the visibility of the operator combo box
        /// </summary>
        public void SetOperatorVisibility(bool visible)
        {
            cboOperator.Visible = visible;
        }
    }
}