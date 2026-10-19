#!/usr/bin/env python3
"""一键创建 .venv 并安装 GUI 依赖（PyQt6）。

用法：python setup_venv.py [--mirror https://pypi.tuna.tsinghua.edu.cn/simple] [--force]

说明：
- 纯 CLI（cli.py）**不需要**任何第三方依赖，本脚本只为 GUI（gui.py）准备环境。
- 默认优先用国内镜像，失败自动回退 PyPI。
"""

from __future__ import annotations

import argparse
import os
import subprocess
import sys

MIRRORS = [
    "https://pypi.tuna.tsinghua.edu.cn/simple",
    "https://mirrors.aliyun.com/pypi/simple",
    "",
]

ROOT = os.path.dirname(os.path.abspath(__file__))
VENV = os.path.join(ROOT, ".venv")


def venv_python(venv_dir: str) -> str:
    return os.path.join(venv_dir, "Scripts" if os.name == "nt" else "bin", "python.exe" if os.name == "nt" else "python")


def clean_env() -> dict:
    env = dict(os.environ)
    for k in ("HTTP_PROXY", "HTTPS_PROXY", "ALL_PROXY", "http_proxy", "https_proxy", "all_proxy"):
        env.pop(k, None)
    env["NO_PROXY"] = "*"
    env["PYTHONIOENCODING"] = "utf-8"
    return env


def main() -> int:
    ap = argparse.ArgumentParser(description="创建 .venv 并安装 PyQt6（GUI 依赖）")
    ap.add_argument("--mirror", default=None, help="指定 pip 镜像源；缺省按内置列表依次尝试")
    ap.add_argument("--force", action="store_true", help="即使 .venv 已存在也重建")
    args = ap.parse_args()

    if os.path.isdir(VENV) and args.force:
        import shutil

        shutil.rmtree(VENV)
        print("[1/3] 已删除旧 .venv")

    if not os.path.isdir(VENV):
        print("[1/3] 创建虚拟环境:", VENV)
        r = subprocess.run([sys.executable, "-m", "venv", VENV], env=clean_env())
        if r.returncode != 0:
            print("[FAIL] 创建虚拟环境失败")
            return 1
    else:
        print("[1/3] 复用已有 .venv:", VENV)

    py = venv_python(VENV)
    if not os.path.isfile(py):
        print("[FAIL] 找不到虚拟环境解释器:", py)
        return 1

    mirrors = [args.mirror] if args.mirror else MIRRORS
    ok = False
    for idx, mirror in enumerate(mirrors, 1):
        cmd = [py, "-m", "pip", "install", "--disable-pip-version-check", "-U", "pip"]
        if mirror:
            cmd += ["-i", mirror]
        print(f"[2/3] 升级 pip（源 {mirror or 'PyPI'}）")
        subprocess.run(cmd, env=clean_env())

        cmd = [py, "-m", "pip", "install", "--disable-pip-version-check", "-r",
               os.path.join(ROOT, "requirements.txt")]
        if mirror:
            cmd += ["-i", mirror]
        print(f"[2/3] 安装依赖（源 {mirror or 'PyPI'}）...")
        r = subprocess.run(cmd, env=clean_env())
        if r.returncode == 0:
            ok = True
            break
        print(f"      源 {mirror or 'PyPI'} 失败（rc={r.returncode}），尝试下一个...")

    if not ok:
        print("[FAIL] 依赖安装失败；可手动执行：")
        print(f'       "{py}" -m pip install -r requirements.txt -i https://pypi.tuna.tsinghua.edu.cn/simple')
        return 1

    print("[3/3] 校验 PyQt6 ...")
    r = subprocess.run([py, "-c", "from PyQt6.QtCore import QT_VERSION_STR; print('Qt', QT_VERSION_STR)"],
                       env=clean_env())
    if r.returncode != 0:
        print("[FAIL] PyQt6 导入失败")
        return 1

    print("\n完成。GUI 启动：")
    print(f'   "{py}" gui.py')
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
