using ThsHevoSyncTool.Core.Environment;

namespace ThsHevoSyncTool.Core.Backup;

public sealed record BackupCategoryScanResult(string CategoryId, int FileCount, long TotalBytes);

public sealed class BackupCategoryScanner
{
    private readonly IReadOnlyList<BackupCategory> _categories;

    public BackupCategoryScanner(IReadOnlyList<BackupCategory> categories)
    {
        _categories = categories;
    }

    public IReadOnlyList<BackupCategoryScanResult> ScanAll(string installRootPath, string userDirName)
    {
        var installRoot = InstallRootValidator.Validate(installRootPath);
        UserDirNameValidator.Validate(userDirName, nameof(userDirName));

        return _categories.Select(c => ScanOne(installRoot.InstallRootPath, userDirName, c)).ToArray();
    }

    private static BackupCategoryScanResult ScanOne(string installRootPath, string userDirName, BackupCategory category)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var count = 0;
        var bytes = 0L;

        foreach (var pathSpec in category.Paths)
        {
            foreach (var file in PathSpecExpander.Expand(installRootPath, userDirName, category.Id, pathSpec))
            {
                if (!seen.Add(file.RelativePath))
                {
                    continue;
                }

                count += 1;
                bytes += file.SizeBytes;
            }
        }

        return new BackupCategoryScanResult(category.Id, count, bytes);
    }
}

