using System.Drawing;
using System.IO;
using Forms = System.Windows.Forms;

namespace NotchPromptWin;

public sealed class SettingsForm : Forms.Form
{
    private readonly PrompterModel _model;
    private readonly Forms.TextBox _scriptBox = new();

    public SettingsForm(PrompterModel model)
    {
        _model = model;
        Text = "NotchPrompt Win Settings";
        Width = 820;
        Height = 720;
        MinimumSize = new Size(680, 520);
        StartPosition = Forms.FormStartPosition.CenterScreen;
        BuildUi();
    }

    private void BuildUi()
    {
        var root = new Forms.TableLayoutPanel
        {
            Dock = Forms.DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Forms.Padding(16),
            AutoScroll = true
        };
        Controls.Add(root);

        var title = new Forms.Label
        {
            Text = "NotchPrompt Win",
            Dock = Forms.DockStyle.Top,
            Height = 34,
            Font = new Font("Segoe UI", 16, FontStyle.Bold)
        };
        root.Controls.Add(title);
        root.Controls.Add(ScriptSection());
        root.Controls.Add(SettingsSection());
        root.Controls.Add(new Forms.Label
        {
            Text = "Shortcuts: F8 start/pause, F9 reset, F7 jump back. Also: Ctrl+Alt+P/R/J/H/O and Ctrl+Alt+=/-.",
            Dock = Forms.DockStyle.Top,
            Height = 34,
            ForeColor = Color.DimGray
        });
    }

    private Forms.Control ScriptSection()
    {
        var box = new Forms.GroupBox { Text = "Script", Dock = Forms.DockStyle.Top, Height = 330 };
        var panel = new Forms.TableLayoutPanel { Dock = Forms.DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Forms.Padding(8) };
        box.Controls.Add(panel);

        var toolbar = new Forms.FlowLayoutPanel { Dock = Forms.DockStyle.Top, Height = 34 };
        toolbar.Controls.Add(ActionButton("Import...", ImportScript));
        toolbar.Controls.Add(ActionButton("Export...", ExportScript));
        toolbar.Controls.Add(ActionButton("Clear", () => SetScript("")));
        panel.Controls.Add(toolbar);

        _scriptBox.Multiline = true;
        _scriptBox.AcceptsTab = true;
        _scriptBox.ScrollBars = Forms.ScrollBars.Vertical;
        _scriptBox.Dock = Forms.DockStyle.Fill;
        _scriptBox.Font = new Font("Consolas", 10);
        _scriptBox.Text = _model.Script;
        _scriptBox.TextChanged += (_, _) => _model.Script = _scriptBox.Text;
        panel.Controls.Add(_scriptBox);
        return box;
    }

    private Forms.Control SettingsSection()
    {
        var box = new Forms.GroupBox { Text = "Playback / Appearance / Window", Dock = Forms.DockStyle.Top, Height = 245 };
        var panel = new Forms.TableLayoutPanel { Dock = Forms.DockStyle.Fill, ColumnCount = 3, RowCount = 8, Padding = new Forms.Padding(8) };
        panel.ColumnStyles.Add(new Forms.ColumnStyle(Forms.SizeType.Absolute, 150));
        panel.ColumnStyles.Add(new Forms.ColumnStyle(Forms.SizeType.Percent, 100));
        panel.ColumnStyles.Add(new Forms.ColumnStyle(Forms.SizeType.Absolute, 70));
        box.Controls.Add(panel);

        AddSlider(panel, 0, "Speed", 10, 300, () => (int)_model.Speed, v => _model.Speed = v);
        AddSlider(panel, 1, "Countdown", 0, 10, () => _model.CountdownSeconds, v => _model.CountdownSeconds = v);
        AddSlider(panel, 2, "Font size", 12, 56, () => (int)_model.FontSize, v => _model.FontSize = v);
        AddSlider(panel, 3, "Overlay width", 420, 1200, () => (int)_model.OverlayWidth, v => _model.OverlayWidth = v);
        AddSlider(panel, 4, "Overlay height", 120, 320, () => (int)_model.OverlayHeight, v => _model.OverlayHeight = v);
        AddCheck(panel, 5, "Show overlay", () => _model.IsOverlayVisible, v => _model.IsOverlayVisible = v);
        AddCheck(panel, 6, "Privacy mode: exclude from capture where Windows allows it", () => _model.PrivacyMode, v => _model.PrivacyMode = v);
        AddCheck(panel, 7, "Stop at end", () => _model.StopAtEnd, v => _model.StopAtEnd = v);
        return box;
    }

    private static void AddSlider(Forms.TableLayoutPanel panel, int row, string label, int min, int max, Func<int> get, Action<int> set)
    {
        panel.Controls.Add(new Forms.Label { Text = label, Dock = Forms.DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        var value = new Forms.Label { Text = get().ToString(), Dock = Forms.DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
        var slider = new Forms.TrackBar { Minimum = min, Maximum = max, TickFrequency = Math.Max(1, (max - min) / 10), Value = get(), Dock = Forms.DockStyle.Fill };
        slider.ValueChanged += (_, _) =>
        {
            value.Text = slider.Value.ToString();
            set(slider.Value);
        };
        panel.Controls.Add(slider, 1, row);
        panel.Controls.Add(value, 2, row);
    }

    private static void AddCheck(Forms.TableLayoutPanel panel, int row, string text, Func<bool> get, Action<bool> set)
    {
        var check = new Forms.CheckBox { Text = text, Checked = get(), Dock = Forms.DockStyle.Fill, AutoSize = true };
        check.CheckedChanged += (_, _) => set(check.Checked);
        panel.SetColumnSpan(check, 3);
        panel.Controls.Add(check, 0, row);
    }

    private static Forms.Button ActionButton(string text, Action action)
    {
        var button = new Forms.Button { Text = text, AutoSize = true, Margin = new Forms.Padding(0, 0, 8, 0) };
        button.Click += (_, _) => action();
        return button;
    }

    private void SetScript(string text)
    {
        _scriptBox.Text = text;
        _model.Script = text;
    }

    private void ImportScript()
    {
        using var dialog = new Forms.OpenFileDialog
        {
            Filter = "Text files|*.txt;*.md;*.csv;*.log;*.json;*.xml|All files|*.*"
        };
        if (dialog.ShowDialog(this) == Forms.DialogResult.OK)
        {
            SetScript(File.ReadAllText(dialog.FileName));
        }
    }

    private void ExportScript()
    {
        using var dialog = new Forms.SaveFileDialog
        {
            Filter = "Text file|*.txt|Markdown|*.md|All files|*.*",
            FileName = "script.txt"
        };
        if (dialog.ShowDialog(this) == Forms.DialogResult.OK)
        {
            File.WriteAllText(dialog.FileName, _model.Script);
        }
    }
}
