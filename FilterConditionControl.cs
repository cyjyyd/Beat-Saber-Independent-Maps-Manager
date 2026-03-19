using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
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
                        var chkBox = new CheckBox
                        {
                            Width = 80,
                            Dock = DockStyle.Fill,
                            Margin = new Padding(2),
                            Text = "启用",
                            Checked = condition.Value != null && Convert.ToBoolean(condition.Value)
                        };
                        chkBox.CheckedChanged += (s, e) =>
                        {
                            condition.Value = chkBox.Checked;
                            ConditionChanged?.Invoke(this, condition);
                        };
                        valueControl = chkBox;
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