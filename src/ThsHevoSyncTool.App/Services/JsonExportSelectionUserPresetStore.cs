using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThsHevoSyncTool.Services;

public sealed class JsonExportSelectionUserPresetStore : IExportSelectionUserPresetStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _filePath;

    public JsonExportSelectionUserPresetStore(string filePath)
    {
        _filePath = Path.GetFullPath(filePath);
    }

    public static string GetDefaultFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "ThsHevoSyncTool", "export-selection-user-presets.json");
    }

    public IReadOnlyList<ExportSelectionUserPreset> LoadAll()
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<ExportSelectionUserPreset>();
        }

        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<ExportSelectionUserPreset>();
        }

        var payload = JsonSerializer.Deserialize<ExportSelectionUserPresetFile>(json, JsonOptions);
        return payload?.Presets?.ToArray() ?? Array.Empty<ExportSelectionUserPreset>();
    }

    public void Save(ExportSelectionUserPreset preset)
    {
        var presets = LoadAll()
            .ToDictionary(static item => item.SlotId, StringComparer.OrdinalIgnoreCase);
        presets[preset.SlotId] = preset;

        var payload = new ExportSelectionUserPresetFile(
            Presets: presets.Values
                .OrderBy(static item => item.SlotId, StringComparer.OrdinalIgnoreCase)
                .ToArray());

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath) ?? ".");
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        File.WriteAllText(_filePath, json);
    }

    private sealed record ExportSelectionUserPresetFile(
        IReadOnlyList<ExportSelectionUserPreset> Presets);
}
