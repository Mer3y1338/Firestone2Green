# Firestone2Green v0.1.5 更新日志

发布日期：2026-07-07

> 这是一次偏稳定性与兼容性的更新。重点修复部分用户“先安装 Overwolf（狼头），再从 Overwolf 内安装 Firestone”后，桌面 `Firestone2Green 启动 Firestone` 快捷方式点击无响应或无法自动授权的问题。

## 重点更新

### 1. 修复不同 Overwolf 安装来源导致的静默启动兼容问题

- 新增多启动参数回退机制，启动 Firestone 时会依次尝试：
  - 旧式启动：`-launchapp <AppId> -from-desktop + automation`
  - 新式启动：`--launchapp <AppId> --origin desktop + automation`
  - 混合启动参数
- 如果 automation 端口仍无法打开，会回退为普通方式启动 Firestone，避免用户感觉“点了没反应”。
- 报告中新增启动尝试记录，便于排查不同电脑上的启动差异：
  - `launchAttempts`
  - `selectedLaunchMode`

### 2. 持续修复与快捷方式体验优化

- `Firestone2Green 启动 Firestone` 快捷方式现在优先使用 Firestone 官方图标：
  - `%LOCALAPPDATA%\Overwolf\AppShortcutIcons\lnknbakkpommmjjdnelmfbjjdbocfpnpbkijjnob.ico`
- 如果官方图标不存在，会自动回退到 Overwolf 启动器图标。
- 升级后建议重新安装持续修复：
  1. 管理员运行 `Firestone2Green.exe`
  2. 点击 **移除持续修复**
  3. 点击 **安装持续修复**

### 3. 高分辨率与界面信息优化

- 增加高 DPI 适配，改善高分辨率屏幕下界面缩放显示。
- 程序右下角显示当前版本号。
- 首次启动增加提示：
  - `本项目仅用于交流学习，有能力者请多多支持正版`
- 授权成功后，GUI 状态与运行日志都会明确显示已成功授权。

### 4. 更新检查与日志解释优化

- 程序启动后会自动检查 GitHub Releases 是否有新版本。
- 如果已经是最新版本，会在右上角显示简单提示。
- 如果无法连接 GitHub，会提示“网络连接失败，更新检查失败”。
- 运行日志增加更多常见错误解释，尤其是：
  - automation 接口不可用
  - 旧版持续修复脚本仍在使用不兼容启动参数
  - Firestone 扩展目录缺失
  - hosts 权限不足
  - GitHub 更新检查失败

### 5. Firestone 数据加载稳定性改进

- 启动 Firestone 前会先切换到 `AuthOnlyOnline` 网络模式。
- 减少套牌、环境套牌、记牌器数据在启动断网窗口中初始化失败的概率。
- 保持授权相关端点隔离，同时尽量恢复 Firestone 正常数据网络。

## 修复的问题

- 修复部分用户点击 `Firestone2Green 启动 Firestone` 后无明显反应的问题。
- 修复部分 Overwolf 环境只接受旧式 `-launchapp -from-desktop` 参数的问题。
- 修复授权成功后 GUI/日志没有明确成功提示的问题。
- 修复高 DPI 环境下部分界面显示不理想的问题。
- 修复只安装 Overwolf、未安装 Firestone 时错误提示不清楚的问题。
- 修复快捷方式图标不是 Firestone 官方图标的问题。

## 升级说明

如果你之前安装过 **持续修复**，升级到本版本后请务必重新安装一次持续修复，否则旧的计划任务可能仍然指向旧脚本：

1. 管理员运行新版 `Firestone2Green.exe`
2. 点击 **移除持续修复**
3. 点击 **安装持续修复**
4. 以后使用桌面的 **Firestone2Green 启动 Firestone** 快捷方式启动 Firestone

## 给普通用户的建议

- 第一次使用：管理员运行 EXE，点击 **一键重启并授权**。
- 需要重启后自动维持：点击 **安装持续修复**。
- 日常启动：使用桌面 **Firestone2Green 启动 Firestone** 快捷方式。
- 如果快捷方式启动后没有立刻看到变化，请等待 Firestone 主界面加载完成后再等约 `30` 到 `60` 秒。

## 文件

推荐上传到 GitHub Releases：

```text
Firestone2Green.exe
```

可选同时上传：

```text
Firestone2Green_dist.zip
```
