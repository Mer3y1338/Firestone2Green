# Firestone2Green v0.2.1 更新日志

## 启动与端口策略

- 版本更新到 `0.2.1`。
- automation 启动改为明确的优先级策略：
  1. 首先使用历史成功方案：`18765` + 旧式 `-launchapp -from-desktop` 参数。
  2. 如果失败，再尝试 `18765` 下的新版/混合启动参数。
  3. 最后才尝试未充分验证的兼容方案，例如等号端口参数、官方两段式启动、默认兼容端口等。
- 避免一开始就尝试多个端口导致启动变慢。
- 保留 `OverwolfLauncher.exe` 优先，`Overwolf.exe` 作为后置备用 launcher。

## 保留修复

- 保留“持续修复”隐藏监听启动，避免开机弹出 PowerShell / Windows Terminal 窗口。
- “移除持续修复”会继续清理旧版 WatchFirestone 监听进程。
- 保留路径选择防呆、自动搜索、头像内置修复、授权成功提示与网络恢复逻辑。

## 升级建议

如果之前安装过“持续修复”，升级到本版本后建议执行一次：

1. 管理员运行 `Firestone2Green_v0.2.1.exe`。
2. 点击“移除持续修复”。
3. 再点击“安装持续修复”。
4. 后续使用桌面“Firestone2Green 启动 Firestone”快捷方式启动。

## 发布文件

- `Firestone2Green_v0.2.1.exe`
- `Firestone2Green_v0.2.1_dist.zip`
