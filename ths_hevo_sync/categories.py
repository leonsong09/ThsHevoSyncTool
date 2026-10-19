"""备份分类目录 + 推荐预设 —— 1:1 对齐 C# 版 BackupCategoryCatalog / ExportSelectionPresetCatalog。

分类 id / 名称 / 路径规则 / IsCoreDefault 必须与 C# 版完全一致，
否则同一份 manifest 在两种实现间会出现「分类不存在」的差异。
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Iterable, Sequence

from .pathspec import PathSpec, PathSpecKind

USER_PLACEHOLDER = "{USER}"
_USER_ROOT = "bin\\users\\" + USER_PLACEHOLDER


def _file(relative_path_template: str) -> PathSpec:
    return PathSpec(PathSpecKind.FILE, relative_path_template)


def _dir(relative_path_template: str, recursive: bool, pattern: str) -> PathSpec:
    return PathSpec(PathSpecKind.DIRECTORY, relative_path_template, recursive, pattern)


def _u(*parts: str) -> str:
    return _USER_ROOT + "\\" + "\\".join(parts)


@dataclass(frozen=True)
class BackupCategory:
    id: str
    name: str
    description: str
    is_core_default: bool
    paths: tuple[PathSpec, ...]

    @property
    def display_path_rules(self) -> list[str]:
        return [p.display_rule for p in self.paths]


CORE_CATEGORIES: tuple[BackupCategory, ...] = (
    BackupCategory(
        "custom_indicators", "自定义指标",
        "自定义指标定义与指标脚本（function\\CustomIndicator、function\\candle）。",
        True, (_dir(_u("function", "CustomIndicator"), False, "*.json"),
               _dir(_u("function", "candle"), False, "*.py")),
    ),
    BackupCategory(
        "custom_indicator_group_order", "自定义指标分组与排序",
        "自定义指标的分组与排序配置（MyLanguageGroupOrder.xml）。",
        True, (_file(_u("function", "MyLanguageGroupOrder.xml")),),
    ),
    BackupCategory(
        "index_combinations", "指标组合（主图/副图预设）",
        "指标组合预设：主图/副图指标列表、默认选中组合等。",
        True, (_file(_u("index_setting", "IndexCombination", "IndexCombination.xml")),),
    ),
    BackupCategory(
        "index_pack_configs", "指标包参数/模板",
        "指标包参数与模板（index_setting\\IndexConfig 下的 json）。",
        True, (_dir(_u("index_setting", "IndexConfig"), True, "*.json"),),
    ),
    BackupCategory(
        "multi_period_layout", "多周期/多分屏指标选择",
        "多周期与多分屏的指标选择（MutilPeriod*.ini、index_show.ini）。",
        True, (_dir(_u("index_setting"), False, "MutilPeriod*.ini"),
               _file(_u("index_setting", "index_show.ini"))),
    ),
    BackupCategory(
        "page_chart_indicator_selection", "页面图位指标勾选（页面级）",
        "页面级图位指标勾选（UserPageCoding.xml）。",
        True, (_file(_u("UserPageCoding.xml")),),
    ),
    BackupCategory(
        "docking_layout", "页面停靠布局（窗口/面板布局）",
        "页面停靠与面板布局模板（CustomDockingTemplate\\*.config）。",
        True, (_dir(_u("CustomPage", "CustomDockingTemplate"), False, "*.config"),),
    ),
    BackupCategory(
        "page_templates", "页面模板配置",
        "页面模板相关配置（CustomPageTemplate）。",
        True, (_dir(_u("CustomPage", "CustomPageTemplate"), False, "*.xml"),
               _file(_u("CustomPage", "pluginusages.json"))),
    ),
    BackupCategory(
        "drawline_settings", "画线工具设置（非画线数据）",
        "画线工具栏、默认线型样式等设置（不包含具体画线数据目录）。",
        True, (_file(_u("DrawLine_New", "config.ini")),
               _file(_u("DrawLine_New", "NewLineDefaultProperty.json"))),
    ),
)

OPTIONAL_CATEGORIES: tuple[BackupCategory, ...] = (
    BackupCategory("grid_headers", "列表表头/列宽/字段方案",
                   "自定义列表列、顺序、列宽、字段方案（grid_config）。",
                   False, (_dir(_u("grid_config"), False, "*.xml"),)),
    BackupCategory("chart_preferences", "图表交互与样式偏好",
                   "十字线、均线显示、缩放等偏好（ChartConfig.json）。",
                   False, (_file(_u("ChartConfig", "ChartConfig.json")),)),
    BackupCategory("panel_sizes", "面板宽度/侧边栏尺寸",
                   "侧边栏/市场宽度等布局尺寸（HevoFunctionManager.xml）。",
                   False, (_file(_u("HevoFunctionManager", "HevoFunctionManager.xml")),)),
    BackupCategory("personal_center", "个人中心基础设置",
                   "个人中心基础偏好设置（personal_center）。",
                   False, (_file(_u("personal_center", "personal_config.xml")),
                           _file(_u("personal_center", "SystemConfig.json")))),
    BackupCategory("plugin_settings", "插件设置（各模块参数）",
                   "各功能模块插件设置（PluginSettings\\Settings.*.xml）。",
                   False, (_dir(_u("PluginSettings"), True, "Settings.*.xml"),)),
    BackupCategory("notifications", "通知中心",
                   "通知中心相关配置（NotifCenter）。",
                   False, (_dir(_u("NotifCenter"), False, "*.xml"),)),
    BackupCategory("trend_playback", "趋势回放窗口状态",
                   "趋势回放窗口大小、选择指标等（HistoryTrendConfig.xml）。",
                   False, (_file(_u("TrendPlayback", "HistoryTrendConfig.xml")),)),
    BackupCategory("overlay_switches", "叠加/覆盖指标开关",
                   "叠加指标开关与状态（OverIndicator/overlap）。",
                   False, (_file(_u("OverIndicator.xml")), _file(_u("overlap.xml")))),
    BackupCategory("favorites_and_attention", "自选/关注相关本地文件",
                   "自选与关注相关本地配置（favorite_info 等）。",
                   False, (_dir(_u("favorite_info"), False, "*.xml"),
                           _file(_u("SelfStockCache.json")),
                           _file(_u("SpecialAttentionSecuritiesConfig.xml")))),
    BackupCategory("watch_and_warn", "预警/盯盘/小工具配置",
                   "预警、盯盘、小工具相关配置（StockWarn/watch_assist 等）。",
                   False, (_dir(_u("StockWarn"), False, "*"),
                           _dir(_u("watch_assist"), False, "*.xml"),
                           _file(_u("WatchAssistUserLevelOneConfig.xml")),
                           _file(_u("watchAssist_plateEdit.ini")),
                           _file(_u("CloudWarnConditionData.json")),
                           _file(_u("CloudWarnResultDataList.json")))),
    BackupCategory("quote_bar", "指数栏/行情栏配置",
                   "顶部指数/行情栏自定义列表（NewIndexConfig.xml）。",
                   False, (_file(_u("NewIndexConfig.xml")),)),
    BackupCategory("bsdata", "BSData（不确定用途）",
                   "疑似按证券存储的本地数据文件（可选）。",
                   False, (_dir(_u("BSData"), True, "*"),)),
    BackupCategory("misc_user_root_files", "其他（零散配置文件）",
                   "一些零散的本地配置文件（可能与指标/统计/功能相关，按需选择）。",
                   False, (_file(_u("UserIndexConfig.xml")),
                           _file(_u("MyLanguageReadonlyIndicator.xml")),
                           _file(_u("KeyStaticsConfig.xml")),
                           _file(_u("ChipsParamConfig.xml")),
                           _file(_u("LimitUpAnalyseItem.ini")),
                           _file(_u("UserFilesClound.xml")))),
    BackupCategory("user_history_and_cache", "历史/缓存（不推荐）",
                   "搜索历史、浏览记录等（体积小但噪声大，一般不建议同步）。",
                   False, (_file(_u("UserConfigs.xml")),
                           _file(_u("Cache", "SearchHistory.xml")))),
    BackupCategory("position_block", "持仓/仓位板块配置",
                   "持仓板块相关配置（PositionBlock）。",
                   False, (_dir(_u("PositionBlock"), False, "*.xml"),)),
    BackupCategory("tape_config", "逐笔/分时模块配置",
                   "逐笔/分时模块的配置文件（tape_config）。",
                   False, (_dir(_u("tape_config"), True, "*"),)),
    BackupCategory("limitup_analyse", "涨停分析配置",
                   "涨停分析相关配置（LimitUpAnalyse）。",
                   False, (_dir(_u("LimitUpAnalyse"), True, "*"),)),
    BackupCategory("discovery", "发现页配置",
                   "发现页（Discovery）相关配置。",
                   False, (_file(_u("Discovery", "Discovery.xml")),)),
)

GLOBAL_CATEGORIES: tuple[BackupCategory, ...] = (
    BackupCategory("global_user_config", "全局配置（语言/复权/最近浏览等）",
                   "跨账号全局偏好（bin\\users\\config）。",
                   False, (_dir("bin\\users\\config", True, "*"),)),
    BackupCategory("public_notes", "公共笔记",
                   "个股笔记内容（notes.xml）。",
                   False, (_file("bin\\users\\public\\notes.xml"),)),
)

ALL_CATEGORIES: tuple[BackupCategory, ...] = CORE_CATEGORIES + OPTIONAL_CATEGORIES + GLOBAL_CATEGORIES

_BY_ID = {c.id.lower(): c for c in ALL_CATEGORIES}


def all_categories() -> tuple[BackupCategory, ...]:
    return ALL_CATEGORIES


def core_defaults() -> tuple[BackupCategory, ...]:
    return tuple(c for c in ALL_CATEGORIES if c.is_core_default)


def get_category(category_id: str) -> BackupCategory:
    try:
        return _BY_ID[category_id.lower()]
    except KeyError:
        raise ValueError(f"未知选项：{category_id}") from None


def has_category(category_id: str) -> bool:
    return category_id.lower() in _BY_ID


# ------------------------------------------------------------------ 推荐预设


@dataclass(frozen=True)
class ExportSelectionPreset:
    id: str
    name: str
    description: str
    selected_category_ids: tuple[str, ...]

    def includes(self, category_id: str) -> bool:
        return category_id.lower() in {c.lower() for c in self.selected_category_ids}


def _exclude(excluded_ids: Sequence[str]) -> tuple[str, ...]:
    excluded = {i.lower() for i in excluded_ids}
    return tuple(c.id for c in ALL_CATEGORIES if c.id.lower() not in excluded)


def _include_only(included_ids: Sequence[str]) -> tuple[str, ...]:
    unknown = [i for i in included_ids if not has_category(i)]
    if unknown:
        raise ValueError(f"推荐预设包含未知分类：{', '.join(unknown)}")
    return tuple(included_ids)


PRESETS: tuple[ExportSelectionPreset, ...] = (
    ExportSelectionPreset(
        "r1", "完整迁移",
        "除零散配置、BSData、历史缓存与未发现内容项外，尽量完整迁移常用环境。",
        _exclude(["misc_user_root_files", "bsdata", "user_history_and_cache", "limitup_analyse"]),
    ),
    ExportSelectionPreset(
        "r2", "轻量常用",
        "保留核心布局与常用偏好，适合快速迁移。",
        _include_only([
            "custom_indicators", "custom_indicator_group_order", "index_combinations",
            "index_pack_configs", "multi_period_layout", "page_chart_indicator_selection",
            "docking_layout", "page_templates", "drawline_settings", "grid_headers",
            "chart_preferences", "favorites_and_attention", "watch_and_warn", "global_user_config",
        ]),
    ),
    ExportSelectionPreset(
        "r3", "指标布局",
        "只迁移指标、页面布局与主要图表偏好。",
        _include_only([
            "custom_indicators", "custom_indicator_group_order", "index_combinations",
            "index_pack_configs", "multi_period_layout", "page_chart_indicator_selection",
            "docking_layout", "page_templates", "drawline_settings", "grid_headers",
            "chart_preferences", "panel_sizes",
        ]),
    ),
)

_PRESETS_BY_ID = {p.id.lower(): p for p in PRESETS}


def get_preset(preset_id: str) -> ExportSelectionPreset:
    try:
        return _PRESETS_BY_ID[preset_id.lower()]
    except KeyError:
        raise ValueError(f"未知推荐预设：{preset_id}") from None


def expand_selection(
    category_ids: Iterable[str] | None = None,
    preset_id: str | None = None,
    level: str | None = None,
) -> list[str]:
    """把「显式 id / 预设 / 安全级别」三种选择方式统一展开为 id 列表。"""
    if category_ids:
        ids = [c.strip() for c in category_ids if c and c.strip()]
        for cid in ids:
            if not has_category(cid):
                raise ValueError(f"未知选项：{cid}")
        return ids
    if preset_id:
        return list(get_preset(preset_id).selected_category_ids)
    if level:
        if level == "safe":
            return [c.id for c in CORE_CATEGORIES] + [c.id for c in GLOBAL_CATEGORIES]
        if level == "all":
            return [c.id for c in ALL_CATEGORIES]
        raise ValueError(f"未知级别：{level}（可用 safe / all）")
    return [c.id for c in core_defaults()]
