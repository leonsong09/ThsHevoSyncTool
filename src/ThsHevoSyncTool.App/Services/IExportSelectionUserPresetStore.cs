namespace ThsHevoSyncTool.Services;

public interface IExportSelectionUserPresetStore
{
    IReadOnlyList<ExportSelectionUserPreset> LoadAll();
    void Save(ExportSelectionUserPreset preset);
}
