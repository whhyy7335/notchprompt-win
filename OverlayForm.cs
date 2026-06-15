using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using Forms = System.Windows.Forms;

namespace NotchPromptWin;

public sealed class OverlayForm : Forms.Form
{
    private readonly PrompterModel _model;
    private readonly PromptSurface _surface;
    private readonly Forms.Panel _toolbar = new();
    private readonly Forms.Timer _tick = new() { Interval = 33 };
    private readonly Forms.Timer _countdownTimer = new() { Interval = 1000 };
    private DateTime _lastTick = DateTime.UtcNow;
    private double _phase;
    private int _countdownRemaining;
    private bool _countdownActive;
    private bool _isClosing;

    public event EventHandler? SettingsRequested;

    protected override bool ShowWithoutActivation => true;

    public OverlayForm(PrompterModel model)
    {
        _model = model;
        _surface = new PromptSurface(_model);

        Text = "NotchPrompt Win";
        FormBorderStyle = Forms.FormBorderStyle.None;
        StartPosition = Forms.FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.Black;
        ForeColor = Color.White;
        Width = (int)_model.OverlayWidth;
        Height = (int)_model.OverlayHeight;
        DoubleBuffered = true;

        BuildUi();
        Reposition();
        ApplyPrivacyMode();

        _tick.Tick += (_, _) => Tick();
        _tick.Start();
        _countdownTimer.Tick += (_, _) => CountdownTick();
        _model.PropertyChanged += OnModelChanged;
        _model.ResetRequested += (_, _) => ResetPhase();
        _model.JumpBackRequested += (_, distance) =>
        {
            _model.IsRunning = false;
            _phase = Math.Max(0, _phase - distance);
            PaintNow();
        };
        DoubleClick += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        _surface.DoubleClick += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        MouseWheel += HandleMouseWheel;
        _surface.MouseWheel += HandleMouseWheel;
        Resize += (_, _) => LayoutOverlay();
    }

    protected override Forms.CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_NOACTIVATE;
            return cp;
        }
    }

    public void ApplyVisibility()
    {
        Visible = _model.IsOverlayVisible;
    }

    private void BuildUi()
    {
        _toolbar.Dock = Forms.DockStyle.None;
        _toolbar.Height = 38;
        _toolbar.BackColor = Color.Black;
        _toolbar.Left = 0;
        _toolbar.Top = 0;
        Controls.Add(_toolbar);

        var toolbar = new Forms.FlowLayoutPanel
        {
            Dock = Forms.DockStyle.Fill,
            Height = 38,
            Padding = new Forms.Padding(12, 6, 0, 0),
            BackColor = Color.Black,
            WrapContents = false
        };
        toolbar.Controls.Add(Button("Start", 62, () => _model.ToggleRunning()));
        toolbar.Controls.Add(Button("Back 5s", 72, () => _model.JumpBack(TimeSpan.FromSeconds(5))));
        toolbar.Controls.Add(Button("Reset", 62, () => _model.Reset()));
        toolbar.Controls.Add(Button("Slower", 66, () => _model.AdjustSpeed(-5)));
        toolbar.Controls.Add(Button("Faster", 66, () => _model.AdjustSpeed(5)));
        toolbar.Controls.Add(Button("Edit", 58, () => SettingsRequested?.Invoke(this, EventArgs.Empty)));
        toolbar.Controls.Add(Button("Quit", 54, () => Forms.Application.Exit()));
        _toolbar.Controls.Add(toolbar);

        _surface.Dock = Forms.DockStyle.None;
        Controls.Add(_surface);
        LayoutOverlay();
    }

    private void LayoutOverlay()
    {
        _toolbar.Width = ClientSize.Width;
        _toolbar.Height = 38;
        _toolbar.Left = 0;
        _toolbar.Top = 0;

        _surface.Left = 0;
        _surface.Top = _toolbar.Bottom;
        _surface.Width = ClientSize.Width;
        _surface.Height = Math.Max(1, ClientSize.Height - _toolbar.Height);
        _surface.InvalidateTextLayout();
    }

    private static Forms.Button Button(string text, int width, Action action)
    {
        var button = new Forms.Button
        {
            Text = text,
            Width = width,
            Height = 25,
            Margin = new Forms.Padding(0, 0, 7, 0),
            FlatStyle = Forms.FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(35, 35, 35)
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
        button.Click += (_, _) => action();
        return button;
    }

    private void OnModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PrompterModel.Script):
                ResetPhase();
                _surface.InvalidateTextLayout();
                break;
            case nameof(PrompterModel.IsRunning):
                if (_model.IsRunning) StartCountdownOrRun();
                else StopCountdown();
                PaintNow();
                break;
            case nameof(PrompterModel.IsOverlayVisible):
                ApplyVisibility();
                break;
            case nameof(PrompterModel.PrivacyMode):
                ApplyPrivacyMode();
                break;
            case nameof(PrompterModel.OverlayWidth):
            case nameof(PrompterModel.OverlayHeight):
                Width = (int)_model.OverlayWidth;
                Height = (int)_model.OverlayHeight;
                LayoutOverlay();
                _surface.InvalidateTextLayout();
                Reposition();
                PaintNow();
                break;
            case nameof(PrompterModel.FontSize):
                _surface.InvalidateTextLayout();
                PaintNow();
                break;
            case nameof(PrompterModel.StopAtEnd):
                PaintNow();
                break;
        }
    }

    private void StartCountdownOrRun()
    {
        if (_model.CountdownSeconds <= 0)
        {
            _lastTick = DateTime.UtcNow;
            return;
        }

        _countdownRemaining = _model.CountdownSeconds;
        _countdownActive = true;
        _surface.SetCountdown(_countdownActive, _countdownRemaining);
        _countdownTimer.Start();
        PaintNow();
    }

    private void CountdownTick()
    {
        _countdownRemaining--;
        if (_countdownRemaining <= 0)
        {
            StopCountdown();
            _lastTick = DateTime.UtcNow;
            return;
        }

        _surface.SetCountdown(_countdownActive, _countdownRemaining);
        PaintNow();
    }

    private void StopCountdown()
    {
        _countdownActive = false;
        _countdownTimer.Stop();
        _surface.SetCountdown(false, 0);
        PaintNow();
    }

    private void Tick()
    {
        var now = DateTime.UtcNow;
        var dt = Math.Clamp((now - _lastTick).TotalSeconds, 0, 0.15);
        _lastTick = now;

        if (_model.IsRunning && !_countdownActive && _model.Script.Trim().Length > 0)
        {
            _phase += _model.Speed * dt;
            var viewport = Math.Max(_surface.ClientSize.Height - PromptSurface.TopInset - 10, 1);
            var contentHeight = Math.Max(_surface.ContentHeight, 1);

            if (_model.StopAtEnd && _phase >= Math.Max(0, contentHeight - viewport))
            {
                _phase = Math.Max(0, contentHeight - viewport);
                _model.IsRunning = false;
            }

            PaintNow();
        }
        else if (!_model.IsRunning || _countdownActive)
        {
            PaintNow();
        }
    }

    private void ResetPhase()
    {
        StopCountdown();
        _phase = 0;
        _lastTick = DateTime.UtcNow;
        _surface.InvalidateTextLayout();
        PaintNow();
    }

    private void PaintNow()
    {
        _surface.Phase = _phase;
        _surface.Invalidate();
    }

    private void HandleMouseWheel(object? sender, Forms.MouseEventArgs e)
    {
        _model.IsRunning = false;
        _phase = Math.Max(0, _phase - e.Delta * 0.45);
        PaintNow();
    }

    private void Reposition()
    {
        var area = Forms.Screen.PrimaryScreen?.WorkingArea ?? Forms.Screen.AllScreens[0].WorkingArea;
        Left = area.Left + (area.Width - Width) / 2;
        Top = area.Top;
    }

    private void ApplyPrivacyMode()
    {
        if (!IsHandleCreated) return;
        Win32.SetWindowDisplayAffinity(Handle, _model.PrivacyMode ? Win32.WDA_EXCLUDEFROMCAPTURE : Win32.WDA_NONE);
    }

    protected override void OnFormClosing(Forms.FormClosingEventArgs e)
    {
        if (!_isClosing && e.CloseReason == Forms.CloseReason.UserClosing)
        {
            e.Cancel = true;
            _model.IsOverlayVisible = false;
            return;
        }
        base.OnFormClosing(e);
    }

    public new void Close()
    {
        _isClosing = true;
        base.Close();
    }

    private sealed class PromptSurface : Forms.Panel
    {
        public const int TopInset = 20;
        private const int HorizontalInset = 18;

        private readonly PrompterModel _model;
        private readonly StringFormat _scriptFormat = new()
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near,
            FormatFlags = StringFormatFlags.LineLimit
        };
        private string? _measuredText;
        private int _measuredWidth;
        private float _measuredFontSize;
        private double _contentHeight = 1;
        private bool _countdownActive;
        private int _countdownRemaining;

        public double Phase { get; set; }
        public double ContentHeight => _contentHeight;

        public PromptSurface(PrompterModel model)
        {
            _model = model;
            BackColor = Color.Black;
            ForeColor = Color.White;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        public void SetCountdown(bool active, int remaining)
        {
            _countdownActive = active;
            _countdownRemaining = remaining;
        }

        public void InvalidateTextLayout()
        {
            _measuredText = null;
            Invalidate();
        }

        protected override void OnResize(EventArgs eventargs)
        {
            InvalidateTextLayout();
            base.OnResize(eventargs);
        }

        protected override void OnPaint(Forms.PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            e.Graphics.Clear(Color.Black);

            var text = _model.Script;
            var trimmed = text.Trim();
            var textRect = new RectangleF(HorizontalInset, TopInset, Math.Max(10, ClientSize.Width - (HorizontalInset * 2)), 20000);
            using var scriptFont = new Font("Consolas", (float)_model.FontSize, FontStyle.Regular, GraphicsUnit.Point);
            EnsureMeasured(e.Graphics, text, textRect.Width, scriptFont);

            if (trimmed.Length == 0)
            {
                DrawStatus(e.Graphics, "No script yet.\nClick Edit, double-click here, or use the tray to paste text.");
            }

            if (trimmed.Length > 0)
            {
                using var brush = new SolidBrush(Color.White);
                var cycle = Math.Max(_contentHeight + 42, 1);
                var offset = _model.StopAtEnd ? Phase : Phase % cycle;
                var y = TopInset - (float)offset;
                e.Graphics.DrawString(text, scriptFont, brush, new RectangleF(HorizontalInset, y, textRect.Width, 20000), _scriptFormat);

                if (!_model.StopAtEnd && _contentHeight > ClientSize.Height)
                {
                    e.Graphics.DrawString(text, scriptFont, brush, new RectangleF(HorizontalInset, y + (float)cycle, textRect.Width, 20000), _scriptFormat);
                }
            }

            if (_countdownActive)
            {
                using var overlay = new SolidBrush(Color.FromArgb(235, 0, 0, 0));
                e.Graphics.FillRectangle(overlay, ClientRectangle);
                using var font = new Font("Segoe UI", 52, FontStyle.Bold, GraphicsUnit.Point);
                using var brush = new SolidBrush(Color.White);
                var layout = ClientRectangle;
                using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(_countdownRemaining.ToString(), font, brush, layout, format);
            }
        }

        private void EnsureMeasured(Graphics graphics, string text, float width, Font font)
        {
            var intWidth = Math.Max(1, (int)Math.Round(width));
            if (_measuredText == text && _measuredWidth == intWidth && Math.Abs(_measuredFontSize - font.Size) < 0.01)
            {
                return;
            }

            var measured = graphics.MeasureString(text.Length == 0 ? " " : text, font, intWidth, _scriptFormat);
            _contentHeight = Math.Max(1, measured.Height + TopInset);
            _measuredText = text;
            _measuredWidth = intWidth;
            _measuredFontSize = font.Size;
        }

        private void DrawStatus(Graphics graphics, string message)
        {
            using var font = new Font("Segoe UI", Math.Max(12, (float)_model.FontSize * 0.68f), FontStyle.Regular, GraphicsUnit.Point);
            using var brush = new SolidBrush(Color.FromArgb(190, 255, 255, 255));
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            graphics.DrawString(message, font, brush, ClientRectangle, format);
        }
    }
}
