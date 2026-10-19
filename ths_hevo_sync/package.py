"""备份包写出 —— 对齐 C# 版 `BackupPackageWriter`。

ZIP 布局：先按计划顺序写入各配置项（条目名用 '/'），最后写根目录 `manifest.json`。
"""

from __future__ import annotations

import os
import sys
import zipfile
from dataclasses import dataclass
from datetime import datetime
from typing import Callable

from . import version
from .io_utils import copy_and_hash, utc_now, zip_date_time
from .manifest import (
    CURRENT_SCHEMA_VERSION,
    MANIFEST_ENTRY_NAME,
    BackupManifest,
    BackupManifestFileEntry,
    BackupSelectionInfo,
    BackupSourceInfo,
    BackupToolInfo,
)
from .pathspec import to_zip_entry
from .planner import BackupPlan


@dataclass(frozen=True)
class BackupProgress:
    message: str
    completed_files: int
    total_files: int
    completed_bytes: int
    total_bytes: int

    @property
    def percent(self) -> float:
        return 0.0 if self.total_files <= 0 else self.completed_files / self.total_files


def runtime_description() -> str:
    return f"Python {sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}"


def package_file_name(now: datetime | None = None) -> str:
    stamp = (now or datetime.now()).strftime("%Y%m%d_%H%M%S")
    return f"ThsHevoSyncTool-{stamp}.zip"


def write_backup_package(
    plan: BackupPlan,
    output_zip_path: str,
    *,
    default_preset: str = "core",
    selected_category_ids: list[str] | None = None,
    app_exe_version: str | None = None,
    progress: Callable[[BackupProgress], None] | None = None,
) -> str:
    """按计划写出备份 ZIP，返回实际写入路径。"""
    output_zip_path = os.path.abspath(output_zip_path)
    os.makedirs(os.path.dirname(output_zip_path) or ".", exist_ok=True)

    total_files = len(plan.files)
    total_bytes = plan.total_bytes
    completed_files = 0
    completed_bytes = 0

    entries: list[BackupManifestFileEntry] = []
    tmp_path = output_zip_path + ".part"

    def report(message: str) -> None:
        if progress is not None:
            progress(BackupProgress(message, completed_files, total_files, completed_bytes, total_bytes))

    try:
        with zipfile.ZipFile(tmp_path, "w", zipfile.ZIP_DEFLATED) as zf:
            for planned in plan.files:
                entry_name = to_zip_entry(planned.relative_path)
                info = zipfile.ZipInfo(entry_name, date_time=zip_date_time(planned.last_write_time_utc))
                info.compress_type = zipfile.ZIP_DEFLATED
                info.external_attr = 0o600 << 16
                with open(planned.absolute_path, "rb") as src, zf.open(info, "w") as dst:
                    sha = copy_and_hash(src, dst)

                entries.append(BackupManifestFileEntry(
                    relative_path=planned.relative_path,
                    size_bytes=planned.size_bytes,
                    sha256=sha,
                    last_write_time_utc=planned.last_write_time_utc,
                    category_id=planned.category_id,
                ))
                completed_files += 1
                completed_bytes += planned.size_bytes
                report(f"导出：{planned.relative_path}")

            manifest = BackupManifest(
                schema_version=CURRENT_SCHEMA_VERSION,
                tool=BackupToolInfo(version.TOOL_NAME, version.TOOL_VERSION, runtime_description()),
                created_at_utc=utc_now(),
                source=BackupSourceInfo(
                    install_root=plan.install_root_path,
                    user_dir_name=plan.user_dir_name,
                    app_exe_version=app_exe_version,
                ),
                selection=BackupSelectionInfo(
                    default_preset=default_preset,
                    categories=list(selected_category_ids or []),
                ),
                files=entries,
            )
            zf.writestr(MANIFEST_ENTRY_NAME, manifest.to_json())

        os.replace(tmp_path, output_zip_path)
    except BaseException:
        if os.path.exists(tmp_path):
            os.remove(tmp_path)
        raise

    report("导出完成。")
    return output_zip_path
