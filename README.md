# Firestone2Green

![Platform](https://img.shields.io/badge/platform-Windows-blue)
![UI](https://img.shields.io/badge/UI-WinForms-7c3aed)
![Build](https://img.shields.io/badge/build-PowerShell%20%2B%20.NET-16a34a)
![License](https://img.shields.io/badge/license-MIT-lightgrey)

**Firestone2Green** 是一个面向 Firestone / Overwolf 本地运行环境的 Windows 一键恢复工具，用于完成本地授权、登录头像修复与全功能网络恢复。

- **仓库名**：`Firestone2Green`
- **程序名**：`Firestone2Green`
- **窗口标题**：`Firestone2Green By Mer3y`
- **程序副标题**：`本地授权 By Mer3y`

> [!NOTE]
> Release 附件中的 `Firestone2Green.exe` 是单文件版本，已经内置 PowerShell 核心脚本、默认头像资源和程序图标。普通用户只需要运行这个 EXE。

## 目录

- [功能特性](#功能特性)
- [快速开始](#快速开始)
- [持续修复](#持续修复)
- [发布文件](#发布文件)
- [仓库结构](#仓库结构)
- [开源说明](#开源说明)
- [常见问题](#常见问题)
- [许可证](#许可证)

## 功能特性

- **一键处理**：提供 WinForms 图形界面，常用功能可直接点击执行。
- **单文件分发**：EXE 内置 `scripts/Firestone2Green.ps1` 与默认头像资源。
- **本地授权**：完成授权状态修复后，恢复 Firestone / Overwolf 正常网络能力。
- **头像修复**：仅替换 Firestone 左下角登录 / 账户头像。
- **避免误伤**：不会替换酒馆战旗英雄头像、环境套牌图标、套牌图标或卡牌图片。
- **静默持续修复**：支持安装计划任务，在系统重启或 Firestone 更新后自动补授权。
- **快捷方式启动**：可创建桌面快捷方式 `Firestone2Green 启动 Firestone`，用于日常无弹窗启动。
- **数据稳定**：启动前恢复 AuthOnlyOnline 网络，避免套牌 / 环境数据在启动断网窗口里加载失败。

## 快速开始

1. 从 GitHub Releases 下载 `Firestone2Green.exe`。
2. 右键 `Firestone2Green.exe`，选择 **以管理员身份运行**。
3. 点击 **一键重启并授权**。
4. 等待日志显示退出码 `0`。
5. 点击 **验证状态**，确认网络模式与授权状态正常。
6. 如需重启后自动维持授权，点击 **安装持续修复**。

> [!TIP]
> 安装持续修复后，建议以后都通过桌面的 **Firestone2Green 启动 Firestone** 快捷方式启动 Firestone。

## 持续修复

Firestone 的授权状态属于运行时状态。Windows 重启、Firestone 完全退出或 Overwolf 更新后，运行时状态可能回到默认值。

Firestone2Green 的持久化方式是 **后台自动补授权**，不会修改 Firestone 的签名文件。

点击 **安装持续修复** 后，程序会先创建持续修复项，然后立即静默重启 Firestone 并完成本次授权：

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

## 发布文件

推荐给普通用户分发 GitHub Releases 中的单文件 EXE：

```text
Firestone2Green.exe
```

也可以分发完整压缩包：

```text
Firestone2Green_dist.zip
```

`Firestone2Green.exe` 已内置：

- `scripts/Firestone2Green.ps1`
- `assets/avatar.jpg`
- `assets/app.ico`

因此在常规使用场景下，用户不需要额外携带 `.ps1` 文件。

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
└─ README.md
```

## 开源说明

GitHub 仓库只保留源码、核心脚本、资源和文档。

以下内容仅作为本地维护 / 发布产物，不纳入 Git 跟踪，也不上传 GitHub：

- `build.ps1`
- `build.cmd`
- `dist/`
- `Firestone2Green_dist.zip`

发布版由维护者在本地构建后放到 GitHub Releases。

## 常见问题

### 可以只发送 EXE 给其他用户吗？

可以。`Firestone2Green.exe` 已经内置脚本、头像和图标，普通用户只需要这个 EXE。

### 重启电脑后还需要手动授权吗？

安装 **持续修复** 后不需要每次手动打开 GUI。推荐重启后使用桌面的 **Firestone2Green 启动 Firestone** 快捷方式。如果使用原始图标手动启动，后台事件监听也会在检测到 `-launchapp` 后立即静默重启补授权；不会在你未启动时主动拉起。

### 完成授权后会弹窗吗？

不会。持续修复与快捷方式启动都会隐藏 PowerShell 窗口，也不会弹出完成提示。

### 为什么不直接永久修改 Firestone 文件？

授权状态属于运行时状态。Firestone2Green 通过后台任务在启动后补齐本地授权状态，避免直接改动 Firestone 的签名文件。

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