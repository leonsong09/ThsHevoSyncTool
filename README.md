# ThsHevoSyncTool —— 同花顺配置备份与转移工具

> **2026-10-19 起为纯 Python 实现**（CLI + PyQt6 GUI）。原 C#/.NET 实现已退役，源码留档于 [`legacy-csharp/`](legacy-csharp/)。
> **备份包格式与 C# 版完全互通**：老的 `ThsHevoSyncTool-*.zip` 可直接用 Python 版导入，反之亦然。

## 你能用它做什么

- 选择同花顺安装根目录（含 `bin\users`）与账号目录（`mx_*`）
- 按分类勾选要迁移的数据（自定义指标、指标分组、页面布局、图表偏好、自选/预警等，共 29 项）
- **导出**为 ZIP 备份包（含 `manifest.json` + 每文件 SHA256）
- 在另一台机器 / 另一个账号 **导入**（导入前自动备份，可一键回滚）

## 快速开始

```powershell
# CLI（零依赖，纯标准库）
python cli.py list                                   # 看分类
python cli.py users  --root "D:\同花顺"               # 看账号
python cli.py scan   --root "D:\同花顺" --user mx_123 # 看体量
python cli.py export --root "D:\同花顺" --user mx_123 --out .\backups --preset r1
python cli.py import --zip .\backups\ThsHevoSyncTool-xxxx.zip --root "D:\同花顺" --user mx_123        # 预演
python cli.py import --zip .\backups\ThsHevoSyncTool-xxxx.zip --root "D:\同花顺" --user mx_123 --apply # 写入

# （可选）图形界面 —— 外壳在 legacy-shell/，不属于 skill 主线
python legacy-shell/setup_venv.py
.\.venv\Scripts\python legacy-shell/gui.py
```

**约定**：写操作（`import`）默认**预演**，必须显式 `--apply` 才落盘；退出码 `0/1/2`。
完整说明（分类清单、包格式、安全与回滚）见 [`SKILL.md`](SKILL.md)。

## 测试

```powershell
python -m pytest -q tests/test_core.py
```

## 目录

```
ths_hevo_sync/     核心库 + CLI（零第三方依赖）
cli.py             命令行入口（薄壳）
tests/             回归测试
legacy-shell/      可选 GUI 外壳（PyQt6；不随包安装，也不被 skill 依赖）
legacy-csharp/     已退役的 C#/.NET 实现（留档）
```
