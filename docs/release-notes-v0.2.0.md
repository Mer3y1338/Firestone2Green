# Firestone2Green v0.2.0 更新日志

## 重点修复

- 修复部分用户安装“持续修复”后，每次开机都会弹出管理员 PowerShell / Windows Terminal 窗口的问题。
- 持续监听任务改为隐藏启动，不再在开机后显示长期停留的命令行窗口。
- 安装新版持续修复前，会自动清理旧版 WatchFirestone 可见监听进程。
- “移除持续修复”现在会同时删除隐藏监听启动器，并尝试结束旧版残留监听进程。

## 启动兼容性优化

- 优化 Overwolf / Firestone 自动启动流程，增加多种启动参数兼容策略。
- 同时尝试 `OverwolfLauncher.exe` 与 `Overwolf.exe`，提升不同安装方式下的启动成功率。
- automation 端口增加兼容尝试，除自定义端口外也会尝试 Overwolf 官方默认端口。
- 当 automation 启动失败时，会回退为普通启动，避免用户感觉快捷方式“无响应”。

## 用户升级建议

如果之前已经安装过“持续修复”，升级到 v0.2.0 后建议执行一次：

1. 管理员运行新版 `Firestone2Green_v0.2.0.exe`。
2. 点击“移除持续修复”。
3. 再点击“安装持续修复”。
4. 重启电脑确认不再弹出 PowerShell 窗口。

## 发布文件

- `Firestone2Green_v0.2.0.exe`
- `Firestone2Green_v0.2.0_dist.zip`
