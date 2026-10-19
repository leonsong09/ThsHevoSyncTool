using ThsHevoSyncTool.Core.Backup;

namespace ThsHevoSyncTool.Core.Tests;

public sealed class ExportSelectionPresetCatalogTests
{
    [Fact]
    public void All_ExposesStablePresetIdsAndNames()
    {
        Assert.Collection(
            ExportSelectionPresetCatalog.All,
            preset =>
            {
                Assert.Equal("r1", preset.Id);
                Assert.Equal("完整迁移", preset.Name);
            },
            preset =>
            {
                Assert.Equal("r2", preset.Id);
                Assert.Equal("轻量常用", preset.Name);
            },
            preset =>
            {
                Assert.Equal("r3", preset.Id);
                Assert.Equal("指标布局", preset.Name);
            });
    }

    [Fact]
    public void GetById_R1_SelectsAllCategoriesExceptExplicitExclusions()
    {
        var preset = ExportSelectionPresetCatalog.GetById("r1");
        var excluded = new[]
        {
            "misc_user_root_files",
            "bsdata",
            "user_history_and_cache",
            "limitup_analyse",
        };

        var expected = BackupCategoryCatalog.All
            .Select(static category => category.Id)
            .Except(excluded, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(expected, preset.SelectedCategoryIds);
        Assert.DoesNotContain(preset.SelectedCategoryIds, id => excluded.Contains(id, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetById_R2AndR3_ReturnExpectedCategorySets()
    {
        Assert.Equal(
            new[]
            {
                "custom_indicators",
                "custom_indicator_group_order",
                "index_combinations",
                "index_pack_configs",
                "multi_period_layout",
                "page_chart_indicator_selection",
                "docking_layout",
                "page_templates",
                "drawline_settings",
                "grid_headers",
                "chart_preferences",
                "favorites_and_attention",
                "watch_and_warn",
                "global_user_config",
            },
            ExportSelectionPresetCatalog.GetById("r2").SelectedCategoryIds);

        Assert.Equal(
            new[]
            {
                "custom_indicators",
                "custom_indicator_group_order",
                "index_combinations",
                "index_pack_configs",
                "multi_period_layout",
                "page_chart_indicator_selection",
                "docking_layout",
                "page_templates",
                "drawline_settings",
                "grid_headers",
                "chart_preferences",
                "panel_sizes",
            },
            ExportSelectionPresetCatalog.GetById("r3").SelectedCategoryIds);
    }

    [Fact]
    public void TryGetById_UnknownPreset_ReturnsFalse()
    {
        var found = ExportSelectionPresetCatalog.TryGetById("unknown", out var preset);

        Assert.False(found);
        Assert.Null(preset);
    }
}
