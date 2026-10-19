namespace ThsHevoSyncTool.Core.IO;

public enum PathSpecKind
{
    File = 1,
    Directory = 2,
}

public sealed record PathSpec(
    PathSpecKind Kind,
    string RelativePathTemplate,
    bool Recursive,
    string SearchPattern)
{
    public string DisplayRule =>
        Kind switch
        {
            PathSpecKind.File => RelativePathTemplate,
            PathSpecKind.Directory => $"{RelativePathTemplate}\\{SearchPattern}" + (Recursive ? " (recursive)" : string.Empty),
            _ => RelativePathTemplate,
        };
}

