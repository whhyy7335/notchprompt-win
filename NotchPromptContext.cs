using System.Drawing;
using Forms = System.Windows.Forms;

namespace NotchPromptWin;

public sealed class NotchPromptContext : Forms.ApplicationContext
{
    private readonly PrompterModel _model = PrompterModel.Load();
    private readonly OverlayForm _overlay;
    private readonly HotkeyWindow _hotkeys;
    private readonly Forms.NotifyIcon _tray;
    private SettingsForm? _settings;

    public NotchPromptContext()
    {
        _overlay = new OverlayForm(_model);
        _overlay.SettingsRequested += (_, _) => ShowSettings();
        _overlay.Show();
        _overlay.ApplyVisibility();

        _hotkeys = new HotkeyWindow(command => Dispatch(command));
        _hotkeys.RegisterAll();

        _tray = BuildTray();
        _model.Changed += (_, _) => _model.Save();
        ShowSettings();
    }

    private Forms.NotifyIcon BuildTray()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Start / Pause    F8 / Ctrl+Alt+P", null, (_, _) => _model.ToggleRunning());
        menu.Items.Add("Reset Scroll     F9 / Ctrl+Alt+R", null, (_, _) => _model.Reset());
        menu.Items.Add("Jump Back 5s     F7 / Ctrl+Alt+J", null, (_, _) => _model.JumpBack(TimeSpan.FromSeconds(5)));
        menu.Items.Add("Toggle Overlay   Ctrl+Alt+O", null, (_, _) => _model.IsOverlayVisible = !_model.IsOverlayVisible);
        menu.Items.Add("Privacy Mode     Ctrl+Alt+H", null, (_, _) => _model.PrivacyMode = !_model.PrivacyMode);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Settings...", null, (_, _) => ShowSettings());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quit NotchPrompt", null, (_, _) => ExitThread());

        var tray = new Forms.NotifyIcon
        {
            Text = "NotchPrompt Win",
            Icon = SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = menu
        };
        tray.DoubleClick += (_, _) => ShowSettings();
        return tray;
    }

    private void Dispatch(HotkeyCommand command)
    {
        switch (command)
        {
            case HotkeyCommand.StartPause:
                _model.IsOverlayVisible = true;
                _model.ToggleRunning();
                break;
            case HotkeyCommand.Reset: _model.Reset(); break;
            case HotkeyCommand.JumpBack: _model.JumpBack(TimeSpan.FromSeconds(5)); break;
            case HotkeyCommand.Privacy: _model.PrivacyMode = !_model.PrivacyMode; break;
            case HotkeyCommand.ToggleOverlay: _model.IsOverlayVisible = !_model.IsOverlayVisible; break;
            case HotkeyCommand.SpeedUp: _model.AdjustSpeed(5); break;
            case HotkeyCommand.SpeedDown: _model.AdjustSpeed(-5); break;
        }
    }

    private void ShowSettings()
    {
        _settings ??= new SettingsForm(_model);
        if (_settings.IsDisposed) _settings = new SettingsForm(_model);
        _settings.Show();
        _settings.Activate();
    }

    protected override void ExitThreadCore()
    {
        _model.Save();
        _hotkeys.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _overlay.Close();
        base.ExitThreadCore();
    }
}
