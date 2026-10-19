# legacy-shell —— 可选图形外壳（PyQt6）

> **不属于 skill 主线**。skill/自动化只用仓库根的 `cli.py` + `ths_hevo_sync/` 核心（**零第三方依赖**）。
> 本目录是「给人手动点选」的外壳，从主线移出是为了让主线保持纯代码、无重依赖。

## 内容

| 路径 | 说明 |
|---|---|
| `gui/` | PyQt6 界面（`app.py` 直接调用 `ths_hevo_sync` 的 create_plan / write_backup_package / create_import_plan / apply_import，**不重复实现业务逻辑**） |
| `gui.py` | 启动器（自动把仓库根与本目录加入 `sys.path`） |
| `setup_venv.py` | 一键在**仓库根**创建 `.venv` 并装 PyQt6（默认走国内镜像，失败回退 PyPI） |
| `requirements.txt` | 仅 `PyQt6` |

## 用

```powershell
# 1) 装环境（只需一次；venv 建在仓库根 .venv）
python legacy-shell/setup_venv.py

# 2) 启动
.\.venv\Scripts\python legacy-shell/gui.py
```

依赖装坏时的典型症状与对策：
- `ImportError: DLL load failed while importing QtCore/QtWidgets` → PyQt6 装残缺（与本机 miniforge3 全局 PyQt6 冲突常见）。
  解决：`setup_venv.py --force` 重建 venv；或直接在干净 venv 里 `pip install PyQt6 -i https://pypi.tuna.tsinghua.edu.cn/simple`。

## 无头验证（不要碰用户桌面窗口）

```python
os.environ["QT_QPA_PLATFORM"] = "offscreen"
from PyQt6.QtWidgets import QApplication, QMessageBox
QMessageBox.information = staticmethod(lambda *a, **k: None)   # 模态框会挂死
w = MainWindow(); w.show(); w.grab().save("shot.png")
```

> 切勿用 `win32gui.EnumWindows` + 模糊标题匹配去找自己的窗口并发 `WM_CLOSE` ——
> 用户的「同花顺远航版」客户端标题也含「同花顺」，会被误伤（真实踩过，见 GA `ga_pitfalls_sop` 坑#5）。
