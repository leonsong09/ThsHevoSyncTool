using System.Collections.ObjectModel;
using ThsHevoSyncTool.Core.Backup;
using ThsHevoSyncTool.Services;

namespace ThsHevoSyncTool.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly BackupPlanner _exportPlanner;
    private readonly BackupCategoryScanner _exportScanner;
    private readonly BackupPackageWriter _packageWriter;
    private readonly BackupPackageReader _packageReader;
    private readonly BackupImportPlanner _importPlanner;
    private readonly BackupPackageImporter _packageImporter;
    private readonly IDialogService _dialogService;
    private readonly IExportSelectionUserPresetStore _exportSelectionUserPresetStore;
    private readonly IProcessGuard _processGuard;
    private readonly Dictionary<string, ExportSelectionUserPreset> _customExportPresetsBySlot;

    private string _installPath = string.Empty;
    private string _statusText = string.Empty;
    private bool _isBusy;
    private double _progressValue;
    private string _progressText = string.Empty;
    private string _logText = string.Empty;
    private string _selectedUserDirectory = string.Empty;

    private CategoryOptionViewModel? _selectedExportOption;
    private CategoryOptionViewModel? _selectedImportOption;

    public MainViewModel(
        IReadOnlyList<BackupCategory> categories,
        BackupCategoryScanner exportScanner,
        BackupPlanner exportPlanner,
        BackupPackageWriter packageWriter,
        BackupPackageReader packageReader,
        BackupImportPlanner importPlanner,
        BackupPackageImporter packageImporter,
        IDialogService dialogService,
        IExportSelectionUserPresetStore exportSelectionUserPresetStore,
        IProcessGuard processGuard)
    {
        _exportScanner = exportScanner;
        _exportPlanner = exportPlanner;
        _packageWriter = packageWriter;
        _packageReader = packageReader;
        _importPlanner = importPlanner;
        _packageImporter = packageImporter;
        _dialogService = dialogService;
        _exportSelectionUserPresetStore = exportSelectionUserPresetStore;
        _processGuard = processGuard;
        _customExportPresetsBySlot = _exportSelectionUserPresetStore.LoadAll()
            .ToDictionary(static item => item.SlotId, StringComparer.OrdinalIgnoreCase);

        ExportOptions = new(CreateOptionViewModels(categories, isImport: false));
        ImportOptions = new(CreateOptionViewModels(categories, isImport: true));

        BrowseInstallPathCommand = new RelayCommand(BrowseInstallPath, () => !IsBusy);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, OnCommandError, () => !IsBusy);

        ExportSelectCoreCommand = new RelayCommand(() => SelectCore(ExportOptions), () => !IsBusy);
        ExportSelectAllCommand = new RelayCommand(() => SelectAll(ExportOptions), () => !IsBusy);
        ExportSelectNoneCommand = new RelayCommand(() => SelectNone(ExportOptions), () => !IsBusy);
        ExportSelectExistingCommand = new RelayCommand(() => SelectExisting(ExportOptions), () => !IsBusy);
        ExportApplyR1Command = new RelayCommand(() => ApplyPreset(ExportOptions, ExportSelectionPresetCatalog.R1), () => !IsBusy);
        ExportApplyR2Command = new RelayCommand(() => ApplyPreset(ExportOptions, ExportSelectionPresetCatalog.R2), () => !IsBusy);
        ExportApplyR3Command = new RelayCommand(() => ApplyPreset(ExportOptions, ExportSelectionPresetCatalog.R3), () => !IsBusy);
        ExportSaveConfig1Command = new RelayCommand(() => SaveCustomPreset("config1", "配置1", ExportOptions), () => !IsBusy);
        ExportApplyConfig1Command = new RelayCommand(() => ApplyCustomPreset("config1", "配置1", ExportOptions), () => !IsBusy && HasCustomPreset("config1"));
        ExportSaveConfig2Command = new RelayCommand(() => SaveCustomPreset("config2", "配置2", ExportOptions), () => !IsBusy);
        ExportApplyConfig2Command = new RelayCommand(() => ApplyCustomPreset("config2", "配置2", ExportOptions), () => !IsBusy && HasCustomPreset("config2"));
        ExportSaveConfig3Command = new RelayCommand(() => SaveCustomPreset("config3", "配置3", ExportOptions), () => !IsBusy);
        ExportApplyConfig3Command = new RelayCommand(() => ApplyCustomPreset("config3", "配置3", ExportOptions), () => !IsBusy && HasCustomPreset("config3"));
        BrowseExportZipCommand = new RelayCommand(BrowseExportZip, () => !IsBusy);
        StartExportCommand = new AsyncRelayCommand(StartExportAsync, OnCommandError, () => !IsBusy);

        BrowseImportZipCommand = new RelayCommand(BrowseImportZip, () => !IsBusy);
        ImportSelectCoreCommand = new RelayCommand(() => SelectCore(ImportOptions), () => !IsBusy);
        ImportSelectAllCommand = new RelayCommand(() => SelectAll(ImportOptions), () => !IsBusy);
        ImportSelectNoneCommand = new RelayCommand(() => SelectNone(ImportOptions), () => !IsBusy);
        BrowseImportBackupDirectoryCommand = new RelayCommand(BrowseImportBackupDirectory, () => !IsBusy);
        StartImportCommand = new AsyncRelayCommand(StartImportAsync, OnCommandError, () => !IsBusy);

        InstallPath = AutoDetectInstallPath() ?? string.Empty;
        ProgressText = "就绪";
        ImportManifestSummary = "未选择备份包。";
    }

    public ObservableCollection<string> UserDirectories { get; } = new();
    public ObservableCollection<CategoryOptionViewModel> ExportOptions { get; }
    public ObservableCollection<CategoryOptionViewModel> ImportOptions { get; }

    public RelayCommand BrowseInstallPathCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }

    public RelayCommand ExportSelectCoreCommand { get; }
    public RelayCommand ExportSelectAllCommand { get; }
    public RelayCommand ExportSelectNoneCommand { get; }
    public RelayCommand ExportSelectExistingCommand { get; }
    public RelayCommand ExportApplyR1Command { get; }
    public RelayCommand ExportApplyR2Command { get; }
    public RelayCommand ExportApplyR3Command { get; }
    public RelayCommand ExportSaveConfig1Command { get; }
    public RelayCommand ExportApplyConfig1Command { get; }
    public RelayCommand ExportSaveConfig2Command { get; }
    public RelayCommand ExportApplyConfig2Command { get; }
    public RelayCommand ExportSaveConfig3Command { get; }
    public RelayCommand ExportApplyConfig3Command { get; }
    public RelayCommand BrowseExportZipCommand { get; }
    public AsyncRelayCommand StartExportCommand { get; }

    public RelayCommand BrowseImportZipCommand { get; }
    public RelayCommand ImportSelectCoreCommand { get; }
    public RelayCommand ImportSelectAllCommand { get; }
    public RelayCommand ImportSelectNoneCommand { get; }
    public RelayCommand BrowseImportBackupDirectoryCommand { get; }
    public AsyncRelayCommand StartImportCommand { get; }

    public string InstallPath
    {
        get => _installPath;
        set => SetProperty(ref _installPath, value);
    }

    public string SelectedUserDirectory
    {
        get => _selectedUserDirectory;
        set => SetProperty(ref _selectedUserDirectory, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseAllCanExecuteChanged();
            }
        }
    }

    public double ProgressValue
    {
        get => _progressValue;
        set => SetProperty(ref _progressValue, value);
    }

    public string ProgressText
    {
        get => _progressText;
        set => SetProperty(ref _progressText, value);
    }

    public string LogText
    {
        get => _logText;
        set => SetProperty(ref _logText, value);
    }

    public CategoryOptionViewModel? SelectedExportOption
    {
        get => _selectedExportOption;
        set => SetProperty(ref _selectedExportOption, value);
    }

    public CategoryOptionViewModel? SelectedImportOption
    {
        get => _selectedImportOption;
        set => SetProperty(ref _selectedImportOption, value);
    }

    private void BrowseInstallPath()
    {
        var selected = _dialogService.BrowseFolder(InstallPath);
        if (selected is null)
        {
            return;
        }

        InstallPath = selected;
        StatusText = string.Empty;
    }

    private void BrowseImportBackupDirectory()
    {
        var selected = _dialogService.BrowseFolder(ImportBackupDirectory);
        if (selected is null)
        {
            return;
        }

        ImportBackupDirectory = selected;
    }
}
