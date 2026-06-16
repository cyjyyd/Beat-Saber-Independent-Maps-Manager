using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BeatSaberIndependentMapsManager
{
    public class BatchExportForm : Form
    {
        private Label lblTip;
        private Label lblPresets;
        private CheckedListBox chkPresets;
        private Button btnImport;
        private Button btnSelectAll;
        private Button btnSelectInverse;
        private Label lblOutput;
        private TextBox txtOutputDir;
        private Button btnBrowse;
        private Button btnStart;
        private Button btnCancel;

        private List<FilterPreset> availablePresets = new List<FilterPreset>();
        private List<FilterPreset> importedPresets = new List<FilterPreset>();

        public List<FilterPreset> SelectedPresets { get; private set; } = new List<FilterPreset>();
        public string OutputDirectory => txtOutputDir.Text;

        public BatchExportForm()
        {
            InitializeComponent();
            LoadLocalPresets();
        }

        private void InitializeComponent()
        {
            this.Text = "批处理导出";
            this.ClientSize = new Size(500, 450);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblTip = new Label
            {
                Text = "提示：预设名称中可使用【】标记封面文字，如\"NPS6+【NPS6】\"将在封面显示\"NPS6\"",
                Location = new Point(10, 10),
                Size = new Size(480, 40),
                ForeColor = Color.Gray
            };

            lblPresets = new Label
            {
                Text = "选择预设（可多选）：",
                Location = new Point(10, 55),
                Size = new Size(200, 20)
            };

            chkPresets = new CheckedListBox
            {
                Location = new Point(10, 80),
                Size = new Size(470, 200),
                CheckOnClick = true
            };

            btnImport = new Button
            {
                Text = "导入预设文件...",
                Location = new Point(10, 290),
                Size = new Size(120, 30)
            };
            btnImport.Click += BtnImport_Click;

            btnSelectAll = new Button
            {
                Text = "全选",
                Location = new Point(140, 290),
                Size = new Size(60, 30)
            };
            btnSelectAll.Click += (s, e) =>
            {
                for (int i = 0; i < chkPresets.Items.Count; i++)
                    chkPresets.SetItemChecked(i, true);
            };

            btnSelectInverse = new Button
            {
                Text = "反选",
                Location = new Point(210, 290),
                Size = new Size(60, 30)
            };
            btnSelectInverse.Click += (s, e) =>
            {
                for (int i = 0; i < chkPresets.Items.Count; i++)
                    chkPresets.SetItemChecked(i, !chkPresets.GetItemChecked(i));
            };

            lblOutput = new Label
            {
                Text = "输出目录：",
                Location = new Point(10, 330),
                Size = new Size(80, 20)
            };

            txtOutputDir = new TextBox
            {
                Location = new Point(90, 330),
                Size = new Size(300, 25),
                Text = Path.Combine(Application.StartupPath, "playlists")
            };

            btnBrowse = new Button
            {
                Text = "浏览...",
                Location = new Point(400, 328),
                Size = new Size(80, 28)
            };
            btnBrowse.Click += BtnBrowse_Click;

            btnStart = new Button
            {
                Text = "开始导出",
                Location = new Point(130, 380),
                Size = new Size(120, 40),
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White
            };
            btnStart.Click += BtnStart_Click;

            btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(270, 380),
                Size = new Size(100, 40),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(lblTip);
            this.Controls.Add(lblPresets);
            this.Controls.Add(chkPresets);
            this.Controls.Add(btnImport);
            this.Controls.Add(btnSelectAll);
            this.Controls.Add(btnSelectInverse);
            this.Controls.Add(lblOutput);
            this.Controls.Add(txtOutputDir);
            this.Controls.Add(btnBrowse);
            this.Controls.Add(btnStart);
            this.Controls.Add(btnCancel);
        }

        private void LoadLocalPresets()
        {
            string presetDir = Path.Combine(Application.StartupPath, "presets");
            if (Directory.Exists(presetDir))
            {
                foreach (var file in Directory.GetFiles(presetDir, "*.bsf"))
                {
                    try
                    {
                        var preset = FilterPreset.LoadFromFile(file);
                        if (preset != null && preset.Groups != null && preset.Groups.Count > 0)
                        {
                            availablePresets.Add(preset);
                            chkPresets.Items.Add(preset.Name, false);
                        }
                    }
                    catch { }
                }
            }
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "导入筛选预设（支持多选）";
                ofd.Filter = "筛选预设文件 (*.bsf)|*.bsf|JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*";
                ofd.Multiselect = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    foreach (string filePath in ofd.FileNames)
                    {
                        try
                        {
                            var preset = FilterPreset.LoadFromFile(filePath);
                            if (preset != null && preset.Groups != null)
                            {
                                importedPresets.Add(preset);
                                chkPresets.Items.Add($"[导入] {preset.Name}", true);
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "选择歌单输出目录";
                fbd.ShowNewFolderButton = true;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtOutputDir.Text = fbd.SelectedPath;
                }
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            SelectedPresets.Clear();
            for (int i = 0; i < chkPresets.Items.Count; i++)
            {
                if (chkPresets.GetItemChecked(i))
                {
                    if (i < availablePresets.Count)
                    {
                        SelectedPresets.Add(availablePresets[i]);
                    }
                    else
                    {
                        int importedIndex = i - availablePresets.Count;
                        if (importedIndex < importedPresets.Count)
                        {
                            SelectedPresets.Add(importedPresets[importedIndex]);
                        }
                    }
                }
            }

            if (SelectedPresets.Count == 0)
            {
                MessageBox.Show("请至少选择一个预设！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
