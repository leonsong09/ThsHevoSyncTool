using System.Reflection;
using ThsHevoSyncTool.Core.Backup;
using ThsHevoSyncTool.Core.IO;
using ThsHevoSyncTool.Services;
using ThsHevoSyncTool.ViewModels;

namespace ThsHevoSyncTool.App.Tests;

public sealed class MainViewModelRefreshTests
{
    [Fact]
    public void ApplyManifestToImportOptions_KeepsExportAndImportSelectionsInSync()
    {
        var categories = new[]
        {
            CreateCategory("present", isCoreDefault: true, "present.txt"),
            CreateCategory("empty_but_selected", isCoreDefault: false, "empty.txt"),
            CreateCategory("not_selected", isCoreDefault: false, "other.txt"),
        };

        var viewModel = new MainViewModel(
            categories: categories,
            exportScanner: new BackupCategoryScanner(categories),
            exportPlanner: new BackupPlanner(categories),
            packageWriter: new BackupPackageWriter(),
            packageReader: new BackupPackageReader(),
            importPlanner: new BackupImportPlanner(),
            packageImporter: new BackupPackageImporter(),
            dialogService: new StubDialogService(),
            processGuard: new StubProcessGuard());

        var manifest = new BackupManifest(
            SchemaVersion: BackupManifest.CurrentSchemaVersion,
            Tool: new BackupToolInfo("ThsHevoSyncTool", "0.0.0-test", ".NET"),
            CreatedAtUtc: DateTime.UtcNow,
            Source: new BackupSourceInfo(@"D:\Src", "mx_src", null),
            Selection: new BackupSelectionInfo(
                DefaultPreset: "custom",
                Categories: ["present", "empty_but_selected"]),
            Files:
            [
                new BackupManifestFileEntry(
                    RelativePath: @"bin\users\mx_src\present.txt",
                    SizeBytes: 2,
                    Sha256: "00",
                    LastWriteTimeUtc: DateTime.UtcNow,
                    CategoryId: "present"),
            ]);

        InvokePrivate(viewModel, "ApplyManifestToImportOptions", manifest);

        var present = Assert.Single(viewModel.ImportOptions.Where(static x => x.Id == "present"));
        var emptyButSelected = Assert.Single(viewModel.ImportOptions.Where(static x => x.Id == "empty_but_selected"));
        var notSelected = Assert.Single(viewModel.ImportOptions.Where(static x => x.Id == "not_selected"));

        Assert.True(present.IsAvailable);
        Assert.True(present.IsSelected);
        Assert.Equal(1, present.FileCount);

        Assert.True(emptyButSelected.IsAvailable);
        Assert.True(emptyButSelected.IsSelected);
        Assert.Equal(0, emptyButSelected.FileCount);

        Assert.False(notSelected.IsAvailable);
        Assert.False(notSelected.IsSelected);
        Assert.Equal(0, notSelected.FileCount);
    }

    [Fact]
    public async Task ExportedManifest_KeepsImportSelectionsInSync()
    {
        var tempRoot = Directory.CreateTempSubdirectory("ths_export_import_sync_");
        try
        {
            var userDir = Path.Combine(tempRoot.FullName, "bin", "users", "mx_demo");
            Directory.CreateDirectory(userDir);
            await File.WriteAllTextAsync(Path.Combine(userDir, "present.txt"), "ok");

            var categories = new[]
            {
                CreateCategory("present", isCoreDefault: true, "present.txt"),
                CreateCategory("empty_but_selected", isCoreDefault: false, "empty.txt"),
                CreateCategory("not_selected", isCoreDefault: false, "other.txt"),
            };

            var planner = new BackupPlanner(categories);
            var writer = new BackupPackageWriter();
            var reader = new BackupPackageReader();

            var zipPath = Path.Combine(tempRoot.FullName, "backup.zip");
            var selectedCategoryIds = new[] { "present", "empty_but_selected" };

            var plan = planner.CreatePlan(tempRoot.FullName, "mx_demo", selectedCategoryIds);
            var manifest = await writer.WriteZipAsync(
                plan: plan,
                zipPath: zipPath,
                selectedCategoryIds: selectedCategoryIds,
                toolVersion: "0.0.0-test",
                appExeVersion: null,
                progress: null,
                cancellationToken: CancellationToken.None);

            var loadedManifest = await reader.ReadManifestAsync(zipPath, CancellationToken.None);

            Assert.Equal(manifest.Selection.Categories, loadedManifest.Selection.Categories);

            var viewModel = new MainViewModel(
                categories: categories,
                exportScanner: new BackupCategoryScanner(categories),
                exportPlanner: planner,
                packageWriter: writer,
                packageReader: reader,
                importPlanner: new BackupImportPlanner(),
                packageImporter: new BackupPackageImporter(),
                dialogService: new StubDialogService(),
                processGuard: new StubProcessGuard());

            InvokePrivate(viewModel, "ApplyManifestToImportOptions", loadedManifest);

            var present = Assert.Single(viewModel.ImportOptions.Where(static x => x.Id == "present"));
            var emptyButSelected = Assert.Single(viewModel.ImportOptions.Where(static x => x.Id == "empty_but_selected"));
            var notSelected = Assert.Single(viewModel.ImportOptions.Where(static x => x.Id == "not_selected"));

            Assert.True(present.IsAvailable);
            Assert.True(present.IsSelected);
            Assert.Equal(1, present.FileCount);

            Assert.True(emptyButSelected.IsAvailable);
            Assert.True(emptyButSelected.IsSelected);
            Assert.Equal(0, emptyButSelected.FileCount);

            Assert.False(notSelected.IsAvailable);
            Assert.False(notSelected.IsSelected);
            Assert.Equal(0, notSelected.FileCount);
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

    private static void InvokePrivate(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        method!.Invoke(instance, args);
    }

    private sealed class StubDialogService : IDialogService
    {
        public string? BrowseFolder(string? initialPath) => initialPath;

        public string? SaveZip(string? initialDirectory, string suggestedFileName) =>
            initialDirectory is null ? suggestedFileName : Path.Combine(initialDirectory, suggestedFileName);

        public string? OpenZip(string? initialDirectory) => null;
    }

    private sealed class StubProcessGuard : IProcessGuard
    {
        public IReadOnlyList<RunningProcessInfo> FindRunningUnderInstallRoot(string installRootPath) =>
            Array.Empty<RunningProcessInfo>();
    }
}
