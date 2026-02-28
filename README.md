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

# 生成 Windows x64 单文件可执行程序（自包含）
dotnet publish .\\src\\ThsHevoSyncTool.App\\ThsHevoSyncTool.App.csproj `
  -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true
```

## 项目结构

- `src/ThsHevoSyncTool.App`：WPF 图形界面
- `src/ThsHevoSyncTool.Core`：导出/导入与文件处理核心逻辑
- `tests/`：单元测试

