"""路径规则（PathSpec）与相对路径安全化 —— 对齐 C# 版 IO/PathSpec.cs、SafeRelativePath.cs。"""

from __future__ import annotations

import os
from dataclasses import dataclass
from enum import IntEnum


class PathSpecKind(IntEnum):
    FILE = 1
    DIRECTORY = 2


@dataclass(frozen=True)
class PathSpec:
    kind: PathSpecKind
    relative_path_template: str
    recursive: bool = False
    search_pattern: str = ""

    @property
    def display_rule(self) -> str:
        if self.kind == PathSpecKind.FILE:
            return self.relative_path_template
        if self.kind == PathSpecKind.DIRECTORY:
            suffix = " (recursive)" if self.recursive else ""
            return f"{self.relative_path_template}\\{self.search_pattern}{suffix}"
        return self.relative_path_template


class PathNotAllowedError(ValueError):
    """相对路径不安全（绝对路径 / 含 . 或 .. 跳转段）。"""


def normalize_relative_path(relative_path: str) -> str:
    """把相对路径规范化为反斜杠分隔；拒绝绝对路径与跳转段。

    与 C# `SafeRelativePath.NormalizeRelativePath` 同义。
    """
    if relative_path is None or not relative_path.strip():
        raise PathNotAllowedError("相对路径为空。")

    trimmed = relative_path.strip()
    # 绝对路径判定：盘符（C:\\）、UNC（\\\\server）、或以 / 或 \\ 开头
    if os.path.isabs(trimmed) or trimmed.startswith(("\\", "/")) or _has_drive(trimmed):
        raise PathNotAllowedError(f"不允许绝对路径：{relative_path}")

    parts = [p for p in trimmed.replace("\\", "/").split("/") if p]
    safe_parts = []
    for part in parts:
        if part in (".", ".."):
            raise PathNotAllowedError(f"不允许包含路径跳转段：{relative_path}")
        safe_parts.append(part)

    if not safe_parts:
        raise PathNotAllowedError("相对路径为空。")

    return "\\".join(safe_parts)


def normalize_for_zip_entry(relative_path: str) -> str:
    """ZIP 条目名用正斜杠（对齐 C# `NormalizeForZipEntry`）。"""
    return normalize_relative_path(relative_path).replace("\\", "/")


def _has_drive(path: str) -> bool:
    return len(path) >= 2 and path[1] == ":" and path[0].isalpha()


def ensure_under_root(root_path: str, full_path: str) -> None:
    """确保 full_path 落在 root_path 之内（对齐 C# `PathSafety.EnsureUnderRoot`）。"""
    root_full = os.path.abspath(root_path).rstrip("\\/") + os.sep
    candidate = os.path.abspath(full_path)
    if not candidate.lower().startswith(root_full.lower()):
        raise PathNotAllowedError(f"路径越界：{candidate}")


def is_path_escape(relative_path: str) -> bool:
    """相对路径是否含绝对/跳转成分（对齐 C# `PathSafety.IsPathEscape`）。"""
    if not relative_path or not relative_path.strip():
        return True
    trimmed = relative_path.strip()
    if os.path.isabs(trimmed) or trimmed.startswith(("\\", "/")) or _has_drive(trimmed):
        return True
    return any(p in (".", "..") for p in trimmed.replace("\\", "/").split("/") if p)


# ZIP 条目名口径（与 C# `SafeRelativePath.NormalizeForZipEntry` 同义）
to_zip_entry = normalize_for_zip_entry

