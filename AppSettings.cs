using System.Text.Json;

namespace AppVolumeHotkeys;

internal sealed class AppSettings
{
    public string TargetProcessName { get; set; } = string.Empty;
    public int VolumeStepPercent { get; set; } = 5;
    public string Language { get; set; } = string.Empty;
    public bool StartWithWindows { get; set; }
    public bool RouteHardwareVolumeKeysToActiveProfile { get; set; }
    public bool LogKeyboardEvents { get; set; }
    public HotkeySetting VolumeUp { get; set; } = HotkeySetting.FromDefinition(new HotkeyDefinition(Keys.Up, HotkeyModifiers.Control | HotkeyModifiers.Alt));
    public HotkeySetting VolumeDown { get; set; } = HotkeySetting.FromDefinition(new HotkeyDefinition(Keys.Down, HotkeyModifiers.Control | HotkeyModifiers.Alt));
    public HotkeySetting ToggleMute { get; set; } = HotkeySetting.FromDefinition(new HotkeyDefinition(Keys.M, HotkeyModifiers.Control | HotkeyModifiers.Alt));
    public List<AppProfile> Profiles { get; set; } = [];
}

internal sealed class AppProfile
{
    public string ProcessName { get; set; } = string.Empty;
    public int VolumeStepPercent { get; set; } = 5;
    public bool IsHotkeyTarget { get; set; } = true;
    public HotkeySetting VolumeUp { get; set; } = HotkeySetting.Empty();
    public HotkeySetting VolumeDown { get; set; } = HotkeySetting.Empty();
    public HotkeySetting ToggleMute { get; set; } = HotkeySetting.Empty();
}

internal sealed class HotkeySetting
{
    public Keys Key { get; set; }
    public HotkeyModifiers Modifiers { get; set; }

    public HotkeyDefinition ToDefinition()
    {
        return new HotkeyDefinition(Key, Modifiers);
    }

    public static HotkeySetting FromDefinition(HotkeyDefinition definition)
    {
        return new HotkeySetting
        {
            Key = definition.Key,
            Modifiers = definition.Modifiers
        };
    }

    public static HotkeySetting Empty()
    {
        return new HotkeySetting
        {
            Key = Keys.None,
            Modifiers = HotkeyModifiers.None
        };
    }
}

internal static class AppSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            var path = GetSettingsPath();
            if (!File.Exists(path))
            {
                var legacyPath = AppPaths.LegacySettingsPath;
                if (!string.IsNullOrWhiteSpace(legacyPath) && File.Exists(legacyPath))
                {
                    var migrated = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(legacyPath)) ?? new AppSettings();
                    Save(migrated);
                    return migrated;
                }

                return new AppSettings();
            }

            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        var path = GetSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(settings, SerializerOptions));
    }

    private static string GetSettingsPath()
    {
        return AppPaths.SettingsPath;
    }
}
