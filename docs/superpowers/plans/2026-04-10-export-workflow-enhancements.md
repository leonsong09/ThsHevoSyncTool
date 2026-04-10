# 导出工作流增强 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为导出页增加中文推荐勾选、3 个自定义配置位、勾选当前存在项、导出前清单预览，并产出新的正式可用单文件包。

**Architecture:** 在现有 WPF 导出页上做增量增强。预设规则独立成可测试配置；“勾选当前存在项”直接复用已扫描的界面状态；导出预览在真正写 zip 前共享同一个 `BackupPlan` 做确认。

**Tech Stack:** WPF (.NET 8)、xUnit、PowerShell 发布脚本

---

### Task 1: 预设规则、自定义配置位与“当前存在项”逻辑

**Files:**
- Create: `src/ThsHevoSyncTool.Core/Backup/ExportSelectionPreset.cs`
- Create: `src/ThsHevoSyncTool.Core/Backup/ExportSelectionPresetCatalog.cs`
- Create: `src/ThsHevoSyncTool.App/Services/ExportSelectionUserPreset.cs`
- Create: `src/ThsHevoSyncTool.App/Services/IExportSelectionUserPresetStore.cs`
- Create: `src/ThsHevoSyncTool.App/Services/JsonExportSelectionUserPresetStore.cs`
- Create: `tests/ThsHevoSyncTool.Core.Tests/ExportSelectionPresetCatalogTests.cs`
- Create: `tests/ThsHevoSyncTool.App.Tests/JsonExportSelectionUserPresetStoreTests.cs`
- Modify: `src/ThsHevoSyncTool.App/ViewModels/MainViewModel.cs`
- Modify: `src/ThsHevoSyncTool.App/ViewModels/MainViewModel.Helpers.cs`
- Modify: `src/ThsHevoSyncTool.App/MainWindow.xaml`

### Task 2: 导出前清单预览

**Files:**
- Create: `src/ThsHevoSyncTool.App/Services/ExportPreviewDialogService.cs`（如需要）
- Modify: `src/ThsHevoSyncTool.App/Services/IDialogService.cs`
- Modify: `src/ThsHevoSyncTool.App/Services/DialogService.cs`
- Modify: `src/ThsHevoSyncTool.App/ViewModels/MainViewModel.Export.cs`
- Create: `tests/ThsHevoSyncTool.App.Tests/MainViewModelExportPreviewTests.cs`
- Modify: `src/ThsHevoSyncTool.App/MainWindow.xaml`

### Task 3: 正式发布产物与文档

**Files:**
- Modify: `scripts/publish-single-file.ps1`
- Modify: `README.md`
- Output: `dist/...`

### Task 4: 最终整合验证

**Files:**
- Verify: `tests/ThsHevoSyncTool.App.Tests/...`
- Verify: `tests/ThsHevoSyncTool.Core.Tests/...`
- Verify: GUI smoke + 单文件发布 smoke test
