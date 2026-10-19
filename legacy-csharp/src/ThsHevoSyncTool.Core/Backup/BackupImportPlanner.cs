using ThsHevoSyncTool.Core.Environment;
using ThsHevoSyncTool.Core.IO;

namespace ThsHevoSyncTool.Core.Backup;

public sealed class BackupImportPlanner
{
    public BackupImportPlan CreatePlan(
        string zipPath,
        BackupManifest manifest,
        string targetInstallRootPath,
        string targetUserDirName,
        IReadOnlyCollection<string> selectedCategoryIds)
    {
        var installRoot = InstallRootValidator.Validate(targetInstallRootPath);
        UserDirNameValidator.Validate(targetUserDirName, nameof(targetUserDirName));

        var selected = new HashSet<string>(selectedCategoryIds, StringComparer.OrdinalIgnoreCase);
        var files = manifest.Files
            .Where(f => selected.Contains(f.CategoryId))
            .Select(f => MapFile(manifest.Source.UserDirName, targetUserDirName, installRoot.InstallRootPath, f))
            .OrderBy(static f => f.TargetRelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new BackupImportPlan(
            ZipPath: Path.GetFullPath(zipPath),
            SourceUserDirName: manifest.Source.UserDirName,
            TargetUserDirName: targetUserDirName,
            TargetInstallRootPath: installRoot.InstallRootPath,
            SelectedCategoryIds: selected
                .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            Files: files);
    }

    private static BackupImportPlannedFile MapFile(
        string sourceUserDirName,
        string targetUserDirName,
        string targetInstallRootPath,
        BackupManifestFileEntry entry)
    {
        var sourceRelative = SafeRelativePath.NormalizeRelativePath(entry.RelativePath);
        var targetRelative = RewriteUserDir(sourceRelative, sourceUserDirName, targetUserDirName);
        var targetAbsolute = Path.GetFullPath(Path.Combine(targetInstallRootPath, targetRelative));

        var willOverwrite = File.Exists(targetAbsolute);
        return new BackupImportPlannedFile(
            CategoryId: entry.CategoryId,
            SourceRelativePath: sourceRelative,
            TargetRelativePath: targetRelative,
            TargetAbsolutePath: targetAbsolute,
            SizeBytes: entry.SizeBytes,
            Sha256: entry.Sha256,
            LastWriteTimeUtc: entry.LastWriteTimeUtc,
            WillOverwrite: willOverwrite);
    }

    private static string RewriteUserDir(string relativePath, string sourceUserDirName, string targetUserDirName)
    {
        var parts = relativePath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3)
        {
            return relativePath;
        }

        if (!parts[0].Equals("bin", StringComparison.OrdinalIgnoreCase))
        {
            return relativePath;
        }

        if (!parts[1].Equals("users", StringComparison.OrdinalIgnoreCase))
        {
            return relativePath;
        }

        if (!parts[2].Equals(sourceUserDirName, StringComparison.OrdinalIgnoreCase))
        {
            return relativePath;
        }

        parts[2] = targetUserDirName;
        return string.Join('\\', parts);
    }

    
}
