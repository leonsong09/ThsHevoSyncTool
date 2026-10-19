using ThsHevoSyncTool.Core.IO;

namespace ThsHevoSyncTool.Core.Backup;

public static partial class BackupCategoryCatalog
{
    public const string UserPlaceholder = "{USER}";

    private static readonly Lazy<IReadOnlyList<BackupCategory>> AllLazy =
        new(CreateAll, isThreadSafe: true);

    public static IReadOnlyList<BackupCategory> All => AllLazy.Value;

    public static IReadOnlyList<BackupCategory> CoreDefaults =>
        All.Where(static c => c.IsCoreDefault).ToArray();

    private static IReadOnlyList<BackupCategory> CreateAll() =>
        CoreCategories
            .Concat(OptionalCategories)
            .Concat(GlobalCategories)
            .ToArray();

    private static string UserRoot() => $"bin\\users\\{UserPlaceholder}";

    private static PathSpec File(string relativePathTemplate) =>
        new(PathSpecKind.File, relativePathTemplate, Recursive: false, SearchPattern: string.Empty);

    private static PathSpec Dir(string relativePathTemplate, bool recursive, string pattern) =>
        new(PathSpecKind.Directory, relativePathTemplate, Recursive: recursive, SearchPattern: pattern);
}
