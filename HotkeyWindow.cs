using Forms = System.Windows.Forms;

namespace NotchPromptWin;

public enum HotkeyCommand
{
    StartPause = 1,
    Reset = 2,
    JumpBack = 3,
    Privacy = 4,
    ToggleOverlay = 5,
    SpeedUp = 6,
    SpeedDown = 7
}

public sealed class HotkeyWindow : Forms.NativeWindow, IDisposable
{
    private readonly Action<HotkeyCommand> _onCommand;
    private readonly Win32.LowLevelKeyboardProc _keyboardProc;
    private IntPtr _hookHandle;
    private DateTime _lastHandledAt = DateTime.MinValue;
    private HotkeyCommand? _lastCommand;

    public HotkeyWindow(Action<HotkeyCommand> onCommand)
    {
        _onCommand = onCommand;
        _keyboardProc = HookCallback;
        CreateHandle(new Forms.CreateParams());
    }

    public void RegisterAll()
    {
        _hookHandle = Win32.SetWindowsHookEx(
            Win32.WH_KEYBOARD_LL,
            _keyboardProc,
            Win32.GetModuleHandle(null),
            0);
    }

    protected override void WndProc(ref Forms.Message m)
    {
        if (m.Msg == Win32.WM_HOTKEY && Enum.IsDefined(typeof(HotkeyCommand), m.WParam.ToInt32()))
        {
            _onCommand((HotkeyCommand)m.WParam.ToInt32());
            return;
        }
        base.WndProc(ref m);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam.ToInt32() == Win32.WM_KEYDOWN || wParam.ToInt32() == Win32.WM_SYSKEYDOWN))
        {
            var data = System.Runtime.InteropServices.Marshal.PtrToStructure<Win32.KbdLlHookStruct>(lParam);
            if (TryMap(data.VkCode, out var command, out var requiresCtrlAlt) &&
                (!requiresCtrlAlt || IsCtrlAltDown()) &&
                !IsRepeat(command))
            {
                _lastCommand = command;
                _lastHandledAt = DateTime.UtcNow;
                Forms.Application.OpenForms[0]?.BeginInvoke(() => _onCommand(command));
                return new IntPtr(1);
            }
        }

        return Win32.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private bool IsRepeat(HotkeyCommand command)
    {
        return _lastCommand == command && (DateTime.UtcNow - _lastHandledAt).TotalMilliseconds < 180;
    }

    private static bool IsCtrlAltDown()
    {
        return (Win32.GetKeyState(Win32.VK_CONTROL) & 0x8000) != 0 &&
               (Win32.GetKeyState(Win32.VK_MENU) & 0x8000) != 0;
    }

    private static bool TryMap(int vkCode, out HotkeyCommand command, out bool requiresCtrlAlt)
    {
        requiresCtrlAlt = true;
        command = vkCode switch
        {
            0x77 => HotkeyCommand.StartPause,
            0x78 => HotkeyCommand.Reset,
            0x76 => HotkeyCommand.JumpBack,
            'P' => HotkeyCommand.StartPause,
            'R' => HotkeyCommand.Reset,
            'J' => HotkeyCommand.JumpBack,
            'H' => HotkeyCommand.Privacy,
            'O' => HotkeyCommand.ToggleOverlay,
            0xBB => HotkeyCommand.SpeedUp,
            0xBD => HotkeyCommand.SpeedDown,
            _ => 0
        };
        if (vkCode is 0x76 or 0x77 or 0x78)
        {
            requiresCtrlAlt = false;
        }
        return command != 0;
    }

    public void Dispose()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            Win32.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
        DestroyHandle();
    }
}
