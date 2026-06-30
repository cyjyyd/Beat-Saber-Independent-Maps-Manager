using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using BeatSaberIndependentMapsManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BSIMM.Avalonia.Views
{
    public class MapPreviewControl : Control
    {
        private List<PreviewNote> _notes = new();
        private List<PreviewObstacle> _obstacles = new();
        private double _bpm = 120;
        private double _currentTime = 0;
        private double _duration = 0;
        private double _njs = 16;
        private double _noteJumpStartBeatOffset = 0;
        private bool _isPlaying = false;
        private DateTime _lastTick;
        private DispatcherTimer? _timer;

        public static readonly StyledProperty<double> ProgressProperty =
            AvaloniaProperty.Register<MapPreviewControl, double>(nameof(Progress));

        public double Progress
        {
            get => GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public event EventHandler<double>? ProgressChanged;

        public MapPreviewControl()
        {
        }

        public void LoadData(MapPreviewData data)
        {
            _notes = data.Notes.OrderBy(n => n.Beat).ToList();
            _obstacles = data.Obstacles.OrderBy(o => o.Beat).ToList();
            _bpm = data.Bpm;
            _njs = data.Difficulties.Find(d => d.Difficulty == data.SelectedDifficulty)?.Njs ?? 16;
            _noteJumpStartBeatOffset = data.Difficulties.Find(d => d.Difficulty == data.SelectedDifficulty)?.NoteJumpStartBeatOffset ?? 0;

            if (_notes.Count > 0)
                _duration = _notes.Max(n => n.Beat) / _bpm * 60.0;
            else
                _duration = 0;

            _currentTime = 0;
            Progress = 0;
            InvalidateVisual();
        }

        public void Play()
        {
            _isPlaying = true;
            _lastTick = DateTime.Now;
            _timer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        public void Pause()
        {
            _isPlaying = false;
            if (_timer != null) _timer.Stop();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (!_isPlaying) return;

            var now = DateTime.Now;
            double delta = (now - _lastTick).TotalSeconds;
            _lastTick = now;

            _currentTime += delta;
            if (_currentTime >= _duration)
            {
                _currentTime = _duration;
                _isPlaying = false;
                _timer?.Stop();
            }

            Progress = _duration > 0 ? _currentTime / _duration * 100 : 0;
            ProgressChanged?.Invoke(this, Progress);
            InvalidateVisual();
        }

        public void Stop()
        {
            _isPlaying = false;
            _timer?.Stop();
            _currentTime = 0;
            Progress = 0;
            ProgressChanged?.Invoke(this, 0);
            InvalidateVisual();
        }

        public void Seek(double time)
        {
            _currentTime = Math.Max(0, Math.Min(time, _duration));
            Progress = _duration > 0 ? _currentTime / _duration * 100 : 0;
            InvalidateVisual();
        }

        public double Duration => _duration;
        public double CurrentTime => _currentTime;
        public bool IsPlaying => _isPlaying;

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var bounds = Bounds;
            double w = bounds.Width;
            double h = bounds.Height;
            if (w < 10 || h < 10) return;

            double cx = w / 2;
            double cy = h / 2;

            double halfJumpDuration = _njs > 0 ? 60.0 / _bpm * 4.0 / _njs : 0.5;
            double jumpDuration = halfJumpDuration * 2;
            double previewDuration = jumpDuration * 2;

            DrawGrid(context, cx, h, w);
            DrawObstacles(context, cx, h, w, previewDuration);
            DrawNotes(context, cx, h, w, previewDuration);
        }

        private void DrawGrid(DrawingContext context, double cx, double h, double w)
        {
            double nearY = h - 20;
            double farY = 20;
            double nearHalfW = Math.Min(w * 0.45, 200);
            double farHalfW = nearHalfW * 0.35;

            var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(60, 128, 128, 128)), 1);
            var borderPen = new Pen(new SolidColorBrush(Color.FromArgb(100, 160, 160, 180)), 1.5);

            for (int i = 0; i <= 4; i++)
            {
                double ratio = i / 4.0;
                double nearX = cx - nearHalfW + ratio * nearHalfW * 2;
                double farX = cx - farHalfW + ratio * farHalfW * 2;
                context.DrawLine(gridPen, new Point(nearX, nearY), new Point(farX, farY));
            }

            for (int layer = 0; layer <= 3; layer++)
            {
                double t = layer / 3.0;
                double y = nearY + (farY - nearY) * t;
                double halfW = nearHalfW + (farHalfW - nearHalfW) * t;
                context.DrawLine(gridPen, new Point(cx - halfW, y), new Point(cx + halfW, y));
            }

            context.DrawLine(borderPen, new Point(cx - nearHalfW, nearY), new Point(cx + nearHalfW, nearY));
        }

        private (double sx, double sy, double size) ProjectNote(int x, int y, double z, double cx, double h, double w)
        {
            double nearY = h - 20;
            double farY = 20;
            double nearHalfW = Math.Min(w * 0.45, 200);
            double farHalfW = nearHalfW * 0.35;

            double t = Math.Max(0, Math.Min(1, z));
            double screenY = nearY + (farY - nearY) * t;
            double halfW = nearHalfW + (farHalfW - nearHalfW) * t;

            double colRatio = (x + 0.5) / 4.0;
            double rowRatio = (y + 0.5) / 3.0;

            double cellW = halfW * 2 / 4.0;
            double cellH = (nearY - farY) / 3.0 * (1 - t * 0.5);

            double sx = cx - halfW + colRatio * halfW * 2;
            double sy = screenY - rowRatio * cellH;
            double size = cellW * (1 - t * 0.5) * 0.8;

            return (sx, sy, size);
        }

        private void DrawNotes(DrawingContext context, double cx, double h, double w, double previewDuration)
        {
            double currentBeat = _currentTime * _bpm / 60.0;

            foreach (var note in _notes)
            {
                double beatDelta = note.Beat - currentBeat;
                if (beatDelta < -0.5 || beatDelta > previewDuration) continue;

                double z = beatDelta / previewDuration;
                if (z < 0) z = 0;
                if (z > 1) z = 1;

                var (sx, sy, size) = ProjectNote(note.X, note.Y, z, cx, h, w);
                if (size < 2) continue;

                if (note.IsBomb)
                {
                    var bombBrush = new SolidColorBrush(Color.FromArgb(200, 100, 100, 100));
                    context.DrawEllipse(bombBrush, null, new Rect(sx - size/2, sy - size/2, size, size));
                }
                else
                {
                    Color noteColor = note.Color == 0
                        ? Color.FromArgb(230, 235, 60, 60)
                        : Color.FromArgb(230, 60, 130, 255);
                    var noteBrush = new SolidColorBrush(noteColor);
                    var borderPen = new Pen(new SolidColorBrush(Colors.White), Math.Max(0.5, size * 0.05));

                    var noteRect = new Rect(sx - size/2, sy - size/2, size, size);
                    context.DrawRectangle(noteBrush, borderPen, noteRect, size * 0.1);

                    if (size > 6)
                    {
                        DrawCutDirectionArrow(context, sx, sy, size, note.CutDirection, Colors.White);
                    }
                }
            }
        }

        private void DrawCutDirectionArrow(DrawingContext context, double cx, double cy, double size, int direction, Color color)
        {
            double arrowLen = size * 0.3;
            double arrowW = size * 0.08;
            var pen = new Pen(new SolidColorBrush(color), Math.Max(1, size * 0.06));
            var brush = new SolidColorBrush(color);

            double dx = 0, dy = 0;
            switch (direction)
            {
                case 0: dy = -1; break;            // Up
                case 1: dy = 1; break;             // Down
                case 2: dx = -1; break;            // Left
                case 3: dx = 1; break;             // Right
                case 4: dx = -0.707; dy = -0.707; break;  // Up-Left
                case 5: dx = 0.707; dy = -0.707; break;   // Up-Right
                case 6: dx = -0.707; dy = 0.707; break;   // Down-Left
                case 7: dx = 0.707; dy = 0.707; break;    // Down-Right
                case 8:                            // Dot
                    context.DrawEllipse(brush, null, new Rect(cx - arrowLen/2, cy - arrowLen/2, arrowLen, arrowLen));
                    return;
            }

            double startX = cx - dx * arrowLen;
            double startY = cy - dy * arrowLen;
            double endX = cx + dx * arrowLen;
            double endY = cy + dy * arrowLen;

            context.DrawLine(pen, new Point(startX, startY), new Point(endX, endY));

            double aax = dy * arrowW;
            double aay = -dx * arrowW;
            var arrowHead = new StreamGeometry();
            using (var ctx = arrowHead.Open())
            {
                ctx.BeginFigure(new Point(endX, endY), true);
                ctx.LineTo(new Point(endX - dx * arrowLen * 0.5 + aax, endY - dy * arrowLen * 0.5 + aay));
                ctx.LineTo(new Point(endX - dx * arrowLen * 0.5 - aax, endY - dy * arrowLen * 0.5 - aay));
                ctx.EndFigure(true);
            }
            context.DrawGeometry(brush, null, arrowHead);
        }

        private void DrawObstacles(DrawingContext context, double cx, double h, double w, double previewDuration)
        {
            double currentBeat = _currentTime * _bpm / 60.0;
            var obsBrush = new SolidColorBrush(Color.FromArgb(80, 200, 100, 100));

            foreach (var obs in _obstacles)
            {
                double startBeat = obs.Beat;
                double endBeat = obs.Beat + obs.Duration;

                if (endBeat < currentBeat || startBeat > currentBeat + previewDuration) continue;

                double startZ = (startBeat - currentBeat) / previewDuration;
                double endZ = (endBeat - currentBeat) / previewDuration;
                startZ = Math.Max(0, Math.Min(1, startZ));
                endZ = Math.Max(0, Math.Min(1, endZ));

                for (int col = obs.X; col < obs.X + obs.Width && col < 4; col++)
                {
                    for (int row = obs.Y; row < obs.Y + obs.Height && row < 3; row++)
                    {
                        var (sx1, sy1, size1) = ProjectNote(col, row, startZ, cx, h, w);
                        var (sx2, sy2, size2) = ProjectNote(col, row, endZ, cx, h, w);

                        double left = Math.Min(sx1 - size1/2, sx2 - size2/2);
                        double right = Math.Max(sx1 + size1/2, sx2 + size2/2);
                        double top = Math.Min(sy1 - size1/2, sy2 - size2/2);
                        double bottom = Math.Max(sy1 + size1/2, sy2 + size2/2);

                        context.DrawRectangle(obsBrush, null, new Rect(left, top, right - left, bottom - top), 2);
                    }
                }
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _isPlaying = false;
            base.OnDetachedFromVisualTree(e);
        }
    }
}
