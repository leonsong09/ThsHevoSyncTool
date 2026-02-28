using ThsHevoSyncTool.Core.IO;

namespace ThsHevoSyncTool.Core.Backup;

public sealed record BackupCategory(
    string Id,
    string Name,
    string Description,
    bool IsCoreDefault,
    IReadOnlyList<PathSpec> Paths)
{
    public IReadOnlyList<string> DisplayPathRules =>
        Paths.Select(static p => p.DisplayRule).ToArray();
}

