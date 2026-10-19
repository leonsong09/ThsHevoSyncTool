"""导入计划与导入执行 —— 对齐 C# 版 `BackupImportPlanner` / `BackupPackageImporter`。"""

from __future__ import annotations

import os
import zipfile
from dataclasses import dataclass, field
from datetime import datetime
from typing import Callable

from .environment import validate_install_root, validate_user_dir_name
from .io_utils import assert_directory_writable, copy_and_hash, file_mtime_utc
from .manifest import MANIFEST_ENTRY_NAME, BackupManifest
from .pathspec import (
    PathNotAllowedError,
    ensure_under_root,
    is_path_escape,
    normalize_relative_path,
)
from .planner import BackupPlan, BackupPlannedFile
from .package import BackupProgress, write_backup_package


@dataclass(frozen=True)
class BackupImportPlanItem:
    category_id: str
    entry_name: str
    relative_path: str
    absolute_path: str
    size_bytes: int
    sha256: str


@dataclass
class BackupImportPlan:
    zip_path: str
    install_root_path: str
    source_user_dir_name: str
    target_user_dir_name: str
    manifest: BackupManifest
    items: list[BackupImportPlanItem] = field(default_factory=list)
    missing_categories: list[str] = field(default_factory=list)

    @property
    def total_bytes(self) -> int:
        return sum(i.size_bytes for i in self.items)


def read_manifest(zip_path: str) -> BackupManifest:
    with zipfile.ZipFile(zip_path, "r") as zf:
        try:
            raw = zf.read(MANIFEST_ENTRY_NAME)
        except KeyError:
            raise ValueError(f"备份包缺少 {MANIFEST_ENTRY_NAME}。") from None
    return BackupManifest.from_json(raw.decode("utf-8"))


def remap_relative_path(relative_path: str, source_user_dir_name: str, target_user_dir_name: str) -> str:
    """把 `bin\\users\\<源账号>\\...` 换成 `bin\\users\\<目标账号>\\...`（对齐 C# 同名函数）。"""
    if not source_user_dir_name or source_user_dir_name.lower() == target_user_dir_name.lower():
        return relative_path
    parts = relative_path.split("\\")
    if len(parts) < 4 or parts[0].lower() != "bin" or parts[1].lower() != "users":
        return relative_path
    if parts[2].lower() != source_user_dir_name.lower():
        return relative_path
    parts[2] = target_user_dir_name
    return "\\".join(parts)


def create_import_plan(
    zip_path: str,
    install_root_path: str,
    target_user_dir_name: str,
    selected_category_ids=None,
    allow_missing_categories: bool = False,
) -> BackupImportPlan:
    install_root = validate_install_root(install_root_path)
    validate_user_dir_name(target_user_dir_name)

    manifest = read_manifest(zip_path)
    if manifest.schema_version != "1":
        raise ValueError(f"不支持的备份包版本：{manifest.schema_version}")

    selected = [c.strip() for c in (selected_category_ids or []) if c and c.strip()]
    wanted = {c.lower() for c in selected}

    manifest_categories = []
    seen_categories: set[str] = set()
    for entry in manifest.files:
        if entry.category_id.lower() not in seen_categories:
            seen_categories.add(entry.category_id.lower())
            manifest_categories.append(entry.category_id)

    if wanted:
        missing = [c for c in selected if c.lower() not in seen_categories]
        if missing and not allow_missing_categories:
            raise ValueError(
                "备份包中不存在的分类：" + ", ".join(missing) + "（可用 allow_missing_categories 忽略）"
            )
    else:
        missing = []

    plan = BackupImportPlan(
        zip_path=os.path.abspath(zip_path),
        install_root_path=install_root.install_root_path,
        source_user_dir_name=manifest.source.user_dir_name,
        target_user_dir_name=target_user_dir_name,
        manifest=manifest,
        missing_categories=missing,
    )

    for entry in manifest.files:
        if wanted and entry.category_id.lower() not in wanted:
            continue
        rel = remap_relative_path(
            normalize_relative_path(entry.relative_path),
            manifest.source.user_dir_name,
            target_user_dir_name,
        )
        if is_path_escape(rel):
            raise PathNotAllowedError(f"路径越界：{entry.relative_path}")
        absolute = os.path.abspath(os.path.join(install_root.install_root_path, rel))
        ensure_under_root(install_root.install_root_path, absolute)
        plan.items.append(BackupImportPlanItem(
            category_id=entry.category_id,
            entry_name=entry.relative_path.replace("\\", "/"),
            relative_path=rel,
            absolute_path=absolute,
            size_bytes=entry.size_bytes,
            sha256=entry.sha256,
        ))

    return plan


def apply_import(
    plan: BackupImportPlan,
    *,
    backup_dir: str | None = None,
    make_backup: bool = True,
    progress: Callable[[BackupProgress], None] | None = None,
) -> str | None:
    """执行导入；返回导入前备份包路径（未生成时返回 None）。"""
    backup_zip: str | None = None

    if make_backup:
        existing = [i for i in plan.items if os.path.isfile(i.absolute_path)]
        if existing:
            target_dir = backup_dir or plan.install_root_path
            assert_directory_writable(target_dir)
            stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            backup_zip = os.path.join(os.path.abspath(target_dir), f"pre_import_backup_{stamp}.zip")
            files = [
                BackupPlannedFile(
                    category_id=i.category_id,
                    absolute_path=i.absolute_path,
                    relative_path=i.relative_path,
                    size_bytes=os.path.getsize(i.absolute_path),
                    last_write_time_utc=file_mtime_utc(i.absolute_path),
                )
                for i in existing
            ]
            files.sort(key=lambda f: f.relative_path.upper())
            backup_plan = BackupPlan(plan.install_root_path, plan.target_user_dir_name, files)
            write_backup_package(
                backup_plan,
                backup_zip,
                default_preset="pre_import",
                selected_category_ids=sorted({i.category_id for i in plan.items}),
                app_exe_version=None,
                progress=None,
            )

    total = len(plan.items)
    done = 0
    done_bytes = 0
    with zipfile.ZipFile(plan.zip_path, "r") as zf:
        for item in plan.items:
            os.makedirs(os.path.dirname(item.absolute_path), exist_ok=True)
            tmp = item.absolute_path + ".tmp"
            with zf.open(item.entry_name, "r") as src, open(tmp, "wb") as dst:
                digest = copy_and_hash(src, dst)
            if digest.lower() != item.sha256.lower():
                os.remove(tmp)
                raise ValueError(f"SHA256 校验失败：{item.relative_path}")
            os.replace(tmp, item.absolute_path)

            done += 1
            done_bytes += item.size_bytes
            if progress is not None:
                progress(BackupProgress(
                    f"导入：{item.relative_path}", done, total, done_bytes, plan.total_bytes,
                ))

    if progress is not None:
        progress(BackupProgress("导入完成。", done, total, done_bytes, plan.total_bytes))
    return backup_zip
