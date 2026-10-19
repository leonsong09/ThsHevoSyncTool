#!/usr/bin/env python3
"""图形界面入口（薄壳）—— 实际实现在 `ths_hevo_sync/gui/`。

用法：python gui.py     （需已安装 PyQt6；见 requirements.txt）
"""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from ths_hevo_sync.gui import main  # noqa: E402

if __name__ == "__main__":
    raise SystemExit(main())
