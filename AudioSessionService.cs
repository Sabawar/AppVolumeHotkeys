using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AppVolumeHotkeys;

internal sealed class AudioSessionService
{
    private static readonly Guid MMDeviceEnumeratorId = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid AudioSessionManager2Id = new("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F");
    private static readonly Guid EventContext = Guid.NewGuid();

    public IReadOnlyList<AudioSessionInfo> GetSessions()
    {
        return WithSessionEnumerator(sessionEnum =>
        {
            ThrowIfFailed(sessionEnum.GetCount(out var count));
            var sessions = new List<AudioSessionInfo>();

            for (var i = 0; i < count; i++)
            {
                IAudioSessionControl? control = null;
                try
                {
                    if (sessionEnum.GetSession(i, out control) != 0 || control is null)
                    {
                        continue;
                    }

                    var control2 = (IAudioSessionControl2)control;
                    control2.GetProcessId(out var processId);

                    if (processId == 0)
                    {
                        continue;
                    }

                    var volume = (ISimpleAudioVolume)control;
                    volume.GetMasterVolume(out var level);
                    volume.GetMute(out var isMuted);
                    control.GetDisplayName(out var displayName);

                    var processName = GetProcessName((int)processId);
                    if (string.IsNullOrWhiteSpace(processName))
                    {
                        continue;
                    }

                    sessions.Add(new AudioSessionInfo(
                        (int)processId,
                        processName,
                        string.IsNullOrWhiteSpace(displayName) ? processName : displayName,
                        Math.Clamp((int)Math.Round(level * 100), 0, 100),
                        isMuted));
                }
                finally
                {
                    ReleaseCom(control);
                }
            }

            return sessions
                .OrderBy(session => session.ProcessName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(session => session.ProcessId)
                .ToList();
        });
    }

    public AudioActionResult ChangeVolume(string processName, int deltaPercent)
    {
        return ForMatchingVolumes(processName, volume =>
        {
            volume.GetMasterVolume(out var current);
            var next = Math.Clamp(current + deltaPercent / 100f, 0f, 1f);
            var eventContext = EventContext;
            ThrowIfFailed(volume.SetMasterVolume(next, ref eventContext));
            volume.GetMasterVolume(out var updated);
            volume.GetMute(out var isMuted);
            return (updated, isMuted);
        });
    }

    public AudioActionResult ToggleMute(string processName)
    {
        return ForMatchingVolumes(processName, volume =>
        {
            volume.GetMute(out var isMuted);
            var eventContext = EventContext;
            ThrowIfFailed(volume.SetMute(!isMuted, ref eventContext));
            volume.GetMasterVolume(out var updated);
            volume.GetMute(out var updatedMute);
            return (updated, updatedMute);
        });
    }

    private AudioActionResult ForMatchingVolumes(string processName, Func<ISimpleAudioVolume, (float Volume, bool IsMuted)> action)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return new AudioActionResult(false, 0, false);
        }

        var normalized = NormalizeProcessName(processName);
        var changedCount = 0;
        var volumeTotal = 0f;
        var allMuted = true;

        WithSessionEnumerator(sessionEnum =>
        {
            ThrowIfFailed(sessionEnum.GetCount(out var count));

            for (var i = 0; i < count; i++)
            {
                IAudioSessionControl? control = null;
                try
                {
                    if (sessionEnum.GetSession(i, out control) != 0 || control is null)
                    {
                        continue;
                    }

                    var control2 = (IAudioSessionControl2)control;
                    control2.GetProcessId(out var processId);

                    if (processId == 0)
                    {
                        continue;
                    }

                    var currentProcessName = NormalizeProcessName(GetProcessName((int)processId));
                    if (!string.Equals(currentProcessName, normalized, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var result = action((ISimpleAudioVolume)control);
                    changedCount++;
                    volumeTotal += result.Volume;
                    allMuted &= result.IsMuted;
                }
                finally
                {
                    ReleaseCom(control);
                }
            }

            return true;
        });

        if (changedCount == 0)
        {
            return new AudioActionResult(false, 0, false);
        }

        return new AudioActionResult(
            true,
            Math.Clamp((int)Math.Round(volumeTotal / changedCount * 100), 0, 100),
            allMuted);
    }

    private static T WithSessionEnumerator<T>(Func<IAudioSessionEnumerator, T> action)
    {
        IMMDeviceEnumerator? deviceEnumerator = null;
        IMMDevice? device = null;
        object? managerObject = null;
        IAudioSessionEnumerator? sessionEnumerator = null;

        try
        {
            var enumeratorType = Type.GetTypeFromCLSID(MMDeviceEnumeratorId)
                ?? throw new InvalidOperationException("Не удалось найти Windows Core Audio device enumerator.");
            deviceEnumerator = (IMMDeviceEnumerator)Activator.CreateInstance(enumeratorType)!;
            ThrowIfFailed(deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out device));
            var managerId = AudioSessionManager2Id;
            ThrowIfFailed(device.Activate(ref managerId, ClsCtx.All, IntPtr.Zero, out managerObject));

            var manager = (IAudioSessionManager2)managerObject!;
            ThrowIfFailed(manager.GetSessionEnumerator(out sessionEnumerator));
            return action(sessionEnumerator);
        }
        finally
        {
            ReleaseCom(sessionEnumerator);
            ReleaseCom(managerObject);
            ReleaseCom(device);
            ReleaseCom(deviceEnumerator);
        }
    }

    private static string GetProcessName(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? process.ProcessName
                : process.ProcessName + ".exe";
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string NormalizeProcessName(string processName)
    {
        return processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? processName
            : processName + ".exe";
    }

    private static void ThrowIfFailed(int hresult)
    {
        if (hresult < 0)
        {
            Marshal.ThrowExceptionForHR(hresult);
        }
    }

    private static void ReleaseCom(object? instance)
    {
        if (instance is not null && Marshal.IsComObject(instance))
        {
            Marshal.ReleaseComObject(instance);
        }
    }
}
