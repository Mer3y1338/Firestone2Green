# Firestone2Green v1.1.10 更新日志

发布日期：2026-07-08

> 本版本修复部分用户“原版 Firestone 可以正常启动，但 Firestone2Green 无法打开 Overwolf automation 接口”的问题。

## 重点更新

### 1. 修复 Overwolf automation 启动兼容性

部分用户环境中，Firestone 原版快捷方式可以正常打开，但 Overwolf 不接受自定义 automation 端口 `18765`，导致工具无法完成授权注入。

常见报错：

```text
automation 接口不可用：已尝试旧式/新版/混合启动参数
```

本版本增强：

- 同时尝试自定义端口 `18765` 和 Overwolf 官方默认 automation 端口 `54284`。
- 同时尝试 `OverwolfLauncher.exe` 和 `Overwolf.exe` 两个启动入口。
- 新增官方两段式启动流程：
  1. 先使用 `--enable-automation` 启动 Overwolf 客户端。
  2. 再使用 `-launchapp <AppId> -from-desktop` 启动 Firestone。
- 保留旧式、新式、混合参数和等号端口参数回退。
- 报告中记录：
  - `selectedLauncher`
  - `effectiveAutomationPort`
  - 每次 `launchAttempts` 使用的启动器、参数和端口。

### 2. 持续修复兼容默认 automation 端口

- 后台持续修复不再只检测 `18765`。
- 如果用户环境使用 Overwolf 官方默认端口 `54284`，也可以识别并执行授权刷新。

### 3. 错误提示更准确

- automation 失败提示中会说明已经尝试：
  - 官方两段式启动
  - 旧式 / 新式 / 混合启动参数
  - `OverwolfLauncher.exe` / `Overwolf.exe`
  - `18765` / `54284` 端口

## 修复的问题

- 修复原版 Firestone 可正常启动，但 Firestone2Green 无法开启 automation 接口的问题。
- 修复部分 `C:\Program Files (x86)\Overwolf` 安装环境中 automation 参数被忽略的问题。
- 修复部分环境只接受 Overwolf 官方默认端口 `54284` 的问题。
- 修复持续修复只识别单一 automation 端口的问题。
- 优化 automation 失败时的日志解释。

## 升级说明

如果之前安装过 **持续修复**，升级后建议重新安装一次：

1. 管理员运行新版 `Firestone2Green_v1.1.10.exe`。
2. 点击 **移除持续修复**。
3. 点击 **一键重启并授权**。
4. 确认授权正常后，点击 **安装持续修复**。

## 推荐上传文件

```text
Firestone2Green_v1.1.10.exe
```

可选同时上传：

```text
Firestone2Green_v1.1.10_dist.zip
```
