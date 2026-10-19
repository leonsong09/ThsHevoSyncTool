using ThsHevoSyncTool.Core.Environment;

namespace ThsHevoSyncTool.ViewModels;

public sealed partial class MainViewModel
{
    private async Task RefreshAsync()
    {
        IsBusy = true;
        ProgressValue = 0;
        ProgressText = "正在扫描...";

        try
        {
            var root = InstallRootValidator.Validate(InstallPath);
            var users = InstallRootValidator.ListUserDirectories(root);
            ResetUserDirectories(users);

            if (string.IsNullOrWhiteSpace(SelectedUserDirectory) && users.Count > 0)
            {
                SelectedUserDirectory = users[0];
            }

            if (!string.IsNullOrWhiteSpace(SelectedUserDirectory))
            {
                var scanResults = await Task.Run(() => _exportScanner.ScanAll(root.InstallRootPath, SelectedUserDirectory));
                ApplyScanResults(ExportOptions, scanResults);
            }

            StatusText = $"已发现账号目录：{UserDirectories.Count}";
            AppendLog($"扫描完成：安装路径={root.InstallRootPath}，账号目录={SelectedUserDirectory}");
        }
        finally
        {
            ProgressText = "就绪";
            IsBusy = false;
        }
    }
}

