#!/usr/bin/env python3
"""命令行入口（薄壳）—— 实际实现在 `ths_hevo_sync/cli.py`。

用法：python cli.py <子命令> [选项]
"""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from ths_hevo_sync.cli import main  # noqa: E402

if __name__ == "__main__":
    raise SystemExit(main())
