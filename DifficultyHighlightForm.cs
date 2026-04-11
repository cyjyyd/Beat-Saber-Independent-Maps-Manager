using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// 难度高亮选择对话框
    /// 展示筛选后歌曲的关键信息（BSR、标题、封面、BPM），
    /// 每首歌提供按模式归类的难度复选框。
    /// 退出时暂存高亮信息，不直接导出。
    /// </summary>
    public class DifficultyHighlightForm : Form
    {
        private readonly List<BeatSaverMap> _maps;
        private DataGridView _songGrid;
        private Panel _toolbarPanel;
        private CheckBox _selectAllCheckbox;
        private Label _tipLabel;

        // 存储每首歌的选中难度: key = 歌曲索引, value = HashSet<(characteristic, difficulty)>
        private Dictionary<int, HashSet<(string characteristic, string difficulty)>> _selectedDiffs;

        // 所有歌曲中出现的难度类型集合（用于动态生成列）
        private List<(string characteristic, string difficulty)> _globalDiffs;

        // 封面异步加载
        private Dictionary<int, Image> _coverCache = new Dictionary<int, Image>();
        private bool _selectingAll = false; // 防止全选复选框递归

        /// <summary>
        /// 每首歌选中的难度集合。key = 歌曲在原始列表中的索引
        /// </summary>
        public Dictionary<int, HashSet<(string characteristic, string difficulty)>> PerSongDifficulties => _selectedDiffs;

        public DifficultyHighlightForm(List<BeatSaverMap> maps)
        {
            _maps = maps;
            _globalDiffs = CollectGlobalDifficulties();
            _selectedDiffs = new Dictionary<int, HashSet<(string, string)>>();
            for (int i = 0; i < maps.Count; i++)
                _selectedDiffs[i] = new HashSet<(string, string)>();
            InitializeUI();
        }

        private List<(string characteristic, string difficulty)> CollectGlobalDifficulties()
        {
            var set = new HashSet<(string, string)>();
            foreach (var map in _maps)
            {
                var version = map.Versions?.FirstOrDefault();
                if (version?.Diffs != null)
                {
                    foreach (var diff in version.Diffs)
                    {
                        if (!string.IsNullOrEmpty(diff.Characteristic) && !string.IsNullOrEmpty(diff.Difficulty))
                            set.Add((diff.Characteristic, diff.Difficulty));
                    }
                }
            }
            var result = set.ToList();
            result.Sort((a, b) =>
            {
                // Standard 优先
                bool aStd = a.Item1.Equals("Standard", StringComparison.OrdinalIgnoreCase);
                bool bStd = b.Item1.Equals("Standard", StringComparison.OrdinalIgnoreCase);
                if (aStd != bStd) return aStd ? -1 : 1;
                int cmp = string.Compare(a.Item1, b.Item1, StringComparison.Ordinal);
                if (cmp != 0) return cmp;
                return DifficultyOrder(a.Item2).CompareTo(DifficultyOrder(b.Item2));
            });
            return result;
        }

        private static int DifficultyOrder(string diff) => diff switch
        {
            "Easy" => 0, "Normal" => 1, "Hard" => 2, "Expert" => 3, "ExpertPlus" => 4, _ => 5
        };

        private void InitializeUI()
        {
            this.Text = "高亮难度";
            this.Size = new Size(1100, 600);
            this.MinimumSize = new Size(800, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.ShowInTaskbar = false;

            // 工具栏
            _toolbarPanel = new Panel
            {
                Height = 36,
                Dock = DockStyle.Top,
                Padding = new Padding(5)
            };

            _selectAllCheckbox = new CheckBox
            {
                Text = "全选所有难度",
                Location = new Point(10, 8),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            _selectAllCheckbox.CheckedChanged += (s, e) =>
            {
                if (_selectingAll) return; // 防止递归
                bool checkAll = _selectAllCheckbox.Checked;
                foreach (DataGridViewRow row in _songGrid.Rows)
                    SetRowDiffChecked(row.Index, checkAll);
            };
            _toolbarPanel.Controls.Add(_selectAllCheckbox);

            var btnClear = new Button
            {
                Text = "清空全部",
                Location = new Point(120, 5),
                Size = new Size(80, 25)
            };
            btnClear.Click += (s, e) =>
            {
                _selectAllCheckbox.Checked = false;
                foreach (DataGridViewRow row in _songGrid.Rows)
                    SetRowDiffChecked(row.Index, false);
            };
            _toolbarPanel.Controls.Add(btnClear);

            _tipLabel = new Label
            {
                Text = $"共 {_maps.Count} 首歌曲",
                Location = new Point(210, 10),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            _toolbarPanel.Controls.Add(_tipLabel);

            // 歌曲 DataGridView
            _songGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.Fixed3D,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleLeft, Padding = new Padding(2) }
            };

            // 固定列
            _songGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SongName", HeaderText = "标题", Width = 200, ReadOnly = true });
            _songGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "BSR", HeaderText = "BSR", Width = 60, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            _songGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "BPM", HeaderText = "BPM", Width = 55, ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
            _songGrid.Columns.Add(new DataGridViewImageColumn { Name = "Cover", HeaderText = "封面", Width = 55, ReadOnly = true, ImageLayout = DataGridViewImageCellLayout.Zoom, DefaultCellStyle = new DataGridViewCellStyle { NullValue = null } });
            _songGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Mapper", HeaderText = "谱师", Width = 120, ReadOnly = true });

            // 动态生成难度列
            BuildDifficultyColumns();

            // 填充行数据
            for (int i = 0; i < _maps.Count; i++)
                BuildSongRow(i);

            // 难度列的 CellValueChanged 事件
            _songGrid.CellValueChanged += SongGrid_CellValueChanged;
            _songGrid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (_songGrid.IsCurrentCellDirty)
                    _songGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            // 难度列表头点击全选/反选
            _songGrid.ColumnHeaderMouseClick += SongGrid_ColumnHeaderMouseClick;

            // 底部按钮
            Panel bottomPanel = new Panel
            {
                Height = 45,
                Dock = DockStyle.Bottom,
                Padding = new Padding(10, 5, 10, 5)
            };

            var btnClose = new Button
            {
                Text = "完成并关闭",
                Width = 110,
                Dock = DockStyle.Right,
                Margin = new Padding(5),
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White,
                DialogResult = DialogResult.OK
            };
            btnClose.Click += (s, e) => this.Close();

            var btnCancel = new Button
            {
                Text = "取消",
                Width = 80,
                Dock = DockStyle.Right,
                Margin = new Padding(5),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.Click += (s, e) => this.Close();

            bottomPanel.Controls.Add(btnCancel);
            bottomPanel.Controls.Add(btnClose);

            // 组装
            this.Controls.Add(_songGrid);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(_toolbarPanel);

            // 窗口加载完成后异步预加载封面
            this.Shown += (s, e) => PreloadCoversAsync();
        }

        /// <summary>
        /// 异步预加载封面，逐批加载避免阻塞 UI
        /// </summary>
        private async void PreloadCoversAsync()
        {
            // 分批加载，每批 10 张
            int batchSize = 10;
            for (int start = 0; start < _maps.Count; start += batchSize)
            {
                int end = Math.Min(start + batchSize, _maps.Count);
                var tasks = new List<Task>();
                for (int i = start; i < end; i++)
                {
                    int idx = i;
                    tasks.Add(LoadCoverForRowAsync(idx));
                }
                await Task.WhenAll(tasks);
            }
        }

        private async Task LoadCoverForRowAsync(int rowIdx)
        {
            if (rowIdx < 0 || rowIdx >= _maps.Count) return;
            var map = _maps[rowIdx];
            var cover = await LoadCoverAsync(map);
            if (this.IsDisposed) return;
            _coverCache[rowIdx] = cover;
            // 更新 DataGridView 封面列
            this.BeginInvoke(() =>
            {
                if (!_songGrid.IsDisposed && rowIdx < _songGrid.Rows.Count)
                {
                    _songGrid.Rows[rowIdx].Cells["Cover"].Value = cover;
                }
            });
        }

        private void BuildDifficultyColumns()
        {
            string lastChar = "";
            foreach (var (characteristic, difficulty) in _globalDiffs)
            {
                if (characteristic != lastChar)
                {
                    lastChar = characteristic;
                    // 添加模式标签列
                    bool isStandard = characteristic.Equals("Standard", StringComparison.OrdinalIgnoreCase);
                    _songGrid.Columns.Add(new DataGridViewTextBoxColumn
                    {
                        Name = $"label_{characteristic}",
                        HeaderText = isStandard ? "Standard" : characteristic,
                        Width = Math.Max(60, TextRenderer.MeasureText(characteristic, this.Font).Width + 16),
                        ReadOnly = true,
                        DefaultCellStyle = new DataGridViewCellStyle
                        {
                            Alignment = DataGridViewContentAlignment.MiddleCenter,
                            BackColor = isStandard ? Color.FromArgb(220, 240, 220) : Color.FromArgb(230, 230, 240),
                            ForeColor = Color.DarkSlateGray,
                            Font = new Font(this.Font, FontStyle.Bold),
                            Padding = new Padding(1)
                        }
                    });
                }
                var col = new DataGridViewCheckBoxColumn
                {
                    Name = MakeDiffColumnName(characteristic, difficulty),
                    HeaderText = DifficultyLabel(difficulty),
                    Width = 55,
                    FlatStyle = FlatStyle.Standard
                };
                _songGrid.Columns.Add(col);
            }
            _tipLabel.Text = $"共 {_maps.Count} 首歌曲，{_globalDiffs.Count} 种难度组合";
        }

        private static string DifficultyLabel(string diff) => diff switch
        {
            "Easy" => "Easy",
            "Normal" => "Normal",
            "Hard" => "Hard",
            "Expert" => "Expert",
            "ExpertPlus" => "Expert+",
            _ => diff
        };

        private void BuildSongRow(int i)
        {
            var map = _maps[i];
            string songName = map.Metadata?.SongName ?? map.Name ?? "Unknown";
            double bpm = map.Metadata?.Bpm ?? 0;
            string bsr = GetBestStars(map);
            string mapper = map.Metadata?.LevelAuthorName ?? map.Uploader?.Name ?? "";

            _songGrid.Rows.Add();
            var row = _songGrid.Rows[i];
            row.Cells["SongName"].Value = songName;
            row.Cells["BSR"].Value = bsr;
            row.Cells["BPM"].Value = bpm > 0 ? bpm.ToString("F0") : "";
            row.Cells["Cover"].Value = null;
            row.Cells["Mapper"].Value = mapper;

            // 遍历所有难度相关的列（包括模式标签列和难度复选框列）
            for (int colIdx = 5; colIdx < _songGrid.Columns.Count; colIdx++)
            {
                var col = _songGrid.Columns[colIdx];
                if (col.Name.StartsWith("label_"))
                {
                    // 模式标签列：显示模式名称
                    var characteristic = col.Name.Substring(6);
                    row.Cells[colIdx].Value = characteristic;
                    row.Cells[colIdx].ReadOnly = true;
                }
                else if (col.Name.StartsWith("diff_"))
                {
                    // 难度复选框列
                    ParseDiffColumnName(col.Name, out string characteristic, out string difficulty);
                    bool hasDiff = SongHasDifficulty(map, characteristic, difficulty);
                    if (!hasDiff)
                    {
                        row.Cells[colIdx].Value = null;
                        row.Cells[colIdx].ReadOnly = true;
                        row.Cells[colIdx].Style.BackColor = Color.FromArgb(240, 240, 240);
                    }
                    else
                    {
                        row.Cells[colIdx].Value = false;
                        row.Cells[colIdx].ReadOnly = false;
                    }
                }
            }
        }

        private void SetRowDiffChecked(int rowIndex, bool checkAll)
        {
            if (rowIndex < 0 || rowIndex >= _maps.Count) return;
            _selectedDiffs[rowIndex].Clear();
            var row = _songGrid.Rows[rowIndex];
            for (int colIdx = 5; colIdx < _songGrid.Columns.Count; colIdx++)
            {
                var col = _songGrid.Columns[colIdx];
                if (col.Name.StartsWith("diff_"))
                {
                    var cell = row.Cells[colIdx];
                    if (!cell.ReadOnly)
                    {
                        cell.Value = checkAll;
                        ParseDiffColumnName(col.Name, out string characteristic, out string difficulty);
                        if (checkAll)
                            _selectedDiffs[rowIndex].Add((characteristic, difficulty));
                    }
                }
            }
        }

        /// <summary>
        /// 点击难度列表头时全选/反选该列所有复选框
        /// </summary>
        private void SongGrid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex != -1) return; // 列头点击时 RowIndex 为 -1
            var col = _songGrid.Columns[e.ColumnIndex];
            if (col == null || !col.Name.StartsWith("diff_")) return;

            ParseDiffColumnName(col.Name, out string characteristic, out string difficulty);

            // 统计该列可编辑的单元格数量和已勾选数量
            int editableCount = 0, checkedCount = 0;
            for (int i = 0; i < _maps.Count; i++)
            {
                var cell = _songGrid.Rows[i].Cells[e.ColumnIndex];
                if (!cell.ReadOnly)
                {
                    editableCount++;
                    if (cell.Value is bool b && b)
                        checkedCount++;
                }
            }

            // 已勾选的超过一半则反选，否则全选
            bool checkAll = checkedCount <= editableCount / 2;

            for (int i = 0; i < _maps.Count; i++)
            {
                var cell = _songGrid.Rows[i].Cells[e.ColumnIndex];
                if (!cell.ReadOnly)
                {
                    cell.Value = checkAll;
                    if (checkAll)
                        _selectedDiffs[i].Add((characteristic, difficulty));
                    else
                        _selectedDiffs[i].Remove((characteristic, difficulty));
                }
            }

            // 更新全选复选框状态
            _selectingAll = true;
            _selectAllCheckbox.Checked = IsAllChecked();
            _selectingAll = false;
        }

        /// <summary>
        /// 检查是否所有可用难度都被勾选
        /// </summary>
        private bool IsAllChecked()
        {
            for (int i = 0; i < _maps.Count; i++)
            {
                for (int colIdx = 5; colIdx < _songGrid.Columns.Count; colIdx++)
                {
                    var col = _songGrid.Columns[colIdx];
                    if (col.Name.StartsWith("diff_"))
                    {
                        var cell = _songGrid.Rows[i].Cells[colIdx];
                        if (!cell.ReadOnly && !(cell.Value is bool b && b))
                            return false;
                    }
                }
            }
            return true;
        }

        private void SongGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _maps.Count) return;
            if (e.ColumnIndex < 5) return;
            var col = _songGrid.Columns[e.ColumnIndex];
            if (col is DataGridViewCheckBoxColumn)
            {
                int songIdx = e.RowIndex;
                bool isChecked = _songGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value is bool b && b;
                ParseDiffColumnName(col.Name, out string characteristic, out string difficulty);
                if (!string.IsNullOrEmpty(characteristic) && !string.IsNullOrEmpty(difficulty))
                {
                    if (isChecked)
                        _selectedDiffs[songIdx].Add((characteristic, difficulty));
                    else
                        _selectedDiffs[songIdx].Remove((characteristic, difficulty));
                }
            }
        }

        private static async Task<Image> LoadCoverAsync(BeatSaverMap map)
        {
            try
            {
                var url = map.GetCoverUrl();
                if (!string.IsNullOrEmpty(url))
                {
                    using var client = new System.Net.Http.HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var data = await client.GetByteArrayAsync(url);
                    using var ms = new MemoryStream(data);
                    return Image.FromStream(ms);
                }
            }
            catch { }
            return null;
        }

        private static string MakeDiffColumnName(string characteristic, string difficulty) => $"diff_{characteristic}_{difficulty}";

        private static void ParseDiffColumnName(string colName, out string characteristic, out string difficulty)
        {
            characteristic = "";
            difficulty = "";
            if (colName.StartsWith("diff_"))
            {
                var parts = colName.Substring(5).Split(new[] { '_' }, 2);
                if (parts.Length == 2) { characteristic = parts[0]; difficulty = parts[1]; }
            }
        }

        private bool SongHasDifficulty(BeatSaverMap map, string characteristic, string difficulty)
        {
            var version = map.Versions?.FirstOrDefault();
            return version?.Diffs?.Any(d =>
                d.Characteristic?.Equals(characteristic, StringComparison.OrdinalIgnoreCase) == true &&
                d.Difficulty?.Equals(difficulty, StringComparison.OrdinalIgnoreCase) == true) ?? false;
        }

        private string GetBestStars(BeatSaverMap map)
        {
            var version = map.Versions?.FirstOrDefault();
            if (version?.Diffs == null) return "";
            var expertDiff = version.Diffs.FirstOrDefault(d =>
                d.Difficulty?.Equals("Expert", StringComparison.OrdinalIgnoreCase) == true &&
                d.Characteristic?.Equals("Standard", StringComparison.OrdinalIgnoreCase) == true);
            if (expertDiff?.Stars != null) return expertDiff.Stars.Value.ToString("F1");
            var expDiff = version.Diffs.FirstOrDefault(d =>
                d.Difficulty?.Equals("ExpertPlus", StringComparison.OrdinalIgnoreCase) == true &&
                d.Characteristic?.Equals("Standard", StringComparison.OrdinalIgnoreCase) == true);
            if (expDiff?.Stars != null) return expDiff.Stars.Value.ToString("F1");
            if (expertDiff?.BlStars != null) return expertDiff.BlStars.Value.ToString("F1");
            return "";
        }
    }
}
