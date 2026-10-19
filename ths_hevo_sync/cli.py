"""同花顺配置备份/迁移 —— 命令行入口（纯 Python，无第三方依赖）。

约定：写操作（import）默认**预演**，必须显式 `--apply` 才落盘；退出码 0/1/2。
"""

from __future__ import annotations

import argparse
import os
import sys
from datetime import datetime

from . import version
from .categories import (
    ALL_CATEGORIES,
    PRESETS,
    core_defaults,
    expand_selection,
    get_category,
    get_preset,
)
from .environment import list_user_directories, validate_install_root
from .importer import apply_import, create_import_plan, read_manifest
from .package import BackupProgress, package_file_name, write_backup_package
from .planner import create_plan, scan_categories

EXE_NAME = "ths-hevo-sync"


def _out(text: str = "") -> None:
    print(text)


def _fmt_bytes(n: int) -> str:
    for unit in ("B", "KB", "MB", "GB"):
        if n < 1024 or unit == "GB":
            return f"{n:.0f} {unit}" if unit == "B" else f"{n / 1:.1f} {unit}"
        n /= 1024
    return f"{n:.1f} GB"


def _human_size(n: int) -> str:
    value = float(n)
    for unit in ("B", "KB", "MB", "GB"):
        if value < 1024 or unit == "GB":
            return f"{value:.1f} {unit}"
        value /= 1024
    return f"{value:.1f} GB"


# --------------------------------------------------------------------- 选择

def _select_categories(args) -> tuple[list[str], str]:
    ids = expand_selection(
        category_ids=getattr(args, "categories", None),
        preset_id=getattr(args, "preset", None),
        level=getattr(args, "level", None),
    )
    preset = getattr(args, "preset", None)
    if preset:
        label = preset
    elif getattr(args, "categories", None):
        label = "custom"
    elif getattr(args, "level", None):
        label = f"level:{args.level}"
    else:
        label = "core"
    return ids, label


def _require_root(path: str):
    if not path:
        raise ValueError("缺少 --root（同花顺安装根目录，含 bin\\users）。")
    return validate_install_root(path)


def _require_root_and_user(args):
    info = _require_root(args.root)
    if not args.user:
        raise ValueError("缺少 --user（账号目录名，如 mx_123456）。")
    return info


# ------------------------------------------------------------------ 子命令

def cmd_list(args) -> int:
    _out(f"{'分类 id':38s} {'核心':4s} 名称 / 路径规则")
    _out("-" * 100)
    for cat in ALL_CATEGORIES:
        mark = "是" if cat.is_core_default else ""
        _out(f"{cat.id:38s} {mark:4s} {cat.name}")
        for rule in cat.display_path_rules:
            _out(f"{'':43s}   {rule}")
    _out()
    _out(f"合计 {len(ALL_CATEGORIES)} 个分类（核心默认 {len(core_defaults())} 个）。")
    return 0


def cmd_presets(args) -> int:
    _out("推荐预设：")
    for preset in PRESETS:
        _out(f"  {preset.id}  {preset.name}（{len(preset.selected_category_ids)} 项）")
        _out(f"      {preset.description}")
    return 0


def cmd_users(args) -> int:
    info = _require_root(args.root)
    users = list_user_directories(info)
    _out(f"安装根目录: {info.install_root_path}")
    if not users:
        _out("（未发现 mx_* 账号目录）")
        return 0
    for name in users:
        _out(f"  {name}")
    return 0


def cmd_scan(args) -> int:
    info = _require_root_and_user(args)
    results = scan_categories(info.install_root_path, args.user)
    total_files = 0
    total_bytes = 0
    _out(f"{'分类 id':38s} {'文件':>6s} {'大小':>12s}  名称")
    _out("-" * 100)
    for res in results:
        total_files += res.file_count
        total_bytes += res.total_bytes
        name = get_category(res.category_id).name
        _out(f"{res.category_id:38s} {res.file_count:>6d} {_human_size(res.total_bytes):>12s}  {name}")
    _out("-" * 100)
    _out(f"合计: {total_files} 个文件 / {_human_size(total_bytes)}")
    return 0


def _progress_printer():
    def report(p: BackupProgress) -> None:
        if p.total_files <= 0:
            _out(f"  {p.message}")
            return
        pct = p.completed_files * 100 // p.total_files
        _out(f"  [{p.completed_files}/{p.total_files}] {pct:3d}%  {p.message}")
    return report


def cmd_export(args) -> int:
    info = _require_root_and_user(args)
    ids, label = _select_categories(args)

    plan = create_plan(info.install_root_path, args.user, ids)
    _out(f"安装根目录: {info.install_root_path}")
    _out(f"账号目录  : {args.user}")
    _out(f"选项      : {label}（{len(ids)} 项）")
    _out(f"待导出文件: {len(plan.files)} 个 / {_human_size(plan.total_bytes)}")

    if args.dry_run:
        for f in plan.files[:50]:
            _out(f"  {f.relative_path}")
        if len(plan.files) > 50:
            _out(f"  ...（其余 {len(plan.files) - 50} 个略）")
        _out("[DRY-RUN] 未写出备份包。")
        return 0

    out_dir = args.out or os.path.join(os.getcwd(), "backups")
    os.makedirs(out_dir, exist_ok=True)
    target = os.path.join(out_dir, package_file_name())

    _progress_printer()  # 保持与 GUI 相同的进度口径（CLI 下逐行输出）
    path = write_backup_package(
        plan, target,
        default_preset=label,
        selected_category_ids=ids,
        progress=_progress_printer(),
    )
    _out(f"[OK] 已导出: {path}")
    _out(f"     manifest: {len(plan.files)} 个文件条目 / schema 1")
    return 0


def cmd_inspect(args) -> int:
    manifest = read_manifest(args.zip)
    _out(f"备份包      : {os.path.abspath(args.zip)}")
    _out(f"schema      : {manifest.schema_version}")
    _out(f"工具        : {manifest.tool.name} v{manifest.tool.version} ({manifest.tool.runtime})")
    _out(f"创建时间(UTC): {manifest.created_at_utc}")
    _out(f"来源        : {manifest.source.install_root} / {manifest.source.user_dir_name}")
    if manifest.source.app_exe_version:
        _out(f"来源程序版本: {manifest.source.app_exe_version}")
    _out(f"预设/分类   : {manifest.selection.default_preset} / {len(manifest.selection.categories)} 项")
    _out(f"文件条目    : {len(manifest.files)} 个 / {_human_size(sum(f.size_bytes for f in manifest.files))}")
    _out()
    counts: dict[str, int] = {}
    for entry in manifest.files:
        counts[entry.category_id] = counts.get(entry.category_id, 0) + 1
    for cid, n in sorted(counts.items()):
        name = get_category(cid).name if _known(cid) else "(未知分类)"
        _out(f"  {cid:38s} {n:>5d} 个  {name}")
    return 0


def _known(category_id: str) -> bool:
    try:
        get_category(category_id)
        return True
    except ValueError:
        return False


def cmd_import(args) -> int:
    info = _require_root_and_user(args)
    plan = create_import_plan(
        args.zip,
        info.install_root_path,
        args.user,
        selected_category_ids=args.categories,
        allow_missing_categories=args.allow_missing,
    )

    _out(f"备份包      : {plan.zip_path}")
    _out(f"来源账号    : {plan.source_user_dir_name} → 目标账号: {plan.target_user_dir_name}")
    _out(f"目标根目录  : {plan.install_root_path}")
    _out(f"将写入      : {len(plan.items)} 个文件 / {_human_size(plan.total_bytes)}")
    if plan.missing_categories:
        _out(f"包中缺失分类: {', '.join(plan.missing_categories)}（已按 --allow-missing 忽略）")
    if not args.apply:
        _out("[DRY-RUN] 预演模式，未写入任何文件；确认后加 --apply 执行。")
        return 0

    backup_zip = apply_import(plan, backup_dir=args.backup_dir, progress=_progress_printer())
    _out(f"[OK] 已导入 {len(plan.items)} 个文件。")
    if backup_zip:
        _out(f"     导入前备份: {backup_zip}（回滚=用它再执行一次 import）")
    return 0


# -------------------------------------------------------------------- 参数

def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog=EXE_NAME,
        description=f"{version.APP_NAME} v{version.APP_VERSION}（命令行）",
    )
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("list", help="列出全部备份分类").set_defaults(func=cmd_list)

    sub.add_parser("presets", help="列出推荐预设 r1/r2/r3").set_defaults(func=cmd_presets)

    p_users = sub.add_parser("users", help="列出安装根目录下的 mx_* 账号目录")
    p_users.add_argument("--root", required=True, help="同花顺安装根目录（含 bin\\users）")
    p_users.set_defaults(func=cmd_users)

    p_scan = sub.add_parser("scan", help="扫描各分类在当前机器上的文件数/大小")
    p_scan.add_argument("--root", required=True)
    p_scan.add_argument("--user", required=True, help="账号目录名（mx_*）")
    p_scan.set_defaults(func=cmd_scan)

    def add_selection(p):
        g = p.add_mutually_exclusive_group()
        g.add_argument("--categories", help="逗号分隔的分类 id（默认=核心项）")
        g.add_argument("--preset", help="推荐预设 r1/r2/r3")
        g.add_argument("--level", choices=["safe", "all"], help="safe=核心+全局；all=全部")

    p_export = sub.add_parser("export", help="导出备份包（ZIP + manifest.json）")
    p_export.add_argument("--root", required=True)
    p_export.add_argument("--user", required=True)
    p_export.add_argument("--out", help="输出目录（默认 ./backups）")
    p_export.add_argument("--dry-run", action="store_true", help="只预演，不写包")
    add_selection(p_export)
    p_export.set_defaults(func=cmd_export)

    p_inspect = sub.add_parser("inspect", help="查看备份包内容（manifest）")
    p_inspect.add_argument("--zip", required=True, help="备份包路径")
    p_inspect.set_defaults(func=cmd_inspect)

    p_import = sub.add_parser("import", help="导入备份包（默认预演，--apply 才写入）")
    p_import.add_argument("--zip", required=True)
    p_import.add_argument("--root", required=True)
    p_import.add_argument("--user", required=True)
    p_import.add_argument("--categories", help="只导入指定分类（逗号分隔）")
    p_import.add_argument("--backup-dir", help="导入前备份包存放目录（默认=目标根目录）")
    p_import.add_argument("--allow-missing", action="store_true", help="忽略包中缺失的分类")
    p_import.add_argument("--apply", action="store_true", help="真正写入（缺省只预演）")
    p_import.set_defaults(func=cmd_import)

    return parser


def main(argv=None) -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
        sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    parser = build_parser()
    args = parser.parse_args(argv)
    if getattr(args, "categories", None):
        args.categories = [c.strip() for c in args.categories.split(",") if c.strip()]
    try:
        return args.func(args)
    except (ValueError, FileNotFoundError) as exc:
        _out(f"[ERROR] {exc}")
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
