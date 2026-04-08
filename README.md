# ThsHevoSyncTool（同花顺远航版 本地配置同步工具）

用于在不同电脑/系统之间**导出/导入**同花顺远航版的本地配置，导出为 `zip` 包，另一端可选择 `zip` 进行导入。

## 下载与运行

在 GitHub Releases 下载 `ThsHevoSyncTool.exe`（Windows x64 单文件版），双击运行即可。

> 导出/导入前请先关闭同花顺进程。

## 使用说明

1. 打开工具后，先选择同花顺远航版**安装路径**。
2. 选择账号目录（形如 `mx_*`），点击“扫描/刷新”。
3. **导出**：
   - 在“导出”页勾选需要的配置项；
   - 选择导出 `zip` 路径；
   - 点击“开始导出”生成备份包。
4. **导入**：
   - 在“导入”页选择要导入的 `zip`；
   - 按提示执行导入。

## 本地构建

需要安装 .NET SDK（建议 8.x）。

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release

# 生成并校验 Windows x64 单文件可执行程序（自包含）
.\scripts\publish-single-file.ps1

# 需要自定义输出目录时
.\scripts\publish-single-file.ps1 -OutputDir 'dist/release'
```

脚本默认输出到 `dist/self-contained-single/`，上传 Release 时使用其中的
`ThsHevoSyncTool.exe`。

如需手动执行底层发布命令，必须保留 `IncludeNativeLibrariesForSelfExtract=true`，否则 WPF
单文件发布可能退化为“只上传 exe 但运行时缺少 native 依赖”的伪单文件：

```powershell
dotnet publish .\\src\\ThsHevoSyncTool.App\\ThsHevoSyncTool.App.csproj `
  -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true
```

## 项目结构

- `src/ThsHevoSyncTool.App/`：WPF 图形界面（选择安装路径、账号目录、勾选导出/导入选项）
- `src/ThsHevoSyncTool.Core/`：导出/导入核心逻辑（选项清单、路径规则、打包与导入）
- `tests/ThsHevoSyncTool.Core.Tests/`：核心逻辑单测
- `dist/`：本地发布输出（不提交到 git，Release 中提供 `ThsHevoSyncTool.exe`）

## 备份包的文件结构

备份包（zip）里保存的是**相对于同花顺安装目录**的文件路径；其中和账号相关的路径使用占位符：

- `{USER}`：账号目录名（形如 `mx_*`，由工具界面下拉框选择）
- 账号相关根目录：`bin\users\{USER}`
- 全局配置目录：`bin\users\config`（跨账号）

## 选项与选择（导出/导入）

工具把可导出/导入的内容拆成若干“选项（Category）”，每个选项对应一组路径规则（文件或目录通配）。

### 导出页默认选择（“仅核心”）

导出页默认勾选“核心”选项（等价于点击“仅核心”），用于在不同机器间迁移最常用且影响最大的配置：

- 自定义指标：`bin\users\{USER}\function\CustomIndicator\*.json`、`bin\users\{USER}\function\candle\*.py`
- 自定义指标分组与排序：`bin\users\{USER}\function\MyLanguageGroupOrder.xml`
- 指标组合（主图/副图预设）：`bin\users\{USER}\index_setting\IndexCombination\IndexCombination.xml`
- 指标包参数/模板：`bin\users\{USER}\index_setting\IndexConfig\*.json`（递归）
- 多周期/多分屏指标选择：`bin\users\{USER}\index_setting\MutilPeriod*.ini`、`bin\users\{USER}\index_setting\index_show.ini`
- 页面图位指标勾选（页面级）：`bin\users\{USER}\UserPageCoding.xml`
- 页面停靠布局（窗口/面板布局）：`bin\users\{USER}\CustomPage\CustomDockingTemplate\*.config`
- 页面模板配置：`bin\users\{USER}\CustomPage\CustomPageTemplate\*.xml`、`bin\users\{USER}\CustomPage\pluginusages.json`
- 画线工具设置（非画线数据）：`bin\users\{USER}\DrawLine_New\config.ini`、`bin\users\{USER}\DrawLine_New\NewLineDefaultProperty.json`

### 其它可选项（“全选/全不选”）

除“核心”外，还包含更多可选项（例如表头列宽、插件设置、自选/预警、全局配置、公共笔记等）。

你可以在界面中：

- 点击“全选 / 全不选 / 仅核心”快速切换勾选
- 在表格中多选行后，用“勾选选中项 / 取消选中项”批量调整
- 右侧“选项详情”查看该选项对应的路径规则

### 导入页的可选/默认勾选规则

导入页在选择 zip 后，会读取备份包的 `manifest`：

- 导出时选中的选项会在导入页保持可勾选，并默认同步为勾选
- 若某个选项在包内有实际文件，会显示对应的文件数和大小
- 未包含在该备份包导出选择中的选项会置灰
