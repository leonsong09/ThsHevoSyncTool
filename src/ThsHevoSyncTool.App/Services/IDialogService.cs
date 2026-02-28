namespace ThsHevoSyncTool.Services;

public interface IDialogService
{
    string? BrowseFolder(string? initialPath);
    string? SaveZip(string? initialDirectory, string suggestedFileName);
    string? OpenZip(string? initialDirectory);
}

