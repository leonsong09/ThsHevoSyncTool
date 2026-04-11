# ThsHevoSyncTool

同花顺远航版本地配置同步工具：把一台机器上的本地配置导出为 `zip` 备份包，再在另一台机器上按分类导入。

---

## 最新更新（v0.1.7）

- 修复导入前自动生成的 `pre_import_backup_*.zip` 无法再次导入的问题
- 修复 `CustomDockingTemplate` / `AStockTemplate.config` 等配置恢复时的 manifest 识别与 SHA256 校验问题
- 修复“没有旧文件可备份”时 pre-import 备份包仍保留已选分类，避免导入项丢失
- 新增对应核心回归测试，统一版本号、发布目录与 Release 资产命名

最新版本页：<https://github.com/leonsong09/ThsHevoSyncTool/releases/latest>  
全部版本 / 更新日志：<https://github.com/leonsong09/ThsHevoSyncTool/releases>

---

## 这是什么

这个工具用于在不同电脑 / 系统之间迁移同花顺远航版的本地配置，覆盖场景包括：

- 自定义指标与指标分组
- 页面布局、停靠布局、模板配置
- 图表偏好、表头列宽、面板尺寸
- 自选 / 预警 / 公共配置
- 导入前自动生成回滚包，支持再次导入恢复

> 使用前请先关闭同花顺主程序。

---

## 下载与运行

在 GitHub Releases 下载以下任一产物即可：

- `ThsHevoSyncTool.exe`：Windows x64 单文件版
- `ThsHevoSyncTool-v<version>-win-x64.zip`：正式发布压缩包

推荐直接下载 zip 正式包；其中包含发布口径下的标准文件命名，更适合归档和分发。

---

## 快速开始

### 1）准备

1. 关闭同花顺主程序
2. 打开工具，选择同花顺远航版安装目录
3. 选择账号目录（形如 `mx_*`）
4. 点击“扫描 / 刷新”

### 2）导出

导出页支持以下操作：

- `全选 / 全不选 / 仅核心`
- `推荐：完整迁移 / 推荐：轻量常用 / 推荐：指标布局`
- `勾选当前存在项`
- `保存为配置1 / 配置2 / 配置3`
- `应用配置1 / 配置2 / 配置3`

点击“开始导出”后，会先弹出导出预览清单（分类、文件数、大小、目标 zip 路径），确认后才真正写包。

### 3）导入

1. 在导入页选择备份包 `zip`
2. 工具会自动读取 `manifest.json`，显示可导入分类、文件数和大小
3. 勾选要导入的分类并开始导入

导入前，工具会自动在你指定目录旁生成：

- `pre_import_backup_yyyyMMdd_HHmmss.zip`

这个导入前备份包现在也带有 `manifest.json`，可以直接重新选择并再次导入，用来做回滚恢复。

---

## 默认“核心”导出项

导出页默认勾选的是“仅核心”，适合跨机器迁移最常用、最影响体验的配置：

- 自定义指标：`bin\users\{USER}\function\CustomIndicator\*.json`
- 指标脚本：`bin\users\{USER}\function\candle\*.py`
- 自定义指标分组与排序：`bin\users\{USER}\function\MyLanguageGroupOrder.xml`
- 指标组合：`bin\users\{USER}\index_setting\IndexCombination\IndexCombination.xml`
- 指标包参数 / 模板：`bin\users\{USER}\index_setting\IndexConfig\*.json`
- 多周期 / 多分屏指标选择：`bin\users\{USER}\index_setting\MutilPeriod*.ini`
- 页面图位指标勾选：`bin\users\{USER}\UserPageCoding.xml`
- 页面停靠布局：`bin\users\{USER}\CustomPage\CustomDockingTemplate\*.config`
- 页面模板配置：`bin\users\{USER}\CustomPage\CustomPageTemplate\*.xml`
- 页面模板插件引用：`bin\users\{USER}\CustomPage\pluginusages.json`
- 画线工具设置：`bin\users\{USER}\DrawLine_New\config.ini`
- 画线默认属性：`bin\users\{USER}\DrawLine_New\NewLineDefaultProperty.json`

除核心项外，还支持更多可选项，例如：

- 列表表头 / 列宽 / 字段方案
- 图表交互与样式偏好
- 面板宽度 / 侧边栏尺寸
- 全局配置（`bin\users\config`）
- 公共笔记（`bin\users\public\notes.xml`）

---

## 备份包格式与导入规则

### 包内结构

备份包本质上是一个 zip，至少包含：

- 业务文件
- `manifest.json`

其中业务文件都保存为**相对于同花顺安装目录**的路径。

### 路径占位规则

- `{USER}`：账号目录名，例如 `mx_123456`
- 账号相关目录根：`bin\users\{USER}`
- 全局配置目录：`bin\users\config`

### manifest 的作用

`manifest.json` 会记录：

- 备份工具版本
- 创建时间
- 源安装路径 / 源账号目录
- 导出时选中的分类
- 每个文件的相对路径、大小、最后修改时间、SHA256

导入页会根据 manifest：

- 自动识别这个包里包含哪些分类
- 显示每类的文件数 / 大小
- 保留“已选但当前无文件”的分类状态
- 在导入时做 SHA256 校验

### 导入前自动备份

当导入会覆盖目标文件时，工具会先把“旧文件”打成 `pre_import_backup_*.zip`：

- 只备份将被覆盖的现有文件
- 备份包同样生成 `manifest.json`
- 支持再次导入，作为回滚包使用

这意味着你现在可以把“导入前备份”当成标准恢复包重新导入，而不是只能手工解压。

---

## 常见问题

### 1）为什么选了 zip 之后没有可导入项？

常见原因：

- 这个 zip 不是工具生成的标准备份包
- 包内缺少 `manifest.json`
- manifest 中没有记录当前选中的分类

### 2）为什么提示目标机没有账号目录？

目标机器第一次导入前，需要先启动同花顺并登录一次，让 `mx_*` 账号目录生成出来。

### 3）如何回滚本次导入？

直接选择导入前生成的 `pre_import_backup_*.zip`，按正常导入流程再次导入即可。

### 4）如果我开了 Clash / 代理，发布 Release 上传很慢怎么办？

不要手工用 `gh release upload` 硬传，直接用仓库内的：

```powershell
.\scripts\publish-release.ps1 -NotesFile .\release-notes.md -MakeLatest
```

脚本会在当前 PowerShell 进程内临时清空 `HTTP_PROXY / HTTPS_PROXY / ALL_PROXY`，并通过 GitHub API 直传 asset，不会修改系统代理设置。

---

## 本地构建与测试

需要 .NET SDK 8.x。

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

---

## 单文件发布

生成并校验 Windows x64 单文件可执行程序：

```powershell
.\scripts\publish-single-file.ps1
```

需要自定义输出目录时：

```powershell
.\scripts\publish-single-file.ps1 -OutputDir 'dist/release'
```

脚本默认产出：

- `dist/self-contained-single/ThsHevoSyncTool.exe`
- `dist/ThsHevoSyncTool-v<version>-win-x64/ThsHevoSyncTool.exe`
- `dist/ThsHevoSyncTool-v<version>-win-x64.zip`

脚本会额外执行冷启动 smoke test，确保单文件包不是“只剩 exe、运行时缺 native 依赖”的伪成功。

---

## GitHub Release 发布

仓库当前版本来源统一取自根目录 `Directory.Build.props` 的 `<Version>`：

- 程序版本信息
- 发布目录名
- 发布 zip 文件名
- GitHub Release tag

当前发布口径统一为：

- 版本：`X.Y.Z`
- Tag：`vX.Y.Z`
- 正式资产：`ThsHevoSyncTool-vX.Y.Z-win-x64.zip`

推荐发布命令：

```powershell
.\scripts\publish-release.ps1 -NotesFile .\release-notes.md -MakeLatest
```

你也可以显式指定 tag 和资产：

```powershell
.\scripts\publish-release.ps1 `
  -Tag v0.1.7 `
  -AssetPath .\dist\ThsHevoSyncTool-v0.1.7-win-x64.zip `
  -MakeLatest
```

---

## 项目结构

- `src/ThsHevoSyncTool.App/`：WPF 图形界面
- `src/ThsHevoSyncTool.Core/`：导出 / 导入核心逻辑
- `tests/ThsHevoSyncTool.App.Tests/`：界面与 ViewModel 相关测试
- `tests/ThsHevoSyncTool.Core.Tests/`：核心逻辑测试
- `scripts/`：构建、单文件发布、GitHub Release 发布脚本
- `dist/`：本地发布输出目录（不提交）

---

## 许可证 / 说明

本仓库当前更偏向个人工具与实用发布形态；如需对外分发或二次封装，建议先确认你自己的使用场景、发布要求与目标环境。
