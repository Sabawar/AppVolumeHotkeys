namespace AppVolumeHotkeys;

internal static class AppPaths
{
    public static string DataDirectory
    {
        get
        {
            var executablePath = Environment.ProcessPath;
            var executableDirectory = string.IsNullOrWhiteSpace(executablePath)
                ? AppContext.BaseDirectory
                : Path.GetDirectoryName(executablePath);
            return string.IsNullOrWhiteSpace(executableDirectory)
                ? AppContext.BaseDirectory
                : executableDirectory;
        }
    }

    public static string SettingsPath => Path.Combine(DataDirectory, "settings.json");
    public static string KeyboardLogPath => Path.Combine(DataDirectory, "keyboard.log");

    public static string LegacySettingsPath
    {
        get
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return string.IsNullOrWhiteSpace(basePath)
                ? string.Empty
                : Path.Combine(basePath, "AppVolumeHotkeys", "settings.json");
        }
    }
}
