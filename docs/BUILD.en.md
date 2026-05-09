# Build and Usage Guide

## Requirements

- Windows 10 or newer
- .NET SDK 8 or newer for building

The published app is self-contained, so end users do not need to install .NET.

## Build

From the repository root:

```powershell
dotnet publish AppVolumeHotkeys.csproj -c Release -r win-x64 --self-contained true -o publish-self-contained /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:EnableCompressionInSingleFile=true
```

The executable will be created at:

```text
publish-self-contained\AppVolumeHotkeys.exe
```

## Usage

1. Run `AppVolumeHotkeys.exe`.
2. Play sound in the target application once.
3. Refresh the session list.
4. Add the app process to targets.
5. Check one or more target apps.
6. Set the three shared hotkeys.

The three hotkeys apply to all checked apps:

- Volume up
- Volume down
- Mute/unmute

## Special Volume Control Button

Enable `Special volume control button controls the active checked app` to intercept standard Windows volume keys. The active foreground app is controlled only when its process is checked in the target list.

## Portable Files

The app stores files next to the executable:

- `settings.json`
- `keyboard.log`

## Autostart

Autostart uses the current user's Windows Run registry key and starts the app with `--minimized`.
