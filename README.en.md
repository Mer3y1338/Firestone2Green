# Firestone2Green

<div align="center">

![Platform](https://img.shields.io/badge/platform-Windows-blue?style=flat-square)
![UI](https://img.shields.io/badge/UI-WinForms-7c3aed?style=flat-square)
![Build](https://img.shields.io/badge/build-PowerShell%20%2B%20.NET-16a34a?style=flat-square)
[![Release](https://img.shields.io/github/v/release/Mer3y1338/Firestone2Green?style=flat-square)](https://github.com/Mer3y1338/Firestone2Green/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/Mer3y1338/Firestone2Green/total?style=flat-square)](https://github.com/Mer3y1338/Firestone2Green/releases)
[![License: MIT](https://img.shields.io/badge/license-MIT-lightgrey?style=flat-square)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen?style=flat-square)](https://github.com/Mer3y1338/Firestone2Green/pulls)

**Firestone2Green** is a Windows one-click local recovery utility for **Firestone / Overwolf / Hearthstone Tracker**.

It helps restore **local authorization, the bottom-left login avatar, full network access, and silent scheduled-task maintenance** for users who need to quickly repair their Firestone local runtime environment.

[⬇️ Download the latest Firestone2Green.exe](https://github.com/Mer3y1338/Firestone2Green/releases/latest) · [中文说明](README.md) · [User Guide](docs/%E4%BD%BF%E7%94%A8%E6%95%99%E7%A8%8B.md)

</div>

## Keywords

`Firestone` · `Overwolf` · `Hearthstone Tracker` · `Windows` · `PowerShell` · `WinForms` · `local authorization repair` · `avatar repair` · `network restore` · `scheduled task` · `single-file EXE`

## Features

- **One-click workflow**: Provides a WinForms GUI for common operations.
- **Single-file distribution**: The EXE embeds `scripts/Firestone2Green.ps1` and the default avatar resource.
- **Adaptive path detection**: Automatically searches for or lets the user select the Overwolf root folder. The correct folder must directly contain `OverwolfLauncher.exe` or `Overwolf.exe`, so it no longer depends on a fixed `D:\overwolf` install path.
- **Local authorization**: Restores the local authorization state and then keeps normal Firestone / Overwolf network functionality available.
- **Avatar repair**: Replaces only the Firestone bottom-left login / account avatar.
- **Safe avatar targeting**: Does not replace Battlegrounds hero portraits, meta deck icons, deck icons, card images, or other content artwork.
- **Silent persistent repair**: Can install scheduled tasks to automatically refresh authorization after Windows restarts or Firestone updates. Installing it does not launch Firestone by itself.
- **Dedicated launch shortcut**: Creates a desktop shortcut named `Firestone2Green 启动 Firestone` for daily silent startup.
- **Automatic update check**: Checks GitHub Releases on startup and shows the result in the top-right status area; network failures show an update-check warning.
- **Stable data loading**: Switches to `AuthOnlyOnline` before starting Firestone, so deck data, meta data, and tracker data can load normally.

## Quick Start

1. Download `Firestone2Green.exe` from GitHub Releases.
2. Right-click `Firestone2Green.exe` and choose **Run as administrator**.
3. Confirm the **路径设置** field. If it is not detected automatically, click **自动搜索** or **选择路径** and choose the Overwolf root folder — the folder where `OverwolfLauncher.exe` or `Overwolf.exe` is directly visible. If a child or parent folder is selected, the app shows the correct location.
4. Click **一键重启并授权**.
5. Wait until the log shows exit code `0`.
6. Click **验证状态** to confirm the network mode and authorization status.
7. If you want automatic repair after reboot, click **安装持续修复**.

> [!TIP]
> After installing persistent repair, it is recommended to launch Firestone through the desktop shortcut **Firestone2Green 启动 Firestone**.
> The shortcut flow is silent and does not show a completion popup. Wait for the Firestone main window to finish loading, then wait about `30` to `60` more seconds while the background task completes authorization, avatar repair, and network recovery.

## Persistent Repair

Firestone authorization is runtime state. After Windows restarts, Firestone exits completely, or Overwolf / Firestone updates, that state can return to the default value.

Firestone2Green persists recovery through **background auto-repair** instead of modifying Firestone signature files.

After clicking **安装持续修复**, the program creates the persistent repair items, starts the background watcher, creates the desktop shortcut, and applies the network rules. It does not actively start or restart Firestone. To authorize immediately, click **一键重启并授权**. For daily use, launch Firestone through the desktop shortcut **Firestone2Green 启动 Firestone**.

| Name | Type | Purpose |
| --- | --- | --- |
| `Firestone2Green` | Background startup watcher task | Runs after logon and watches for manual Firestone launch events. It only reacts when a `-launchapp` Firestone startup event is detected, and it does not start Firestone by itself. |
| `Firestone2Green Launch` | Background on-demand task | Triggered by the desktop shortcut. It silently launches Firestone and completes authorization, avatar repair, and network recovery. |
| `Firestone2Green 启动 Firestone.lnk` | Desktop shortcut | Recommended daily entry point for regular users. |
| `%LOCALAPPDATA%\Firestone2Green\LaunchFirestone2Green.vbs` | Hidden launcher script | Triggers the on-demand scheduled task without showing a console window. |

Persistent repair behavior:

- No PowerShell window.
- No authorization-completed popup.
- No extra confirmation popup.
- After reboot, the startup watcher can keep local authorization repaired automatically.
- If the original Firestone icon is used and Firestone starts without automation, the watcher can detect the manual launch event and silently restart/repair it. It will not launch Firestone when the user has not started Firestone.

## Shortcut Auto-Authorization Flow

The desktop shortcut **Firestone2Green 启动 Firestone** does not open Firestone directly. It silently triggers the background task `Firestone2Green Launch`.

The task runs these steps:

1. Verifies local Firestone file integrity.
2. Clears abnormal caches and old processes.
3. Switches to `AuthOnlyOnline` before launching Firestone, so deck data, meta data, and tracker data can load from the network.
4. Starts Firestone with the local automation port enabled.
5. Waits for the Firestone background window and main window authorization services to initialize.
6. Repairs the local authorization state in both the background window and the main window.
7. Repairs the bottom-left login avatar.
8. Keeps full functional network access restored.

> [!IMPORTANT]
> The shortcut flow is silent and does not show a "completed" popup. Under normal conditions, authorization becomes fully effective about `30` to `60` seconds after clicking the shortcut, depending on Firestone / Overwolf startup speed.

After automatic authorization succeeds, premium-gated entries in the main window, such as My Decks, Meta Decks, Meta Archetypes, Statistics, Ranks, and Deck Building, should become available.

If the shortcut does not appear to authorize automatically:

1. Wait at least `60` seconds.
2. Make sure you launched Firestone through the desktop shortcut **Firestone2Green 启动 Firestone**, not the original Firestone icon.
3. Run `Firestone2Green.exe` as administrator and click **安装持续修复** to reinstall the shortcut and background tasks.
4. Click **一键重启并授权** to immediately restart and repair authorization.
5. Click **验证状态** and confirm the report shows `NetworkMode = AuthOnlyOnline`.

## Release Files

For regular users, distribute the single-file EXE from GitHub Releases:

```text
Firestone2Green.exe
```

A full archive can also be distributed:

```text
Firestone2Green_dist.zip
```

`Firestone2Green.exe` embeds:

- `scripts/Firestone2Green.ps1`
- `assets/avatar.jpg`
- `assets/app.ico`

In normal use, users do not need to carry an extra `.ps1` file or avatar file.

When only `Firestone2Green.exe` is distributed, the program extracts the embedded script and default avatar resource on first run. If `assets/avatar.jpg` is not present next to the program, the embedded avatar is used and written into the local runtime directory, so other users can still get the bottom-left login avatar replacement.

## Repository Layout

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

## Open Source Notes

The GitHub repository only tracks source code, the core script, assets, and documentation.

The following files are local maintenance / release artifacts and are not tracked by Git or uploaded to GitHub:

- `build.ps1`
- `build.cmd`
- `dist/`
- `Firestone2Green_dist.zip`

Release builds are created locally by the maintainer and then uploaded to GitHub Releases.

## FAQ

### Can I send only the EXE to other users?

Yes. `Firestone2Green.exe` embeds the script, avatar, and icon. Regular users only need this EXE.

### Do I need to authorize manually after reboot?

After installing **安装持续修复**, you do not need to open the GUI every time. After reboot, it is recommended to use the desktop shortcut **Firestone2Green 启动 Firestone**. If the original Firestone icon is used, the background watcher can also react to a `-launchapp` event and silently repair authorization. The persistent task itself will not launch Firestone when you have not started it.

### Will there be popups after authorization completes?

No. Persistent repair and shortcut startup both hide the PowerShell window and do not show a completion popup.

### Why not permanently modify Firestone files?

Authorization is runtime state. Firestone2Green repairs the local authorization state after startup through background tasks, avoiding direct modification of Firestone signature files.

### What if data, deck, or online features do not work?

In the GUI, click **恢复全功能网络**, then click **验证状态**. The report should show:

```text
NetworkMode = AuthOnlyOnline
```

### What if the avatar is replaced in the wrong place?

The current version only targets the bottom-left login / account avatar and excludes heroes, decks, cards, and other content artwork. If anything looks wrong, click **刷新授权+头像**.

### Where is the detailed Chinese tutorial?

See [`docs/使用教程.md`](docs/%E4%BD%BF%E7%94%A8%E6%95%99%E7%A8%8B.md).

## License

This project is licensed under the [MIT License](LICENSE).
