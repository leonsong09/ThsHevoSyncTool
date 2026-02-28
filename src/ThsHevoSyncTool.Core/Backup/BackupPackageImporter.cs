using System.IO.Compression;
using ThsHevoSyncTool.Core.IO;

namespace ThsHevoSyncTool.Core.Backup;

public sealed class BackupPackageImporter
{
    private const int BufferSizeBytes = 1024 * 128;

    public async Task CreatePreImportBackupZipAsync(
        BackupImportPlan plan,
        string backupZipPath,
        IProgress<BackupProgress>? progress,
        CancellationToken cancellationToken)
    {
        var backupItems = BuildBackupItems(plan);
        var totalFiles = backupItems.Count;
        var totalBytes = backupItems.Sum(static i => i.ExistingSizeBytes);

        var completedFiles = 0;
        var completedBytes = 0L;

        var fullBackupZip = Path.GetFullPath(backupZipPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullBackupZip) ?? ".");

        await using var zipStream = new FileStream(fullBackupZip, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true);

        foreach (var item in backupItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new BackupProgress(
                Message: $"导入前备份：{item.File.TargetRelativePath}",
                CompletedFiles: completedFiles,
                TotalFiles: totalFiles,
                CompletedBytes: completedBytes,
                TotalBytes: totalBytes));

            PathSafety.EnsureUnderRoot(plan.TargetInstallRootPath, item.File.TargetAbsolutePath);

            var entryPath = SafeRelativePath.NormalizeForZipEntry(item.File.TargetRelativePath);
            var entry = zip.CreateEntry(entryPath, CompressionLevel.Optimal);
            entry.LastWriteTime = new DateTimeOffset(item.ExistingLastWriteTimeUtc, TimeSpan.Zero);

            await using var input = new FileStream(
                item.File.TargetAbsolutePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: BufferSizeBytes,
                useAsync: true);

            await using var output = entry.Open();
            await input.CopyToAsync(output, cancellationToken);

            completedFiles += 1;
            completedBytes += item.ExistingSizeBytes;
        }

        progress?.Report(new BackupProgress(
            Message: "导入前备份完成。",
            CompletedFiles: completedFiles,
            TotalFiles: totalFiles,
            CompletedBytes: completedBytes,
            TotalBytes: totalBytes));
    }

    public async Task ImportAsync(
        BackupImportPlan plan,
        IProgress<BackupProgress>? progress,
        CancellationToken cancellationToken)
    {
        var totalFiles = plan.Files.Count;
        var totalBytes = plan.TotalBytes;
        var completedFiles = 0;
        var completedBytes = 0L;

        using var zipStream = new FileStream(plan.ZipPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);

        foreach (var file in plan.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new BackupProgress(
                Message: $"导入：{file.TargetRelativePath}",
                CompletedFiles: completedFiles,
                TotalFiles: totalFiles,
                CompletedBytes: completedBytes,
                TotalBytes: totalBytes));

            PathSafety.EnsureUnderRoot(plan.TargetInstallRootPath, file.TargetAbsolutePath);
            Directory.CreateDirectory(Path.GetDirectoryName(file.TargetAbsolutePath) ?? ".");

            var entryPath = SafeRelativePath.NormalizeForZipEntry(file.SourceRelativePath);
            var entry = FindEntry(zip, entryPath)
                ?? throw new InvalidOperationException($"备份包缺少文件：{file.SourceRelativePath}");

            await using var entryStream = entry.Open();
            await using var output = new FileStream(
                file.TargetAbsolutePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: BufferSizeBytes,
                useAsync: true);

            var sha256 = await Sha256.CopyAndHashAsync(entryStream, output, cancellationToken);
            if (!sha256.Equals(file.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"SHA256 校验失败：{file.TargetRelativePath}");
            }

            File.SetLastWriteTimeUtc(file.TargetAbsolutePath, file.LastWriteTimeUtc);
            completedFiles += 1;
            completedBytes += file.SizeBytes;
        }

        progress?.Report(new BackupProgress(
            Message: "导入完成。",
            CompletedFiles: completedFiles,
            TotalFiles: totalFiles,
            CompletedBytes: completedBytes,
            TotalBytes: totalBytes));
    }

    private static ZipArchiveEntry? FindEntry(ZipArchive zip, string entryPath)
    {
        var entry = zip.GetEntry(entryPath);
        if (entry is not null)
        {
            return entry;
        }

        return zip.Entries.FirstOrDefault(e =>
            e.FullName.Equals(entryPath, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<BackupItem> BuildBackupItems(BackupImportPlan plan)
    {
        var result = new List<BackupItem>();
        foreach (var file in plan.Files.Where(static f => f.WillOverwrite))
        {
            if (!File.Exists(file.TargetAbsolutePath))
            {
                continue;
            }

            var info = new FileInfo(file.TargetAbsolutePath);
            result.Add(new BackupItem(file, info.Length, info.LastWriteTimeUtc));
        }

        return result;
    }

    private sealed record BackupItem(BackupImportPlannedFile File, long ExistingSizeBytes, DateTime ExistingLastWriteTimeUtc);
}
