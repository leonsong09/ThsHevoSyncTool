namespace ThsHevoSyncTool.Core.Backup;

public sealed record BackupImportPlannedFile(
    string CategoryId,
    string SourceRelativePath,
    string TargetRelativePath,
    string TargetAbsolutePath,
    long SizeBytes,
    string Sha256,
    DateTime LastWriteTimeUtc,
    bool WillOverwrite);

public sealed record BackupImportPlan(
    string ZipPath,
    string SourceUserDirName,
    string TargetUserDirName,
    string TargetInstallRootPath,
    IReadOnlyList<BackupImportPlannedFile> Files)
{
    public int OverwriteCount => Files.Count(static f => f.WillOverwrite);
    public int NewFileCount => Files.Count(static f => !f.WillOverwrite);
    public long TotalBytes => Files.Sum(static f => f.SizeBytes);
}

