using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace BeatSaberIndependentMapsManager
{
    /// <summary>
    /// 自定义音量条控件，支持透明背景和拖动交互
    /// </summary>
    public class VolumeBarEx : Panel
    {
        private int _value = 20;
        private int _maximum = 100;
        private int _minimum = 0;
        private bool _isDragging = false;
        private bool _suppressEvent = false;
        private Color _volumeColor = Color.FromArgb(46, 139, 87);
        private Color _backgroundColor = Color.FromArgb(240, 240, 240);

        public event EventHandler ValueChanged;

        public VolumeBarEx()
        {
            this.DoubleBuffered = true;
            this.Height = 20;
            this.Width = 120;
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
        }

        [DefaultValue(20)]
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
        /// 设置值但不触发事件
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

        [DefaultValue(typeof(Color), "46, 139, 87")]
        public Color VolumeColor
        {
            get { return _volumeColor; }
            set
            {
                _volumeColor = value;
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

            // 绘制音量条
            if (_maximum > _minimum)
            {
                float ratio = (float)(_value - _minimum) / (_maximum - _minimum);
                int volumeWidth = (int)(this.Width * ratio);

                if (volumeWidth > 0)
                {
                    using (SolidBrush volumeBrush = new SolidBrush(_volumeColor))
                    {
                        g.FillRectangle(volumeBrush, 0, 0, volumeWidth, this.Height);
                    }
                }
            }

            // 绘制边框
            using (Pen borderPen = new Pen(Color.FromArgb(200, 200, 200)))
            {
                g.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
            }

            // 绘制音量百分比文字
            string volumeText = _value + "%";
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                using (SolidBrush textBrush = new SolidBrush(this.ForeColor))
                {
                    g.DrawString(volumeText, this.Font, textBrush, this.ClientRectangle, sf);
                }
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
                ratio = Math.Max(0, Math.Min(1, ratio));
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