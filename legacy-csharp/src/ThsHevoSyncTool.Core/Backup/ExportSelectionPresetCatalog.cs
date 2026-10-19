namespace ThsHevoSyncTool.Core.Backup;

public static class ExportSelectionPresetCatalog
{
    private static readonly StringComparer IdComparer = StringComparer.OrdinalIgnoreCase;

    private static readonly Lazy<IReadOnlyList<ExportSelectionPreset>> AllLazy =
        new(CreateAll, isThreadSafe: true);

    public static IReadOnlyList<ExportSelectionPreset> All => AllLazy.Value;

    public static ExportSelectionPreset R1 => GetById("r1");
    public static ExportSelectionPreset R2 => GetById("r2");
    public static ExportSelectionPreset R3 => GetById("r3");

    public static ExportSelectionPreset GetById(string presetId) =>
        All.FirstOrDefault(preset => IdComparer.Equals(preset.Id, presetId))
        ?? throw new ArgumentException($"未知推荐预设：{presetId}", nameof(presetId));

    public static bool TryGetById(string presetId, out ExportSelectionPreset? preset)
    {
        preset = All.FirstOrDefault(candidate => IdComparer.Equals(candidate.Id, presetId));
        return preset is not null;
    }

    private static IReadOnlyList<ExportSelectionPreset> CreateAll()
    {
        var allCategoryIds = BackupCategoryCatalog.All
            .Select(static category => category.Id)
            .ToArray();

        return
        [
            new ExportSelectionPreset(
                Id: "r1",
                Name: "完整迁移",
                Description: "除零散配置、BSData、历史缓存与未发现内容项外，尽量完整迁移常用环境。",
                SelectedCategoryIds: Exclude(
                    allCategoryIds,
                    "misc_user_root_files",
                    "bsdata",
                    "user_history_and_cache",
                    "limitup_analyse")),
            new ExportSelectionPreset(
                Id: "r2",
                Name: "轻量常用",
                Description: "保留核心布局与常用偏好，适合快速迁移。",
                SelectedCategoryIds: IncludeOnly(
                    allCategoryIds,
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
                    "global_user_config")),
            new ExportSelectionPreset(
                Id: "r3",
                Name: "指标布局",
                Description: "只迁移指标、页面布局与主要图表偏好。",
                SelectedCategoryIds: IncludeOnly(
                    allCategoryIds,
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
                    "panel_sizes")),
        ];
    }

    private static IReadOnlyList<string> Exclude(
        IEnumerable<string> allCategoryIds,
        params string[] excludedIds)
    {
        var excluded = excludedIds.ToHashSet(IdComparer);
        return allCategoryIds
            .Where(id => !excluded.Contains(id))
            .ToArray();
    }

    private static IReadOnlyList<string> IncludeOnly(
        IReadOnlyCollection<string> allCategoryIds,
        params string[] includedIds)
    {
        var allIds = allCategoryIds.ToHashSet(IdComparer);
        var unknown = includedIds.Where(id => !allIds.Contains(id)).ToArray();
        if (unknown.Length > 0)
        {
            throw new InvalidOperationException($"推荐预设包含未知分类：{string.Join(", ", unknown)}");
        }

        return includedIds.ToArray();
    }
}
