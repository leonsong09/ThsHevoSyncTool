"""IO 小工具：SHA256、写权限探测、时间格式（与 C# 版逐字对齐）。"""

from __future__ import annotations

import hashlib
import os
import re
import uuid
from datetime import datetime, timezone

BUFFER_SIZE = 1024 * 128

_UTC_7 = "%Y-%m-%dT%H:%M:%S.%f"


def sha256_file(path: str) -> str:
    """文件 SHA256（小写 hex），对齐 C# `Sha256.HashFileAsync`。"""
    h = hashlib.sha256()
    with open(path, "rb") as fp:
        while True:
            chunk = fp.read(BUFFER_SIZE)
            if not chunk:
                break
            h.update(chunk)
    return h.hexdigest()


def copy_and_hash(src, dst) -> str:
    """边拷贝边算 SHA256（返回小写 hex），对齐 C# `Sha256.CopyAndHashAsync`。"""
    h = hashlib.sha256()
    while True:
        chunk = src.read(BUFFER_SIZE)
        if not chunk:
            break
        h.update(chunk)
        dst.write(chunk)
    return h.hexdigest()


def assert_directory_writable(directory_path: str) -> None:
    """对齐 C# `WriteAccessChecker.AssertDirectoryWritable`。"""
    full = os.path.abspath(directory_path)
    os.makedirs(full, exist_ok=True)
    test_file = os.path.join(full, f"write_test_{uuid.uuid4().hex}.tmp")
    try:
        with open(test_file, "w", encoding="utf-8") as fp:
            fp.write("ok")
    finally:
        if os.path.exists(test_file):
            os.remove(test_file)


# ---------------------------------------------------------------- 时间格式
# System.Text.Json 把 DateTime(Kind=Utc) 写成 7 位小数秒 + 'Z'，例如
#   2026-10-19T13:50:28.3760281Z
# 没有小数秒时写成 2026-10-19T13:50:28Z。下面两个函数保证双向兼容。


def format_utc(dt: datetime) -> str:
    """datetime(UTC) → C# 兼容 ISO-8601 字符串（7 位小数秒）。"""
    if dt.tzinfo is not None:
        dt = dt.astimezone(timezone.utc).replace(tzinfo=None)
    micro = dt.microsecond
    if micro == 0:
        return dt.strftime("%Y-%m-%dT%H:%M:%SZ")
    return dt.strftime(_UTC_7) + "0" + "Z"


def parse_utc(text: str) -> datetime:
    """C# 写出的 ISO-8601（可能 7 位小数秒）→ naive UTC datetime。"""
    if not text:
        raise ValueError("空时间字符串。")
    s = text.strip()
    if s.endswith("Z"):
        s = s[:-1]
    # 去掉超过 6 位的小数秒
    m = re.match(r"^(.*?)(?:\.(\d+))?$", s)
    head, frac = m.group(1), m.group(2)
    frac6 = (frac or "")[:6].ljust(6, "0")
    return datetime.strptime(f"{head}.{frac6}", _UTC_7)


def utc_now() -> datetime:
    return datetime.now(timezone.utc).replace(tzinfo=None)


def file_mtime_utc(path: str) -> datetime:
    return datetime.fromtimestamp(os.path.getmtime(path), tz=timezone.utc).replace(tzinfo=None)


def set_file_mtime_utc(path: str, dt: datetime) -> None:
    ts = dt.replace(tzinfo=timezone.utc).timestamp()
    os.utime(path, (ts, ts))


def zip_date_time(dt: datetime) -> tuple[int, int, int, int, int, int]:
    """ZIP(DOS) 时间元组；C# 版写入的是 UTC 墙钟时间，这里保持一致。"""
    year = max(1980, dt.year)
    return (year, dt.month, dt.day, dt.hour, dt.minute, dt.second)
