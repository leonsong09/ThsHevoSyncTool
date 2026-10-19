using ThsHevoSyncTool.Core.Backup;
using ThsHevoSyncTool.Core.IO;

namespace ThsHevoSyncTool.Core.Tests;

public sealed class BackupPackageImporterTests
{
    [Fact]
    public async Task CreatePreImportBackupZipAsync_CreatesReimportableBackupPackage()
    {
        var sourceRoot = Directory.CreateTempSubdirectory("ths_pre_import_src_");
        var targetRoot = Directory.CreateTempSubdirectory("ths_pre_import_dst_");

        try
        {
            const string sourceUser = "mx_src";
            const string targetUser = "mx_dst";
            const string categoryId = "docking_layout";
            const string fileRelativeToUser = @"CustomPage\CustomDockingTemplate\AStockTemplate.config";
            var relativePath = $@"bin\users\{targetUser}\{fileRelativeToUser}";

            var sourceFile = Path.Combine(sourceRoot.FullName, "bin", "users", sourceUser, "CustomPage", "CustomDockingTemplate", "AStockTemplate.config");
            var targetFile = Path.Combine(targetRoot.FullName, "bin", "users", targetUser, "CustomPage", "CustomDockingTemplate", "AStockTemplate.config");

            Directory.CreateDirectory(Path.GetDirectoryName(sourceFile)!);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

            await File.WriteAllTextAsync(sourceFile, "new-layout");
            await File.WriteAllTextAsync(targetFile, "old-layout");

            var originalTargetHash = await Sha256.HashFileAsync(targetFile, CancellationToken.None);

            var categories = new[]
            {
                new BackupCategory(
                    Id: categoryId,
                    Name: "页面停靠布局",
                    Description: "测试用 CustomDockingTemplate 配置",
                    IsCoreDefault: true,
                    Paths:
                    [
                        new PathSpec(
                            Kind: PathSpecKind.File,
                            RelativePathTemplate: $@"bin\users\{BackupCategoryCatalog.UserPlaceholder}\{fileRelativeToUser}",
                            Recursive: false,
                            SearchPattern: string.Empty),
                    ]),
            };

            var exportPlanner = new BackupPlanner(categories);
            var packageWriter = new BackupPackageWriter();
            var packageReader = new BackupPackageReader();
            var importPlanner = new BackupImportPlanner();
            var packageImporter = new BackupPackageImporter();

            var exportZipPath = Path.Combine(sourceRoot.FullName, "export.zip");
            var exportPlan = exportPlanner.CreatePlan(sourceRoot.FullName, sourceUser, [categoryId]);
            var exportManifest = await packageWriter.WriteZipAsync(
                plan: exportPlan,
                zipPath: exportZipPath,
                selectedCategoryIds: [categoryId],
                toolVersion: "0.0.0-test",
                appExeVersion: null,
                progress: null,
                cancellationToken: CancellationToken.None);

            var importPlan = importPlanner.CreatePlan(
                zipPath: exportZipPath,
                manifest: exportManifest,
                targetInstallRootPath: targetRoot.FullName,
                targetUserDirName: targetUser,
                selectedCategoryIds: [categoryId]);

            var preImportBackupZipPath = Path.Combine(targetRoot.FullName, "pre_import_backup.zip");
            await packageImporter.CreatePreImportBackupZipAsync(
                plan: importPlan,
                backupZipPath: preImportBackupZipPath,
                progress: null,
                cancellationToken: CancellationToken.None);

            var backupManifest = await packageReader.ReadManifestAsync(preImportBackupZipPath, CancellationToken.None);

            Assert.Equal(targetRoot.FullName, backupManifest.Source.InstallRoot);
            Assert.Equal(targetUser, backupManifest.Source.UserDirName);
            Assert.Equal([categoryId], backupManifest.Selection.Categories);

            var file = Assert.Single(backupManifest.Files);
            Assert.Equal(relativePath, file.RelativePath);
            Assert.Equal(originalTargetHash, file.Sha256);

            await packageImporter.ImportAsync(importPlan, progress: null, cancellationToken: CancellationToken.None);
            Assert.Equal("new-layout", await File.ReadAllTextAsync(targetFile));

            var restorePlan = importPlanner.CreatePlan(
                zipPath: preImportBackupZipPath,
                manifest: backupManifest,
                targetInstallRootPath: targetRoot.FullName,
                targetUserDirName: targetUser,
                selectedCategoryIds: [categoryId]);

            await packageImporter.ImportAsync(restorePlan, progress: null, cancellationToken: CancellationToken.None);
            Assert.Equal("old-layout", await File.ReadAllTextAsync(targetFile));
        }
        finally
        {
            sourceRoot.Delete(recursive: true);
            targetRoot.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task CreatePreImportBackupZipAsync_PreservesSelectedCategories_WhenNoFilesNeedBackup()
    {
        var sourceRoot = Directory.CreateTempSubdirectory("ths_pre_import_manifest_src_");
        var targetRoot = Directory.CreateTempSubdirectory("ths_pre_import_manifest_dst_");

        try
        {
            const string sourceUser = "mx_src";
            const string targetUser = "mx_dst";
            const string categoryId = "docking_layout";
            const string fileRelativeToUser = @"CustomPage\CustomDockingTemplate\AStockTemplate.config";

            var sourceFile = Path.Combine(sourceRoot.FullName, "bin", "users", sourceUser, "CustomPage", "CustomDockingTemplate", "AStockTemplate.config");
            Directory.CreateDirectory(Path.GetDirectoryName(sourceFile)!);
            Directory.CreateDirectory(Path.Combine(targetRoot.FullName, "bin", "users", targetUser));
            await File.WriteAllTextAsync(sourceFile, "new-layout");

            var categories = new[]
            {
                new BackupCategory(
                    Id: categoryId,
                    Name: "页面停靠布局",
                    Description: "测试用 CustomDockingTemplate 配置",
                    IsCoreDefault: true,
                    Paths:
                    [
                        new PathSpec(
                            Kind: PathSpecKind.File,
                            RelativePathTemplate: $@"bin\users\{BackupCategoryCatalog.UserPlaceholder}\{fileRelativeToUser}",
                            Recursive: false,
                            SearchPattern: string.Empty),
                    ]),
            };

            var exportPlanner = new BackupPlanner(categories);
            var packageWriter = new BackupPackageWriter();
            var packageReader = new BackupPackageReader();
            var importPlanner = new BackupImportPlanner();
            var packageImporter = new BackupPackageImporter();

            var exportZipPath = Path.Combine(sourceRoot.FullName, "export.zip");
            var exportPlan = exportPlanner.CreatePlan(sourceRoot.FullName, sourceUser, [categoryId]);
            var exportManifest = await packageWriter.WriteZipAsync(
                plan: exportPlan,
                zipPath: exportZipPath,
                selectedCategoryIds: [categoryId],
                toolVersion: "0.0.0-test",
                appExeVersion: null,
                progress: null,
                cancellationToken: CancellationToken.None);

            var importPlan = importPlanner.CreatePlan(
                zipPath: exportZipPath,
                manifest: exportManifest,
                targetInstallRootPath: targetRoot.FullName,
                targetUserDirName: targetUser,
                selectedCategoryIds: [categoryId]);

            var preImportBackupZipPath = Path.Combine(targetRoot.FullName, "pre_import_backup_empty.zip");
            await packageImporter.CreatePreImportBackupZipAsync(
                plan: importPlan,
                backupZipPath: preImportBackupZipPath,
                progress: null,
                cancellationToken: CancellationToken.None);

            var backupManifest = await packageReader.ReadManifestAsync(preImportBackupZipPath, CancellationToken.None);

            Assert.Equal([categoryId], backupManifest.Selection.Categories);
            Assert.Empty(backupManifest.Files);
        }
        finally
        {
            sourceRoot.Delete(recursive: true);
            targetRoot.Delete(recursive: true);
        }
    }
}
