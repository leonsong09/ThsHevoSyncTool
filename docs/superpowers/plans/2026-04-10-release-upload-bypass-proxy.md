# Release Upload Bypass Proxy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新增一个独立的 GitHub Release 上传脚本，在上传资产前自动清空当前进程代理环境变量，避免本机代理导致的大文件上传卡住。

**Architecture:** 保持 `publish-single-file.ps1` 只负责本地构建，新增 `publish-release.ps1` 负责 release 元数据与 asset 上传。脚本通过 `gh auth token` 读取认证信息，再使用 GitHub REST API 直传资产，并在上传前后包裹代理环境变量清理/恢复。

**Tech Stack:** PowerShell 7, GitHub REST API, `gh auth token`

---

### Task 1: 新增 release 上传脚本

**Files:**
- Create: `scripts/publish-release.ps1`

- [ ] **Step 1: 实现参数与默认解析**
- [ ] **Step 2: 实现 repo / version / asset 路径解析**
- [ ] **Step 3: 实现无代理环境包装器**
- [ ] **Step 4: 实现 release 查询/创建/替换资产/上传逻辑**
- [ ] **Step 5: 输出 release URL、asset URL、耗时**

### Task 2: 更新文档

**Files:**
- Modify: `README.md`

- [ ] **Step 1: 补充 `publish-release.ps1` 用法**
- [ ] **Step 2: 说明脚本只临时清当前进程代理**

### Task 3: 验证

**Files:**
- Verify only

- [ ] **Step 1: 运行 `pwsh -NoProfile -File scripts/publish-release.ps1 -DryRun`**
- [ ] **Step 2: 用小探针文件执行一次真实上传**
- [ ] **Step 3: 删除探针资产并确认工作区干净**
