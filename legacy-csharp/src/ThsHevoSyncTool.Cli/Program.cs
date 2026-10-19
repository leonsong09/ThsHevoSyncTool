using System.Reflection;
using System.Text;
using ThsHevoSyncTool.Core.Backup;
using ThsHevoSyncTool.Core.Environment;

namespace ThsHevoSyncTool.Cli;

/// <summary>
/// ThsHevoSyncTool 命令行入口（CLI）。
/// 与 WPF GUI 共用 ThsHevoSyncTool.Core 的全部逻辑，不弹窗、可脚本化/自动化调用。
///
/// 子命令:
///   list      列出全部备份分类
///   presets   列出推荐预设 r1/r2/r3
///   users     列出安装根目录下的同花顺账号目录（mx_*）
///   scan      扫描各分类在当前机器上的文件数/字节数
///   export    导出为 ZIP 备份包（含 manifest.json + 每文件 SHA256）
///   inspect   读取并展示 ZIP 备份包的 manifest
///   import    从 ZIP 导入（默认预演，--apply 才写入；写入前自动生成导入前备份）
///
/// 退出码: 0 成功 / 1 业务失败 / 2 用法错误
/// </summary>
internal static class Program
{
    private const string ExeName = "ths-hevo-sync";

    private static int Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
            // 无控制台（重定向）时忽略
        }

        try
        {
            return Run(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] {ex.Message}");
            return 1;
        }
    }

    private static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintUsage();
            return args.Length == 0 ? 2 : 0;
        }

        var opts = Options.Parse(args.Skip(1).ToArray());
        return args[0].ToLowerInvariant() switch
        {
            "list" => CmdList(),
            "presets" => CmdPresets(),
            "users" => CmdUsers(opts),
            "scan" => CmdScan(opts),
            "export" => CmdExport(opts),
            "inspect" => CmdInspect(opts),
            "import" => CmdImport(opts),
            _ => Fail($"未知子命令：{args[0]}（用 --help 查看用法）"),
        };
    }

    // ------------------------------------------------------------- commands

    private static int CmdList()
    {
        Console.WriteLine($"分类清单（{BackupCategoryCatalog.All.Count} 项；core=默认勾选）");
        foreach (var c in BackupCategoryCatalog.All)
        {
            var flag = c.IsCoreDefault ? "core" : "    ";
            Console.WriteLine($"  {c.Id,-34} {flag}  {c.Name}");
            foreach (var rule in c.DisplayPathRules)
            {
                Console.WriteLine($"  {"",-34} {"",-6}    {rule}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("提示：--categories 传上面的 id（逗号分隔）；或用 --preset r1|r2|r3。");
        return 0;
    }

    private static int CmdPresets()
    {
        Console.WriteLine("推荐预设：");
        foreach (var p in ExportSelectionPresetCatalog.All)
        {
            Console.WriteLine($"  {p.Id}  {p.Name}（{p.SelectedCategoryIds.Count} 项）");
            Console.WriteLine($"      {p.Description}");
        }

        return 0;
    }

    private static int CmdUsers(Options opts)
    {
        var info = RequireRoot(opts);
        var users = InstallRootValidator.ListUserDirectories(info);
        Console.WriteLine($"安装根目录: {info.InstallRootPath}");
        if (users.Count == 0)
        {
            Console.WriteLine("未发现账号目录（bin\\users\\mx_*）。");
            return 1;
        }

        Console.WriteLine($"发现 {users.Count} 个账号目录：");
        foreach (var u in users)
        {
            Console.WriteLine($"  {u}");
        }

        return 0;
    }

    private static int CmdScan(Options opts)
    {
        var (root, user) = RequireRootAndUser(opts);
        var scanner = new BackupCategoryScanner(BackupCategoryCatalog.All);
        var results = scanner.ScanAll(root.InstallRootPath, user);

        Console.WriteLine($"安装根目录: {root.InstallRootPath}");
        Console.WriteLine($"账号目录  : {user}");
        Console.WriteLine();
        Console.WriteLine($"{"category",-34} {"files",7} {"size",12}");
        long totalBytes = 0;
        var totalFiles = 0;
        foreach (var r in results)
        {
            var cat = BackupCategoryCatalog.All.First(c => c.Id == r.CategoryId);
            totalFiles += r.FileCount;
            totalBytes += r.TotalBytes;
            Console.WriteLine($"{r.CategoryId,-34} {r.FileCount,7} {Human(r.TotalBytes),12}   {cat.Name}");
        }

        Console.WriteLine();
        Console.WriteLine($"合计: {totalFiles} 个文件 / {Human(totalBytes)}");
        return 0;
    }

    private static int CmdExport(Options opts)
    {
        var (root, user) = RequireRootAndUser(opts);
        var outDir = opts.Get("out") ?? throw new ArgumentException("缺少 --out <输出目录>");
        var ids = ResolveCategoryIds(opts);

        var planner = new BackupPlanner(BackupCategoryCatalog.All);
        var plan = planner.CreatePlan(root.InstallRootPath, user, ids);

        Console.WriteLine($"源安装根目录: {plan.InstallRootPath}");
        Console.WriteLine($"账号目录    : {plan.UserDirName}");
        Console.WriteLine($"分类        : {string.Join(", ", ids)}");
        Console.WriteLine($"待导出文件  : {plan.Files.Count} 个 / {Human(plan.TotalBytes)}");

        if (plan.Files.Count == 0)
        {
            Console.WriteLine("[ERROR] 没有匹配到任何文件（检查账号目录名与分类是否选对）。");
            return 1;
        }

        if (opts.Has("dry-run"))
        {
            Console.WriteLine("[DRY-RUN] 未写包。去掉 --dry-run 即执行导出。");
            return 0;
        }

        Directory.CreateDirectory(outDir);
        var zipPath = opts.Get("zip-name") is { } name
            ? Path.Combine(outDir, name)
            : Path.Combine(outDir, $"ThsHevoSyncTool-{DateTime.Now:yyyyMMdd_HHmmss}.zip");

        var writer = new BackupPackageWriter();
        var manifest = writer.WriteZipAsync(
            plan, zipPath, ids, ToolVersion, appExeVersion: null,
            new ConsoleProgress(), CancellationToken.None).GetAwaiter().GetResult();

        Console.WriteLine($"[OK] 已导出: {zipPath}");
        Console.WriteLine($"     manifest: {manifest.Files.Count} 个文件条目 / schema {manifest.SchemaVersion}");
        return 0;
    }

    private static int CmdInspect(Options opts)
    {
        var zip = opts.Get("zip") ?? throw new ArgumentException("缺少 --zip <备份包.zip>");
        var manifest = ReadManifest(zip);
        Console.WriteLine($"备份包      : {Path.GetFullPath(zip)}");
        Console.WriteLine($"schema      : {manifest.SchemaVersion}");
        Console.WriteLine($"工具        : {manifest.Tool.Name} v{manifest.Tool.Version} ({manifest.Tool.Runtime})");
        Console.WriteLine($"创建时间UTC : {manifest.CreatedAtUtc:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"源安装根目录: {manifest.Source.InstallRoot}");
        Console.WriteLine($"源账号目录  : {manifest.Source.UserDirName}");
        Console.WriteLine($"预设        : {manifest.Selection.DefaultPreset}");
        Console.WriteLine($"分类        : {string.Join(", ", manifest.Selection.Categories)}");
        Console.WriteLine($"文件条目    : {manifest.Files.Count} 个 / {Human(manifest.Files.Sum(f => f.SizeBytes))}");

        foreach (var group in manifest.Files.GroupBy(f => f.CategoryId).OrderBy(g => g.Key))
        {
            Console.WriteLine($"  {group.Key,-34} {group.Count(),7} {Human(group.Sum(f => f.SizeBytes)),12}");
        }

        return 0;
    }

    private static int CmdImport(Options opts)
    {
        var zip = opts.Get("zip") ?? throw new ArgumentException("缺少 --zip <备份包.zip>");
        var (root, user) = RequireRootAndUser(opts);
        var manifest = ReadManifest(zip);

        var ids = opts.Has("categories")
            ? SplitIds(opts.Get("categories")!)
            : manifest.Selection.Categories.ToArray();
        ValidateIds(ids);

        var planner = new BackupImportPlanner();
        var plan = planner.CreatePlan(zip, manifest, root.InstallRootPath, user, ids);

        Console.WriteLine($"备份包      : {plan.ZipPath}");
        Console.WriteLine($"源账号目录  : {plan.SourceUserDirName}");
        Console.WriteLine($"目标根目录  : {plan.TargetInstallRootPath}");
        Console.WriteLine($"目标账号目录: {plan.TargetUserDirName}");
        Console.WriteLine($"导入分类    : {string.Join(", ", plan.SelectedCategoryIds)}");
        Console.WriteLine($"文件        : {plan.Files.Count} 个 / {Human(plan.TotalBytes)}（覆盖 {plan.OverwriteCount} / 新增 {plan.NewFileCount}）");

        if (plan.Files.Count == 0)
        {
            Console.WriteLine("[WARN] 计划为空：备份包中没有这些分类的文件。");
            return 1;
        }

        foreach (var f in plan.Files.Take(20))
        {
            Console.WriteLine($"    {(f.WillOverwrite ? "覆盖" : "新增")} {f.TargetRelativePath}");
        }

        if (plan.Files.Count > 20)
        {
            Console.WriteLine($"    ... 其余 {plan.Files.Count - 20} 个省略");
        }

        if (!opts.Has("apply"))
        {
            Console.WriteLine("[DRY-RUN] 预演结束，未写入。确认无误后加 --apply 执行（写入前会自动生成导入前备份）。");
            return 0;
        }

        var backupDir = opts.Get("backup-dir") ?? Directory.GetCurrentDirectory();
        Directory.CreateDirectory(backupDir);
        var preBackupPath = Path.Combine(backupDir, $"pre_import_backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

        var importer = new BackupPackageImporter();
        importer.CreatePreImportBackupZipAsync(plan, preBackupPath, new ConsoleProgress(), CancellationToken.None)
            .GetAwaiter().GetResult();
        Console.WriteLine($"[OK] 导入前备份: {preBackupPath}（可用 `import --zip {preBackupPath} --root ... --user ... --apply` 回滚）");

        importer.ImportAsync(plan, new ConsoleProgress(), CancellationToken.None).GetAwaiter().GetResult();
        Console.WriteLine($"[OK] 已导入 {plan.Files.Count} 个文件到 {plan.TargetInstallRootPath}");
        return 0;
    }

    // ------------------------------------------------------------- helpers

    private static BackupManifest ReadManifest(string zipPath)
    {
        var reader = new BackupPackageReader();
        return reader.ReadManifestAsync(zipPath, CancellationToken.None).GetAwaiter().GetResult();
    }

    private static InstallRootInfo RequireRoot(Options opts)
    {
        var root = opts.Get("root") ?? throw new ArgumentException("缺少 --root <同花顺安装根目录>");
        return InstallRootValidator.Validate(root);
    }

    private static (InstallRootInfo Root, string User) RequireRootAndUser(Options opts)
    {
        var root = RequireRoot(opts);
        var user = opts.Get("user") ?? throw new ArgumentException("缺少 --user <账号目录名，如 mx_123456789>");
        UserDirNameValidator.Validate(user, "user");
        return (root, user);
    }

    private static string[] ResolveCategoryIds(Options opts)
    {
        if (opts.Get("categories") is { } raw)
        {
            var ids = SplitIds(raw);
            ValidateIds(ids);
            return ids;
        }

        if (opts.Get("preset") is { } presetId)
        {
            var preset = ExportSelectionPresetCatalog.GetById(presetId);
            Console.WriteLine($"预设 {preset.Id} = {preset.Name}");
            return preset.SelectedCategoryIds.ToArray();
        }

        Console.WriteLine("[INFO] 未指定 --categories/--preset，使用默认「仅核心」分类。");
        return BackupCategoryCatalog.CoreDefaults.Select(c => c.Id).ToArray();
    }

    private static string[] SplitIds(string raw) =>
        raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static void ValidateIds(IReadOnlyCollection<string> ids)
    {
        var known = BackupCategoryCatalog.All.Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = ids.Where(id => !known.Contains(id)).ToArray();
        if (unknown.Length > 0)
        {
            throw new ArgumentException($"未知分类 id：{string.Join(", ", unknown)}（用 list 子命令查看）");
        }
    }

    private static string ToolVersion =>
        typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(Program).Assembly.GetName().Version?.ToString()
        ?? "0.0.0";

    private static string Human(long bytes)
    {
        double v = bytes;
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var i = 0;
        while (v >= 1024 && i < units.Length - 1)
        {
            v /= 1024;
            i++;
        }

        return i == 0 ? $"{(long)v}B" : $"{v:0.0}{units[i]}";
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine($"[ERROR] {message}");
        return 2;
    }

    private static void PrintUsage()
    {
        Console.WriteLine($"""
            {ExeName} — 同花顺本地配置备份/迁移（命令行，与 GUI 共用 core 逻辑）

            用法:
              {ExeName} list
              {ExeName} presets
              {ExeName} users  --root <安装根目录>
              {ExeName} scan   --root <安装根目录> --user <mx_xxx>
              {ExeName} export --root <安装根目录> --user <mx_xxx> --out <目录>
                               [--preset r1|r2|r3 | --categories a,b,c] [--zip-name X.zip] [--dry-run]
              {ExeName} inspect --zip <备份包.zip>
              {ExeName} import --zip <备份包.zip> --root <安装根目录> --user <mx_xxx>
                               [--categories a,b] [--backup-dir <目录>] [--apply]

            约定:
              * export 只读源配置、写新 ZIP（安全）；加 --dry-run 可只预演
              * import 默认预演，必须显式 --apply 才写入；写入前自动生成 pre_import_backup_*.zip
              * 回滚 = 用 pre_import_backup_*.zip 再跑一次 import --apply
              * 退出码: 0 成功 / 1 业务失败 / 2 用法错误
            """);
    }

    // ------------------------------------------------------------- options

    private sealed class Options
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _flags = new(StringComparer.OrdinalIgnoreCase);

        public static Options Parse(string[] args)
        {
            var opts = new Options();
            for (var i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (!a.StartsWith("--", StringComparison.Ordinal))
                {
                    continue;
                }

                var key = a[2..];
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    opts._values[key] = args[++i];
                }
                else
                {
                    opts._flags.Add(key);
                }
            }

            return opts;
        }

        public string? Get(string key) => _values.TryGetValue(key, out var v) ? v : null;

        public bool Has(string key) => _flags.Contains(key) || _values.ContainsKey(key);
    }

    private sealed class ConsoleProgress : IProgress<BackupProgress>
    {
        public void Report(BackupProgress value)
        {
            if (value.TotalFiles <= 0)
            {
                Console.WriteLine($"  {value.Message}");
                return;
            }

            var pct = value.CompletedFiles * 100 / value.TotalFiles;
            var msg = value.Message.Length > 48 ? value.Message[..48] : value.Message;
            Console.Write($"\r  [{value.CompletedFiles}/{value.TotalFiles}] {pct,3}%  {msg,-48}");
            if (value.CompletedFiles >= value.TotalFiles)
            {
                Console.WriteLine();
            }
        }
    }
}
