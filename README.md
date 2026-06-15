# notchprompt-win

An unofficial Windows remake of [Notchprompt](https://github.com/saif0200/notchprompt): a lightweight teleprompter that stays near the top of your screen for presentations, recordings, and live demos.

This project is not affiliated with or endorsed by the original Notchprompt project. It is a community Windows version inspired by Saif's macOS app.

## Features

- Windows system tray workflow.
- Top-centered, always-on-top teleprompter overlay.
- Start, pause, reset, and jump back 5 seconds.
- Adjustable speed, countdown, font size, overlay width, and overlay height.
- Optional stop-at-end mode.
- Import and export plain text scripts.
- Global shortcuts for hands-off control while presenting.
- Best-effort privacy mode using `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)` where Windows and the capture app support it.

## Shortcuts

| Shortcut | Action |
| --- | --- |
| `F8` or `Ctrl+Alt+P` | Start / pause |
| `F9` or `Ctrl+Alt+R` | Reset scroll |
| `F7` or `Ctrl+Alt+J` | Jump back 5 seconds |
| `Ctrl+Alt+H` | Toggle privacy mode |
| `Ctrl+Alt+O` | Toggle overlay |
| `Ctrl+Alt+=` | Increase speed |
| `Ctrl+Alt+-` | Decrease speed |

## Usage

Run `NotchPromptWin.exe`. The app opens a settings window and starts a top overlay.

- Paste or type your script in the settings window.
- Click `Start` or press `F8` to begin.
- Click `Reset` or press `F9` to return to the first line.
- Click `Edit`, double-click the overlay, or double-click the tray icon to reopen settings.

## Build From Source

Requirements:

- Windows
- .NET 8 SDK with Windows Desktop support

Build and publish:

```powershell
dotnet restore .\NotchPromptWin.csproj --configfile .\NuGet.Config
dotnet publish .\NotchPromptWin.csproj -c Release --no-restore -o .\dist\NotchPromptWin
```

Run:

```powershell
.\dist\NotchPromptWin\NotchPromptWin.exe
```

## Attribution

This project was inspired by [saif0200/notchprompt](https://github.com/saif0200/notchprompt), the original macOS Notchprompt app.

The Windows implementation is separate and uses C#/.NET WinForms plus Win32 APIs for the tray app, overlay window, shortcuts, and capture-exclusion behavior.

## License

MIT. See [LICENSE](LICENSE).

Original Notchprompt attribution and license information is included in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
