using System.Diagnostics;
using System.Reflection;
using ThsHevoSyncTool.Core.Backup;
using ThsHevoSyncTool.Core.Environment;
using ThsHevoSyncTool.Core.IO;
using ThsHevoSyncTool.Formatting;
using ThsHevoSyncTool.Services;

namespace ThsHevoSyncTool.ViewModels;

public sealed partial class MainViewModel
{
    private void EnsureInstallRootReady() => InstallRootValidator.Validate(InstallPath);

    private void EnsureUserSelected()
    {
        if (string.IsNullOrWhiteSpace(SelectedUserDirectory))
        {
            throw new InvalidOperationException("未选择账号目录（mx_*）。");
        }
    }

    private void EnsureNoRunningProcesses()
    {
        var runningAll = _processGuard.FindRunningUnderInstallRoot(InstallPath);
        if (runningAll.Count == 0)
        {
            return;
        }

        var currentPid = System.Environment.ProcessId;
        var runningMain = runningAll
            .Where(p => p.ProcessId != currentPid)
            .Where(static p => !string.Equals(p.Name, "ThsHevoSyncTool", StringComparison.OrdinalIgnoreCase))
            .Where(static p => IsThsMainExecutable(p.ExecutablePath))
            .OrderBy(static p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (runningMain.Length == 0)
        {
            return;
        }

        var lines = runningMain.Select(p => $"{p.Name} (PID {p.ProcessId}) - {p.ExecutablePath}");
        var nl = System.Environment.NewLine;
        throw new InvalidOperationException("检测到同花顺主程序仍在运行，请先退出：" + nl + string.Join(nl, lines));
    }

    private static bool IsThsMainExecutable(string executablePath)
    {
        var fileName = Path.GetFileName(executablePath);
        return string.Equals(fileName, "happ.exe", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(fileName, "hexinlauncher.exe", StringComparison.OrdinalIgnoreCase);
    }

    private void EnsureTargetUserDirectoryExists()
    {
        var root = InstallRootValidator.Validate(InstallPath);
        var userPath = Path.Combine(root.UsersRootPath, SelectedUserDirectory);
        if (Directory.Exists(userPath))
        {
            return;
        }

        throw new InvalidOperationException(
            $"目标机未找到账号目录：{userPath}\n请先运行同花顺并登录一次生成目录后再导入。");
    }

    private void EnsureTargetWritable()
    {
        var root = InstallRootValidator.Validate(InstallPath);
        WriteAccessChecker.AssertDirectoryWritable(root.UsersRootPath);
    }

    private void ResetUserDirectories(IReadOnlyList<string> users)
    {
        UserDirectories.Clear();
        foreach (var user in users)
        {
            UserDirectories.Add(user);
        }
    }

    private static void ApplyScanResults(
        IEnumerable<CategoryOptionViewModel> options,
        IReadOnlyList<BackupCategoryScanResult> scanResults)
    {
        var map = scanResults.ToDictionary(static r => r.CategoryId, StringComparer.OrdinalIgnoreCase);
        foreach (var option in options)
        {
            if (map.TryGetValue(option.Id, out var stats))
            {
                option.FileCount = stats.FileCount;
                option.TotalBytes = stats.TotalBytes;
            }
            else
            {
                option.FileCount = 0;
                option.TotalBytes = 0;
            }
        }
    }

    private static string BuildManifestSummary(BackupManifest manifest)
    {
        var fileCount = manifest.Files.Count;
        var totalBytes = manifest.Files.Sum(static f => f.SizeBytes);
        var categories = string.Join(", ", manifest.Selection.Categories);

        return
            $"工具：{manifest.Tool.Name} {manifest.Tool.Version}\n" +
            $"创建时间(UTC)：{manifest.CreatedAtUtc:yyyy-MM-dd HH:mm:ss}\n" +
            $"源安装路径：{manifest.Source.InstallRoot}\n" +
            $"源账号目录：{manifest.Source.UserDirName}\n" +
            $"同花顺版本：{manifest.Source.AppExeVersion ?? "未知"}\n" +
            $"文件数：{fileCount}\n" +
            $"大小：{ByteFormatter.Format(totalBytes)}\n" +
            $"包含选项：{categories}";
    }

    private static void SelectCore(IEnumerable<CategoryOptionViewModel> options)
    {
        foreach (var option in options)
        {
            option.IsSelected = option.IsCoreDefault && option.IsSelectable;
        }
    }

    private static void SelectAll(IEnumerable<CategoryOptionViewModel> options)
    {
        foreach (var option in options)
        {
            option.IsSelected = option.IsSelectable;
        }
    }

    private static void SelectNone(IEnumerable<CategoryOptionViewModel> options)
    {
        foreach (var option in options)
        {
            option.IsSelected = false;
        }
    }

    private static void SelectExisting(IEnumerable<CategoryOptionViewModel> options)
    {
        foreach (var option in options)
        {
            option.IsSelected = option.IsSelectable && option.FileCount > 0;
        }
    }

    private static void ApplyPreset(
        IEnumerable<CategoryOptionViewModel> options,
        ExportSelectionPreset preset)
    {
        var included = preset.SelectedCategoryIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var option in options)
        {
            option.IsSelected = option.IsSelectable && included.Contains(option.Id);
        }
    }

    private void SaveCustomPreset(
        string slotId,
        string displayName,
        IEnumerable<CategoryOptionViewModel> options)
    {
        var selectedCategoryIds = options
            .Where(static option => option.IsSelected)
            .Select(static option => option.Id)
            .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (selectedCategoryIds.Length == 0)
        {
            throw new InvalidOperationException("未选择任何导出选项，无法保存配置。");
        }

        var preset = new ExportSelectionUserPreset(
            SlotId: slotId,
            DisplayName: displayName,
            SelectedCategoryIds: selectedCategoryIds,
            SavedAtUtc: DateTime.UtcNow);

        _exportSelectionUserPresetStore.Save(preset);
        _customExportPresetsBySlot[slotId] = preset;
        AppendLog($"已保存 {displayName}：{selectedCategoryIds.Length} 项。");
        RaiseAllCanExecuteChanged();
    }

    private void ApplyCustomPreset(
        string slotId,
        string displayName,
        IEnumerable<CategoryOptionViewModel> options)
    {
        if (!_customExportPresetsBySlot.TryGetValue(slotId, out var preset))
        {
            throw new InvalidOperationException($"尚未保存 {displayName}。");
        }

        var selected = preset.SelectedCategoryIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var option in options)
        {
            option.IsSelected = option.IsSelectable && selected.Contains(option.Id);
        }

        AppendLog($"已应用 {preset.DisplayName}：{preset.SelectedCategoryIds.Count} 项。");
    }

    private bool HasCustomPreset(string slotId) =>
        _customExportPresetsBySlot.ContainsKey(slotId);

    private static IReadOnlyList<CategoryOptionViewModel> CreateOptionViewModels(
        IReadOnlyList<BackupCategory> categories,
        bool isImport)
    {
        return categories
            .Select(c =>
            {
                var vm = new CategoryOptionViewModel(c.Id, c.Name, c.Description, c.IsCoreDefault, c.DisplayPathRules);
                vm.IsSelected = !isImport && c.IsCoreDefault;
                vm.IsAvailable = true;
                return vm;
            })
            .ToArray();
    }

    private void AppendLog(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
        LogText = string.IsNullOrWhiteSpace(LogText) ? line : $"{LogText}\n{line}";
    }

    private void OnCommandError(Exception ex)
    {
        AppendLog("错误：" + ex.Message);
        System.Windows.MessageBox.Show(
            ex.Message,
            "错误",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Error);
    }

    private void RaiseAllCanExecuteChanged()
    {
        BrowseInstallPathCommand.RaiseCanExecuteChanged();
        RefreshCommand.RaiseCanExecuteChanged();

        ExportSelectCoreCommand.RaiseCanExecuteChanged();
        ExportSelectAllCommand.RaiseCanExecuteChanged();
        ExportSelectNoneCommand.RaiseCanExecuteChanged();
        ExportSelectExistingCommand.RaiseCanExecuteChanged();
        ExportApplyR1Command.RaiseCanExecuteChanged();
        ExportApplyR2Command.RaiseCanExecuteChanged();
        ExportApplyR3Command.RaiseCanExecuteChanged();
        ExportSaveConfig1Command.RaiseCanExecuteChanged();
        ExportApplyConfig1Command.RaiseCanExecuteChanged();
        ExportSaveConfig2Command.RaiseCanExecuteChanged();
        ExportApplyConfig2Command.RaiseCanExecuteChanged();
        ExportSaveConfig3Command.RaiseCanExecuteChanged();
        ExportApplyConfig3Command.RaiseCanExecuteChanged();
        BrowseExportZipCommand.RaiseCanExecuteChanged();
        StartExportCommand.RaiseCanExecuteChanged();

        BrowseImportZipCommand.RaiseCanExecuteChanged();
        ImportSelectCoreCommand.RaiseCanExecuteChanged();
        ImportSelectAllCommand.RaiseCanExecuteChanged();
        ImportSelectNoneCommand.RaiseCanExecuteChanged();
        BrowseImportBackupDirectoryCommand.RaiseCanExecuteChanged();
        StartImportCommand.RaiseCanExecuteChanged();
    }

    private static string GetToolVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version?.ToString() ?? "0.0.0";
    }

    private static string? TryGetThsExeVersion(string installRootPath)
    {
        var candidate = Path.Combine(installRootPath, "bin", "hexinlauncher.exe");
        if (!File.Exists(candidate))
        {
            return null;
        }

        var info = FileVersionInfo.GetVersionInfo(candidate);
        return info.FileVersion;
    }

    private static string? AutoDetectInstallPath()
    {
        var current = Environment.CurrentDirectory;
        if (LooksLikeInstallRoot(current))
        {
            return current;
        }

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        if (LooksLikeInstallRoot(baseDir))
        {
            return baseDir;
        }

        return null;
    }

    private static bool LooksLikeInstallRoot(string path)
    {
        try
        {
            return Directory.Exists(Path.Combine(path, "bin", "users"));
        }
        catch
        {
            return false;
        }
    }
}

