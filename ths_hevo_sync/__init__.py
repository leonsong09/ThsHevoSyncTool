"""ThsHevoSyncTool —— 同花顺远航版 本地配置 备份/迁移工具（纯 Python 实现）。

与历史 C# 版本（见 `legacy-csharp/`）共用同一套备份包格式：
ZIP + 根目录 `manifest.json`（schemaVersion=1），可双向互通。
"""

from .version import APP_NAME, APP_VERSION, TOOL_NAME, TOOL_VERSION

__all__ = ["APP_NAME", "APP_VERSION", "TOOL_NAME", "TOOL_VERSION"]
