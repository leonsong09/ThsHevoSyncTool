# legacy-csharp —— 已退役的 C#/.NET 实现

> **状态：DEPRECATED（2026-10-19）**。本目录仅作**历史留档与格式对照**，不再维护、不再构建发布。
> 现役实现 = 仓库根目录的纯 Python 版（`ths_hevo_sync/` + `cli.py` + `gui.py`），见 [`../SKILL.md`](../SKILL.md)。

## 为什么退役

- 原 C# 版需要 .NET SDK 才能从源码运行、需要发布自包含 EXE 才能免依赖；
- 2026-10-19 用户令改为**纯 Python 全量重写**（核心 + CLI + PyQt6 GUI），与 GA/skill 生态一致，可直接以代码运行。

## 目录

| 路径 | 内容 |
|---|---|
| `src/ThsHevoSyncTool.Core/` | 导出/导入核心逻辑（分类目录、PathSpec 展开、ZIP+manifest 读写、导入规划） |
| `src/ThsHevoSyncTool.App/` | WPF 图形界面 |
| `src/ThsHevoSyncTool.Cli/` | 命令行入口（`AssemblyName=ths-hevo-sync`） |
| `tests/` | App.Tests + Core.Tests（xUnit） |
| `scripts/` | 发布脚本（publish-release.ps1 等） |
| `Directory.Build.props` / `NuGet.Config` / `.editorconfig` | 构建配置（版本号曾为 0.1.7） |

## 与 Python 版的关系

**备份包格式完全互通**（`ZIP + manifest.json`，`schemaVersion=1`）：

- C# 版产出的 `ThsHevoSyncTool-*.zip` → Python 版可直接 `import`；
- Python 版产出的包 → C# 版同样可读（已实测双向通过）。

因此本目录是 Python 版实现的**口径真源（reference）**：若两者行为出现分歧，以 C# 版 + `SKILL.md` 记录的格式约定为准。

## 如需重新构建（不推荐）

需要 .NET SDK 8（本机系统 dotnet 8.0.x 即可；原先仓库内的 `.tools\dotnet` 便携 SDK 已随 C# 退役一并删除）：

```powershell
dotnet build src\ThsHevoSyncTool.Core\ThsHevoSyncTool.Core.csproj -c Release
```
