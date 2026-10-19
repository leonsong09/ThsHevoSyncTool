"""PyQt6 图形界面 —— 与 CLI/核心共用同一套逻辑（不重复实现）。

启动：python -m ths_hevo_sync.gui   （或 ths-hevo-sync-gui 入口）
"""

from __future__ import annotations

import os
import sys

from PyQt6.QtCore import Qt
from PyQt6.QtWidgets import (
    QApplication, QCheckBox, QComboBox, QFileDialog, QGridLayout, QGroupBox,
    QHBoxLayout, QLabel, QLineEdit, QMainWindow, QMessageBox, QPlainTextEdit,
    QPushButton, QTableWidget, QTableWidgetItem, QTabWidget, QVBoxLayout, QWidget,
)

from .. import version
from ..categories import ALL_CATEGORIES, PRESETS, get_category
from ..environment import list_user_directories, validate_install_root
from ..importer import apply_import, create_import_plan, read_manifest
from ..package import package_file_name, write_backup_package
from ..planner import create_plan, scan_categories


def _human(n: int) -> str:
    value = float(n)
    for unit in ("B", "KB", "MB", "GB"):
        if value < 1024 or unit == "GB":
            return f"{value:.1f} {unit}"
        value /= 1024
    return f"{value:.1f} GB"


class ExportPage(QWidget):
    def __init__(self, log):
        super().__init__()
        self.log = log
        self._scan_cache: dict[str, tuple[int, int]] = {}
        self._build()

    def _build(self):
        root_box = QGroupBox("① 同花顺安装根目录（含 bin\\users）")
        rl = QHBoxLayout(root_box)
        self.root_edit = QLineEdit()
        btn_root = QPushButton("浏览…")
        btn_root.clicked.connect(self._pick_root)
        rl.addWidget(self.root_edit, 1)
        rl.addWidget(btn_root)

        user_box = QGroupBox("② 账号目录")
        ul = QHBoxLayout(user_box)
        self.user_combo = QComboBox()
        btn_refresh = QPushButton("刷新账号")
        btn_refresh.clicked.connect(self.refresh_users)
        ul.addWidget(self.user_combo, 1)
        ul.addWidget(btn_refresh)

        cat_box = QGroupBox("③ 选择要备份的分类")
        cl = QVBoxLayout(cat_box)
        pl = QHBoxLayout()
        for preset in PRESETS:
            b = QPushButton(f"{preset.id} {preset.name}")
            b.setToolTip(preset.description)
            b.clicked.connect(lambda _=False, pid=preset.id: self.apply_preset(pid))
            pl.addWidget(b)
        b_core = QPushButton("仅核心")
        b_core.clicked.connect(lambda: self.apply_preset("core"))
        b_all = QPushButton("全选")
        b_all.clicked.connect(lambda: self._set_all(True))
        b_none = QPushButton("全不选")
        b_none.clicked.connect(lambda: self._set_all(False))
        b_scan = QPushButton("扫描本机")
        b_scan.clicked.connect(self.scan)
        for b in (b_core, b_all, b_none, b_scan):
            pl.addWidget(b)
        pl.addStretch(1)
        cl.addLayout(pl)

        self.table = QTableWidget(len(ALL_CATEGORIES), 4)
        self.table.setHorizontalHeaderLabels(["选", "分类 id", "文件数", "大小"])
        self.table.verticalHeader().setVisible(False)
        self.table.setColumnWidth(0, 36)
        self.table.setColumnWidth(1, 300)
        self.table.setColumnWidth(2, 70)
        self.table.setColumnWidth(3, 90)
        for row, cat in enumerate(ALL_CATEGORIES):
            chk = QCheckBox()
            chk.setChecked(cat.is_core_default)
            self.table.setCellWidget(row, 0, chk)
            self.table.setItem(row, 1, QTableWidgetItem(f"{cat.id}  —  {cat.name}"))
            self.table.setItem(row, 2, QTableWidgetItem("-"))
            self.table.setItem(row, 3, QTableWidgetItem("-"))
        cl.addWidget(self.table)

        out_box = QGroupBox("④ 输出目录")
        ol = QHBoxLayout(out_box)
        self.out_edit = QLineEdit(os.path.join(os.getcwd(), "backups"))
        btn_out = QPushButton("浏览…")
        btn_out.clicked.connect(self._pick_out)
        ol.addWidget(self.out_edit, 1)
        ol.addWidget(btn_out)

        self.btn_export = QPushButton("导出备份包（ZIP + manifest.json）")
        self.btn_export.clicked.connect(self.do_export)

        layout = QVBoxLayout(self)
        for w in (root_box, user_box, cat_box, out_box, self.btn_export):
            layout.addWidget(w)

    # ------------------------------------------------------------- helpers
    def _pick_root(self):
        path = QFileDialog.getExistingDirectory(self, "选择同花顺安装根目录")
        if path:
            self.root_edit.setText(path)
            self.refresh_users()

    def _pick_out(self):
        path = QFileDialog.getExistingDirectory(self, "选择输出目录")
        if path:
            self.out_edit.setText(path)

    def refresh_users(self):
        self.user_combo.clear()
        try:
            info = validate_install_root(self.root_edit.text())
        except Exception as exc:
            self.log(f"[错误] {exc}")
            return
        users = list_user_directories(info)
        self.user_combo.addItems(users)
        self.log(f"发现 {len(users)} 个账号目录。")

    def _set_all(self, value: bool):
        for row in range(self.table.rowCount()):
            self.table.cellWidget(row, 0).setChecked(value)

    def apply_preset(self, preset_id: str):
        if preset_id == "core":
            selected = {c.id for c in ALL_CATEGORIES if c.is_core_default}
        else:
            selected = set(next(p for p in PRESETS if p.id == preset_id).selected_category_ids)
        for row, cat in enumerate(ALL_CATEGORIES):
            self.table.cellWidget(row, 0).setChecked(cat.id in selected)

    def selected_ids(self) -> list[str]:
        return [
            cat.id for row, cat in enumerate(ALL_CATEGORIES)
            if self.table.cellWidget(row, 0).isChecked()
        ]

    def scan(self):
        try:
            info = validate_install_root(self.root_edit.text())
            user = self.user_combo.currentText()
            if not user:
                raise ValueError("请先选择账号目录。")
            results = scan_categories(info.install_root_path, user)
        except Exception as exc:
            self.log(f"[错误] {exc}")
            return
        self._scan_cache = {r.category_id: (r.file_count, r.total_bytes) for r in results}
        for row, cat in enumerate(ALL_CATEGORIES):
            count, size = self._scan_cache.get(cat.id, (0, 0))
            self.table.setItem(row, 2, QTableWidgetItem(str(count)))
            self.table.setItem(row, 3, QTableWidgetItem(_human(size)))
        self.log("扫描完成。")

    def do_export(self):
        try:
            info = validate_install_root(self.root_edit.text())
            user = self.user_combo.currentText()
            ids = self.selected_ids()
            if not ids:
                raise ValueError("请至少选择一个分类。")
            plan = create_plan(info.install_root_path, user, ids)
            out_dir = self.out_edit.text().strip() or os.path.join(os.getcwd(), "backups")
            os.makedirs(out_dir, exist_ok=True)
            target = os.path.join(out_dir, package_file_name())
            self.log(f"待导出 {len(plan.files)} 个文件 / {_human(plan.total_bytes)}")
            path = write_backup_package(
                plan, target, default_preset="gui", selected_category_ids=ids,
                progress=lambda p: self.log(f"  {p.message}"),
            )
            self.log(f"[完成] 已导出：{path}")
            QMessageBox.information(self, "导出完成", f"已导出：\n{path}")
        except Exception as exc:
            self.log(f"[错误] {exc}")
            QMessageBox.warning(self, "导出失败", str(exc))


class ImportPage(QWidget):
    def __init__(self, log):
        super().__init__()
        self.log = log
        self.manifest = None
        self._build()

    def _build(self):
        zip_box = QGroupBox("① 备份包（ZIP）")
        zl = QHBoxLayout(zip_box)
        self.zip_edit = QLineEdit()
        btn_zip = QPushButton("浏览…")
        btn_zip.clicked.connect(self._pick_zip)
        btn_preview = QPushButton("预览内容")
        btn_preview.clicked.connect(self.preview)
        zl.addWidget(self.zip_edit, 1)
        zl.addWidget(btn_zip)
        zl.addWidget(btn_preview)

        tgt_box = QGroupBox("② 目标环境")
        gl = QGridLayout(tgt_box)
        self.root_edit = QLineEdit()
        btn_root = QPushButton("浏览…")
        btn_root.clicked.connect(self._pick_root)
        self.user_combo = QComboBox()
        btn_refresh = QPushButton("刷新账号")
        btn_refresh.clicked.connect(self.refresh_users)
        gl.addWidget(QLabel("安装根目录"), 0, 0)
        gl.addWidget(self.root_edit, 0, 1)
        gl.addWidget(btn_root, 0, 2)
        gl.addWidget(QLabel("目标账号"), 1, 0)
        gl.addWidget(self.user_combo, 1, 1)
        gl.addWidget(btn_refresh, 1, 2)

        self.cat_box = QGroupBox("③ 包内分类（勾选=导入）")
        self.cat_layout = QVBoxLayout(self.cat_box)
        self._cat_checks: list[QCheckBox] = []

        btn_row = QHBoxLayout()
        self.btn_dry = QPushButton("预演（不写入）")
        self.btn_dry.clicked.connect(lambda: self.do_import(apply=False))
        self.btn_apply = QPushButton("开始导入（写入）")
        self.btn_apply.clicked.connect(lambda: self.do_import(apply=True))
        btn_row.addWidget(self.btn_dry)
        btn_row.addWidget(self.btn_apply)
        btn_row.addStretch(1)

        layout = QVBoxLayout(self)
        for w in (zip_box, tgt_box, self.cat_box):
            layout.addWidget(w)
        layout.addLayout(btn_row)

    def _pick_zip(self):
        path, _ = QFileDialog.getOpenFileName(self, "选择备份包", "", "ZIP 备份包 (*.zip)")
        if path:
            self.zip_edit.setText(path)
            self.preview()

    def _pick_root(self):
        path = QFileDialog.getExistingDirectory(self, "选择同花顺安装根目录")
        if path:
            self.root_edit.setText(path)
            self.refresh_users()

    def refresh_users(self):
        self.user_combo.clear()
        try:
            info = validate_install_root(self.root_edit.text())
        except Exception as exc:
            self.log(f"[错误] {exc}")
            return
        self.user_combo.addItems(list_user_directories(info))

    def preview(self):
        try:
            self.manifest = read_manifest(self.zip_edit.text().strip())
        except Exception as exc:
            self.log(f"[错误] {exc}")
            return
        for chk in self._cat_checks:
            chk.deleteLater()
        self._cat_checks = []
        counts: dict[str, int] = {}
        sizes: dict[str, int] = {}
        for entry in self.manifest.files:
            counts[entry.category_id] = counts.get(entry.category_id, 0) + 1
            sizes[entry.category_id] = sizes.get(entry.category_id, 0) + entry.size_bytes
        for cid in sorted(counts):
            try:
                name = get_category(cid).name
            except ValueError:
                name = "(未知分类)"
            chk = QCheckBox(f"{cid}  —  {name}   （{counts[cid]} 个 / {_human(sizes[cid])}）")
            chk.setChecked(True)
            self.cat_layout.addWidget(chk)
            self._cat_checks.append(chk)
        m = self.manifest
        self.log(
            f"包信息：{m.tool.name} v{m.tool.version}；来源 {m.source.install_root} / "
            f"{m.source.user_dir_name}；共 {len(m.files)} 个文件。"
        )

    def do_import(self, apply: bool):
        try:
            if self.manifest is None:
                self.preview()
            if self.manifest is None:
                return
            info = validate_install_root(self.root_edit.text())
            user = self.user_combo.currentText()
            if not user:
                raise ValueError("请选择目标账号目录。")
            ids = [c.split("  —  ")[0] for c, chk in zip(
                [c.text() for c in self._cat_checks], self._cat_checks) if chk.isChecked()]
            plan = create_import_plan(
                self.zip_edit.text().strip(), info.install_root_path, user,
                selected_category_ids=ids, allow_missing_categories=True,
            )
            self.log(f"将写入 {len(plan.items)} 个文件 / {_human(plan.total_bytes)}")
            if not apply:
                self.log("[预演] 未写入任何文件。")
                return
            backup = apply_import(plan, progress=lambda p: self.log(f"  {p.message}"))
            self.log(f"[完成] 已导入 {len(plan.items)} 个文件。")
            if backup:
                self.log(f"导入前备份：{backup}")
            QMessageBox.information(self, "导入完成", f"已导入 {len(plan.items)} 个文件。")
        except Exception as exc:
            self.log(f"[错误] {exc}")
            QMessageBox.warning(self, "导入失败", str(exc))


class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle(f"{version.APP_NAME} v{version.APP_VERSION}")
        self.resize(1000, 780)

        self.log_view = QPlainTextEdit()
        self.log_view.setReadOnly(True)
        self.log_view.setMaximumBlockCount(500)

        def log(text: str) -> None:
            self.log_view.appendPlainText(text)

        self.tabs = QTabWidget()
        self.tabs.addTab(ExportPage(log), "导出（备份）")
        self.tabs.addTab(ImportPage(log), "导入（迁移/恢复）")

        central = QWidget()
        layout = QVBoxLayout(central)
        layout.addWidget(self.tabs)
        layout.addWidget(QLabel("日志"))
        layout.addWidget(self.log_view)
        self.setCentralWidget(central)


def main(argv=None) -> int:
    app = QApplication(argv if argv is not None else sys.argv)
    win = MainWindow()
    win.show()
    return app.exec()


if __name__ == "__main__":
    raise SystemExit(main())
