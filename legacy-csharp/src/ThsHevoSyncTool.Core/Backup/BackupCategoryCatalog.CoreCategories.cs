namespace ThsHevoSyncTool.Core.Backup;

public static partial class BackupCategoryCatalog
{
    private static readonly BackupCategory[] CoreCategories =
    [
        new BackupCategory(
            Id: "custom_indicators",
            Name: "自定义指标",
            Description: "自编指标源码与相关脚本（JSON/PY）。缺失会导致自定义指标不可用。",
            IsCoreDefault: true,
            Paths:
            [
                Dir($"{UserRoot()}\\function\\CustomIndicator", recursive: false, pattern: "*.json"),
                Dir($"{UserRoot()}\\function\\candle", recursive: false, pattern: "*.py"),
            ]),
        new BackupCategory(
            Id: "custom_indicator_group_order",
            Name: "自定义指标分组与排序",
            Description: "自定义指标分组名与指标列表顺序。",
            IsCoreDefault: true,
            Paths:
            [
                File($"{UserRoot()}\\function\\MyLanguageGroupOrder.xml"),
            ]),
        new BackupCategory(
            Id: "index_combinations",
            Name: "指标组合（主图/副图预设）",
            Description: "指标组合预设：主图/副图指标列表、默认选中组合等。",
            IsCoreDefault: true,
            Paths:
            [
                File($"{UserRoot()}\\index_setting\\IndexCombination\\IndexCombination.xml"),
            ]),
        new BackupCategory(
            Id: "index_pack_configs",
            Name: "指标包参数/模板",
            Description: "指标包参数与模板（如 CandleMaPack 等 JSON 配置）。",
            IsCoreDefault: true,
            Paths:
            [
                Dir($"{UserRoot()}\\index_setting\\IndexConfig", recursive: true, pattern: "*.json"),
            ]),
        new BackupCategory(
            Id: "multi_period_layout",
            Name: "多周期/多分屏指标选择",
            Description: "多周期/多布局下的指标勾选与周期配置（INI）。",
            IsCoreDefault: true,
            Paths:
            [
                Dir($"{UserRoot()}\\index_setting", recursive: false, pattern: "MutilPeriod*.ini"),
                File($"{UserRoot()}\\index_setting\\index_show.ini"),
            ]),
        new BackupCategory(
            Id: "page_chart_indicator_selection",
            Name: "页面图位指标勾选（页面级）",
            Description: "按页面/图位/周期记录已勾选的指标包（UserPageCoding.xml）。",
            IsCoreDefault: true,
            Paths:
            [
                File($"{UserRoot()}\\UserPageCoding.xml"),
            ]),
        new BackupCategory(
            Id: "docking_layout",
            Name: "页面停靠布局（窗口/面板布局）",
            Description: "面板停靠、分栏比例、浮窗位置等（CustomDockingTemplate）。",
            IsCoreDefault: true,
            Paths:
            [
                Dir($"{UserRoot()}\\CustomPage\\CustomDockingTemplate", recursive: false, pattern: "*.config"),
            ]),
        new BackupCategory(
            Id: "page_templates",
            Name: "页面模板配置",
            Description: "页面模板相关配置（CustomPageTemplate）。",
            IsCoreDefault: true,
            Paths:
            [
                Dir($"{UserRoot()}\\CustomPage\\CustomPageTemplate", recursive: false, pattern: "*.xml"),
                File($"{UserRoot()}\\CustomPage\\pluginusages.json"),
            ]),
        new BackupCategory(
            Id: "drawline_settings",
            Name: "画线工具设置（非画线数据）",
            Description: "画线工具栏、默认线型样式等设置（不包含具体画线数据目录）。",
            IsCoreDefault: true,
            Paths:
            [
                File($"{UserRoot()}\\DrawLine_New\\config.ini"),
                File($"{UserRoot()}\\DrawLine_New\\NewLineDefaultProperty.json"),
            ]),
    ];
}

