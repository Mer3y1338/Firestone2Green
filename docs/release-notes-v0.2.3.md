# Firestone2Green v0.2.3 更新日志

发布日期：2026-07-11

> 本版本修复异常日志被二次错误覆盖的问题，并在 Windows hosts 缺失或为空时自动恢复默认模板，让退出码 1 和 hosts 异常都能由工具直接处理或明确定位。

## 错误定位修复

- 修复最外层异常处理在 `$ErrorActionPreference = 'Stop'` 状态下再次调用 `Write-Error`，导致原始异常被新的 `WriteErrorException` 覆盖的问题。
- 发生异常时先固定保存原始 ErrorRecord，不再让后续输出改变错误对象。
- 日志新增稳定的错误字段：
  - `F2G_ERROR`：原始错误内容。
  - `F2G_ERROR_TYPE`：原始异常类型。
  - `F2G_ERROR_LINE`：原始代码行。
  - `F2G_ERROR_COMMAND`：触发异常的原始命令。
- 控制台日志会明确显示“原始错误位置”，不再把最外层 catch 中的错误输出行误报为实际故障点。
- JSON 报告继续保存 `error`、`errorType`、`errorLine`、`errorCommand` 和 `errorStack`，便于完整排查。

## hosts 自动恢复

- 当实际 Windows `hosts` 不存在、为零字节、只有空格/Tab/换行，或只有 UTF BOM 时，自动写入标准 Windows HOSTS 模板，再追加 Firestone2Green 管理段。
- 如果系统中只有 `hosts.txt`，仍会保留原文件并复制到无扩展名 `hosts`；复制结果为空时自动补入默认模板。
- 已有非空 hosts 内容继续保留，不会因模板恢复功能被覆盖；重复执行不会重复模板或 Firestone2Green 标记段。
- 继续使用动态注册表路径、原内容备份、临时 ACL 恢复、写后校验和失败回滚，不会永久给 Everyone / Users 授权。

## 保持不变

- v0.2.2 的一体化界面、多分辨率和高 DPI 适配保持不变。
- hosts 动态路径、只读解除、临时 ACL 修复、写后校验和失败回滚等安全机制保持不变。
- 授权、头像修复、AuthOnlyOnline 网络恢复、路径搜索、持续修复和更新检查功能保持不变。

## 升级说明

由于内置 PowerShell 脚本已经更新，如果以前安装过 **持续修复**，升级后请执行一次：

1. 以管理员身份运行 `Firestone2Green_v0.2.3.exe`。
2. 点击 **移除持续修复**。
3. 再点击 **安装持续修复**。

这样计划任务、桌面快捷方式和后台脚本才会使用 v0.2.3 的错误定位逻辑。

## 发布文件

- `Firestone2Green_v0.2.3.exe`：推荐普通用户下载，右键以管理员身份运行。
- `Firestone2Green_v0.2.3_dist.zip`：包含 EXE、脚本、资源文件、使用教程和 README 的完整分发包。
- `Source code (zip)` / `Source code (tar.gz)`：GitHub 根据 v0.2.3 标签自动生成的源码归档。

## 注意事项

- Firestone2Green 完全免费，仅通过项目 GitHub 发布。
- 如果安全软件拦截 PowerShell、hosts 或计划任务，请按照日志中的原始错误和中文说明处理。
