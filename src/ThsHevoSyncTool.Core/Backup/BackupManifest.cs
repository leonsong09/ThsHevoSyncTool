using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThsHevoSyncTool.Core.Backup;

public sealed record BackupManifest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("tool")] BackupToolInfo Tool,
    [property: JsonPropertyName("createdAtUtc")] DateTime CreatedAtUtc,
    [property: JsonPropertyName("source")] BackupSourceInfo Source,
    [property: JsonPropertyName("selection")] BackupSelectionInfo Selection,
    [property: JsonPropertyName("files")] IReadOnlyList<BackupManifestFileEntry> Files)
{
    public const string CurrentSchemaVersion = "1";

    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static BackupManifest FromJson(string json) =>
        JsonSerializer.Deserialize<BackupManifest>(json, JsonOptions)
        ?? throw new InvalidOperationException("无法解析 manifest.json。");
}

public sealed record BackupToolInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("runtime")] string Runtime);

public sealed record BackupSourceInfo(
    [property: JsonPropertyName("installRoot")] string InstallRoot,
    [property: JsonPropertyName("userDirName")] string UserDirName,
    [property: JsonPropertyName("appExeVersion")] string? AppExeVersion);

public sealed record BackupSelectionInfo(
    [property: JsonPropertyName("defaultPreset")] string DefaultPreset,
    [property: JsonPropertyName("categories")] IReadOnlyList<string> Categories);

public sealed record BackupManifestFileEntry(
    [property: JsonPropertyName("relativePath")] string RelativePath,
    [property: JsonPropertyName("sizeBytes")] long SizeBytes,
    [property: JsonPropertyName("sha256")] string Sha256,
    [property: JsonPropertyName("lastWriteTimeUtc")] DateTime LastWriteTimeUtc,
    [property: JsonPropertyName("categoryId")] string CategoryId);

