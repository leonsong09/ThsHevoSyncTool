"""manifest.json 读写 —— 与 C# 版 `BackupManifest` 逐字段对齐（schemaVersion=1）。

C# 侧用 System.Text.Json（WriteIndented、忽略 null、默认编码器会把非 ASCII 与 '+' 转成 \\uXXXX），
这里保持同样的缩进/键序/转义与 CRLF 行尾，使两边产物在文本层面也基本一致。
"""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from datetime import datetime
from typing import Any

from .io_utils import format_utc, parse_utc

CURRENT_SCHEMA_VERSION = "1"
MANIFEST_ENTRY_NAME = "manifest.json"

# System.Text.Json 默认 JavaScriptEncoder 会转义这些字符
_EXTRA_ESCAPES = {"+": "\\u002B"}


@dataclass
class BackupToolInfo:
    name: str
    version: str
    runtime: str


@dataclass
class BackupSourceInfo:
    install_root: str
    user_dir_name: str
    app_exe_version: str | None = None


@dataclass
class BackupSelectionInfo:
    default_preset: str
    categories: list[str] = field(default_factory=list)


@dataclass
class BackupManifestFileEntry:
    relative_path: str
    size_bytes: int
    sha256: str
    last_write_time_utc: datetime
    category_id: str


@dataclass
class BackupManifest:
    schema_version: str
    tool: BackupToolInfo
    created_at_utc: datetime
    source: BackupSourceInfo
    selection: BackupSelectionInfo
    files: list[BackupManifestFileEntry] = field(default_factory=list)

    # ------------------------------------------------------------- 序列化
    def to_dict(self) -> dict[str, Any]:
        source: dict[str, Any] = {
            "installRoot": self.source.install_root,
            "userDirName": self.source.user_dir_name,
        }
        if self.source.app_exe_version is not None:
            source["appExeVersion"] = self.source.app_exe_version

        return {
            "schemaVersion": self.schema_version,
            "tool": {
                "name": self.tool.name,
                "version": self.tool.version,
                "runtime": self.tool.runtime,
            },
            "createdAtUtc": format_utc(self.created_at_utc),
            "source": source,
            "selection": {
                "defaultPreset": self.selection.default_preset,
                "categories": list(self.selection.categories),
            },
            "files": [
                {
                    "relativePath": f.relative_path,
                    "sizeBytes": f.size_bytes,
                    "sha256": f.sha256,
                    "lastWriteTimeUtc": format_utc(f.last_write_time_utc),
                    "categoryId": f.category_id,
                }
                for f in self.files
            ],
        }

    def to_json(self) -> str:
        text = json.dumps(self.to_dict(), ensure_ascii=True, indent=2, separators=(",", ": "))
        for raw, esc in _EXTRA_ESCAPES.items():
            text = text.replace(raw, esc)
        return text.replace("\n", "\r\n")

    # ------------------------------------------------------------- 反序列化
    @staticmethod
    def from_json(text: str) -> "BackupManifest":
        data = json.loads(text)
        tool = data["tool"]
        source = data.get("source") or {}
        selection = data.get("selection") or {}
        return BackupManifest(
            schema_version=data["schemaVersion"],
            tool=BackupToolInfo(
                name=tool["name"], version=tool["version"], runtime=tool.get("runtime", ""),
            ),
            created_at_utc=parse_utc(data["createdAtUtc"]),
            source=BackupSourceInfo(
                install_root=source.get("installRoot", ""),
                user_dir_name=source.get("userDirName", ""),
                app_exe_version=source.get("appExeVersion"),
            ),
            selection=BackupSelectionInfo(
                default_preset=selection.get("defaultPreset", "core"),
                categories=list(selection.get("categories") or []),
            ),
            files=[
                BackupManifestFileEntry(
                    relative_path=e["relativePath"],
                    size_bytes=int(e["sizeBytes"]),
                    sha256=e["sha256"],
                    last_write_time_utc=parse_utc(e["lastWriteTimeUtc"]),
                    category_id=e["categoryId"],
                )
                for e in (data.get("files") or [])
            ],
        )
