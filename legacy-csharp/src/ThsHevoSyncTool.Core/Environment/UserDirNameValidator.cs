namespace ThsHevoSyncTool.Core.Environment;

public static class UserDirNameValidator
{
    public static void Validate(string userDirName, string paramName)
    {
        if (string.IsNullOrWhiteSpace(userDirName))
        {
            throw new ArgumentException("账号目录为空（mx_*）。", paramName);
        }

        if (userDirName.Contains('\\') || userDirName.Contains('/') || userDirName.Contains(':'))
        {
            throw new ArgumentException($"账号目录不合法：{userDirName}", paramName);
        }

        if (userDirName.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException($"账号目录不合法：{userDirName}", paramName);
        }
    }
}

