---
name: ths-hevo-sync
description: 同花顺远航版本地配置备份/迁移工具（纯 Python：CLI + PyQt6 GUI，本机 git repo）。把一台机器上的同花顺账号目录(bin\users\mx_*)配置导出为 ZIP 备份包（含 manifest.json + 每文件 SHA256），在另一台机器/另一个账号按分类导入：自定义指标与指标分组、指标脚本、页面布局/停靠布局/模板、图表偏好/表头列宽/面板尺寸、自选与预警公共配置等；导入前自动生成 pre_import_backup_*.zip（同样带 manifest，可直接重新导入=回滚）。备份包格式与历史 C# 版完全互通。触发词=同花顺备份/同花顺配置备份/同花顺配置迁移/同花顺换电脑/同花顺配置同步/同花顺配置转移/自定义指标备份/同花顺指标迁移/ths备份。
---

# ths-hevo-sync（同花顺配置备份/迁移）

> **2026-10-19 起为纯 Python 实现**（原 C#/.NET 版已退役，源码留档于 `legacy-csharp/`）。
> 备份包格式（ZIP + `manifest.json`，schemaVersion=1）**与 C# 版双向互通**，老备份包可直接导入。

## 1. 形态

| 入口 | 说明 |
|---|---|
| **CLI** | `python cli.py <子命令>`（无第三方依赖，纯标准库） |
| **GUI** | `python gui.py`（需 PyQt6；见 `requirements.txt`） |
| 核心库 | `ths_hevo_sync/`（`categories / planner / package / importer / manifest / …`） |

## 2. 安装与运行

```powershell
# 仅 CLI：无需安装任何依赖
python cli.py list

# GUI：装 PyQt6（约 100MB，建议用国内镜像）
python -m venv .venv
.\.venv\Scripts\python -m pip install -i https://pypi.tuna.tsinghua.edu.cn/simple PyQt6
.\.venv\Scripts\python gui.py

# 或者装成包后用入口名
python -m pip install -e .
ths-hevo-sync list          # CLI
ths-hevo-sync-gui           # GUI
```

## 3. CLI 子命令

```powershell
python cli.py list                                    # 列出全部备份分类（id/核心/路径规则）
python cli.py presets                                 # 推荐预设 r1/r2/r3
python cli.py users   --root <安装根目录>              # 列出 mx_* 账号目录
python cli.py scan    --root <根> --user mx_111        # 扫描各分类文件数/大小
python cli.py export  --root <根> --user mx_111 --out <目录> [--preset r1|--level safe|--categories a,b] [--dry-run]
python cli.py inspect --zip <备份包.zip>               # 查看包内容（manifest）
python cli.py import  --zip <包> --root <根> --user mx_222 [--categories a,b] [--apply]
```

- **写操作（`import`）默认预演**，必须显式 `--apply` 才落盘；`export` 只读源配置、写新包（安全，`--dry-run` 可只预演）。
- 退出码：`0` 成功 / `1` 业务失败 / `2` 用法错误。
- 分类缺省 = **仅核心**（与 GUI 的 core 预设一致）；`--level safe` = 核心+全局；`--level all` = 全部 29 项。

## 4. 备份分类（29 项）

- **核心 9 项**（默认勾选）：`custom_indicators`（自定义指标 + `function\candle` 脚本）、`custom_indicator_group_order`、`index_combinations`、`index_pack_configs`、`multi_period_layout`、`page_chart_indicator_selection`、`docking_layout`、`page_templates`、`drawline_settings`。
- **可选 18 项**：`grid_headers`、`chart_preferences`、`panel_sizes`、`personal_center`、`plugin_settings`、`notifications`、`trend_playback`、`overlay_switches`、`favorites_and_attention`、`watch_and_warn`、`quote_bar`、`bsdata`、`misc_user_root_files`、`user_history_and_cache`、`position_block`、`tape_config`、`limitup_analyse`、`discovery`。
- **全局 2 项**：`global_user_config`（`bin\users\config`）、`public_notes`。

推荐预设：`r1` 完整迁移（25 项）、`r2` 轻量常用（14 项）、`r3` 指标布局（12 项）。

## 5. 备份包格式（与 C# 版互通）

ZIP 根目录含 `manifest.json`（`schemaVersion=1`），条目名用 `/`，其余字段用 `\`：

```json
{
  "schemaVersion": "1",
  "tool": {"name": "ThsHevoSyncTool", "version": "0.2.0", "runtime": "Python 3.12.12"},
  "createdAtUtc": "2026-10-19T13:50:28.3760280Z",
  "source": {"installRoot": "D:\\...", "userDirName": "mx_111", "appExeVersion": null},
  "selection": {"defaultPreset": "r1", "categories": ["custom_indicators", "..."]},
  "files": [{"relativePath": "bin\\users\\mx_111\\...", "sizeBytes": 7,
             "sha256": "...", "lastWriteTimeUtc": "2026-10-19T13:50:28.2231280Z",
             "categoryId": "custom_indicators"}]
}
```

- 时间字段 = 7 位小数秒 + `Z`（System.Text.Json 口径）；行尾 CRLF、`+` 转义为 `\u002B`，与 C# 产物逐字兼容。
- 导入时**逐文件校验 SHA256**，不一致即中止；账号目录名自动重映射（`mx_111` → 目标 `mx_222`）。

## 6. 安全与回滚

- 导入前自动生成 `pre_import_backup_<时间戳>.zip`（带 manifest，**可再次导入=回滚**）。
- 相对路径安全化：拒绝绝对路径 / `..` 跳转 / 越界写入。
- 只读写同花顺本地配置目录；**不联网上传**。
- 建议先 `scan` 看体量、`--dry-run` 预演，再 `--apply`。

## 7. 测试

```powershell
python -m pytest -q tests/test_core.py     # 14 项：格式互通/预设项数/端到端往返/SHA256 校验
```

## 8. 结构

- `ths_hevo_sync/`：核心库 + `cli.py` + `gui/`（PyQt6）
- `cli.py` / `gui.py`：薄壳入口
- `tests/test_core.py`：回归测试
- `legacy-csharp/`：**已退役**的 C#/.NET 实现（留档，仅作格式对照）
