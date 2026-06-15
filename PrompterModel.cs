using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace NotchPromptWin;

public sealed class PrompterModel : INotifyPropertyChanged
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NotchPromptWin");
    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    private string _script = "Paste your script here.\n\nTip: use the tray icon or Alt+Ctrl+P to start.";
    private bool _isRunning;
    private bool _isOverlayVisible = true;
    private bool _privacyMode = true;
    private double _speed = 85;
    private double _fontSize = 24;
    private double _overlayWidth = 680;
    private double _overlayHeight = 170;
    private int _countdownSeconds = 0;
    private bool _stopAtEnd = true;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? Changed;
    public event EventHandler? ResetRequested;
    public event EventHandler<double>? JumpBackRequested;

    public string Script { get => _script; set => Set(ref _script, value); }
    public bool IsRunning { get => _isRunning; set => Set(ref _isRunning, value); }
    public bool IsOverlayVisible { get => _isOverlayVisible; set => Set(ref _isOverlayVisible, value); }
    public bool PrivacyMode { get => _privacyMode; set => Set(ref _privacyMode, value); }
    public double Speed { get => _speed; set => Set(ref _speed, Math.Clamp(Math.Round(value / 5) * 5, 10, 300)); }
    public double FontSize { get => _fontSize; set => Set(ref _fontSize, Math.Clamp(value, 12, 56)); }
    public double OverlayWidth { get => _overlayWidth; set => Set(ref _overlayWidth, Math.Clamp(value, 420, 1200)); }
    public double OverlayHeight { get => _overlayHeight; set => Set(ref _overlayHeight, Math.Clamp(value, 120, 320)); }
    public int CountdownSeconds { get => _countdownSeconds; set => Set(ref _countdownSeconds, Math.Clamp(value, 0, 10)); }
    public bool StopAtEnd { get => _stopAtEnd; set => Set(ref _stopAtEnd, value); }

    public void ToggleRunning() => IsRunning = !IsRunning;
    public void AdjustSpeed(double delta) => Speed += delta;

    public void Reset()
    {
        IsRunning = false;
        ResetRequested?.Invoke(this, EventArgs.Empty);
    }

    public void JumpBack(TimeSpan duration)
    {
        var distance = Math.Max(0, duration.TotalSeconds * Speed);
        JumpBackRequested?.Invoke(this, distance);
    }

    public static PrompterModel Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new PrompterModel();
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<PrompterModel>(json) ?? new PrompterModel();
        }
        catch
        {
            return new PrompterModel();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
