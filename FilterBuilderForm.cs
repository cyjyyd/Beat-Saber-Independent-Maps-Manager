using System;
using System.Drawing;
using System.Windows.Forms;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// 筛选构建器窗口
    /// </summary>
    public class FilterBuilderForm : Form
    {
        private FilterBuilderPanel filterBuilderPanel;
        private Button btnSearch;
        private Button btnCancel;

        public FilterPreset CurrentPreset => filterBuilderPanel?.CurrentPreset;

        public event EventHandler<FilterPreset> SearchRequested;

        public FilterBuilderForm(FilterPreset preset = null)
        {
            InitializeUI(preset);
        }

        private void InitializeUI(FilterPreset preset)
        {
            this.Text = "筛选条件构建器";
            this.Size = new Size(800, 600);
            this.MinimumSize = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.ShowInTaskbar = false;

            // 创建筛选构建器面板
            filterBuilderPanel = new FilterBuilderPanel
            {
                Dock = DockStyle.Fill
            };

            // 如果传入了预设，加载它
            if (preset != null)
            {
                // 通过反射或其他方式设置预设
            }

            // 创建底部按钮面板
            Panel buttonPanel = new Panel
            {
                Height = 45,
                Dock = DockStyle.Bottom,
                Padding = new Padding(10, 5, 10, 5)
            };

            btnSearch = new Button
            {
                Text = "搜索",
                Width = 100,
                Dock = DockStyle.Right,
                Margin = new Padding(5),
                BackColor = Color.FromArgb(46, 139, 87),
                ForeColor = Color.White
            };
            btnSearch.Click += (s, e) =>
            {
                SearchRequested?.Invoke(this, CurrentPreset);
                this.Close();
            };

            btnCancel = new Button
            {
                Text = "取消",
                Width = 80,
                Dock = DockStyle.Right,
                Margin = new Padding(5)
            };
            btnCancel.Click += (s, e) => this.Close();

            buttonPanel.Controls.Add(btnSearch);
            buttonPanel.Controls.Add(btnCancel);

            // 组装窗体
            this.Controls.Add(filterBuilderPanel);
            this.Controls.Add(buttonPanel);

            // 订阅事件
            filterBuilderPanel.SearchRequested += (s, preset) =>
            {
                SearchRequested?.Invoke(this, preset);
                this.Close();
            };
        }
    }
}