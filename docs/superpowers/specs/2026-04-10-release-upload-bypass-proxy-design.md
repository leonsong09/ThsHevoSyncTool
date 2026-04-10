# GitHub Release 无代理上传设计

## 目标

为仓库新增一个独立的 GitHub Release 上传脚本，在创建/更新 release 与上传 asset 时临时清空当前进程的代理环境变量，避免 `gh` / `curl` 走 `127.0.0.1:7890` 后出现大文件上传卡住的问题。

## 背景

- 本机环境变量与 `~/.gitconfig` 中都配置了 `http://127.0.0.1:7890`
- 代理进程为 `Clash Verge / mihomo`
- 实测小文件可经代理上传，但 67MB zip 经代理链路上传时会长时间挂起
- 在当前进程内清空代理环境变量后，完整 zip 可成功直传到 `uploads.github.com`

## 方案对比

### 方案 A（采用）：新增独立 release 上传脚本，内部自动绕过代理

- 优点：
  - 不修改现有本地构建脚本职责
  - 不改系统代理或 Clash 配置
  - 上传逻辑可复用，便于后续单独发版
- 缺点：
  - 需要维护一个额外脚本

### 方案 B：把上传逻辑塞进 `publish-single-file.ps1`

- 优点：一步完成
- 缺点：脚本职责混合，本地打包与远端发布耦合过高

### 方案 C：继续使用 `gh release upload`，只在外部 shell 手工清代理

- 优点：最少代码
- 缺点：依赖人工步骤，容易再次踩坑

## 设计

新增 `scripts/publish-release.ps1`：

1. 从 `Directory.Build.props` 读取版本号
2. 默认解析：
   - tag：`v<version>`
   - asset：`dist/ThsHevoSyncTool-v<version>-win-x64.zip`
   - repo：从 `git remote origin` 推导 `owner/repo`
3. 通过 `gh auth token` 读取本机 GitHub Token
4. 在“无代理环境”中执行 GitHub REST API：
   - 查询 tag 对应 release
   - 若不存在则创建 draft release
   - 若同名 asset 已存在则先删除
   - 直传 asset 到 `uploads.github.com`
   - 根据参数决定是否发布 draft
5. 上传结束后恢复当前进程代理环境变量

## 边界

- 只清当前脚本进程的代理环境变量
- 不修改系统代理、WinHTTP 代理或 Clash 规则
- 不自动清理历史 release 资产

## 验证

- `-DryRun` 验证参数解析与默认路径解析
- 用小探针文件验证脚本端到端上传
- 保留已有“无代理直传完整 zip 成功”的运行证据作为大文件链路依据
