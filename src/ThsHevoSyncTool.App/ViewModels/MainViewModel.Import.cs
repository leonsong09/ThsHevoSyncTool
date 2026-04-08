using ThsHevoSyncTool.Core.Backup;
using ThsHevoSyncTool.Formatting;

namespace ThsHevoSyncTool.ViewModels;

public sealed partial class MainViewModel
{
    private string _importZipPath = string.Empty;
    private string _importManifestSummary = string.Empty;
    private string _importBackupDirectory = string.Empty;
    private BackupManifest? _loadedManifest;

    public string ImportZipPath
    {
        get => _importZipPath;
        set => SetProperty(ref _importZipPath, value);
    }

    public string ImportManifestSummary
    {
        get => _importManifestSummary;
        set => SetProperty(ref _importManifestSummary, value);
    }

    public string ImportBackupDirectory
    {
        get => _importBackupDirectory;
        set => SetProperty(ref _importBackupDirectory, value);
    }

    private void BrowseImportZip()
    {
        var selected = _dialogService.OpenZip(Path.GetDirectoryName(ImportZipPath) ?? InstallPath);
        if (selected is null)
        {
            return;
        }

        ImportZipPath = selected;
        _ = LoadManifestAsync();
    }

    private async Task LoadManifestAsync()
    {
        if (string.IsNullOrWhiteSpace(ImportZipPath))
        {
            return;
        }

        IsBusy = true;
        ProgressValue = 0;
        ProgressText = "正在读取 manifest...";

        try
        {
            _loadedManifest = await _packageReader.ReadManifestAsync(ImportZipPath, CancellationToken.None);
            ImportManifestSummary = BuildManifestSummary(_loadedManifest);
            ApplyManifestToImportOptions(_loadedManifest);

            if (string.IsNullOrWhiteSpace(ImportBackupDirectory))
            {
                ImportBackupDirectory = Path.GetDirectoryName(ImportZipPath) ?? string.Empty;
            }

            AppendLog($"已读取备份包：{ImportZipPath}");
        }
        finally
        {
            ProgressText = "就绪";
            IsBusy = false;
        }
    }

    private async Task StartImportAsync()
    {
        EnsureInstallRootReady();
        EnsureNoRunningProcesses();
        EnsureUserSelected();

        if (string.IsNullOrWhiteSpace(ImportZipPath))
        {
            throw new InvalidOperationException("未选择备份包(zip)。");
        }

        var manifest = _loadedManifest ?? await _packageReader.ReadManifestAsync(ImportZipPath, CancellationToken.None);
        _loadedManifest = manifest;

        EnsureTargetUserDirectoryExists();
        EnsureTargetWritable();

        var selectedCategoryIds = ImportOptions.Where(static o => o.IsSelected).Select(static o => o.Id).ToArray();
        if (selectedCategoryIds.Length == 0)
        {
            throw new InvalidOperationException("未选择任何导入选项。");
        }

        var plan = _importPlanner.CreatePlan(
            zipPath: ImportZipPath,
            manifest: manifest,
            targetInstallRootPath: InstallPath,
            targetUserDirName: SelectedUserDirectory,
            selectedCategoryIds: selectedCategoryIds);

        var backupDir = string.IsNullOrWhiteSpace(ImportBackupDirectory)
            ? Path.GetDirectoryName(ImportZipPath) ?? throw new InvalidOperationException("无法确定导入前备份目录。")
            : ImportBackupDirectory;

        var backupZipPath = Path.Combine(backupDir, $"pre_import_backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

        IsBusy = true;
        ProgressValue = 0;
        ProgressText = "正在导入...";

        try
        {
            AppendLog($"导入计划：覆盖={plan.OverwriteCount}，新增={plan.NewFileCount}，大小={ByteFormatter.Format(plan.TotalBytes)}");
            AppendLog($"导入前备份：{backupZipPath}");

            var progress = new Progress<BackupProgress>(p =>
            {
                ProgressValue = p.Percent;
                ProgressText = p.Message;
            });

            await _packageImporter.CreatePreImportBackupZipAsync(plan, backupZipPath, progress, CancellationToken.None);
            await _packageImporter.ImportAsync(plan, progress, CancellationToken.None);

            AppendLog("导入完成。");
        }
        finally
        {
            ProgressText = "就绪";
            IsBusy = false;
        }
    }

    private void ApplyManifestToImportOptions(BackupManifest manifest)
    {
        var selectedCategories = new HashSet<string>(
            manifest.Selection.Categories,
            StringComparer.OrdinalIgnoreCase);

        var map = manifest.Files
            .GroupBy(static f => f.CategoryId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static g => g.Key,
                static g => new { Count = g.Count(), Bytes = g.Sum(x => x.SizeBytes) },
                StringComparer.OrdinalIgnoreCase);

        foreach (var option in ImportOptions)
        {
            if (map.TryGetValue(option.Id, out var stats))
            {
                option.FileCount = stats.Count;
                option.TotalBytes = stats.Bytes;
                option.IsAvailable = true;
                option.IsSelected = selectedCategories.Contains(option.Id);
            }
            else
            {
                option.FileCount = 0;
                option.TotalBytes = 0;
                option.IsAvailable = selectedCategories.Contains(option.Id);
                option.IsSelected = selectedCategories.Contains(option.Id);
            }
        }
    }
}
