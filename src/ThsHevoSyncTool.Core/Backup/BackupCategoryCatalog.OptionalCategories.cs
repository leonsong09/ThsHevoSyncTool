namespace ThsHevoSyncTool.Core.Backup;

public static partial class BackupCategoryCatalog
{
    private static readonly BackupCategory[] OptionalCategories =
    [
        new BackupCategory(
            Id: "grid_headers",
            Name: "列表表头/列宽/字段方案",
            Description: "自定义列表列、顺序、列宽、字段方案（grid_config）。",
            IsCoreDefault: false,
            Paths:
            [
                Dir($"{UserRoot()}\\grid_config", recursive: false, pattern: "*.xml"),
            ]),
        new BackupCategory(
            Id: "chart_preferences",
            Name: "图表交互与样式偏好",
            Description: "十字线、均线显示、缩放等偏好（ChartConfig.json）。",
            IsCoreDefault: false,
            Paths:
            [
                File($"{UserRoot()}\\ChartConfig\\ChartConfig.json"),
            ]),
        new BackupCategory(
            Id: "panel_sizes",
            Name: "面板宽度/侧边栏尺寸",
            Description: "侧边栏/市场宽度等布局尺寸（HevoFunctionManager.xml）。",
            IsCoreDefault: false,
            Paths:
            [
                File($"{UserRoot()}\\HevoFunctionManager\\HevoFunctionManager.xml"),
            ]),
        new BackupCategory(
            Id: "personal_center",
            Name: "个人中心基础设置",
            Description: "个人中心基础偏好设置（personal_center）。",
            IsCoreDefault: false,
            Paths:
            [
                File($"{UserRoot()}\\personal_center\\personal_config.xml"),
                File($"{UserRoot()}\\personal_center\\SystemConfig.json"),
            ]),
        new BackupCategory(
            Id: "plugin_settings",
            Name: "插件设置（各模块参数）",
            Description: "各功能模块插件设置（PluginSettings\\Settings.*.xml）。",
            IsCoreDefault: false,
            Paths:
            [
                Dir($"{UserRoot()}\\PluginSettings", recursive: true, pattern: "Settings.*.xml"),
            ]),
        new BackupCategory(
            Id: "notifications",
            Name: "通知中心",
            Description: "通知中心相关配置（NotifCenter）。",
            IsCoreDefault: false,
            Paths:
            [
                Dir($"{UserRoot()}\\NotifCenter", recursive: false, pattern: "*.xml"),
            ]),
        new BackupCategory(
            Id: "trend_playback",
            Name: "趋势回放窗口状态",
            Description: "趋势回放窗口大小、选择指标等（HistoryTrendConfig.xml）。",
            IsCoreDefault: false,
            Paths:
            [
                File($"{UserRoot()}\\TrendPlayback\\HistoryTrendConfig.xml"),
            ]),
        new BackupCategory(
            Id: "overlay_switches",
            Name: "叠加/覆盖指标开关",
            Description: "叠加指标开关与状态（OverIndicator/overlap）。",
            IsCoreDefault: false,
            Paths:
            [
                File($"{UserRoot()}\\OverIndicator.xml"),
                File($"{UserRoot()}\\overlap.xml"),
            ]),
        new BackupCategory(
            Id: "favorites_and_attention",
            Name: "自选/关注相关本地文件",
            Description: "自选与关注相关本地配置（favorite_info 等）。",
            IsCoreDefault: false,
            Paths:
            [
                Dir($"{UserRoot()}\\favorite_info", recursive: false, pattern: "*.xml"),
                File($"{UserRoot()}\\SelfStockCache.json"),
                File($"{UserRoot()}\\SpecialAttentionSecuritiesConfig.xml"),
            ]),
        new BackupCategory(
            Id: "watch_and_warn",
            Name: "预警/盯盘/小工具配置",
            Description: "预警、盯盘、小工具相关配置（StockWarn/watch_assist 等）。",
            IsCoreDefault: false,
            Paths:
            [
                Dir($"{UserRoot()}\\StockWarn", recursive: false, pattern: "*"),
                Dir($"{UserRoot()}\\watch_assist", recursive: false, pattern: "*.xml"),
                File($"{UserRoot()}\\WatchAssistUserLevelOneConfig.xml"),
                File($"{UserRoot()}\\watchAssist_plateEdit.ini"),
                File($"{UserRoot()}\\CloudWarnConditionData.json"),
                File($"{UserRoot()}\\CloudWarnResultDataList.json"),
            ]),
        new BackupCategory(
            Id: "quote_bar",
            Name: "指数栏/行情栏配置",
            Description: "顶部指数/行情栏自定义列表（NewIndexConfig.xml）。",
            IsCoreDefault: false,
            Paths:
            [
                File($"{UserRoot()}\\NewIndexConfig.xml"),
            ]),
        new BackupCategory(
            Id: "bsdata",
            Name: "BSData（不确定用途）",
            Description: "疑似按证券存储的本地数据文件（可选）。",
            IsCoreDefault: false,
            Paths:
            [
                Dir($"{UserRoot()}\\BSData", recursive: true, pattern: "*"),
            ]),
        new BackupCategory(
            Id: "misc_user_root_files",
            Name: "其他（零散配置文件）",
            Description: "一些零散的本地配置文件（可能与指标/统计/功能相关，按需选择）。",
            IsCoreDefault: false,
            Paths:
            [
                File($"{UserRoot()}\\UserIndexConfig.xml"),
                File($"{UserRoot()}\\MyLanguageReadonlyIndicator.xml"),
                File($"{UserRoot()}\\KeyStaticsConfig.xml"),
                File($"{UserRoot()}\\ChipsParamConfig.xml"),
                File($"{UserRoot()}\\LimitUpAnalyseItem.ini"),
                File($"{UserRoot()}\\UserFilesClound.xml"),
            ]),
        new BackupCategory(
            Id: "user_history_and_cache",
            Name: "历史/缓存（不推荐）",
            Description: "搜索历史、浏览记录等（体积小但噪声大，一般不建议同步）。",
            IsCoreDefault: false,
            Paths:
            [
                File($"{UserRoot()}\\UserConfigs.xml"),
                File($"{UserRoot()}\\Cache\\SearchHistory.xml"),
            ]),
        new BackupCategory(
            Id: "position_block",
            Name: "持仓/仓位板块配置",
            Description: "持仓板块相关配置（PositionBlock）。",
            IsCoreDefault: false,
            Paths:
            [
                Dir($"{UserRoot()}\\PositionBlock", recursive: false, pattern: "*.xml"),
            ]),
        new BackupCategory(
            Id: "tape_config",
            Name: "逐笔/分时模块配置",
            Description: "逐笔/分时模块的配置文件（tape_config）。",
            IsCoreDefault: false,
            Paths:
            [
                Dir($"{UserRoot()}\\tape_config", recursive: true, pattern: "*"),
            ]),
        new BackupCategory(
            Id: "limitup_analyse",
            Name: "涨停分析配置",
            Description: "涨停分析相关配置（LimitUpAnalyse）。",
            IsCoreDefault: false,
            Paths:
            [
                Dir($"{UserRoot()}\\LimitUpAnalyse", recursive: true, pattern: "*"),
            ]),
        new BackupCategory(
            Id: "discovery",
            Name: "发现页配置",
            Description: "发现页（Discovery）相关配置。",
            IsCoreDefault: false,
            Paths:
            [
                File($"{UserRoot()}\\Discovery\\Discovery.xml"),
            ]),
    ];
}

