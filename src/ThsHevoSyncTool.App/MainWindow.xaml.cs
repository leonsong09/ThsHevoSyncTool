using System.Windows;
using System.Windows.Controls;
using ThsHevoSyncTool.Core.Backup;
using ThsHevoSyncTool.Services;
using ThsHevoSyncTool.ViewModels;

namespace ThsHevoSyncTool.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = BuildViewModel();
    }

    private static MainViewModel BuildViewModel()
    {
        var categories = BackupCategoryCatalog.All;
        return new MainViewModel(
            categories: categories,
            exportScanner: new BackupCategoryScanner(categories),
            exportPlanner: new BackupPlanner(categories),
            packageWriter: new BackupPackageWriter(),
            packageReader: new BackupPackageReader(),
            importPlanner: new BackupImportPlanner(),
            packageImporter: new BackupPackageImporter(),
            dialogService: new DialogService(),
            processGuard: new InstallRootProcessGuard());
    }

    private void CheckSelectedExportRows(object sender, RoutedEventArgs e) =>
        SetSelectedRowsChecked(ExportDataGrid, isChecked: true);

    private void UncheckSelectedExportRows(object sender, RoutedEventArgs e) =>
        SetSelectedRowsChecked(ExportDataGrid, isChecked: false);

    private void CheckSelectedImportRows(object sender, RoutedEventArgs e) =>
        SetSelectedRowsChecked(ImportDataGrid, isChecked: true);

    private void UncheckSelectedImportRows(object sender, RoutedEventArgs e) =>
        SetSelectedRowsChecked(ImportDataGrid, isChecked: false);

    private static void SetSelectedRowsChecked(DataGrid grid, bool isChecked)
    {
        foreach (var item in grid.SelectedItems.OfType<CategoryOptionViewModel>())
        {
            if (!item.IsSelectable)
            {
                continue;
            }

            item.IsSelected = isChecked;
        }
    }
}
