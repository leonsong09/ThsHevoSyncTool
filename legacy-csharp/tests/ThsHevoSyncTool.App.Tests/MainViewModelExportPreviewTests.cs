using System.Reflection;
using ThsHevoSyncTool.Core.Backup;
using ThsHevoSyncTool.Core.IO;
using ThsHevoSyncTool.Services;
using ThsHevoSyncTool.ViewModels;

namespace ThsHevoSyncTool.App.Tests;

public sealed class MainViewModelExportPreviewTests
{
    [Fact]
    public async Task StartExportAsync_WhenPreviewRejected_DoesNotCreateZip()
    {
        var tempRoot = Directory.CreateTempSubdirectory("ths_export_preview_reject_");
        try
        {
            var userDir = Path.Combine(tempRoot.FullName, "bin", "users", "mx_demo");
            Directory.CreateDirectory(userDir);
            await File.WriteAllTextAsync(Path.Combine(userDir, "present.txt"), "ok");

            var categories = new[]
            {
                CreateCategory("present", isCoreDefault: true, "present.txt"),
            };

            var dialog = new PreviewDialogService(confirmExportPreview: false);
            var viewModel = new MainViewModel(
                categories: categories,
                exportScanner: new BackupCategoryScanner(categories),
                exportPlanner: new BackupPlanner(categories),
                packageWriter: new BackupPackageWriter(),
                packageReader: new BackupPackageReader(),
                importPlanner: new BackupImportPlanner(),
                packageImporter: new BackupPackageImporter(),
                dialogService: dialog,
                exportSelectionUserPresetStore: new StubExportSelectionUserPresetStore(),
                processGuard: new StubProcessGuard());

            viewModel.InstallPath = tempRoot.FullName;
            viewModel.SelectedUserDirectory = "mx_demo";
            viewModel.ExportZipPath = Path.Combine(tempRoot.FullName, "backup.zip");

            await InvokePrivateAsync(viewModel, "StartExportAsync");

            Assert.False(File.Exists(viewModel.ExportZipPath));
            Assert.Contains("已取消导出", viewModel.LogText, StringComparison.Ordinal);
            Assert.NotNull(dialog.LastPreview);
            Assert.Single(dialog.LastPreview!.Items);
            Assert.Equal(1, dialog.LastPreview.TotalFiles);
        }
        finally
        {
            tempRoot.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task StartExportAsync_WhenPreviewAccepted_CreatesZip()
    {
        var tempRoot = Directory.CreateTempSubdirectory("ths_export_preview_accept_");
        try
        {
            var userDir = Path.Combine(tempRoot.FullName, "bin", "users", "mx_demo");
            Directory.CreateDirectory(userDir);
            await File.WriteAllTextAsync(Path.Combine(userDir, "present.txt"), "ok");

            var categories = new[]
            {
                CreateCategory("present", isCoreDefault: true, "present.txt"),
            };

            var dialog = new PreviewDialogService(confirmExportPreview: true);
            var viewModel = new MainViewModel(
                categories: categories,
                exportScanner: new BackupCategoryScanner(categories),
                exportPlanner: new BackupPlanner(categories),
                packageWriter: new BackupPackageWriter(),
                packageReader: new BackupPackageReader(),
                importPlanner: new BackupImportPlanner(),
                packageImporter: new BackupPackageImporter(),
                dialogService: dialog,
                exportSelectionUserPresetStore: new StubExportSelectionUserPresetStore(),
                processGuard: new StubProcessGuard());

            viewModel.InstallPath = tempRoot.FullName;
            viewModel.SelectedUserDirectory = "mx_demo";
            viewModel.ExportZipPath = Path.Combine(tempRoot.FullName, "backup.zip");

            await InvokePrivateAsync(viewModel, "StartExportAsync");

            Assert.True(File.Exists(viewModel.ExportZipPath));
            Assert.NotNull(dialog.LastPreview);
            Assert.Single(dialog.LastPreview!.Items);
            Assert.Contains("导出完成", viewModel.LogText, StringComparison.Ordinal);
        }
        finally
        {
            tempRoot.Delete(recursive: true);
        }
    }

    private static BackupCategory CreateCategory(string id, bool isCoreDefault, string fileName) =>
        new(
            Id: id,
            Name: id,
            Description: id,
            IsCoreDefault: isCoreDefault,
            Paths:
            [
                new PathSpec(
                    Kind: PathSpecKind.File,
                    RelativePathTemplate: $@"bin\users\{BackupCategoryCatalog.UserPlaceholder}\{fileName}",
                    Recursive: false,
                    SearchPattern: string.Empty),
            ]);

    private static async Task InvokePrivateAsync(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = method!.Invoke(instance, args);
        if (result is Task task)
        {
            await task;
        }
    }

    private sealed class PreviewDialogService : IDialogService
    {
        private readonly bool _confirmExportPreview;

        public PreviewDialogService(bool confirmExportPreview)
        {
            _confirmExportPreview = confirmExportPreview;
        }

        public ExportPreviewSummary? LastPreview { get; private set; }

        public string? BrowseFolder(string? initialPath) => initialPath;

        public string? SaveZip(string? initialDirectory, string suggestedFileName) =>
            initialDirectory is null ? suggestedFileName : Path.Combine(initialDirectory, suggestedFileName);

        public string? OpenZip(string? initialDirectory) => null;

        public bool ConfirmExportPreview(ExportPreviewSummary summary)
        {
            LastPreview = summary;
            return _confirmExportPreview;
        }
    }

    private sealed class StubProcessGuard : IProcessGuard
    {
        public IReadOnlyList<RunningProcessInfo> FindRunningUnderInstallRoot(string installRootPath) =>
            Array.Empty<RunningProcessInfo>();
    }

    private sealed class StubExportSelectionUserPresetStore : IExportSelectionUserPresetStore
    {
        public IReadOnlyList<ExportSelectionUserPreset> LoadAll() => Array.Empty<ExportSelectionUserPreset>();

        public void Save(ExportSelectionUserPreset preset)
        {
        }
    }
}
