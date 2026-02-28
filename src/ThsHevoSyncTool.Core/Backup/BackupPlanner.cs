using ThsHevoSyncTool.Core.Environment;

namespace ThsHevoSyncTool.Core.Backup;

public sealed class BackupPlanner
{
    private readonly IReadOnlyDictionary<string, BackupCategory> _categoriesById;

    public BackupPlanner(IReadOnlyList<BackupCategory> categories)
    {
        _categoriesById = categories.ToDictionary(static c => c.Id, StringComparer.OrdinalIgnoreCase);
    }

    public BackupPlan CreatePlan(
        string installRootPath,
        string userDirName,
        IReadOnlyCollection<string> selectedCategoryIds)
    {
        var installRoot = InstallRootValidator.Validate(installRootPath);
        UserDirNameValidator.Validate(userDirName, nameof(userDirName));

        var categories = ResolveCategories(selectedCategoryIds);

        var plannedFiles = new List<BackupPlannedFile>();
        var seenRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in categories)
        {
            foreach (var pathSpec in category.Paths)
            {
                foreach (var file in PathSpecExpander.Expand(installRoot.InstallRootPath, userDirName, category.Id, pathSpec))
                {
                    if (!seenRelativePaths.Add(file.RelativePath))
                    {
                        throw new InvalidOperationException($"文件被多个选项重复包含：{file.RelativePath}");
                    }

                    plannedFiles.Add(file);
                }
            }
        }

        return new BackupPlan(
            InstallRootPath: installRoot.InstallRootPath,
            UserDirName: userDirName,
            Files: plannedFiles.OrderBy(static f => f.RelativePath, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private IReadOnlyList<BackupCategory> ResolveCategories(IReadOnlyCollection<string> selectedCategoryIds)
    {
        if (selectedCategoryIds.Count == 0)
        {
            return Array.Empty<BackupCategory>();
        }

        var result = new List<BackupCategory>(selectedCategoryIds.Count);
        foreach (var id in selectedCategoryIds)
        {
            if (!_categoriesById.TryGetValue(id, out var category))
            {
                throw new ArgumentException($"未知选项：{id}", nameof(selectedCategoryIds));
            }

            result.Add(category);
        }

        return result;
    }
}
