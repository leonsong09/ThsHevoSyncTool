using ThsHevoSyncTool.Core.IO;

namespace ThsHevoSyncTool.Core.Backup;

public static class PathSpecExpander
{
    public static IEnumerable<BackupPlannedFile> Expand(
        string installRootPath,
        string userDirName,
        string categoryId,
        PathSpec pathSpec)
    {
        var relativeTemplate = pathSpec.RelativePathTemplate.Replace(BackupCategoryCatalog.UserPlaceholder, userDirName);
        var relativePath = SafeRelativePath.NormalizeRelativePath(relativeTemplate);
        var absolutePath = Path.GetFullPath(Path.Combine(installRootPath, relativePath));

        return pathSpec.Kind switch
        {
            PathSpecKind.File => ExpandFile(categoryId, absolutePath, relativePath),
            PathSpecKind.Directory => ExpandDirectory(categoryId, installRootPath, absolutePath, pathSpec),
            _ => Array.Empty<BackupPlannedFile>(),
        };
    }

    private static IEnumerable<BackupPlannedFile> ExpandFile(string categoryId, string absolutePath, string relativePath)
    {
        if (!File.Exists(absolutePath))
        {
            return Array.Empty<BackupPlannedFile>();
        }

        var info = new FileInfo(absolutePath);
        return
        [
            new BackupPlannedFile(
                CategoryId: categoryId,
                AbsolutePath: absolutePath,
                RelativePath: relativePath,
                SizeBytes: info.Length,
                LastWriteTimeUtc: info.LastWriteTimeUtc),
        ];
    }

    private static IEnumerable<BackupPlannedFile> ExpandDirectory(
        string categoryId,
        string installRootPath,
        string absoluteDirectoryPath,
        PathSpec pathSpec)
    {
        if (!Directory.Exists(absoluteDirectoryPath))
        {
            return Array.Empty<BackupPlannedFile>();
        }

        var option = pathSpec.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.EnumerateFiles(absoluteDirectoryPath, pathSpec.SearchPattern, option);

        var result = new List<BackupPlannedFile>();
        foreach (var file in files)
        {
            var relative = Path.GetRelativePath(installRootPath, file);
            var normalizedRelative = SafeRelativePath.NormalizeRelativePath(relative);
            var info = new FileInfo(file);

            result.Add(new BackupPlannedFile(
                CategoryId: categoryId,
                AbsolutePath: info.FullName,
                RelativePath: normalizedRelative,
                SizeBytes: info.Length,
                LastWriteTimeUtc: info.LastWriteTimeUtc));
        }

        return result;
    }
}

