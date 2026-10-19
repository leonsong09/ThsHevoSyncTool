using ThsHevoSyncTool.Core.Backup;

namespace ThsHevoSyncTool.Core.Tests;

public sealed class BackupManifestTests
{
    [Fact]
    public void Roundtrip_ToJson_FromJson()
    {
        var manifest = new BackupManifest(
            SchemaVersion: BackupManifest.CurrentSchemaVersion,
            Tool: new BackupToolInfo("ThsHevoSyncTool", "0.0.0-test", ".NET"),
            CreatedAtUtc: new DateTime(2026, 2, 27, 0, 0, 0, DateTimeKind.Utc),
            Source: new BackupSourceInfo("D:\\App", "mx_123", "1.0.0"),
            Selection: new BackupSelectionInfo("core", ["custom_indicators"]),
            Files:
            [
                new BackupManifestFileEntry(
                    RelativePath: "bin\\users\\mx_123\\UserPageCoding.xml",
                    SizeBytes: 123,
                    Sha256: "00",
                    LastWriteTimeUtc: new DateTime(2026, 2, 27, 1, 2, 3, DateTimeKind.Utc),
                    CategoryId: "page_chart_indicator_selection"),
            ]);

        var json = manifest.ToJson();
        var parsed = BackupManifest.FromJson(json);

        Assert.Equal(manifest.SchemaVersion, parsed.SchemaVersion);
        Assert.Equal(manifest.Tool.Name, parsed.Tool.Name);
        Assert.Equal(manifest.Source.UserDirName, parsed.Source.UserDirName);
        Assert.Single(parsed.Files);
        Assert.Equal(manifest.Files[0].RelativePath, parsed.Files[0].RelativePath);
    }
}

