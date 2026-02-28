using ThsHevoSyncTool.Core.Backup;
using ThsHevoSyncTool.Formatting;

namespace ThsHevoSyncTool.ViewModels;

public sealed partial class MainViewModel
{
    private string _exportZipPath = string.Empty;

    public string ExportZipPath
    {
        get => _exportZipPath;
        set => SetProperty(ref _exportZipPath, value);
    }

    private void BrowseExportZip()
    {
        var suggestedName = $"ths_settings_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
        var selected = _dialogService.SaveZip(
            initialDirectory: Path.GetDirectoryName(ExportZipPath) ?? InstallPath,
            suggestedFileName: suggestedName);

        if (selected is null)
        {
            return;
        }

        ExportZipPath = selected;
    }

    private async Task StartExportAsync()
    {
        EnsureInstallRootReady();
        EnsureNoRunningProcesses();
        EnsureUserSelected();

        var selectedCategoryIds = ExportOptions.Where(static o => o.IsSelected).Select(static o => o.Id).ToArray();
        if (selectedCategoryIds.Length == 0)
        {
            throw new InvalidOperationException("未选择任何导出选项。");
        }

        if (string.IsNullOrWhiteSpace(ExportZipPath))
        {
            throw new InvalidOperationException("未选择导出 zip 路径。");
        }

        IsBusy = true;
        ProgressValue = 0;
        ProgressText = "正在导出...";

        try
        {
            var plan = _exportPlanner.CreatePlan(InstallPath, SelectedUserDirectory, selectedCategoryIds);
            AppendLog($"开始导出：文件数={plan.Files.Count}，大小={ByteFormatter.Format(plan.TotalBytes)}");

            var progress = new Progress<BackupProgress>(p =>
            {
                ProgressValue = p.Percent;
                ProgressText = p.Message;
            });

            await _packageWriter.WriteZipAsync(
                plan: plan,
                zipPath: ExportZipPath,
                selectedCategoryIds: selectedCategoryIds,
                toolVersion: GetToolVersion(),
                appExeVersion: TryGetThsExeVersion(InstallPath),
                progress: progress,
                cancellationToken: CancellationToken.None);

            AppendLog($"导出完成：{ExportZipPath}");
        }
        finally
        {
            ProgressText = "就绪";
            IsBusy = false;
        }
    }
}

