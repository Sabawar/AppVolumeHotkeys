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
    string ReleaseName);

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
            string.IsNullOrWhiteSpace(release.Name) ? release.TagName : release.Name);
    }

    public static void OpenRelease(string releaseUrl)
    {
        if (string.IsNullOrWhiteSpace(releaseUrl))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(releaseUrl) { UseShellExecute = true });
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
    }
}
