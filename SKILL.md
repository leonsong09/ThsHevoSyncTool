---
name: ths-hevo-sync
description: 同花顺远航版本地配置备份/迁移工具（ThsHevoSyncTool，C# WPF GUI，.NET 8，本机 git repo）。把一台机器上的同花顺账号目录(bin\users\mx_*)配置导出为 ZIP 备份包（含 manifest.json + 每文件 SHA256），在另一台机器/另一个账号按分类导入：自定义指标与指标分组、指标脚本、页面布局/停靠布局/模板、图表偏好/表头列宽/面板尺寸、自选与预警公共配置等；导入前自动生成 pre_import_backup_*.zip（同样带 manifest，可直接重新导入=回滚）。触发词=同花顺备份/同花顺配置备份/同花顺配置迁移/同花顺换电脑/同花顺配置同步/同花顺配置转移/自定义指标备份/同花顺指标迁移/ths备份。
---

# ths-hevo-sync（同花顺远航版 本地配置备份/迁移工具）

**仓库**：`D:\Documents\Code\Stock auskill\ThsHevoSyncTool\`（独立 git repo，remote `git@github.com:leonsong09/ThsHevoSyncTool.git`）

> 家族成员之一，家族总入口见上级 `..\README.md`（同花顺/通达信 本地客户端工具族）。

## 1. 用途

在不同电脑 / 账号之间迁移**同花顺远航版**的本地配置。覆盖：自定义指标与指标分组、页面布局/停靠布局/模板配置、图表偏好/表头列宽/面板尺寸、自选/预警/公共配置。

> 使用前请先**关闭同花顺主程序**。

## 2. 入口

```powershell
# 已打包单文件 EXE（推荐，免装 .NET 运行时）
.\dist\self-contained-single\ThsHevoSyncTool.exe
.\dist\ThsHevoSyncTool-v<version>-win-x64\ThsHevoSyncTool.exe

# 源码运行（需 .NET SDK 8.x）
dotnet restore ; dotnet build -c Release ; dotnet run --project src/ThsHevoSyncTool.App
```

## 2.1 命令行（CLI，GA/脚本无头调用）

`src/ThsHevoSyncTool.Cli/`（console，`AssemblyName=ths-hevo-sync`），与 WPF GUI 共用 `ThsHevoSyncTool.Core`，不弹窗。

**首选：免依赖单文件 EXE（无需 .NET SDK / 运行时）** —— 已发布到 `dist\cli\ths-hevo-sync.exe`（自包含 win-x64，约 65MB；`dist/` 已 gitignore，不入库）：

```powershell
.\dist\cli\ths-hevo-sync.exe <子命令> [选项]      # 直接跑，零依赖
```

**备选：源码运行（需 .NET SDK 8）** —— 本机已有系统 dotnet 8.0.x；本仓还自带 `.tools\dotnet\dotnet.exe`：

```powershell
.\.tools\dotnet\dotnet.exe build src\ThsHevoSyncTool.Cli\ThsHevoSyncTool.Cli.csproj -c Release
.\.tools\dotnet\dotnet.exe src\ThsHevoSyncTool.Cli\bin\Release\net8.0\ths-hevo-sync.dll <子命令> [选项]

# 重新生成免依赖 EXE（需要时）
.\.tools\dotnet\dotnet.exe publish src\ThsHevoSyncTool.Cli\ThsHevoSyncTool.Cli.csproj -c Release `
    -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist\cli

list                                                # 列出全部备份分类（core=默认勾选）
presets                                             # 列出推荐预设 r1/r2/r3
users  --root <安装根目录>                           # 列出 bin\users\mx_* 账号目录
scan   --root <安装根目录> --user <mx_xxx>            # 扫描各分类文件数/大小
export --root <安装根目录> --user <mx_xxx> --out <目录>
       [--preset r1|r2|r3 | --categories a,b,c] [--zip-name X.zip] [--dry-run]
inspect --zip <备份包.zip>                            # 读 manifest（版本/源路径/分类/文件清单）
import --zip <备份包.zip> --root <安装根目录> --user <mx_xxx>
       [--categories a,b] [--backup-dir <目录>] [--apply]
```

- **约定**：`export` 只读源配置、写新 ZIP（安全，`--dry-run` 可只预演）；`import` **默认预演**，必须显式 `--apply` 才写入，写入前自动生成 `pre_import_backup_*.zip`。
- **回滚** = 用 `pre_import_backup_*.zip` 再跑一次 `import --apply`。
- 缺省分类 = 「仅核心」（与 GUI 的 `core` 预设一致）；`--categories` 的 id 用 `list` 查看。
- 退出码：`0` 成功 / `1` 业务失败 / `2` 用法错误。
- 已实测：list / presets / users / scan / export / inspect / import(预演 + `--apply` + 导入前备份) 全通。

## 3. 使用流程

**准备**：关同花顺 → 打开工具 → 选同花顺远航版**安装目录** → 选账号目录（形如 `mx_*`）→ 点「扫描/刷新」。

**导出**：勾选分类（`全选/全不选/仅核心`、`推荐：完整迁移/轻量常用/指标布局`、`勾选当前存在项`；可 `保存为配置1/2/3` 复用）→「开始导出」先弹**导出预览清单**（分类/文件数/大小/目标 zip），确认后才写包。

**导入**：选备份包 zip → 工具读 `manifest.json` 列出可导入分类/文件数/大小 → 勾选后导入。

## 4. 默认「核心」导出项

- 自定义指标：`bin\users\{USER}\function\CustomIndicator\*.json`
- 指标脚本：`bin\users\{USER}\function\candle\*.py`
- 自定义指标分组与排序：`bin\users\{USER}\function\MyLanguage\GroupOrder.xml`
- 指标组合：`bin\users\{USER}\index_setting\IndexCombination\IndexCombination.xml`
- 指标包参数/模板：`bin\users\{USER}\index_setting\IndexConfig\*.json`
- 多周期/多分屏指标选择：`bin\users\{USER}\index_setting\MutilPeriod*.ini`
- 页面图位指标勾选：`bin\users\{USER}\UserPageCoding.xml`
- 页面停靠布局：`bin\users\{USER}\CustomPage\CustomDockingTemplate\*.config`
- 页面模板配置：`bin\users\{USER}\CustomPage\CustomPageTemplate\*.xml`
- 页面模板插件引用：`bin\users\{USER}\CustomPage\pluginusages.json`
- 画线工具设置/默认属性：`bin\users\{USER}\DrawLine_New\{config.ini,NewLineDefaultProperty.json}`

更多可选：列表表头/列宽/字段方案、图表交互与样式偏好、面板宽度/侧边栏尺寸、全局配置 `bin\users\config`、公共笔记 `bin\users\public\notes.xml`。

## 5. 备份包格式与导入规则

- 包 = zip，含业务文件（路径均为**相对同花顺安装目录**）+ `manifest.json`
- 路径占位：`{USER}` = 账号目录名（如 `mx_123456`）；账号根 = `bin\users\{USER}`；全局配置 = `bin\users\config`
- `manifest.json` 记录：工具版本、创建时间、源安装路径/源账号、导出时选中分类、每文件相对路径/大小/mtime/SHA256
- 导入页据 manifest 自动识别分类并做 **SHA256 校验**
- **导入前自动备份**：覆盖前把将被覆盖的现有文件打成 `pre_import_backup_yyyyMMdd_HHmmss.zip`（同样带 manifest）→ 该包可直接重新导入作为**回滚**

## 6. 常见问题

1. 选了 zip 没有可导入项 → 非工具生成的标准包 / 缺 `manifest.json` / manifest 未记录所选分类
2. 提示目标机没有账号目录 → 目标机需先启动同花顺并登录一次，生成 `mx_*` 目录
3. 回滚 → 直接选 `pre_import_backup_*.zip` 按正常流程再导入
4. Release 上传慢（开了 Clash/代理）→ 用 `.\scripts\publish-release.ps1 -NotesFile .\release-notes.md -MakeLatest`（脚本在 PowerShell 进程内临时清空 `HTTP_PROXY/HTTPS_PROXY/ALL_PROXY` 直传，不改系统代理）

## 7. 构建 / 测试 / 发布

```powershell
dotnet test -c Release                       # 单元测试
.\scripts\publish-single-file.ps1            # 单文件发布（含冷启动 smoke test）
.\scripts\publish-release.ps1 -NotesFile .\release-notes.md -MakeLatest
```

版本唯一来源：根目录 `Directory.Build.props` 的 `<Version>`（同时决定程序版本 / 发布目录名 / zip 名 / Release tag）。当前发布口径：版本 `X.Y.Z`、tag `vX.Y.Z`、资产 `ThsHevoSyncTool-vX.Y.Z-win-x64.zip`。

## 8. 结构

- `src/ThsHevoSyncTool.App/`：WPF 图形界面
- `src/ThsHevoSyncTool.Core/`：导出/导入核心逻辑（BackupPackageImporter / BackupManifest / BackupImportPlanner …）
- `tests/`：App.Tests（ViewModel）+ Core.Tests（导入规划/清单/路径安全）

## 9. 边界

- 只读写同花顺本地配置目录；不联网上传
- 跨版本迁移前建议先用「仅核心」小范围验证；异常用导入前备份包回滚
