namespace ThsHevoSyncTool.Core.IO;

public static class PathSafety
{
    public static void EnsureUnderRoot(string rootPath, string fullPath)
    {
        var rootFull = Path.GetFullPath(rootPath).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
        var candidateFull = Path.GetFullPath(fullPath);

        if (!candidateFull.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"路径越界：{candidateFull}");
        }
    }
}

