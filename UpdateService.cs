using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppVolumeHotkeys;

internal sealed record UpdateCheckResult(
    bool IsUpdateAvailable,
    string CurrentVersion,
    string LatestVersion,
    string ReleaseUrl,
    string ReleaseName,
    string AssetDownloadUrl);

internal static class UpdateService
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/Sabawar/AppVolumeHotkeys/releases/latest";

    public static async Task<UpdateCheckResult> CheckLatestAsync(CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AppVolumeHotkeys");

        using var response = await client.GetAsync(LatestReleaseUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("GitHub release response was empty.");

        var current = GetCurrentVersion();
        var latest = NormalizeVersion(release.TagName);
        var hasUpdate = Version.TryParse(current, out var currentVersion)
            && Version.TryParse(latest, out var latestVersion)
            && latestVersion > currentVersion;

        return new UpdateCheckResult(
            hasUpdate,
            current,
            latest,
            release.HtmlUrl,
            string.IsNullOrWhiteSpace(release.Name) ? release.TagName : release.Name,
            release.Assets.FirstOrDefault(asset => asset.Name.Equals("AppVolumeHotkeys.exe", StringComparison.OrdinalIgnoreCase))?.BrowserDownloadUrl ?? string.Empty);
    }

    public static async Task<string> DownloadUpdateAsync(UpdateCheckResult update, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(update.AssetDownloadUrl))
        {
            throw new InvalidOperationException("Release does not contain AppVolumeHotkeys.exe.");
        }

        var targetPath = Path.Combine(Path.GetTempPath(), $"AppVolumeHotkeys-{update.LatestVersion}.exe");
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AppVolumeHotkeys");
        using var response = await client.GetAsync(update.AssetDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalLength = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(targetPath);

        var buffer = new byte[1024 * 128];
        long readTotal = 0;
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            readTotal += read;
            if (totalLength is > 0)
            {
                progress?.Report((int)Math.Clamp(readTotal * 100 / totalLength.Value, 0, 100));
            }
        }

        progress?.Report(100);
        return targetPath;
    }

    public static void InstallAndRestart(string downloadedExePath)
    {
        var currentExe = Environment.ProcessPath ?? Application.ExecutablePath;
        var scriptPath = Path.Combine(Path.GetTempPath(), $"AppVolumeHotkeys-update-{Guid.NewGuid():N}.cmd");
        var processId = Environment.ProcessId;
        var script = $"""
@echo off
setlocal
set "SRC={downloadedExePath}"
set "DST={currentExe}"
set "PID={processId}"
:wait
tasklist /FI "PID eq %PID%" | find "%PID%" >nul
if not errorlevel 1 (
  timeout /t 1 /nobreak >nul
  goto wait
)
copy /Y "%SRC%" "%DST%" >nul
start "" "%DST%"
del "%SRC%" >nul 2>nul
del "%~f0" >nul 2>nul
""";
        File.WriteAllText(scriptPath, script);
        Process.Start(new ProcessStartInfo(scriptPath) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
        Application.Exit();
    }

    private static string GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        return NormalizeVersion(version);
    }

    private static string NormalizeVersion(string? version)
    {
        version = (version ?? "0.0.0").Trim();
        if (version.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            version = version[1..];
        }

        var plusIndex = version.IndexOf('+');
        if (plusIndex >= 0)
        {
            version = version[..plusIndex];
        }

        return version;
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = [];
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
