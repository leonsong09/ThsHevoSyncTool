using ThsHevoSyncTool.Core.Backup;
using ThsHevoSyncTool.Core.IO;
using ThsHevoSyncTool.Services;
using ThsHevoSyncTool.ViewModels;

namespace ThsHevoSyncTool.App.Tests;

public sealed class MainViewModelExportSelectionTests
{
    [Fact]
    public void ExportSelectExistingCommand_SelectsOnlyCategoriesWithFiles()
    {
        var categories = new[]
        {
            CreateCategory("present", isCoreDefault: false, "present.txt"),
            CreateCategory("empty", isCoreDefault: false, "empty.txt"),
        };

        var viewModel = CreateViewModel(categories);
        var present = Assert.Single(viewModel.ExportOptions.Where(static x => x.Id == "present"));
        var empty = Assert.Single(viewModel.ExportOptions.Where(static x => x.Id == "empty"));

        present.FileCount = 2;
        empty.FileCount = 0;
        present.IsSelected = false;
        empty.IsSelected = true;

        viewModel.ExportSelectExistingCommand.Execute(parameter: null);

        Assert.True(present.IsSelected);
        Assert.False(empty.IsSelected);
    }

    [Fact]
    public void ExportApplyR1Command_UsesPresetCategoryIds()
    {
        var categories = BackupCategoryCatalog.All;
        var viewModel = CreateViewModel(categories);

        viewModel.ExportSelectNoneCommand.Execute(parameter: null);
        viewModel.ExportApplyR1Command.Execute(parameter: null);

        var selectedIds = viewModel.ExportOptions
            .Where(static option => option.IsSelected)
            .Select(static option => option.Id)
            .ToArray();

        Assert.Equal(
            ExportSelectionPresetCatalog.GetById("r1").SelectedCategoryIds,
            selectedIds);
    }

    [Fact]
    public void ExportSaveConfig1Command_SavesCurrentSelectedCategoryIds()
    {
        var categories = new[]
        {
            CreateCategory("first", isCoreDefault: false, "first.txt"),
            CreateCategory("second", isCoreDefault: false, "second.txt"),
        };

        var presetStore = new StubExportSelectionUserPresetStore();
        var viewModel = CreateViewModel(categories, presetStore);
        viewModel.ExportOptions.Single(x => x.Id == "first").IsSelected = true;
        viewModel.ExportOptions.Single(x => x.Id == "second").IsSelected = false;

        viewModel.ExportSaveConfig1Command.Execute(parameter: null);

        var saved = Assert.Single(presetStore.Items);
        Assert.Equal("config1", saved.SlotId);
        Assert.Equal("配置1", saved.DisplayName);
        Assert.Equal(["first"], saved.SelectedCategoryIds);
    }

    [Fact]
    public void ExportApplyConfig1Command_AppliesSavedCategoryIds()
    {
        var categories = new[]
        {
            CreateCategory("first", isCoreDefault: false, "first.txt"),
            CreateCategory("second", isCoreDefault: false, "second.txt"),
        };

        var presetStore = new StubExportSelectionUserPresetStore(
            new ExportSelectionUserPreset(
                SlotId: "config1",
                DisplayName: "配置1",
                SelectedCategoryIds: ["second"],
                SavedAtUtc: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc)));

        var viewModel = CreateViewModel(categories, presetStore);
        viewModel.ExportSelectAllCommand.Execute(parameter: null);

        viewModel.ExportApplyConfig1Command.Execute(parameter: null);

        Assert.False(viewModel.ExportOptions.Single(x => x.Id == "first").IsSelected);
        Assert.True(viewModel.ExportOptions.Single(x => x.Id == "second").IsSelected);
    }

    private static MainViewModel CreateViewModel(
        IReadOnlyList<BackupCategory> categories,
        IExportSelectionUserPresetStore? presetStore = null) =>
        new(
            categories: categories,
            exportScanner: new BackupCategoryScanner(categories),
            exportPlanner: new BackupPlanner(categories),
            packageWriter: new BackupPackageWriter(),
            packageReader: new BackupPackageReader(),
            importPlanner: new BackupImportPlanner(),
            packageImporter: new BackupPackageImporter(),
            dialogService: new StubDialogService(),
            processGuard: new StubProcessGuard(),
            exportSelectionUserPresetStore: presetStore ?? new StubExportSelectionUserPresetStore());

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

    private sealed class StubDialogService : IDialogService
    {
        public string? BrowseFolder(string? initialPath) => initialPath;
        public string? SaveZip(string? initialDirectory, string suggestedFileName) => null;
        public string? OpenZip(string? initialDirectory) => null;
        public bool ConfirmExportPreview(ExportPreviewSummary summary) => true;
    }

    private sealed class StubExportSelectionUserPresetStore : IExportSelectionUserPresetStore
    {
        public StubExportSelectionUserPresetStore(params ExportSelectionUserPreset[] presets)
        {
            Items = presets.ToList();
        }

        public List<ExportSelectionUserPreset> Items { get; }

        public IReadOnlyList<ExportSelectionUserPreset> LoadAll() => Items.ToArray();

        public void Save(ExportSelectionUserPreset preset)
        {
            var existingIndex = Items.FindIndex(item => string.Equals(item.SlotId, preset.SlotId, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                Items[existingIndex] = preset;
                return;
            }

            Items.Add(preset);
        }
    }

    private sealed class StubProcessGuard : IProcessGuard
    {
        public IReadOnlyList<RunningProcessInfo> FindRunningUnderInstallRoot(string installRootPath) =>
            Array.Empty<RunningProcessInfo>();
    }
}

