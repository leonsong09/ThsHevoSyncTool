"""核心逻辑回归测试（纯 Python，无需 PyQt6）。

重点保障：
1. 与 C# 版**备份包格式互通**（manifest 字段/转义/行尾）；
2. 分类目录与推荐预设的**项数与 C# 版逐项对齐**（r1=25 / r2=14 / r3=12）；
3. 导出→导入端到端（含账号重映射、SHA256 校验、导入前备份可回灌）。
"""

from __future__ import annotations

import json
import os
import zipfile
from datetime import datetime

import pytest

from ths_hevo_sync import categories as cats
from ths_hevo_sync import pathspec
from ths_hevo_sync.importer import apply_import, create_import_plan, read_manifest
from ths_hevo_sync.manifest import (
    BackupManifest,
    BackupManifestFileEntry,
    BackupSelectionInfo,
    BackupSourceInfo,
    BackupToolInfo,
)
from ths_hevo_sync.package import package_file_name, write_backup_package
from ths_hevo_sync.planner import _pattern_to_regex, create_plan, scan_categories

SAMPLE_FILES = {
    r"bin\users\mx_111\function\CustomIndicator\ma.json": '{"a":1}',
    r"bin\users\mx_111\function\candle\my.py": "print(1)",
    r"bin\users\mx_111\function\MyLanguageGroupOrder.xml": "<x/>",
    r"bin\users\mx_111\index_setting\IndexCombination\IndexCombination.xml": "<i/>",
    r"bin\users\mx_111\index_setting\IndexConfig\p1.json": '{"b":2}',
    r"bin\users\mx_111\index_setting\index_show.ini": "[a]\nb=1\n",
    r"bin\users\mx_111\CustomPage\CustomDockingTemplate\d.config": "<c/>",
    r"bin\users\mx_111\UserPageCoding.xml": "<p/>",
}


def make_install_root(base: str, user: str = "mx_111", files=None) -> str:
    root = os.path.join(base, "ths")
    for rel, content in (files or SAMPLE_FILES).items():
        path = os.path.join(root, rel.replace("mx_111", user))
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w", encoding="utf-8") as fp:
            fp.write(content)
    os.makedirs(os.path.join(root, "bin", "users"), exist_ok=True)
    return root


# ------------------------------------------------------------------ 目录/预设

def test_category_catalog_size():
    assert len(cats.ALL_CATEGORIES) == 29
    assert len(cats.CORE_CATEGORIES) == 9
    assert len(cats.core_defaults()) == 9
    ids = [c.id for c in cats.ALL_CATEGORIES]
    assert len(ids) == len(set(ids)), "分类 id 必须唯一"


def test_preset_counts_match_csharp():
    counts = {p.id: len(p.selected_category_ids) for p in cats.PRESETS}
    assert counts == {"r1": 25, "r2": 14, "r3": 12}


def test_expand_selection_modes():
    assert cats.expand_selection() == [c.id for c in cats.core_defaults()]
    assert len(cats.expand_selection(level="safe")) == 11
    assert len(cats.expand_selection(level="all")) == 29
    assert cats.expand_selection(preset_id="r3") == list(cats.get_preset("r3").selected_category_ids)
    with pytest.raises(ValueError):
        cats.expand_selection(category_ids=["not_a_category"])


# -------------------------------------------------------------------- 路径安全

def test_normalize_relative_path_rejects_unsafe():
    assert pathspec.normalize_relative_path("a\\b/c.txt") == "a\\b\\c.txt"
    for bad in ["C:\\abs\\x", "\\\\server\\share", "\\root", "a\\..\\b", ""]:
        with pytest.raises(pathspec.PathNotAllowedError):
            pathspec.normalize_relative_path(bad)


def test_is_path_escape():
    assert pathspec.is_path_escape("../x") is True
    assert pathspec.is_path_escape("bin\\users\\mx_1\\a.txt") is False


def test_pattern_matches_win32_three_char_extension_quirk():
    # .NET/Win32：扩展名恰为 3 字符时按前缀匹配
    assert _pattern_to_regex("*.xml").match("a.xmlx")
    assert _pattern_to_regex("*.json").match("a.json")
    assert not _pattern_to_regex("*.json").match("a.jsonx")
    assert _pattern_to_regex("MutilPeriod*.ini").match("MutilPeriod1.ini2")


# --------------------------------------------------------------------- manifest

def _sample_manifest() -> BackupManifest:
    return BackupManifest(
        schema_version="1",
        tool=BackupToolInfo("ThsHevoSyncTool", "0.2.0", "Python 3.12.0"),
        created_at_utc=datetime(2026, 10, 19, 13, 50, 28, 376028),
        source=BackupSourceInfo("D:\\ths", "mx_111", "1.0.0"),
        selection=BackupSelectionInfo("r1", ["custom_indicators"]),
        files=[BackupManifestFileEntry(
            r"bin\users\mx_111\function\CustomIndicator\ma.json", 7,
            "0" * 64, datetime(2026, 10, 19, 13, 50, 28, 223128), "custom_indicators",
        )],
    )


def test_manifest_roundtrip():
    original = _sample_manifest()
    restored = BackupManifest.from_json(original.to_json())
    assert restored.to_dict() == original.to_dict()


def test_manifest_json_shape_matches_csharp():
    text = _sample_manifest().to_json()
    assert "\r\n" in text, "C# 侧是 CRLF 行尾"
    data = json.loads(text)
    assert list(data.keys()) == ["schemaVersion", "tool", "createdAtUtc", "source", "selection", "files"]
    assert list(data["tool"].keys()) == ["name", "version", "runtime"]
    assert list(data["files"][0].keys()) == [
        "relativePath", "sizeBytes", "sha256", "lastWriteTimeUtc", "categoryId",
    ]
    # 7 位小数秒 + Z（System.Text.Json 口径）
    assert data["createdAtUtc"] == "2026-10-19T13:50:28.3760280Z"


def test_manifest_parses_csharp_seven_digit_fraction():
    m = BackupManifest.from_json(
        '{"schemaVersion":"1","tool":{"name":"ThsHevoSyncTool","version":"0.1.7","runtime":".NET 8.0.24"},'
        '"createdAtUtc":"2026-10-19T13:54:43.5085103Z","source":{"installRoot":"D:\\\\ths","userDirName":"mx_111"},'
        '"selection":{"defaultPreset":"core","categories":[]},"files":[]}'
    )
    assert m.created_at_utc.microsecond == 508510
    assert m.source.user_dir_name == "mx_111"


def test_package_file_name_format():
    name = package_file_name(datetime(2026, 10, 19, 21, 54, 32))
    assert name == "ThsHevoSyncTool-20261019_215432.zip"


# --------------------------------------------------------------- 端到端往返

def test_export_import_roundtrip(tmp_path):
    src_root = make_install_root(str(tmp_path))
    target_root = make_install_root(
        str(tmp_path / "pc2"), user="mx_222",
        files={r"bin\users\mx_222\function\CustomIndicator\ma.json": '{"OLD":1}'},
    )

    selected = cats.expand_selection(preset_id="r1")
    plan = create_plan(src_root, "mx_111", selected)
    assert len(plan.files) == len(SAMPLE_FILES)

    zip_path = write_backup_package(
        plan, str(tmp_path / "out" / package_file_name(datetime(2026, 10, 19, 12, 0, 0))),
        default_preset="r1", selected_category_ids=selected,
    )
    with zipfile.ZipFile(zip_path) as zf:
        names = zf.namelist()
        assert "manifest.json" in names
        assert all("\\" not in n for n in names), "ZIP 条目名必须用 '/'"

    manifest = read_manifest(zip_path)
    assert manifest.source.user_dir_name == "mx_111"
    assert len(manifest.files) == len(SAMPLE_FILES)

    import_plan = create_import_plan(zip_path, target_root, "mx_222")
    assert all("mx_222" in i.relative_path for i in import_plan.items)

    backup_zip = apply_import(import_plan)
    assert backup_zip and os.path.isfile(backup_zip), "导入前必须生成备份包"

    restored = os.path.join(target_root, r"bin\users\mx_222\function\CustomIndicator\ma.json")
    with open(restored, encoding="utf-8") as fp:
        assert fp.read() == '{"a":1}'

    # 导入前备份包本身也可再次导入（C# 侧曾修过的回归点）
    assert len(read_manifest(backup_zip).files) == 1


def test_import_detects_sha256_mismatch(tmp_path):
    src_root = make_install_root(str(tmp_path))
    plan = create_plan(src_root, "mx_111", cats.expand_selection(level="safe"))
    zip_path = write_backup_package(plan, str(tmp_path / "b.zip"), default_preset="safe")

    target_root = make_install_root(str(tmp_path / "pc2"), user="mx_222", files={})
    import_plan = create_import_plan(zip_path, target_root, "mx_222")
    with zipfile.ZipFile(zip_path) as zf:
        items = {n: zf.read(n) for n in zf.namelist()}
    items[import_plan.items[0].entry_name] = b"tampered"
    with zipfile.ZipFile(zip_path, "w") as zf:
        for name, data in items.items():
            zf.writestr(name, data)

    with pytest.raises(ValueError, match="SHA256"):
        apply_import(import_plan, make_backup=False)


def test_scan_categories_counts(tmp_path):
    root = make_install_root(str(tmp_path))
    results = {r.category_id: r for r in scan_categories(root, "mx_111")}
    assert results["custom_indicators"].file_count == 2
    assert results["index_pack_configs"].file_count == 1
    assert results["global_user_config"].file_count == 0


def test_install_root_validation(tmp_path):
    from ths_hevo_sync.environment import list_user_directories, validate_install_root

    root = make_install_root(str(tmp_path))
    info = validate_install_root(root)
    assert list_user_directories(info) == ["mx_111"]
    with pytest.raises(FileNotFoundError):
        validate_install_root(str(tmp_path / "nope"))
