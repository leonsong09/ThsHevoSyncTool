using System.IO.Compression;
using System.Runtime.InteropServices;
using ThsHevoSyncTool.Core.IO;

namespace ThsHevoSyncTool.Core.Backup;

public sealed class BackupPackageWriter
{
    public const string ManifestEntryName = "manifest.json";
    private const string DefaultPresetCore = "core";
    private const int BufferSizeBytes = 1024 * 128;

    public async Task<BackupManifest> WriteZipAsync(
        BackupPlan plan,
        string zipPath,
        IReadOnlyCollection<string> selectedCategoryIds,
        string toolVersion,
        string? appExeVersion,
        IProgress<BackupProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (plan.Files.Any(static f => string.Equals(f.RelativePath, ManifestEntryName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"不允许备份文件与 {ManifestEntryName} 同名。");
        }

        var fullZipPath = Path.GetFullPath(zipPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullZipPath) ?? ".");

        var totalFiles = plan.Files.Count;
        var totalBytes = plan.TotalBytes;
        var completedBytes = 0L;
        var completedFiles = 0;

        var fileEntries = new List<BackupManifestFileEntry>(totalFiles);
        await using var zipStream = new FileStream(fullZipPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true);

        foreach (var file in plan.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new BackupProgress(
                Message: $"导出：{file.RelativePath}",
                CompletedFiles: completedFiles,
                TotalFiles: totalFiles,
                CompletedBytes: completedBytes,
                TotalBytes: totalBytes));

            var entryPath = SafeRelativePath.NormalizeForZipEntry(file.RelativePath);
            var entry = zip.CreateEntry(entryPath, CompressionLevel.Optimal);
            entry.LastWriteTime = new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero);

            await using var input = new FileStream(file.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: BufferSizeBytes, useAsync: true);
            await using var entryStream = entry.Open();
            var sha256 = await Sha256.CopyAndHashAsync(input, entryStream, cancellationToken);

            fileEntries.Add(new BackupManifestFileEntry(
                RelativePath: file.RelativePath,
                SizeBytes: file.SizeBytes,
                Sha256: sha256,
                LastWriteTimeUtc: file.LastWriteTimeUtc,
                CategoryId: file.CategoryId));

            completedFiles += 1;
            completedBytes += file.SizeBytes;
        }

        var manifest = new BackupManifest(
            SchemaVersion: BackupManifest.CurrentSchemaVersion,
            Tool: new BackupToolInfo(
                Name: "ThsHevoSyncTool",
                Version: toolVersion,
                Runtime: RuntimeInformation.FrameworkDescription),
            CreatedAtUtc: DateTime.UtcNow,
            Source: new BackupSourceInfo(
                InstallRoot: plan.InstallRootPath,
                UserDirName: plan.UserDirName,
                AppExeVersion: appExeVersion),
            Selection: new BackupSelectionInfo(
                DefaultPreset: DefaultPresetCore,
                Categories: selectedCategoryIds.OrderBy(static s => s, StringComparer.OrdinalIgnoreCase).ToArray()),
            Files: fileEntries.OrderBy(static f => f.RelativePath, StringComparer.OrdinalIgnoreCase).ToArray());

        var manifestEntry = zip.CreateEntry(ManifestEntryName, CompressionLevel.Optimal);
        await using (var writer = new StreamWriter(manifestEntry.Open()))
        {
            await writer.WriteAsync(manifest.ToJson());
        }

        progress?.Report(new BackupProgress(
            Message: "导出完成。",
            CompletedFiles: completedFiles,
            TotalFiles: totalFiles,
            CompletedBytes: completedBytes,
            TotalBytes: totalBytes));

        return manifest;
    }
}
