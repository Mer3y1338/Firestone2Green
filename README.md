# Firestone2Green

<div align="center">

![Platform](https://img.shields.io/badge/platform-Windows-blue?style=flat-square)
![UI](https://img.shields.io/badge/UI-WinForms-7c3aed?style=flat-square)
![Build](https://img.shields.io/badge/build-PowerShell%20%2B%20.NET-16a34a?style=flat-square)
[![Release](https://img.shields.io/github/v/release/Mer3y1338/Firestone2Green?style=flat-square)](https://github.com/Mer3y1338/Firestone2Green/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/Mer3y1338/Firestone2Green/total?style=flat-square)](https://github.com/Mer3y1338/Firestone2Green/releases)
[![License: MIT](https://img.shields.io/badge/license-MIT-lightgrey?style=flat-square)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen?style=flat-square)](https://github.com/Mer3y1338/Firestone2Green/pulls)

**Firestone2Green** 是一个面向 **Firestone / Overwolf / Hearthstone Tracker** 的 Windows 一键本地恢复工具。

一键完成 **本地授权修复、左下角登录头像修复、全功能网络恢复、静默持续修复任务安装**，适合需要快速恢复 Firestone 本地运行环境的普通用户。

[⬇️ 下载最新版 Firestone2Green_v???.exe](https://github.com/Mer3y1338/Firestone2Green/releases/latest) · [English README](README.en.md) · [使用教程](docs/%E4%BD%BF%E7%94%A8%E6%95%99%E7%A8%8B.md)

</div>

### 📺 [视频教程](https://www.bilibili.com/video/BV1uDTy6aEY6 "BiliBili")

## 功能特性

- **一键处理**：提供 WinForms 图形界面，常用功能可直接点击执行。
- **单文件分发**：EXE 内置 `scripts/Firestone2Green.ps1` 与默认头像资源。
- **路径自适应**：支持自动搜索或手动选择 Overwolf 根目录；正确目录必须直接包含 `OverwolfLauncher.exe` 或 `Overwolf.exe`，不再依赖固定 `D:\overwolf` 安装路径。
- **本地授权**：完成授权状态修复后，恢复 Firestone / Overwolf 正常网络能力。
- **头像修复**：仅替换 Firestone 左下角登录 / 账户头像。
- **避免误伤**：不会替换酒馆战旗英雄头像、环境套牌图标、套牌图标或卡牌图片。
- **静默持续修复**：支持安装计划任务，在系统重启或 Firestone 更新后自动补授权；安装本身不会主动启动 Firestone。
- **快捷方式启动**：可创建桌面快捷方式 `Firestone2Green 启动 Firestone`，用于日常无弹窗启动。
- **自动更新检查**：启动后自动对比 GitHub Releases，右上角提示是否已是最新版；网络不可用时会提示更新检查失败。
- **日志错误解释**：运行失败时保留原始异常、类型、代码行和命令，并为常见错误自动追加简单原因和处理建议。
- **首次启动免责声明**：首次打开会提醒“本项目仅用于交流学习，有能力者请多多支持正版”。
- **高 DPI 适配与版本显示**：适配高分辨率屏幕，并在右下角显示当前版本号。
- **数据稳定**：启动前恢复 AuthOnlyOnline 网络，避免套牌 / 环境数据在启动断网窗口里加载失败。

## 快速开始

1. 从 GitHub Releases 下载 `Firestone2Green_v???.exe`。
2. 右键 `Firestone2Green_v???.exe`，选择 **以管理员身份运行**。
3. 在 **路径设置** 区域确认路径；如果没有自动识别，点击 **自动搜索** 或 **选择路径**，选择 Overwolf 根目录（打开后能直接看到 `OverwolfLauncher.exe` 或 `Overwolf.exe` 的那一层目录）。选到子目录或上级目录时程序会弹窗提示正确位置。若用户只安装了 Overwolf（狼头）但还没安装 Firestone，请先在 Overwolf 中安装并正常打开一次 Firestone。
4. 点击 **一键重启并授权**。
5. 等待日志显示退出码 `0`。
6. 点击 **验证状态**，确认网络模式与授权状态正常。
7. 如需重启后自动维持授权，点击 **安装持续修复**。

> [!TIP]
> 安装持续修复后，建议以后都通过桌面的 **Firestone2Green 启动 Firestone** 快捷方式启动 Firestone。
> 快捷方式启动后不会弹出完成提示，请等待 Firestone 主界面加载完成并额外等待约 `30` 到 `60` 秒，后台会自动完成授权、头像修复和网络恢复。

## 持续修复

Firestone 的授权状态属于运行时状态。Windows 重启、Firestone 完全退出或 Overwolf 更新后，运行时状态可能回到默认值。

Firestone2Green 的持久化方式是 **后台自动补授权**，不会修改 Firestone 的签名文件。

点击 **安装持续修复** 后，程序只会创建持续修复项、启动后台监听、创建桌面快捷方式并应用网络规则，不会主动启动或重启 Firestone。需要立即完成本次授权时，请点击 **一键重启并授权**；日常使用建议通过桌面 **Firestone2Green 启动 Firestone** 快捷方式启动：

| 名称 | 类型 | 作用 |
| --- | --- | --- |
| `Firestone2Green` | 后台事件监听任务 | 登录后常驻监听 Firestone 手动启动事件；只有检测到 `-launchapp` 启动 Firestone 时才会静默补授权，不会主动拉起 Firestone。 |
| `Firestone2Green Launch` | 后台按需任务 | 由桌面快捷方式触发，静默启动 Firestone 并完成授权、头像修复和网络恢复。 |
| `Firestone2Green 启动 Firestone.lnk` | 桌面快捷方式 | 普通用户日常启动 Firestone 的入口。 |
| `%LOCALAPPDATA%\Firestone2Green\LaunchFirestone2Green.vbs` | 隐藏启动脚本 | 负责无窗口触发后台按需任务。 |

持续修复模式的行为：

- 不弹 PowerShell 窗口。
- 不弹授权完成提示。
- 不弹额外确认窗口。
- 重启电脑后仍可通过启动事件监听自动维持本地授权。
- 如果用原始 Firestone 图标手动启动且未开启 automation，后台事件监听检测到后会自动静默重启并补授权；不会在你未启动 Firestone 时主动拉起。

## 快捷方式自动授权流程

桌面 **Firestone2Green 启动 Firestone** 快捷方式不是直接打开 Firestone，而是无窗口触发后台任务 `Firestone2Green Launch`。

该任务会按顺序执行：

1. 校验 Firestone 本地文件完整性。
2. 清理异常缓存和旧进程。
3. 在 Firestone 启动前切换到 `AuthOnlyOnline` 网络模式，保证套牌、环境数据和记牌器数据可以正常联网加载。
4. 使用兼容启动回退打开 Firestone：优先尝试旧式 `-launchapp -from-desktop + automation`，再尝试新版 `--launchapp --origin desktop + automation`，以适配不同来源安装的 Overwolf。
5. 等待 Firestone 后台窗口和主窗口授权服务初始化。
6. 同时补齐后台窗口与主窗口的本地授权状态。
7. 修复左下角登录头像。
8. 保持全功能网络恢复状态。

> [!IMPORTANT]
> 快捷方式启动是静默流程，不会弹出“完成授权”窗口。正常情况下从点击快捷方式到授权完全生效大约需要 `30` 到 `60` 秒，取决于 Firestone / Overwolf 启动速度。

自动授权成功后，主窗口中的会员锁、我的套牌、环境套牌、环境套牌类型、统计数据、等级和构筑套牌等依赖授权的入口应恢复可用。

### 为什么“先装 Overwolf，再从里面安装 Firestone”的用户点击快捷方式没反应？

不同来源安装的 Overwolf 对启动参数的兼容性不完全一致：部分环境只稳定接受旧式 `-launchapp <AppId> -from-desktop`，部分环境接受新版 `--launchapp <AppId> --origin desktop`。旧版本只使用单一新版参数，所以某些用户会看到桌面快捷方式静默触发后没有明显反应。

新版已加入多启动方式回退：旧式 + automation、新版 + automation、混合参数都会依次尝试；如果 automation 仍不可用，也会尝试普通方式打开 Firestone，并在日志里提示原因。升级后请重新安装持续修复，让快捷方式指向新版脚本。

如果点击快捷方式后没有自动授权：

1. 等待至少 `60` 秒。
2. 确认是通过桌面的 **Firestone2Green 启动 Firestone** 快捷方式启动，而不是原始 Firestone 图标。
3. 如果刚升级过 Firestone2Green，管理员运行 `Firestone2Green_v???.exe`，先点 **移除持续修复**，再点 **安装持续修复**，确保桌面快捷方式和后台任务使用新版脚本。
4. 点击 **一键重启并授权** 立即重启并补授权。
5. 点击 **验证状态**，确认报告中显示 `NetworkMode = AuthOnlyOnline`。

## 发布文件

推荐给普通用户分发 GitHub Releases 中的单文件 EXE：

```text
Firestone2Green_v???.exe
```

也可以分发完整压缩包：

```text
Firestone2Green_v???_dist.zip
```

`Firestone2Green_v???.exe` 已内置：

- `scripts/Firestone2Green.ps1`
- `assets/avatar.jpg`
- `assets/app.ico`

因此在常规使用场景下，用户不需要额外携带 `.ps1` 文件或头像文件。

单独分发 `Firestone2Green_v???.exe` 时，程序首次运行会自动释放内置脚本和默认头像资源；如果目录里没有 `assets/avatar.jpg`，会使用 EXE 内置头像并写入本地运行目录，保证其他用户也能完成左下角登录头像替换。

## 仓库结构

```text
Firestone2Green/
├─ assets/
│  ├─ app.ico
│  └─ avatar.jpg
├─ docs/
│  └─ 使用教程.md
├─ scripts/
│  └─ Firestone2Green.ps1
├─ src/
│  ├─ app.manifest
│  └─ Firestone2Green.cs
├─ .editorconfig
├─ .gitattributes
├─ .gitignore
├─ LICENSE
├─ README.en.md
└─ README.md
```

## 常见问题

### 可以只发送 EXE 给其他用户吗？

可以。`Firestone2Green_v???.exe` 已经内置脚本、头像和图标，普通用户只需要这个 EXE。

### 重启电脑后还需要手动授权吗？

安装 **持续修复** 后不需要每次手动打开 GUI。推荐重启后使用桌面的 **Firestone2Green 启动 Firestone** 快捷方式。如果使用原始图标手动启动，后台事件监听也会在检测到 `-launchapp` 后立即静默重启补授权；持续任务本身不会在你未启动时主动拉起 Firestone。

### 完成授权后会弹窗吗？

不会。持续修复与快捷方式启动都会隐藏 PowerShell 窗口，也不会弹出完成提示。

### 为什么不直接永久修改 Firestone 文件？

授权状态属于运行时状态。Firestone2Green 通过后台任务在启动后补齐本地授权状态，避免直接改动 Firestone 的签名文件。

### hosts 不存在、被保存成 hosts.txt，或提示被保护怎么办？

新版会读取 Windows 注册表中的实际 hosts 目录，不再只认固定路径；缺少无扩展名 `hosts` 时会自动创建，`hosts` 不存在、为零字节或只有空白内容时，会先恢复 Windows 默认 HOSTS 模板，再加入 Firestone2Green 阻断段。只有 `hosts.txt` 时会复制内容并保留原文件；如果 `hosts.txt` 也是空的，目标 `hosts` 同样会恢复默认模板。已有非空 hosts 内容不会被覆盖。写入前还会自动解除只读、重试短时占用、临时修复文件 ACL，并把原内容备份到 `%LOCALAPPDATA%\Firestone2Green\hosts-backups`（最多保留 10 份）。

如果日志仍显示 `HOSTS_PROTECTION_ACTIVE`、`HOSTS_FILE_BUSY`、`HOSTS_CREATE_FAILED` 或 `HOSTS_WRITE_VERIFY_FAILED`，按日志关闭安全软件的 **Hosts 保护 / 系统文件防护**，或允许 `Firestone2Green.exe` 和 `powershell.exe` 修改 hosts，然后重新点击原按钮即可。不要下载所谓“hosts 修复文件”，也不要手工给 Everyone / Users 完全控制权限；无需联系作者。

### 日志出现退出码 1 时如何定位？

从 v0.2.3 开始，日志会直接显示 `F2G_ERROR`、`F2G_ERROR_TYPE`、`F2G_ERROR_LINE` 和 `F2G_ERROR_COMMAND`，对应真正的异常内容、类型、原始代码行和命令，不会再把最外层错误处理代码误报为故障位置。需要进一步排查时，点击 **打开报告**，查看最新的 `FirestoneOfflineReport_*.json`。

### 数据、套牌或联网功能无法使用怎么办？

在 GUI 中点击 **恢复全功能网络**，再点击 **验证状态**。报告中应显示：

```text
NetworkMode = AuthOnlyOnline
```

### 头像被替换到其他图片怎么办？

当前版本只允许命中左下角登录 / 账户头像，并会排除英雄、套牌、卡牌等内容图片。出现异常时可点击 **刷新授权+头像**。

### 更详细的教程在哪里？

见 [`docs/使用教程.md`](docs/%E4%BD%BF%E7%94%A8%E6%95%99%E7%A8%8B.md)。

## 许可证

本项目使用 [MIT License](LICENSE)。

## 友链
【感谢Linux.do社区及GitHub社区各位开发者对项目的支持与贡献】
