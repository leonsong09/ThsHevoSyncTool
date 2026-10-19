#!/usr/bin/env python3
"""图形界面入口（**可选外壳**，不属于 skill 主线）。

主线 = `ths_hevo_sync` 核心 + 仓库根 `cli.py`（零第三方依赖）。
本目录只是「给人手动点选」的外壳，需要 PyQt6。

用法：python legacy-shell/gui.py     （需已安装 PyQt6；见 legacy-shell/requirements.txt）
装环境：python legacy-shell/setup_venv.py
"""

from __future__ import annotations

import sys
from pathlib import Path

_HERE = Path(__file__).resolve().parent   # legacy-shell/
_ROOT = _HERE.parent                      # 仓库根（含 ths_hevo_sync/）
for _p in (str(_ROOT), str(_HERE)):
    if _p not in sys.path:
        sys.path.insert(0, _p)

from gui import main  # noqa: E402

if __name__ == "__main__":
    raise SystemExit(main())
