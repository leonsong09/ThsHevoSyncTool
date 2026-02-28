namespace ThsHevoSyncTool.Services;

public sealed record RunningProcessInfo(int ProcessId, string Name, string ExecutablePath);

public interface IProcessGuard
{
    IReadOnlyList<RunningProcessInfo> FindRunningUnderInstallRoot(string installRootPath);
}

