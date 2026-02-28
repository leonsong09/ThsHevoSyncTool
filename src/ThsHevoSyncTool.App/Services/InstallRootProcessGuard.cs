using System.Diagnostics;

namespace ThsHevoSyncTool.Services;

public sealed class InstallRootProcessGuard : IProcessGuard
{
    public IReadOnlyList<RunningProcessInfo> FindRunningUnderInstallRoot(string installRootPath)
    {
        var root = Path.GetFullPath(installRootPath).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
        var result = new List<RunningProcessInfo>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var path = TryGetExecutablePath(process);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(new RunningProcessInfo(process.Id, process.ProcessName, path));
                }
            }
            finally
            {
                process.Dispose();
            }
        }

        return result;
    }

    private static string? TryGetExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }
}

