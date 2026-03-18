using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// 自定义进度条控件，支持透明背景和拖动交互
    /// </summary>
    public class ProgressBarEx : Panel
    {
        private int _value = 0;
        private int _maximum = 100;
        private int _minimum = 0;
        private bool _isDragging = false;
        private bool _suppressEvent = false;
        private Color _progressColor = Color.FromArgb(0, 122, 204);
        private Color _backgroundColor = Color.FromArgb(240, 240, 240);

        public event EventHandler ValueChanged;

        public ProgressBarEx()
        {
            this.DoubleBuffered = true;
            this.Height = 20;
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
        }

        [DefaultValue(0)]
        public int Value
        {
            get { return _value; }
            set
            {
                if (value < _minimum) value = _minimum;
                if (value > _maximum) value = _maximum;
                if (_value != value)
                {
                    _value = value;
                    this.Invalidate();
                    if (!_suppressEvent)
                    {
                        ValueChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// 设置值但不触发事件（用于程序更新）
        /// </summary>
        public void SetValueSilent(int value)
        {
            _suppressEvent = true;
            Value = value;
            _suppressEvent = false;
        }

        [DefaultValue(100)]
        public int Maximum
        {
            get { return _maximum; }
            set
            {
                if (value > _minimum)
                {
                    _maximum = value;
                    if (_value > _maximum) _value = _maximum;
                    this.Invalidate();
                }
            }
        }

        [DefaultValue(0)]
        public int Minimum
        {
            get { return _minimum; }
            set
            {
                if (value < _maximum)
                {
                    _minimum = value;
                    if (_value < _minimum) _value = _minimum;
                    this.Invalidate();
                }
            }
        }

        [DefaultValue(typeof(Color), "0, 122, 204")]
        public Color ProgressColor
        {
            get { return _progressColor; }
            set
            {
                _progressColor = value;
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 绘制背景
            using (SolidBrush bgBrush = new SolidBrush(_backgroundColor))
            {
                g.FillRectangle(bgBrush, 0, 0, this.Width, this.Height);
            }

            // 绘制进度
            if (_maximum > _minimum)
            {
                float progress = (float)(_value - _minimum) / (_maximum - _minimum);
                int progressWidth = (int)(this.Width * progress);

                if (progressWidth > 0)
                {
                    using (SolidBrush progressBrush = new SolidBrush(_progressColor))
                    {
                        g.FillRectangle(progressBrush, 0, 0, progressWidth, this.Height);
                    }
                }
            }

            // 绘制边框
            using (Pen borderPen = new Pen(Color.FromArgb(200, 200, 200)))
            {
                g.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _isDragging = true;
            UpdateValueFromMouse(e.X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging)
            {
                UpdateValueFromMouse(e.X);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isDragging = false;
        }

        private void UpdateValueFromMouse(int x)
        {
            if (this.Width > 0 && _maximum > _minimum)
            {
                float ratio = (float)x / this.Width;
                ratio = Math.Max(0, Math.Min(1, ratio)); // 限制在0-1范围内
                int newValue = _minimum + (int)((_maximum - _minimum) * ratio);
                Value = newValue;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.Invalidate();
        }
    }
}