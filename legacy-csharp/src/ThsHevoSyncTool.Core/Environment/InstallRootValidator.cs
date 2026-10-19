namespace ThsHevoSyncTool.Core.Environment;

public static class InstallRootValidator
{
    public static InstallRootInfo Validate(string installRootPath)
    {
        if (string.IsNullOrWhiteSpace(installRootPath))
        {
            throw new ArgumentException("安装路径为空。", nameof(installRootPath));
        }

        var fullRoot = Path.GetFullPath(installRootPath);
        if (!Directory.Exists(fullRoot))
        {
            throw new DirectoryNotFoundException($"安装路径不存在：{fullRoot}");
        }

        var usersRoot = Path.Combine(fullRoot, "bin", "users");
        if (!Directory.Exists(usersRoot))
        {
            throw new DirectoryNotFoundException($"未找到 bin\\users：{usersRoot}");
        }

        return new InstallRootInfo(fullRoot, usersRoot);
    }

    public static IReadOnlyList<string> ListUserDirectories(InstallRootInfo root)
    {
        if (!Directory.Exists(root.UsersRootPath))
        {
            return Array.Empty<string>();
        }

        var candidates = Directory.EnumerateDirectories(root.UsersRootPath, "mx_*", SearchOption.TopDirectoryOnly)
            .Select(static path => Path.GetFileName(path))
            .Where(static n => !string.IsNullOrWhiteSpace(n))
            .OrderBy(static n => n, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return candidates;
    }
}
