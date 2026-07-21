# Firestone2Green

<div align="center">

![Platform](https://img.shields.io/badge/platform-Windows-blue?style=flat-square)
![UI](https://img.shields.io/badge/UI-WPF-7c3aed?style=flat-square)
![Build](https://img.shields.io/badge/build-PowerShell%20%2B%20.NET%2010-16a34a?style=flat-square)
[![Release](https://img.shields.io/github/v/release/Mer3y1338/Firestone2Green?style=flat-square)](https://github.com/Mer3y1338/Firestone2Green/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/Mer3y1338/Firestone2Green/total?style=flat-square)](https://github.com/Mer3y1338/Firestone2Green/releases)
[![License: MIT](https://img.shields.io/badge/license-MIT-lightgrey?style=flat-square)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen?style=flat-square)](https://github.com/Mer3y1338/Firestone2Green/pulls)

**Firestone2Green** is a Windows one-click local recovery utility for **Firestone / Overwolf / Hearthstone Tracker**.

It helps restore **local authorization, the bottom-left login avatar, full network access, and silent scheduled-task maintenance** for users who need to quickly repair their Firestone local runtime environment.

[⬇️ Download the latest Firestone2Green_vVERSION.exe](https://github.com/Mer3y1338/Firestone2Green/releases/latest) · [中文说明](README.md) · [User Guide](docs/%E4%BD%BF%E7%94%A8%E6%95%99%E7%A8%8B.md)

</div>

## Keywords

`Firestone` · `Overwolf` · `Hearthstone Tracker` · `Windows` · `PowerShell` · `WPF` · `local authorization repair` · `avatar repair` · `network restore` · `scheduled task` · `single-file EXE`

## Features

- **One-click workflow**: Provides a WPF GUI for common operations.
- **Single-file distribution**: The final download is one approximately 1 MiB EXE; it embeds the WPF main program, `scripts/Firestone2Green.ps1`, and the default avatar resource.
- **Adaptive path detection**: Automatically searches for or lets the user select the Overwolf root folder. The correct folder must directly contain `OverwolfLauncher.exe` or `Overwolf.exe`, so it no longer depends on a fixed drive letter or install directory.
- **Local authorization**: Restores the local authorization state and then keeps normal Firestone / Overwolf network functionality available.
- **Avatar repair**: Replaces only the Firestone bottom-left login / account avatar.
- **Safe avatar targeting**: Does not replace Battlegrounds hero portraits, meta deck icons, deck icons, card images, or other content artwork.
- **Silent persistent repair**: Can install scheduled tasks to automatically refresh authorization after Windows restarts or Firestone updates. Installing it does not launch Firestone by itself.
- **Dedicated launch shortcut**: Creates a desktop shortcut named `Firestone2Green 启动 Firestone` for daily silent startup.
- **Automatic update check**: Checks GitHub Releases on startup and shows the result in the top-right status area; network failures show an update-check warning.
- **Runtime preflight**: A lightweight launcher checks for the .NET 10 Desktop Runtime before opening the WPF UI; if it is missing, a button opens Microsoft's official download page and another button retries the check.
- **Preserved root-cause errors**: Failed runs now keep the original exception, type, source line, and command, while still adding short explanations and suggested fixes for common errors.
- **First-run disclaimer**: Shows a short learning-only / support-official disclaimer on first launch.
- **Responsive high-DPI interface**: Reflows the WPF layout for 100%–200% scaling, narrow windows, and small working areas. Compact layouts hide secondary copy, keep the footer slim and fixed, place GitHub on the left, and keep the version at the far right.
- **Stable data loading**: Switches to `AuthOnlyOnline` before starting Firestone, so deck data, meta data, and tracker data can load normally.

> [!NOTE]
> v0.2.8 keeps the existing WPF UI unchanged and retains the layout already audited at real 100% geometry plus simulated 125%–200% DPI. The left-side priority remains **one-click repair → persistence/package → fine control**.

## Quick Start

1. Download `Firestone2Green_vVERSION.exe` from GitHub Releases.
2. Right-click `Firestone2Green_vVERSION.exe` and choose **Run as administrator**. On first launch, the program checks for the .NET 10 Desktop Runtime; if it is missing, click **Official download**, install the Windows x64 Desktop Runtime, then click **Retry detection**.
3. Confirm the **路径设置** field. If it is not detected automatically, click **自动搜索** or **选择路径** and choose the Overwolf root folder — the folder where `OverwolfLauncher.exe` or `Overwolf.exe` is directly visible. If a child or parent folder is selected, the app shows the correct location. If the user has only installed Overwolf but not Firestone yet, install and open Firestone once from Overwolf first.
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
| `Firestone2Green` | Background startup watcher task | Runs after logon and watches for manual Firestone launch events. After a `-launchapp` event it probes only the effective Automation candidates; it repairs authorization when one is valid, otherwise it keeps the network rules without launching or repeatedly restarting Firestone. |
| `Firestone2Green Launch` | Background on-demand task | Triggered by the desktop shortcut. It silently launches Firestone and completes authorization, avatar repair, and network recovery. |
| `Firestone2Green 启动 Firestone.lnk` | Desktop shortcut | Recommended daily entry point for regular users. |
| `%LOCALAPPDATA%\Firestone2Green\LaunchFirestone2Green.vbs` | Hidden launcher script | Triggers the on-demand scheduled task without showing a console window. |

Persistent repair behavior:

- No PowerShell window.
- No authorization-completed popup.
- No extra confirmation popup.
- After reboot, the startup watcher can keep local authorization repaired automatically.
- If the original Firestone icon starts Firestone without a valid Automation endpoint, the watcher keeps the network rules and waits for a later startup event. It does not terminate or repeatedly restart Firestone. Use the **Firestone2Green 启动 Firestone** shortcut for reliable automatic authorization.

## Shortcut Auto-Authorization Flow

The desktop shortcut **Firestone2Green 启动 Firestone** does not open Firestone directly. It silently triggers the background task `Firestone2Green Launch`.

The task runs these steps:

1. Verifies local Firestone file integrity.
2. Clears abnormal caches and old processes.
3. Switches to `AuthOnlyOnline` before launching Firestone, so deck data, meta data, and tracker data can load from the network.
4. Probes the default Automation port `18765`. When it is free, the tool first restores the v0.2.7-compatible command `OverwolfLauncher.exe -launchapp <AppId> -from-desktop --automation 18765 --enable-automation`. If the port is occupied or that launch does not pass `pingServer` validation, only `18766` through `18770` are tried. When Firestone has not been launched yet, fallback starts the Automation runtime, immediately sends one standard launch request, and then validates `pingServer`. If the compatibility request was already accepted, fallback initializes only the runtime and does not launch Firestone again. The tool never terminates a port owner.
5. Waits for the Firestone background window and main window authorization services to initialize.
6. Repairs the local authorization state in both the background window and the main window.
7. Repairs the bottom-left login avatar.
8. Keeps full functional network access restored.

> [!IMPORTANT]
> The shortcut flow is silent and does not show a "completed" popup. Under normal conditions, authorization becomes fully effective about `30` to `60` seconds after clicking the shortcut, depending on Firestone / Overwolf startup speed.

After automatic authorization succeeds, premium-gated entries in the main window, such as My Decks, Meta Decks, Meta Archetypes, Statistics, Ranks, and Deck Building, should become available.

### Why can the shortcut appear to do nothing after installing Firestone from inside Overwolf?

Some Overwolf builds expose Automation correctly only through the direct compatibility command used by v0.2.7. The initial v0.2.8 two-stage-only flow could therefore open Firestone without exposing the authorization endpoint. The repaired flow restores the direct path only when the requested port is free and still requires a successful standard `pingServer` response before it is accepted; otherwise it falls back to the finite two-stage candidates.

An existing valid Automation service on `18765` is reused. If the port is closed, the v0.2.7-compatible direct launch is tried first. If it is occupied or the direct launch fails validation, the tool tries only `18766` through `18770`. At most one Firestone launch request is accepted per run; fallback ports perform finite Automation initialization without creating a restart loop. If every candidate fails while Firestone was launched, the run is recorded as `LaunchOnlyDegraded`: networking stays in `AuthOnlyOnline`, runtime authorization is skipped, and the tool does not return a generic exit code `1`. It never terminates `NetHost.exe`, System, or any unknown port owner.

If the shortcut does not appear to authorize automatically:

1. Wait at least `60` seconds.
2. Make sure you launched Firestone through the desktop shortcut **Firestone2Green 启动 Firestone**, not the original Firestone icon.
3. If you upgraded Firestone2Green recently, run the new `Firestone2Green_vVERSION.exe` as administrator and click **一键重启并授权** once. The new build safely refreshes an installed Firestone2Green watcher; no system process needs to be terminated. Click **安装持续修复** again only if you also want to recreate the desktop shortcut.
4. Click **一键重启并授权** to immediately restart and repair authorization.
5. Click **验证状态** and confirm the report shows `NetworkMode = AuthOnlyOnline`.

## Release Files

For regular users, distribute the single-file EXE from GitHub Releases:

```text
Firestone2Green_vVERSION.exe
```

A full archive can also be distributed:

```text
Firestone2Green_vVERSION_dist.zip
```

`Firestone2Green_vVERSION.exe` embeds:

- `scripts/Firestone2Green.ps1`
- `assets/avatar.jpg`
- `assets/app.ico`

In normal use, users do not need to carry an extra `.ps1` file or avatar file.

When only `Firestone2Green_vVERSION.exe` is distributed, the program extracts the embedded script and default avatar resource on first run. If `assets/avatar.jpg` is not present next to the program, the embedded avatar is used and written into the local runtime directory, so other users can still get the bottom-left login avatar replacement.

## Diagnosing Exit Code 1

Starting with v0.2.3, failed runs print `F2G_ERROR`, `F2G_ERROR_TYPE`, `F2G_ERROR_LINE`, and `F2G_ERROR_COMMAND`. These fields identify the original exception and source location instead of pointing at the outer error handler. For the full state, open the newest `FirestoneOfflineReport_*.json` from the GUI's **打开报告** button.

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
- `Firestone2Green_vVERSION_dist.zip`

Release builds are created locally by the maintainer and then uploaded to GitHub Releases.

## FAQ

### Can I send only the EXE to other users?

Yes. `Firestone2Green_vVERSION.exe` embeds the script, avatar, and icon. Regular users only need this EXE.

### Do I need to authorize manually after reboot?

After installing **安装持续修复**, you do not need to open the GUI every time. After reboot, it is recommended to use the desktop shortcut **Firestone2Green 启动 Firestone**. If the original Firestone icon is used, the watcher probes the effective Automation candidates after a `-launchapp` event: it repairs authorization when an endpoint is valid and otherwise keeps the network rules without repeatedly restarting Firestone. The persistent task itself will not launch Firestone when you have not started it.

### Will there be popups after authorization completes?

No. Persistent repair and shortcut startup both hide the PowerShell window and do not show a completion popup.

### Why not permanently modify Firestone files?

Authorization is runtime state. Firestone2Green repairs the local authorization state after startup through background tasks, avoiding direct modification of Firestone signature files.

### What if hosts is missing, saved as hosts.txt, busy, or protected?

The current build reads the real Windows hosts directory from the registry instead of relying only on a fixed path. If the extensionless `hosts` is missing, zero bytes long, or contains only whitespace, Firestone2Green restores the standard Windows HOSTS template before adding its managed block. It copies and preserves an existing `hosts.txt`; if that file is empty, the destination `hosts` receives the default template. Existing non-empty hosts content is preserved. The tool also clears read-only protection, retries short-lived locks, and temporarily repairs common file ACL restrictions. Before changing existing content, it stores up to 10 backups under `%LOCALAPPDATA%\Firestone2Green\hosts-backups`, then verifies the result and attempts rollback if the write fails.

Starting with v0.2.8, if the log shows `HOSTS_PROTECTION_ACTIVE`, `HOSTS_FILE_BUSY`, `HOSTS_CREATE_FAILED`, or `HOSTS_WRITE_VERIFY_FAILED`, Firestone2Green rolls hosts back and automatically switches to an exact-domain Windows Firewall fallback. It prefers dynamic FQDN rules and uses resolved-IP program rules on older systems. A complete fallback is reused on later launches, so protected machines do not retry the same hosts write every time. In normal cases you do not need to disable security software. Only when both hosts and firewall changes are blocked will the report show `UnprotectedRuntime`; startup and local authorization continue, while the log clearly states that exact-endpoint protection is not active.

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

<img width="1387" height="793" alt="image" src="https://github.com/user-attachments/assets/5c2bb2af-1fa4-4e77-8605-51ca972bf617" />
