"""安装根目录 / 账号目录校验 —— 对齐 C# 版 Environment/*.cs。"""

from __future__ import annotations

import os
from dataclasses import dataclass


@dataclass(frozen=True)
class InstallRootInfo:
    install_root_path: str
    users_root_path: str

    @property
    def users_config_path(self) -> str:
        return os.path.join(self.users_root_path, "config")

    @property
    def users_public_path(self) -> str:
        return os.path.join(self.users_root_path, "public")

    @property
    def users_internal_path(self) -> str:
        return os.path.join(self.users_root_path, "internal")


def validate_install_root(install_root_path: str) -> InstallRootInfo:
    """校验「同花顺安装根目录」：必须存在且含 bin\\users。"""
    if not install_root_path or not install_root_path.strip():
        raise ValueError("安装路径为空。")

    full_root = os.path.abspath(install_root_path)
    if not os.path.isdir(full_root):
        raise FileNotFoundError(f"安装路径不存在：{full_root}")

    users_root = os.path.join(full_root, "bin", "users")
    if not os.path.isdir(users_root):
        raise FileNotFoundError(f"未找到 bin\\users：{users_root}")

    return InstallRootInfo(full_root, users_root)


def list_user_directories(root: InstallRootInfo) -> list[str]:
    """列出 bin\\users 下的 mx_* 账号目录名（忽略大小写排序）。"""
    if not os.path.isdir(root.users_root_path):
        return []
    names = [
        name
        for name in os.listdir(root.users_root_path)
        if os.path.isdir(os.path.join(root.users_root_path, name)) and name.lower().startswith("mx_")
    ]
    return sorted(names, key=str.lower)


def validate_user_dir_name(user_dir_name: str) -> None:
    """账号目录名（mx_*）不得含路径分隔符 / 盘符 / '..'。"""
    if not user_dir_name or not user_dir_name.strip():
        raise ValueError("账号目录为空（mx_*）。")
    if any(ch in user_dir_name for ch in ("\\", "/", ":")):
        raise ValueError(f"账号目录不合法：{user_dir_name}")
    if ".." in user_dir_name:
        raise ValueError(f"账号目录不合法：{user_dir_name}")
