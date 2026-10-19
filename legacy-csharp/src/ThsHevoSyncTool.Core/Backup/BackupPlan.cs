namespace ThsHevoSyncTool.Core.Backup;

public sealed record BackupPlannedFile(
    string CategoryId,
    string AbsolutePath,
    string RelativePath,
    long SizeBytes,
    DateTime LastWriteTimeUtc);

public sealed record BackupPlan(
    string InstallRootPath,
    string UserDirName,
    IReadOnlyList<BackupPlannedFile> Files)
{
    public long TotalBytes => Files.Sum(static f => f.SizeBytes);
}

