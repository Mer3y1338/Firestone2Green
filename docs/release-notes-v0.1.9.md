# Firestone2Green v0.1.9 更新日志

## 端口优先级与性能调整

- 版本号回退到 `0.1.9`。
- automation 启动策略改为分层优先级，不再每次同时轮询多个端口。
- 第一优先级使用已验证稳定的 `18765`。
- 启动流程最先使用之前成功过的旧式参数：
  `-launchapp <AppId> -from-desktop --automation 18765 --enable-automation`
- 第二优先级尝试 `18765` 下的新版/混合启动参数。
- 没验证过的兼容方案放到最后，包括等号端口参数、官方两段式启动、默认兼容端口等。
- 这样正常用户会优先命中历史成功路径，只有失败时才进入慢速兼容兜底。

## 保留修复

- 保留开机持续监听隐藏启动修复，避免安装“持续修复”后开机弹出 PowerShell / Windows Terminal 窗口。
- “移除持续修复”仍会清理旧版 WatchFirestone 监听进程。

## 升级建议

如果用户之前安装过持续修复，升级/回退到本版本后建议：

1. 管理员运行 `Firestone2Green_v0.1.9.exe`。
2. 点击“移除持续修复”。
3. 再点击“安装持续修复”。
4. 之后使用桌面“Firestone2Green 启动 Firestone”快捷方式启动。

## 发布文件

- `Firestone2Green_v0.1.9.exe`
- `Firestone2Green_v0.1.9_dist.zip`
