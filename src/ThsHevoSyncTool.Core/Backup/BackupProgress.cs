namespace ThsHevoSyncTool.Core.Backup;

public sealed record BackupProgress(
    string Message,
    int CompletedFiles,
    int TotalFiles,
    long CompletedBytes,
    long TotalBytes)
{
    public double Percent =>
        TotalFiles <= 0 ? 0 : (double)CompletedFiles / TotalFiles;
}

