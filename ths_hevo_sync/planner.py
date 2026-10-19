"""导出计划与分类扫描 —— 对齐 C# 版 PathSpecExpander / BackupPlanner / BackupCategoryScanner。"""

from __future__ import annotations

import os
import re
from dataclasses import dataclass
from datetime import datetime

from .categories import BackupCategory, USER_PLACEHOLDER, all_categories, get_category
from .environment import InstallRootInfo, validate_install_root, validate_user_dir_name
from .io_utils import file_mtime_utc
from .pathspec import PathSpec, PathSpecKind, normalize_relative_path


@dataclass(frozen=True)
class BackupPlannedFile:
    category_id: str
    absolute_path: str
    relative_path: str
    size_bytes: int
    last_write_time_utc: datetime


@dataclass
class BackupPlan:
    install_root_path: str
    user_dir_name: str
    files: list[BackupPlannedFile]

    @property
    def total_bytes(self) -> int:
        return sum(f.size_bytes for f in self.files)


def _sort_key(text: str) -> str:
    """近似 StringComparer.OrdinalIgnoreCase 的排序键。"""
    return text.upper()


def _pattern_to_regex(pattern: str) -> re.Pattern[str]:
    """把 Win32/.NET 搜索模式转成正则，并模拟「3 字符扩展名前缀匹配」的历史怪癖。

    见 .NET 文档：`*.xls` 也会匹配 `book.xlsx`（仅当扩展名恰为 3 字符时）。
    """
    body = "".join(
        "[^\\\\]*" if ch == "*" else "[^\\\\]" if ch == "?" else re.escape(ch)
        for ch in pattern
    )
    tail = pattern.rsplit(".", 1)[-1] if "." in pattern else ""
    anchored = not (len(tail) == 3 and "*" not in tail and "?" not in tail)
    return re.compile(body + ("$" if anchored else ""), re.IGNORECASE)


def _expand_file(category_id: str, absolute_path: str, relative_path: str) -> list[BackupPlannedFile]:
    if not os.path.isfile(absolute_path):
        return []
    st = os.stat(absolute_path)
    return [BackupPlannedFile(
        category_id=category_id,
        absolute_path=os.path.abspath(absolute_path),
        relative_path=relative_path,
        size_bytes=st.st_size,
        last_write_time_utc=file_mtime_utc(absolute_path),
    )]


def _expand_directory(
    category_id: str, install_root_path: str, absolute_directory_path: str, path_spec: PathSpec,
) -> list[BackupPlannedFile]:
    if not os.path.isdir(absolute_directory_path):
        return []

    matcher = _pattern_to_regex(path_spec.search_pattern or "*")
    results: list[BackupPlannedFile] = []

    if path_spec.recursive:
        for dirpath, _dirnames, filenames in os.walk(absolute_directory_path):
            for name in filenames:
                if not matcher.match(name):
                    continue
                results.append(_make(category_id, install_root_path, os.path.join(dirpath, name)))
    else:
        try:
            names = os.listdir(absolute_directory_path)
        except OSError:
            return []
        for name in names:
            full = os.path.join(absolute_directory_path, name)
            if not os.path.isfile(full) or not matcher.match(name):
                continue
            results.append(_make(category_id, install_root_path, full))

    return results


def _make(category_id: str, install_root_path: str, file_path: str) -> BackupPlannedFile:
    rel = normalize_relative_path(os.path.relpath(file_path, install_root_path))
    st = os.stat(file_path)
    return BackupPlannedFile(
        category_id=category_id,
        absolute_path=os.path.abspath(file_path),
        relative_path=rel,
        size_bytes=st.st_size,
        last_write_time_utc=file_mtime_utc(file_path),
    )


def expand_path_spec(
    install_root_path: str, user_dir_name: str, category_id: str, path_spec: PathSpec,
) -> list[BackupPlannedFile]:
    """展开单个 PathSpec（对齐 C# `PathSpecExpander.Expand`）。"""
    relative_template = path_spec.relative_path_template.replace(USER_PLACEHOLDER, user_dir_name)
    relative_path = normalize_relative_path(relative_template)
    absolute_path = os.path.abspath(os.path.join(install_root_path, relative_path))

    if path_spec.kind == PathSpecKind.FILE:
        return _expand_file(category_id, absolute_path, relative_path)
    if path_spec.kind == PathSpecKind.DIRECTORY:
        return _expand_directory(category_id, install_root_path, absolute_path, path_spec)
    return []


def resolve_categories(selected_category_ids) -> list[BackupCategory]:
    if not selected_category_ids:
        return []
    return [get_category(cid) for cid in selected_category_ids]


def create_plan(
    install_root_path: str, user_dir_name: str, selected_category_ids,
) -> BackupPlan:
    """生成导出计划；同一文件被多个分类重复包含时报错（与 C# 版同判据）。"""
    install_root: InstallRootInfo = validate_install_root(install_root_path)
    validate_user_dir_name(user_dir_name)

    planned: list[BackupPlannedFile] = []
    seen: set[str] = set()

    for category in resolve_categories(selected_category_ids):
        for path_spec in category.paths:
            for f in expand_path_spec(install_root.install_root_path, user_dir_name, category.id, path_spec):
                key = f.relative_path.lower()
                if key in seen:
                    raise ValueError(f"文件被多个选项重复包含：{f.relative_path}")
                seen.add(key)
                planned.append(f)

    planned.sort(key=lambda f: _sort_key(f.relative_path))
    return BackupPlan(install_root.install_root_path, user_dir_name, planned)


@dataclass(frozen=True)
class CategoryScanResult:
    category_id: str
    file_count: int
    total_bytes: int


def scan_categories(
    install_root_path: str, user_dir_name: str, categories=None,
) -> list[CategoryScanResult]:
    """扫描各分类在当前机器上的文件数/字节数（对齐 C# `BackupCategoryScanner`）。"""
    install_root = validate_install_root(install_root_path)
    validate_user_dir_name(user_dir_name)
    cats = list(categories) if categories is not None else list(all_categories())

    results: list[CategoryScanResult] = []
    for category in cats:
        seen: set[str] = set()
        count = 0
        total = 0
        for path_spec in category.paths:
            for f in expand_path_spec(install_root.install_root_path, user_dir_name, category.id, path_spec):
                if f.relative_path.lower() in seen:
                    continue
                seen.add(f.relative_path.lower())
                count += 1
                total += f.size_bytes
        results.append(CategoryScanResult(category.id, count, total))
    return results
