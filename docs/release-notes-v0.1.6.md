# Firestone2Green v0.1.6 更新日志

发布日期：2026-07-07

> 这是一个针对 **持续修复安装失败** 的小版本修复。重点解决部分用户电脑上 Windows 任务计划程序服务未运行，导致点击“安装持续修复”时报英文错误的问题。

## 修复内容

### 1. 修复 Task Scheduler 服务未运行导致持续修复安装失败

部分用户安装持续修复时会出现：

```text
The Task Scheduler Service is not running.
```

原因是用户系统中的 **Windows 任务计划程序服务 / Task Scheduler / Schedule** 没有运行，Firestone2Green 无法创建后台计划任务和桌面静默启动任务。

新版处理方式：

- 安装持续修复前自动检查 `Schedule` 服务状态。
- 删除持续修复前也会检查该服务，避免移除任务时报错不清楚。
- 如果服务未运行，会自动尝试启动。
- 如果服务被优化工具禁用，会尝试恢复为自动启动后再启动。
- 如果仍然无法启动，会在日志中给出中文处理步骤。

### 2. GUI 日志增加中文错误解释

当日志中出现 Task Scheduler 相关错误时，GUI 会自动追加解释：

- 说明这是 Windows 任务计划程序服务未运行。
- 提示打开 `services.msc`。
- 找到 `Task Scheduler / 任务计划程序`。
- 将启动类型改成“自动”并启动服务。
- 然后重新点击 **安装持续修复**。

## 给用户的处理建议

如果升级前已经遇到该错误，可以让用户这样处理：

1. 下载并运行新版 `Firestone2Green.exe`。
2. 右键选择 **以管理员身份运行**。
3. 点击 **安装持续修复**。
4. 如果仍然失败：
   - 按 `Win + R`
   - 输入 `services.msc`
   - 找到 **Task Scheduler / 任务计划程序**
   - 启动类型改为 **自动**
   - 点击 **启动**
   - 回到 Firestone2Green 再点 **安装持续修复**

## 升级说明

如果已经安装过持续修复，升级后建议重新安装一次：

1. 管理员运行新版 `Firestone2Green.exe`
2. 点击 **移除持续修复**
3. 点击 **安装持续修复**

## 推荐上传文件

```text
Firestone2Green.exe
```

可选同时上传：

```text
Firestone2Green_dist.zip
```
