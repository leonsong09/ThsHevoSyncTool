namespace ThsHevoSyncTool.Core.Backup;

public sealed record ExportSelectionPreset(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<string> SelectedCategoryIds)
{
    public bool Includes(string categoryId) =>
        SelectedCategoryIds.Contains(categoryId, StringComparer.OrdinalIgnoreCase);
}
