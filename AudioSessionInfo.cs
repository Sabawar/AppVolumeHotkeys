namespace AppVolumeHotkeys;

public sealed record AudioSessionInfo(
    int ProcessId,
    string ProcessName,
    string DisplayName,
    int VolumePercent,
    bool IsMuted);

public sealed record AudioActionResult(
    bool Changed,
    int VolumePercent,
    bool IsMuted);
