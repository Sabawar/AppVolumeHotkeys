# App Volume Hotkeys

Portable Windows tray app for controlling per-application volume with global hotkeys, a keyboard volume wheel/button, and a small overlay.

## Features

- Control selected app audio sessions instead of Windows master volume.
- Three shared global hotkeys: volume up, volume down, mute/unmute.
- Apply one hotkey to one app or several checked apps at once.
- Optional routing of a special hardware volume control button/wheel to the active checked app.
- Topmost overlay with app name, action, and resulting volume percent.
- Portable settings and keyboard logs stored next to `AppVolumeHotkeys.exe`.
- UI language selection: system default, English, Russian, Chinese, German, Spanish, French, Portuguese.
- Self-contained single-file Windows build, no separate .NET runtime installation required.
- Automatic update checks against GitHub Releases at startup and manual update checks from the tray menu.
- One-click self-update: when a newer release is found, the app can download the latest exe, replace itself, and restart.
- Embedded application icon for Explorer, taskbar, tray, and window usage.
- Guided setup for the special volume control button using temporary keyboard logging and automatic key detection.

## Author

Created by Saba.

## Downloads

GitHub Actions builds a `win-x64` self-contained artifact on every push and pull request. Pushing a tag like `v1.1.0` automatically creates a GitHub Release with generated notes and uploads `AppVolumeHotkeys.exe`.

## Documentation

- [English manual](docs/BUILD.en.md)
- [Русская инструкция](docs/BUILD.ru.md)

## Quick Start

1. Run `AppVolumeHotkeys.exe`.
2. Start audio in the target app once so Windows creates an audio session.
3. Click refresh.
4. Select a process and add it to targets.
5. Check the target apps that should react to the three shared hotkeys.
6. Set hotkeys and minimize the app to tray.

Settings and logs are stored in the same folder as the executable:

- `settings.json`
- `keyboard.log`

## Fullscreen Overlay Note

The overlay is a topmost/no-activate window and is repinned while visible. It should work over normal apps and borderless/windowed fullscreen games. True exclusive fullscreen can block regular Windows overlays; guaranteed drawing over exclusive fullscreen requires render injection or a dedicated game overlay API.

## License

MIT. See [LICENSE](LICENSE).
