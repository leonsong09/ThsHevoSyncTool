using ThsHevoSyncTool.Core.Backup;

namespace ThsHevoSyncTool.Core.Tests;

public sealed class BackupImportPlannerTests
{
    [Fact]
    public void CreatePlan_RewritesUserDir_WhenMatchesSourceUser()
    {
        var manifest = new BackupManifest(
            SchemaVersion: BackupManifest.CurrentSchemaVersion,
            Tool: new BackupToolInfo("ThsHevoSyncTool", "0.0.0-test", ".NET"),
            CreatedAtUtc: DateTime.UtcNow,
            Source: new BackupSourceInfo("D:\\Src", "mx_src", null),
            Selection: new BackupSelectionInfo("core", ["page_chart_indicator_selection"]),
            Files:
            [
                new BackupManifestFileEntry(
                    RelativePath: "bin\\users\\mx_src\\UserPageCoding.xml",
                    SizeBytes: 1,
                    Sha256: "00",
                    LastWriteTimeUtc: DateTime.UtcNow,
                    CategoryId: "page_chart_indicator_selection"),
            ]);

        var tempRoot = Directory.CreateTempSubdirectory("ths_import_plan_");
        try
        {
            Directory.CreateDirectory(Path.Combine(tempRoot.FullName, "bin", "users"));
            var planner = new BackupImportPlanner();

            var plan = planner.CreatePlan(
                zipPath: "D:\\dummy.zip",
                manifest: manifest,
                targetInstallRootPath: tempRoot.FullName,
                targetUserDirName: "mx_dst",
                selectedCategoryIds: ["page_chart_indicator_selection"]);

            Assert.Single(plan.Files);
            Assert.Equal("bin\\users\\mx_dst\\UserPageCoding.xml", plan.Files[0].TargetRelativePath);
        }
        finally
        {
            tempRoot.Delete(recursive: true);
        }
    }
}

