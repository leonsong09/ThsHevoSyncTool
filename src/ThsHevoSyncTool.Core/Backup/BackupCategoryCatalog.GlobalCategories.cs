namespace ThsHevoSyncTool.Core.Backup;

public static partial class BackupCategoryCatalog
{
    private static readonly BackupCategory[] GlobalCategories =
    [
        new BackupCategory(
            Id: "global_user_config",
            Name: "全局配置（语言/复权/最近浏览等）",
            Description: "跨账号全局偏好（bin\\users\\config）。",
            IsCoreDefault: false,
            Paths:
            [
                Dir("bin\\users\\config", recursive: true, pattern: "*"),
            ]),
        new BackupCategory(
            Id: "public_notes",
            Name: "公共笔记",
            Description: "个股笔记内容（notes.xml）。",
            IsCoreDefault: false,
            Paths:
            [
                File("bin\\users\\public\\notes.xml"),
            ]),
    ];
}

