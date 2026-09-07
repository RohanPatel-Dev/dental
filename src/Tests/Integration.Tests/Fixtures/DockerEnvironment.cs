using System.Diagnostics;

namespace Dental.Integration.Tests.Fixtures;

/// <summary>
/// Whether a container runtime is reachable.
/// </summary>
/// <remarks>
/// These tests need a real Postgres, so on a machine without Docker they SKIP rather than fail.
/// A red suite that only means "no Docker here" trains people to ignore red suites; CI has Docker,
/// so nothing is silently lost there.
/// </remarks>
public static class DockerEnvironment
{
    private static readonly Lazy<bool> Available = new(Probe, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>True when a container runtime answered.</summary>
    public static bool IsAvailable => Available.Value;

    /// <summary>Reason shown on a skipped test.</summary>
    public const string SkipReason = "No container runtime is available on this machine.";

    private static bool Probe()
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo("docker", "info")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });

            if (process is null)
            {
                return false;
            }

            return process.WaitForExit(TimeSpan.FromSeconds(10)) && process.ExitCode == 0;
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }
}
