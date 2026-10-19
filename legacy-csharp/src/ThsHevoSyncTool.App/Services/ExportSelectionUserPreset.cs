namespace ThsHevoSyncTool.Services;

public sealed record ExportSelectionUserPreset(
    string SlotId,
    string DisplayName,
    IReadOnlyList<string> SelectedCategoryIds,
    DateTime SavedAtUtc);
