# Firestone2Green v0.2.8 更新日志

发布日期：2026-07-21

> 本版本保持现有 WPF 主界面、按钮顺序和布局不变，重点修复火绒、360、电脑管家或系统策略保护 hosts 时导致启动/授权流程退出的问题。

## 修复

- 修复 `HOSTS_WRITE_VERIFY_FAILED` 会让“一键重启并授权”直接退出的问题。
- hosts 写入被安全软件实时还原时，程序会自动回滚到写入前内容，不再让恢复流程重复触发同一错误。
- 新增 Windows 防火墙精确域名备用方案：
  - 优先创建动态 FQDN 防火墙规则；
  - 当前 Windows 不支持动态域名规则时，自动解析目标域名并创建程序级 IP 规则；
  - 只处理当前网络模式要求的精确域名，不会扩大为阻断全部 AWS Lambda 域名。
- 已有完整备用规则时直接复用，不再每次启动重复写入受保护的 hosts。
- 严格离线模式已有程序级全断网规则时，即使 hosts 被保护也不会中断修复。
- 修复“验证状态”仅依赖旧状态字段、可能把缺少实际网络保护的状态误判为正常的问题。
- 修复最初 v0.2.8 在部分 Overwolf 版本中只使用两段式启动、导致 Firestone 已打开但 Automation 接口没有建立的问题；恢复 v0.2.7 已验证的 `-launchapp <AppId> -from-desktop --automation 18765 --enable-automation` 作为受控兼容路径。
- 默认端口空闲时优先尝试旧版兼容直接启动，并且只有标准 `pingServer` 返回成功才接受该端口；默认端口被占用时，备用端口先执行 `Overwolf.exe --automation <effectivePort> --enable-automation`，紧接着发送一次标准 Firestone 启动命令，再做验证。兼容启动请求已经发出时只初始化备用 runtime，不会再次启动 Firestone。
- 默认端口仍为 `18765`。程序会使用标准 `pingServer` 请求区分 `ValidAutomation`、`Closed`、`OccupiedNonAutomation` 和 `TimedOut`；默认端口不可用时只按顺序尝试 `18766`～`18770`，不会扫描全部端口。
- 端口被普通 HTTP、非 HTTP 服务或其他程序占用时不会结束占用进程；不会结束 `NetHost.exe`、System 或任何未知进程。
- 备用端口成功后，自动授权、持续修复、头像修复和诊断报告统一使用本次 `effectiveAutomationPort`。
- 每次运行最多接受一次 Firestone 启动请求；端口回退只执行有限的 Automation 初始化，不会杀掉已经正常打开的 Firestone，也不会因候选端口重试制造频繁重启。
- 标准启动命令已发送但所有有限候选端口都不可用时，报告会显示 `LaunchOnlyDegraded`，保持 `AuthOnlyOnline` 并跳过本次运行时补授权，不再返回退出码 `1`。只有所有标准启动命令都无法执行时才报告 `LaunchFailed`。
- 修复已安装持续修复的用户在 Automation 暂不可用时可能被旧驻留监听器反复结束并重启 Firestone 的问题。新版持续修复只维持网络规则并等待下一次启动事件，不再重启 Firestone。
- 管理员运行新版并点击 **一键重启并授权** 时，会识别并安全刷新 Firestone2Green 自己的旧 `WatchFirestone` / `AutoAuth` 监听任务；刷新失败只记录警告，不阻断本次启动。

## 日志与报告

- 报告新增或完善：
  - `ExactDomainMethod`
  - `ExactDomainFirewallRules`
  - `NetworkProtectionDegraded`
  - `domainFirewall`
  - `hostsWriteError`
  - `requestedAutomationPort` / `effectiveAutomationPort`
  - `automationPortFallbackUsed` / `attemptedAutomationPorts`
  - `originalPortStatus` / `originalPortOwner` / `originalPortPid` / `originalPortPath`
  - `automationPingResponse` / `automationLaunchResult`
- 正常降级方式可能显示：
  - `DynamicFqdnFirewall`：动态精确域名规则；
  - `ResolvedIpFirewall`：解析 IP 备用规则，需要持续修复后续刷新；
- `ProgramFirewallOnly`：严格离线程序级防火墙已覆盖。
- 只有 hosts 与防火墙修改都被禁止时才显示 `UnprotectedRuntime`。程序仍会继续本地启动与授权，但会明确提示精确端点阻断未生效，不再用“已保护”文案造成误解。
- 诊断工具查找离线报告时优先检查 Firestone2Green 报告目录、下载目录和桌面；宽泛目录扫描有时间、目录数、文件数和递归深度限界，并跳过重解析点，不会因备份目录或目录链接无限卡住。

## 用户操作

- 火绒用户一般不再需要关闭“Hosts 保护/系统文件防护”，也无需联系作者。
- 不要下载第三方 hosts 文件，不要给 Everyone / Users 永久完全控制权限。
- 已安装旧版持续修复的用户升级后，管理员运行新版并点击一次 **一键重启并授权**。程序会自动刷新 Firestone2Green 自己的旧后台监听器，无需结束系统进程。
- 如果还需要重新创建桌面快捷方式，可以再点击一次 **安装持续修复**；不要求先删除。
- 默认端口被占用时无需手工选择端口，也不要结束 `NetHost.exe` 或关闭系统服务。

## 发布文件

- `Firestone2Green_v0.2.8.exe`：框架依赖的单 EXE 主程序，需要 .NET 10 Desktop Runtime。
- 源码仍随 GitHub Release 自动提供。
