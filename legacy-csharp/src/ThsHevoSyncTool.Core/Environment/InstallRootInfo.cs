namespace ThsHevoSyncTool.Core.Environment;

public sealed record InstallRootInfo(
    string InstallRootPath,
    string UsersRootPath)
{
    public string UsersConfigPath => Path.Combine(UsersRootPath, "config");
    public string UsersPublicPath => Path.Combine(UsersRootPath, "public");
    public string UsersInternalPath => Path.Combine(UsersRootPath, "internal");
}

