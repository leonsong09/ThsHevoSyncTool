using ThsHevoSyncTool.Services;

namespace ThsHevoSyncTool.App.Tests;

public sealed class JsonExportSelectionUserPresetStoreTests
{
    [Fact]
    public void SaveAndLoadAll_RoundTripsCustomPresets()
    {
        var tempRoot = Directory.CreateTempSubdirectory("ths_user_preset_store_");
        try
        {
            var filePath = Path.Combine(tempRoot.FullName, "presets.json");
            var store = new JsonExportSelectionUserPresetStore(filePath);

            store.Save(new ExportSelectionUserPreset(
                SlotId: "config1",
                DisplayName: "配置1",
                SelectedCategoryIds: ["custom_indicators", "docking_layout"],
                SavedAtUtc: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc)));

            store.Save(new ExportSelectionUserPreset(
                SlotId: "config2",
                DisplayName: "配置2",
                SelectedCategoryIds: ["watch_and_warn"],
                SavedAtUtc: new DateTime(2026, 4, 10, 1, 0, 0, DateTimeKind.Utc)));

            var loaded = store.LoadAll();

            Assert.Collection(
                loaded,
                item =>
                {
                    Assert.Equal("config1", item.SlotId);
                    Assert.Equal(["custom_indicators", "docking_layout"], item.SelectedCategoryIds);
                },
                item =>
                {
                    Assert.Equal("config2", item.SlotId);
                    Assert.Equal(["watch_and_warn"], item.SelectedCategoryIds);
                });
        }
        finally
        {
            tempRoot.Delete(recursive: true);
        }
    }

    [Fact]
    public void Save_WhenSlotAlreadyExists_OverwritesPreset()
    {
        var tempRoot = Directory.CreateTempSubdirectory("ths_user_preset_store_overwrite_");
        try
        {
            var filePath = Path.Combine(tempRoot.FullName, "presets.json");
            var store = new JsonExportSelectionUserPresetStore(filePath);

            store.Save(new ExportSelectionUserPreset(
                SlotId: "config1",
                DisplayName: "配置1",
                SelectedCategoryIds: ["custom_indicators"],
                SavedAtUtc: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc)));

            store.Save(new ExportSelectionUserPreset(
                SlotId: "config1",
                DisplayName: "配置1",
                SelectedCategoryIds: ["docking_layout"],
                SavedAtUtc: new DateTime(2026, 4, 10, 2, 0, 0, DateTimeKind.Utc)));

            var loaded = Assert.Single(store.LoadAll());
            Assert.Equal(["docking_layout"], loaded.SelectedCategoryIds);
            Assert.Equal(new DateTime(2026, 4, 10, 2, 0, 0, DateTimeKind.Utc), loaded.SavedAtUtc);
        }
        finally
        {
            tempRoot.Delete(recursive: true);
        }
    }
}
