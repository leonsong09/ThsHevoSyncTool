using System.Windows.Forms;

namespace ThsHevoSyncTool.Services;

public sealed class DialogService : IDialogService
{
    public string? BrowseFolder(string? initialPath)
    {
        using var dialog = new FolderBrowserDialog
        {
            SelectedPath = initialPath ?? string.Empty,
            ShowNewFolderButton = false,
        };

        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
    }

    public string? SaveZip(string? initialDirectory, string suggestedFileName)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Zip 文件 (*.zip)|*.zip",
            FileName = suggestedFileName,
            InitialDirectory = initialDirectory,
            AddExtension = true,
            DefaultExt = ".zip",
            OverwritePrompt = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? OpenZip(string? initialDirectory)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Zip 文件 (*.zip)|*.zip",
            InitialDirectory = initialDirectory,
            CheckFileExists = true,
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
