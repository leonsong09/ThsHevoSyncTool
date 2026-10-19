using System.IO.Compression;
using System.Text;

namespace ThsHevoSyncTool.Core.Backup;

public sealed class BackupPackageReader
{
    public async Task<BackupManifest> ReadManifestAsync(string zipPath, CancellationToken cancellationToken)
    {
        var fullZipPath = Path.GetFullPath(zipPath);
        if (!File.Exists(fullZipPath))
        {
            throw new FileNotFoundException($"备份包不存在：{fullZipPath}", fullZipPath);
        }

        using var zipStream = new FileStream(fullZipPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);

        var entry = zip.GetEntry(BackupPackageWriter.ManifestEntryName);
        if (entry is null)
        {
            throw new InvalidOperationException($"备份包缺少 {BackupPackageWriter.ManifestEntryName}。");
        }

        await using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);

        var json = await reader.ReadToEndAsync(cancellationToken);
        return BackupManifest.FromJson(json);
    }
}

