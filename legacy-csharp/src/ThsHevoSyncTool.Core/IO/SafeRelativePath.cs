namespace ThsHevoSyncTool.Core.IO;

public static class SafeRelativePath
{
    public static string NormalizeForZipEntry(string relativePath)
    {
        var normalized = NormalizeRelativePath(relativePath);
        return normalized.Replace('\\', '/');
    }

    public static string NormalizeRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("相对路径为空。", nameof(relativePath));
        }

        var trimmed = relativePath.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            throw new ArgumentException($"不允许绝对路径：{relativePath}", nameof(relativePath));
        }

        var parts = trimmed.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
        var safeParts = new List<string>(parts.Length);
        foreach (var part in parts)
        {
            if (part is "." or "..")
            {
                throw new ArgumentException($"不允许包含路径跳转段：{relativePath}", nameof(relativePath));
            }

            safeParts.Add(part);
        }

        return string.Join('\\', safeParts);
    }
}

