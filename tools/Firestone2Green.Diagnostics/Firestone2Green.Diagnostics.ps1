<#
Firestone2Green Diagnostics
A read-only one-click diagnostic engine. It never changes hosts, firewall rules,
scheduled tasks, shortcuts, Overwolf, or Firestone files. The only writes are
its own TXT/JSON reports under the requested output directory.
#>
[CmdletBinding()]
param(
  [string]$OutputDirectory = "$env:LOCALAPPDATA\Firestone2Green\Diagnostics",
  [string]$RunId = '',
  [string]$ReportPath = ''
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch {}

$Script:ToolVersion = '0.1.0'
$Script:AppId = 'lnknbakkpommmjjdnelmfbjjdbocfpnpbkijjnob'
$Script:AutomationPort = 18765
$Script:ProtectedDomain = '73ybnsv6auhl6x2hv5tvdoppcq0oecmq.lambda-url.us-west-2.on.aws'
$Script:StartedAt = Get-Date
$Script:Log = New-Object System.Collections.Generic.List[string]
$Script:Findings = New-Object System.Collections.Generic.List[object]
$Script:FindingCodes = @{}
$Script:TaskScriptPaths = New-Object System.Collections.Generic.List[string]

function Write-DiagLog {
  param([string]$Message)
  $line = '[{0}] {1}' -f (Get-Date).ToString('HH:mm:ss'), $Message
  $Script:Log.Add($line)
  Write-Host $line
}

function Convert-ToStringArray {
  param($Value)
  $result = @()
  foreach ($item in @($Value)) {
    if ($null -eq $item) { continue }
    if (($item -is [System.Collections.IEnumerable]) -and -not ($item -is [string])) {
      $result += @(Convert-ToStringArray $item)
      continue
    }
    if (-not [string]::IsNullOrWhiteSpace([string]$item)) { $result += [string]$item }
  }
  return @($result)
}

function Add-Finding {
  param(
    [Parameter(Mandatory=$true)][string]$Code,
    [Parameter(Mandatory=$true)][string]$Title,
    [Parameter(Mandatory=$true)][string]$Cause,
    [Parameter(Mandatory=$true)][int]$Score,
    [ValidateSet('critical','error','warning','info','ok')][string]$Severity = 'warning',
    $Evidence = @(),
    $Solutions = @(),
    [bool]$Blocking = $false
  )
  if ($Script:FindingCodes.ContainsKey($Code)) { return }
  $finding = [pscustomobject][ordered]@{
    code = $Code
    title = $Title
    cause = $Cause
    score = $Score
    severity = $Severity
    blocking = $Blocking
    evidence = @(Convert-ToStringArray $Evidence)
    solutions = @(Convert-ToStringArray $Solutions)
  }
  $Script:FindingCodes[$Code] = $true
  $Script:Findings.Add($finding)
}

function Get-ObjectProperty {
  param($Object, [string]$Name, $Default = $null)
  if ($null -eq $Object) { return $Default }
  $property = $Object.PSObject.Properties[$Name]
  if ($null -eq $property) { return $Default }
  return $property.Value
}

function Test-IsAdministrator {
  try {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
  } catch { return $false }
}

function Get-SafeFileVersion {
  param([string]$Path)
  try {
    if (Test-Path -LiteralPath $Path -PathType Leaf) {
      $info = [Diagnostics.FileVersionInfo]::GetVersionInfo($Path)
      if ($info.FileVersion) { return [string]$info.FileVersion }
      if ($info.ProductVersion) { return [string]$info.ProductVersion }
    }
  } catch {}
  return ''
}

function Get-SafeFileHash {
  param([string]$Path)
  try {
    if (Test-Path -LiteralPath $Path -PathType Leaf) {
      return (Get-FileHash -LiteralPath $Path -Algorithm SHA256 -ErrorAction Stop).Hash.ToLowerInvariant()
    }
  } catch {}
  return ''
}

function Expand-EnvironmentPath {
  param([string]$Path)
  if ([string]::IsNullOrWhiteSpace($Path)) { return '' }
  try { return [Environment]::ExpandEnvironmentVariables($Path.Trim().Trim('"')) } catch { return $Path }
}

function Get-ExecutableFromCommandLine {
  param([string]$CommandLine)
  if ([string]::IsNullOrWhiteSpace($CommandLine)) { return '' }
  $text = $CommandLine.Trim()
  $match = [regex]::Match($text, '^\s*"(?<path>[^"]+\.exe)"', 'IgnoreCase')
  if ($match.Success) { return $match.Groups['path'].Value }
  $match = [regex]::Match($text, '^\s*(?<path>[^\s]+\.exe)', 'IgnoreCase')
  if ($match.Success) { return $match.Groups['path'].Value }
  return ''
}

function Get-PowerShellFileArgument {
  param([string]$Arguments)
  if ([string]::IsNullOrWhiteSpace($Arguments)) { return '' }
  $match = [regex]::Match($Arguments, '(?i)(?:^|\s)-File\s+(?:"(?<quoted>[^"]+)"|(?<plain>\S+))')
  if (-not $match.Success) { return '' }
  $value = if ($match.Groups['quoted'].Success) { $match.Groups['quoted'].Value } else { $match.Groups['plain'].Value }
  return Expand-EnvironmentPath $value
}

function Test-ValidOverwolfRoot {
  param([string]$Path)
  if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
  try {
    $full = [IO.Path]::GetFullPath((Expand-EnvironmentPath $Path))
    return ((Test-Path -LiteralPath (Join-Path $full 'OverwolfLauncher.exe') -PathType Leaf) -or
            (Test-Path -LiteralPath (Join-Path $full 'Overwolf.exe') -PathType Leaf))
  } catch { return $false }
}

function Find-OverwolfRootNearPath {
  param([string]$Path)
  if ([string]::IsNullOrWhiteSpace($Path)) { return '' }
  try {
    $expanded = Expand-EnvironmentPath $Path
    if (Test-Path -LiteralPath $expanded -PathType Leaf) { $expanded = Split-Path -Parent $expanded }
    $dir = [IO.DirectoryInfo][IO.Path]::GetFullPath($expanded)
    for ($i = 0; $i -lt 6 -and $null -ne $dir; $i++) {
      if (Test-ValidOverwolfRoot $dir.FullName) { return $dir.FullName }
      foreach ($childName in @('Overwolf','overwolf')) {
        $child = Join-Path $dir.FullName $childName
        if (Test-ValidOverwolfRoot $child) { return [IO.Path]::GetFullPath($child) }
      }
      $dir = $dir.Parent
    }
  } catch {}
  return ''
}

function Get-DotNetDesktopRuntimes {
  $versions = New-Object System.Collections.Generic.List[string]
  $roots = New-Object System.Collections.Generic.List[object]
  $seenRoots = @{}
  $x64ProgramFiles = [string]$env:ProgramW6432
  if (-not $x64ProgramFiles -and [Environment]::Is64BitProcess) { $x64ProgramFiles = [string]$env:ProgramFiles }
  $x86ProgramFiles = [string][Environment]::GetEnvironmentVariable('ProgramFiles(x86)')
  if (-not [Environment]::Is64BitOperatingSystem) { $x86ProgramFiles = [string]$env:ProgramFiles }
  foreach ($candidate in @(
    [pscustomobject]@{ arch='x64'; base=$x64ProgramFiles },
    [pscustomobject]@{ arch='x86'; base=$x86ProgramFiles }
  )) {
    if ([string]::IsNullOrWhiteSpace([string]$candidate.base)) { continue }
    $root = Join-Path ([string]$candidate.base) 'dotnet'
    try { $root = [IO.Path]::GetFullPath($root) } catch {}
    $key = $root.ToLowerInvariant()
    if ($seenRoots.ContainsKey($key)) { continue }
    $seenRoots[$key] = $true
    $roots.Add([pscustomobject]@{ arch=[string]$candidate.arch; path=$root })
  }
  foreach ($root in $roots.ToArray()) {
    $shared = Join-Path $root.path 'shared\Microsoft.WindowsDesktop.App'
    if (-not (Test-Path -LiteralPath $shared -PathType Container)) { continue }
    try {
      foreach ($folder in @(Get-ChildItem -LiteralPath $shared -Directory -ErrorAction Stop)) {
        if ($folder.Name -match '^\d+\.\d+') { $versions.Add("$($root.arch) $($folder.Name)") }
      }
    } catch {}
  }
  try {
    $dotnet = Get-Command dotnet.exe -ErrorAction Stop
    $source = [IO.Path]::GetFullPath([string]$dotnet.Source)
    $known = @($roots.ToArray() | Where-Object { $source.StartsWith(($_.path.TrimEnd('\') + '\'), [StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1)
    if ($known.Count -eq 0) {
      $arch = ''
      try {
        $info = @(& $source --info 2>$null)
        $archLine = @($info | Where-Object { [string]$_ -match '^\s*Architecture\s*:\s*(?<arch>\S+)' } | Select-Object -First 1)
        if ($archLine.Count -gt 0 -and [string]$archLine[0] -match '^\s*Architecture\s*:\s*(?<arch>\S+)') { $arch = $matches['arch'].ToLowerInvariant() }
      } catch {}
      if ($arch) {
        foreach ($line in @(& $source --list-runtimes 2>$null)) {
          if ([string]$line -match '^Microsoft\.WindowsDesktop\.App\s+(?<version>\S+)') {
            $entry = "$arch $($matches['version'])"
            if (-not $versions.Contains($entry)) { $versions.Add($entry) }
          }
        }
      }
    }
  } catch {}
  return @($versions | Sort-Object -Unique)
}

function Get-SecuritySoftwareSnapshot {
  $result = New-Object System.Collections.Generic.List[object]
  $patterns = [ordered]@{
    '火绒' = '^(Hips|HipsTray|HipsDaemon|sysdiag|wsctrl)'
    '360' = '^(360|ZhuDongFangYu|QH|safemon)'
    '腾讯电脑管家' = '^(QQPC|PCMgr|TAV|QM)'
    'Windows Defender' = '^(MsMpEng|SecurityHealthService|NisSrv)'
  }
  try {
    foreach ($process in @(Get-Process -ErrorAction SilentlyContinue)) {
      foreach ($name in $patterns.Keys) {
        if ($process.ProcessName -match $patterns[$name]) {
          if (-not @($result | Where-Object { $_.name -eq $name -and $_.process -eq $process.ProcessName }).Count) {
            $result.Add([pscustomobject]@{ name = $name; process = $process.ProcessName; id = $process.Id })
          }
        }
      }
    }
  } catch {}
  return $result.ToArray()
}

function Get-SystemSnapshot {
  Write-DiagLog '检查 Windows、权限、PowerShell、.NET 10 和安全软件...'
  $osCaption = [Environment]::OSVersion.VersionString
  $osVersion = [Environment]::OSVersion.Version.ToString()
  try {
    $os = Get-CimInstance Win32_OperatingSystem -ErrorAction Stop
    $osCaption = [string]$os.Caption
    $osVersion = [string]$os.Version
  } catch {}
  $admin = Test-IsAdministrator
  $desktopRuntimes = @(Get-DotNetDesktopRuntimes)
  # The released WPF payload targets win-x64, so an x86-only desktop runtime is not sufficient.
  $hasDesktop10 = @($desktopRuntimes | Where-Object { $_ -match '^x64\s+10\.' }).Count -gt 0
  $security = @(Get-SecuritySoftwareSnapshot)
  $defender = $null
  try {
    if (Get-Command Get-MpComputerStatus -ErrorAction SilentlyContinue) {
      $mp = Get-MpComputerStatus -ErrorAction Stop
      $defender = [pscustomobject]@{
        antivirusEnabled = [bool]$mp.AntivirusEnabled
        realTimeProtectionEnabled = [bool]$mp.RealTimeProtectionEnabled
        behaviorMonitorEnabled = [bool]$mp.BehaviorMonitorEnabled
      }
    }
  } catch {}

  if (-not $admin) {
    Add-Finding -Code 'NOT_ELEVATED' -Title '本次排查未使用管理员权限' -Cause '普通权限可以完成大部分只读检查，但部分进程命令行、防火墙细节和计划任务权限可能无法完整读取。' -Score 5 -Severity 'info' -Evidence @('当前进程不是管理员') -Solutions @('先查看本次结论；如果报告中出现“无法读取”或结论不明确，再点击排查工具里的“管理员重新排查”。')
  }
  if (-not $hasDesktop10) {
    Add-Finding -Code 'DOTNET10_DESKTOP_RUNTIME_MISSING' -Title '缺少 .NET 10 Desktop Runtime x64' -Cause 'Firestone2Green 当前 WPF 主程序需要 Microsoft.WindowsDesktop.App 10.x；缺失时主界面无法启动。' -Score 88 -Severity 'error' -Blocking $true -Evidence @('未检测到 Microsoft.WindowsDesktop.App 10.x', ('已检测到：' + ($desktopRuntimes -join '；'))) -Solutions @('安装微软官方 .NET 10 Desktop Runtime x64（Windows Desktop Runtime，不是仅安装 ASP.NET Runtime）。','安装完成后重新打开 Firestone2Green；不需要重新安装 Overwolf。')
  }
  return [pscustomobject][ordered]@{
    computer = $env:COMPUTERNAME
    user = "$env:USERDOMAIN\$env:USERNAME"
    elevated = $admin
    osCaption = $osCaption
    osVersion = $osVersion
    osArchitecture = [Environment]::Is64BitOperatingSystem
    processArchitecture64Bit = [Environment]::Is64BitProcess
    powershellVersion = $PSVersionTable.PSVersion.ToString()
    powershellEdition = [string](Get-ObjectProperty $PSVersionTable 'PSEdition' 'Desktop')
    dotNetDesktopRuntimes = $desktopRuntimes
    hasDotNet10Desktop = $hasDesktop10
    securitySoftware = $security
    defender = $defender
  }
}

function Add-OverwolfCandidate {
  param([System.Collections.Generic.List[object]]$List, [hashtable]$Seen, [string]$Path, [string]$Source, [int]$Priority)
  if ([string]::IsNullOrWhiteSpace($Path)) { return }
  $expanded = Expand-EnvironmentPath $Path
  if ($expanded -match '(?i)\.exe$') { $expanded = Split-Path -Parent $expanded }
  try { $expanded = [IO.Path]::GetFullPath($expanded) } catch { return }
  $key = $expanded.ToLowerInvariant()
  if ($Seen.ContainsKey($key)) { return }
  $Seen[$key] = $true
  $valid = Test-ValidOverwolfRoot $expanded
  $suggested = if ($valid) { $expanded } else { Find-OverwolfRootNearPath $expanded }
  $List.Add([pscustomobject][ordered]@{
    path = $expanded
    source = $Source
    priority = $Priority
    exists = Test-Path -LiteralPath $expanded -PathType Container
    valid = $valid
    suggestedRoot = $suggested
    launcherExists = Test-Path -LiteralPath (Join-Path $expanded 'OverwolfLauncher.exe') -PathType Leaf
    runtimeExists = Test-Path -LiteralPath (Join-Path $expanded 'Overwolf.exe') -PathType Leaf
  })
}

function Get-OverwolfSnapshot {
  Write-DiagLog '定位并校验 Overwolf 根目录...'
  $candidates = New-Object System.Collections.Generic.List[object]
  $seen = @{}
  $configPath = Join-Path $env:LOCALAPPDATA 'Firestone2Green\config.ini'
  $configuredRoot = ''
  try {
    if (Test-Path -LiteralPath $configPath -PathType Leaf) {
      foreach ($line in @(Get-Content -LiteralPath $configPath -Encoding UTF8 -ErrorAction Stop)) {
        $match = [regex]::Match([string]$line, '^\s*OverwolfRoot\s*=\s*(?<value>.+?)\s*$', 'IgnoreCase')
        if ($match.Success) { $configuredRoot = $match.Groups['value'].Value.Trim(); break }
      }
      Add-OverwolfCandidate $candidates $seen $configuredRoot 'Firestone2Green config.ini' 0
    }
  } catch {}

  $processes = @()
  try {
    $processes = @(Get-CimInstance Win32_Process -ErrorAction Stop | Where-Object {
      ([string]$_.Name -match '(?i)^Overwolf(?:Launcher|Browser|Helper)?\.exe$') -or ([string]$_.Name -match '(?i)^ow-.*\.exe$')
    } | Select-Object ProcessId,Name,ExecutablePath,CommandLine)
    foreach ($process in $processes) {
      if ($process.ExecutablePath) { Add-OverwolfCandidate $candidates $seen (Split-Path -Parent $process.ExecutablePath) '运行中的 Overwolf 进程' 1 }
    }
  } catch {}

  foreach ($key in @(
    'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run',
    'HKLM:\Software\Microsoft\Windows\CurrentVersion\Run',
    'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run'
  )) {
    try {
      if (-not (Test-Path -LiteralPath $key)) { continue }
      $item = Get-ItemProperty -LiteralPath $key -ErrorAction Stop
      foreach ($property in $item.PSObject.Properties) {
        if ($property.Name -match '^PS') { continue }
        if ($property.Name -match '(?i)overwolf' -or [string]$property.Value -match '(?i)overwolf') {
          $exe = Get-ExecutableFromCommandLine ([string]$property.Value)
          if ($exe) { Add-OverwolfCandidate $candidates $seen (Split-Path -Parent $exe) "注册表启动项：$($property.Name)" 2 }
        }
      }
    } catch {}
  }

  foreach ($baseKey in @(
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*',
    'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*'
  )) {
    try {
      foreach ($item in @(Get-ItemProperty $baseKey -ErrorAction SilentlyContinue | Where-Object { [string]$_.DisplayName -match '(?i)overwolf' })) {
        if ($item.InstallLocation) { Add-OverwolfCandidate $candidates $seen ([string]$item.InstallLocation) "卸载注册表：$($item.DisplayName)" 3 }
        if ($item.DisplayIcon) {
          $icon = ([string]$item.DisplayIcon -replace ',\d+$','').Trim('"')
          Add-OverwolfCandidate $candidates $seen (Split-Path -Parent $icon) "卸载注册表图标：$($item.DisplayName)" 3
        }
      }
    } catch {}
  }

  $fixedDrives = @()
  try { $fixedDrives = @(Get-CimInstance Win32_LogicalDisk -Filter 'DriveType=3' -ErrorAction Stop | ForEach-Object { $_.DeviceID }) } catch { $fixedDrives = @($env:SystemDrive) }
  foreach ($drive in $fixedDrives) {
    foreach ($relative in @('overwolf','Overwolf','Program Files\Overwolf','Program Files (x86)\Overwolf','Program Files (x86)\Common Files\Overwolf','Program Files\Common Files\Overwolf')) {
      Add-OverwolfCandidate $candidates $seen (Join-Path ($drive + '\') $relative) '常见安装位置' 5
    }
  }

  $validCandidates = @($candidates | Where-Object { $_.valid } | Sort-Object priority,path)
  $selected = if ($validCandidates.Count -gt 0) { $validCandidates[0] } else { $null }
  if (-not [string]::IsNullOrWhiteSpace($configuredRoot) -and -not (Test-ValidOverwolfRoot $configuredRoot)) {
    $near = Find-OverwolfRootNearPath $configuredRoot
    $evidence = @("config.ini 保存路径：$configuredRoot")
    if ($near) { $evidence += "可用根目录：$near" }
    Add-Finding -Code 'OVERWOLF_ROOT_INVALID' -Title 'Firestone2Green 保存了错误的 Overwolf 路径' -Cause '保存路径没有直接包含 OverwolfLauncher.exe 或 Overwolf.exe，可能选到了上级目录、子目录或旧安装位置。' -Score 84 -Severity 'error' -Evidence $evidence -Solutions @($(if($near){"在 Firestone2Green 中把路径改为：$near"}else{'点击“自动搜索”；仍找不到时选择直接包含 OverwolfLauncher.exe 的目录。'}),'不要选择 %LOCALAPPDATA%\Overwolf\Extensions；那里是扩展数据，不是 Overwolf 程序根目录。')
  }
  if ($null -eq $selected) {
    Add-Finding -Code 'OVERWOLF_NOT_FOUND' -Title '未找到可用的 Overwolf 程序根目录' -Cause '系统中没有发现直接包含 OverwolfLauncher.exe 或 Overwolf.exe 的目录。' -Score 98 -Severity 'critical' -Blocking $true -Evidence @('已检查进程、注册表、config.ini 和所有固定磁盘常见位置') -Solutions @('先确认 Overwolf 可以单独正常启动。','在 Firestone2Green 中点击“自动搜索”；仍失败时手动选择直接包含 OverwolfLauncher.exe 的目录。','如果 Overwolf 已被卸载或安装损坏，请先从 Overwolf 官网重新安装。')
  }

  $executables = @()
  if ($null -ne $selected) {
    foreach ($name in @('OverwolfLauncher.exe','Overwolf.exe')) {
      $path = Join-Path $selected.path $name
      if (Test-Path -LiteralPath $path -PathType Leaf) {
        $executables += [pscustomobject]@{ path = $path; version = Get-SafeFileVersion $path; size = (Get-Item -LiteralPath $path).Length }
      }
    }
  }
  return [pscustomobject][ordered]@{
    configPath = $configPath
    configuredRoot = $configuredRoot
    selectedRoot = if ($selected) { $selected.path } else { '' }
    selectedSource = if ($selected) { $selected.source } else { '' }
    candidates = $candidates.ToArray()
    executables = @($executables)
    processes = @($processes)
  }
}

function Test-FirestonePackageIntegrity {
  param([string]$VersionRoot)
  $metadataPath = Join-Path $VersionRoot '_metadata\verified_contents.json'
  $result = [ordered]@{
    metadataPath = $metadataPath
    metadataExists = Test-Path -LiteralPath $metadataPath -PathType Leaf
    ok = $false
    expected = 0
    checked = 0
    missing = @()
    mismatches = @()
    error = ''
  }
  if (-not $result.metadataExists) {
    $result.error = '缺少 _metadata\verified_contents.json'
    return [pscustomobject]$result
  }
  try {
    $json = Get-Content -LiteralPath $metadataPath -Raw -Encoding UTF8 -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
    $hashes = Get-ObjectProperty $json 'hashes'
    if ($null -eq $hashes) { throw '签名文件中没有 hashes 字段' }
    $missing = New-Object System.Collections.Generic.List[string]
    $mismatches = New-Object System.Collections.Generic.List[object]
    $properties = @($hashes.PSObject.Properties)
    $result.expected = $properties.Count
    foreach ($property in $properties) {
      $relative = [string]$property.Name
      $expectedHash = ([string]$property.Value).ToLowerInvariant()
      $path = Join-Path $VersionRoot ($relative -replace '/', '\')
      if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $missing.Add($relative)
        continue
      }
      $actualHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256 -ErrorAction Stop).Hash.ToLowerInvariant()
      $result.checked++
      if ($actualHash -ne $expectedHash) {
        $mismatches.Add([pscustomobject]@{ path = $relative; expected = $expectedHash; actual = $actualHash })
      }
    }
    $result.missing = @($missing)
    $result.mismatches = $mismatches.ToArray()
    $result.ok = ($missing.Count -eq 0 -and $mismatches.Count -eq 0 -and $result.expected -gt 0)
  } catch {
    $result.error = $_.Exception.Message
  }
  return [pscustomobject]$result
}

function Get-LocalRepairScriptSnapshot {
  $paths = New-Object System.Collections.Generic.List[string]
  $defaultScript = Join-Path $env:LOCALAPPDATA 'Firestone2Green\scripts\Firestone2Green.ps1'
  if (Test-Path -LiteralPath $defaultScript -PathType Leaf) { $paths.Add($defaultScript) }
  foreach ($path in @($Script:TaskScriptPaths)) {
    if ($path -and -not $paths.Contains($path) -and (Test-Path -LiteralPath $path -PathType Leaf)) { $paths.Add($path) }
  }
  $items = @()
  foreach ($path in $paths) {
    $content = ''
    $readError = ''
    try { $content = Get-Content -LiteralPath $path -Raw -Encoding UTF8 -ErrorAction Stop } catch { $readError = $_.Exception.Message }
    $mixed = $false
    $fixed = $false
    $hasLegacyCompatibility = $false
    if ($content) {
      $hasMixedArguments = ($content -match "(?s)standardArgs\s*=\s*@\('?-launchapp.*?--automation") -or
                           ($content -match "(?s)-launchapp.{0,220}--automation.{0,80}--enable-automation")
      $hasLegacyCompatibility = ($content -match 'LegacyDirectLauncher') -and
                                ($content -match 'StartedAutomationLegacyDirect') -and
                                ($content -match 'legacyAutomationCompatibilityUsed') -and
                                ($content -match 'Get-AutomationPortsToTry')
      $mixed = $hasMixedArguments -and -not $hasLegacyCompatibility
      $fixed = ($content -match 'LaunchOnlyDegraded') -and
               (($content -match "automationArgs\s*=\s*@\('--automation'") -or ($content -match "runtimeArgs\s*=\s*@\('--automation'")) -and
               (($content -match "launchArgs\s*=\s*@\('-launchapp'") -or ($content -match "standardArgs\s*=\s*@\('-launchapp'"))
    }
    $item = [pscustomobject][ordered]@{
      path = $path
      size = if (Test-Path -LiteralPath $path) { (Get-Item -LiteralPath $path).Length } else { 0 }
      lastWriteTime = if (Test-Path -LiteralPath $path) { (Get-Item -LiteralPath $path).LastWriteTime.ToString('o') } else { '' }
      sha256 = Get-SafeFileHash $path
      readError = $readError
      hasMixedLaunchArguments = $mixed
      hasLegacyCompatibleAutomationLaunch = $hasLegacyCompatibility
      hasSeparatedAutomationLaunch = $fixed
    }
    $items += $item
    if ($mixed) {
      Add-Finding -Code 'OLD_PERSISTENT_SCRIPT' -Title '本机仍在使用旧版持续修复脚本' -Cause '检测到早期单一路径启动脚本：它把 --automation 参数固定在 Firestone 启动命令中，但缺少 pingServer 验证、有限备用端口和安全降级。' -Score 96 -Severity 'critical' -Blocking $true -Evidence @("旧脚本：$path",'检测到未受保护的旧式混合启动参数') -Solutions @('使用包含“旧版兼容启动 + 有限备用端口”修复的最新版。','打开新版，先点击“移除持续修复”，再点击“安装持续修复”，让计划任务和桌面快捷方式使用新脚本。','完成后再执行一次“一键处理”。')
    }
  }
  if (-not (Test-Path -LiteralPath $defaultScript -PathType Leaf)) {
    Add-Finding -Code 'EXTRACTED_SCRIPT_MISSING' -Title '未找到 Firestone2Green 本地修复脚本' -Cause '主程序尚未成功释放脚本，或脚本被安全软件删除。' -Score 72 -Severity 'warning' -Evidence @("预期位置：$defaultScript") -Solutions @('重新打开最新版 Firestone2Green，让主程序重新释放脚本。','如果再次消失，在安全软件隔离区检查是否误删 Firestone2Green.ps1，并把 Firestone2Green 加入允许列表。')
  }
  return @($items)
}

function Get-FirestoneExtensionSnapshot {
  Write-DiagLog '检查 Firestone 扩展目录、版本、OPK 缓存和 451 项签名文件...'
  $base = Join-Path $env:LOCALAPPDATA "Overwolf\Extensions\$($Script:AppId)"
  $packageBase = Join-Path $env:LOCALAPPDATA "Overwolf\PackagesCache\$($Script:AppId)"
  $versions = @()
  if (Test-Path -LiteralPath $base -PathType Container) {
    try {
      $versions = @(Get-ChildItem -LiteralPath $base -Directory -ErrorAction Stop | Sort-Object LastWriteTime -Descending | ForEach-Object {
        [pscustomobject]@{ name = $_.Name; path = $_.FullName; lastWriteTime = $_.LastWriteTime.ToString('o') }
      })
    } catch {}
  }
  if ($versions.Count -eq 0) {
    Add-Finding -Code 'FIRESTONE_EXTENSION_MISSING' -Title '没有找到 Firestone 扩展版本' -Cause 'Overwolf 可能只安装了平台本体，Firestone 尚未安装、更新未完成，或扩展目录被清理。' -Score 97 -Severity 'critical' -Blocking $true -Evidence @("扩展目录：$base") -Solutions @('先直接打开 Overwolf，在应用商店确认 Firestone 已安装。','正常打开 Firestone 一次并等待下载/更新完成，再关闭后重新排查。','不要把 Overwolf 程序目录与 %LOCALAPPDATA%\Overwolf\Extensions 混为同一路径。')
    return [pscustomobject][ordered]@{ basePath=$base; packageBasePath=$packageBase; versions=@(); selectedVersion=''; selectedPath=''; opkPath=''; opkExists=$false; integrity=$null }
  }
  $selected = $versions[0]
  $opkPath = Join-Path $packageBase "$($selected.name)\app.opk"
  $integrity = Test-FirestonePackageIntegrity $selected.path
  if (-not $integrity.ok) {
    $evidence = @("当前版本：$($selected.name)","目录：$($selected.path)","缺失文件：$(@($integrity.missing).Count)","hash 不匹配：$(@($integrity.mismatches).Count)")
    if ($integrity.error) { $evidence += "校验错误：$($integrity.error)" }
    Add-Finding -Code 'FIRESTONE_PACKAGE_INCOMPLETE' -Title 'Firestone 安装包不完整或签名文件不一致' -Cause '当前版本目录有文件缺失、被修改，或更新过程没有完整结束。' -Score 94 -Severity 'critical' -Blocking $true -Evidence $evidence -Solutions @('先关闭 Firestone 和 Overwolf。','确认 app.opk 缓存存在时，在 Firestone2Green 中执行一次“一键处理”恢复签名文件。','如果 app.opk 也缺失，打开 Overwolf 让 Firestone 重新下载；仍失败时在 Overwolf 中重装 Firestone。')
  }
  if (-not (Test-Path -LiteralPath $opkPath -PathType Leaf)) {
    $opkSeverity = if ($integrity.ok) { 'info' } else { 'error' }
    $opkScore = if ($integrity.ok) { 4 } else { 86 }
    Add-Finding -Code 'FIRESTONE_OPK_CACHE_MISSING' -Title '缺少 Firestone 安装缓存 app.opk' -Cause '签名文件当前完整，因此这通常只表示 Firestone2Green 无法从 OPK 自动还原文件；本身不等于当前安装损坏。' -Score $opkScore -Severity $opkSeverity -Evidence @("缓存路径：$opkPath","签名校验通过：$($integrity.ok)") -Solutions @('当前 Firestone 可以正常启动时无需处理。','若以后出现文件缺失或 hash 不匹配，请先让 Overwolf 完整更新或重装 Firestone，以重新生成 app.opk。')
  }
  if ($versions.Count -gt 2) {
    Add-Finding -Code 'MULTIPLE_STALE_FIRESTONE_VERSIONS' -Title '发现多个旧 Firestone 版本目录' -Cause 'Overwolf 更新后留下了较多旧版本；计划任务或旧报告可能仍指向旧目录。' -Score 25 -Severity 'info' -Evidence @("版本目录数量：$($versions.Count)", ('版本：' + (($versions | ForEach-Object {$_.name}) -join '、'))) -Solutions @('先不要手工删除；确认最新版正常后由 Overwolf 自身清理。','如果持续修复仍引用旧脚本，请移除并重新安装持续修复。')
  }
  return [pscustomobject][ordered]@{
    basePath = $base
    packageBasePath = $packageBase
    versions = $versions
    selectedVersion = $selected.name
    selectedPath = $selected.path
    opkPath = $opkPath
    opkExists = Test-Path -LiteralPath $opkPath -PathType Leaf
    opkSize = if (Test-Path -LiteralPath $opkPath -PathType Leaf) { (Get-Item -LiteralPath $opkPath).Length } else { 0 }
    integrity = $integrity
  }
}

function Get-ProcessAndAutomationSnapshot {
  Write-DiagLog '检查 Overwolf/Firestone 进程和本机 automation 端口 18765...'
  $processes = @()
  $commandLineAccessError = ''
  try {
    $processes = @(Get-CimInstance Win32_Process -ErrorAction Stop | Where-Object {
      $name = [string]$_.Name
      $cmd = [string]$_.CommandLine
      $name -match '(?i)^(Overwolf|OverwolfLauncher|OverwolfBrowser|ow-.*)\.exe$' -or $cmd -like "*$($Script:AppId)*" -or $cmd -like '*Firestone - *'
    } | Select-Object ProcessId,ParentProcessId,Name,ExecutablePath,CommandLine)
  } catch { $commandLineAccessError = $_.Exception.Message }
  $firestoneProcesses = @($processes | Where-Object { [string]$_.CommandLine -like "*$($Script:AppId)*" -or [string]$_.CommandLine -like '*Firestone - *' })

  $listeners = @()
  try {
    if (Get-Command Get-NetTCPConnection -ErrorAction SilentlyContinue) {
      $listeners = @(Get-NetTCPConnection -State Listen -LocalPort $Script:AutomationPort -ErrorAction Stop | ForEach-Object {
        $owner = $null
        try { $owner = Get-Process -Id $_.OwningProcess -ErrorAction Stop } catch {}
        [pscustomobject]@{ localAddress=$_.LocalAddress; localPort=$_.LocalPort; owningProcess=$_.OwningProcess; processName=if($owner){$owner.ProcessName}else{''}; processPath=if($owner){try{$owner.Path}catch{''}}else{''} }
      })
    }
  } catch {}
  if ($listeners.Count -eq 0) {
    try {
      foreach ($line in @(& netstat.exe -ano -p tcp 2>$null)) {
        if ($line -match "^\s*TCP\s+(?<local>\S+:$($Script:AutomationPort))\s+\S+\s+LISTENING\s+(?<pid>\d+)\s*$") {
          $pidValue = [int]$matches['pid']; $owner = Get-Process -Id $pidValue -ErrorAction SilentlyContinue
          $listeners += [pscustomobject]@{localAddress=$matches['local'];localPort=$Script:AutomationPort;owningProcess=$pidValue;processName=if($owner){$owner.ProcessName}else{''};processPath=if($owner){try{$owner.Path}catch{''}}else{''}}
        }
      }
    } catch {}
  }

  $automation = [ordered]@{ port=$Script:AutomationPort; listenerPresent=($listeners.Count -gt 0); listeners=$listeners; status='closed'; validResponse=$false; response=$null; error='' }
  if ($listeners.Count -gt 0) {
    $body = @{ id='pingServer'; args=@{} } | ConvertTo-Json -Compress
    $targets = @(
      [pscustomobject]@{ uri="http://localhost:$($Script:AutomationPort)/"; host='' },
      [pscustomobject]@{ uri="http://127.0.0.1:$($Script:AutomationPort)/"; host="localhost:$($Script:AutomationPort)" }
    )
    $errors = @()
    foreach ($target in $targets) {
      try {
        $request = [System.Net.HttpWebRequest][System.Net.WebRequest]::Create([uri]$target.uri)
        $request.Method='POST'; $request.Proxy=$null; $request.ContentType='application/json; charset=utf-8'; $request.Accept='application/json'; $request.Timeout=3000; $request.ReadWriteTimeout=3000; $request.KeepAlive=$false
        if ($target.host) { $request.Host=$target.host }
        $bytes=(New-Object System.Text.UTF8Encoding($false)).GetBytes($body); $request.ContentLength=$bytes.Length
        $stream=$request.GetRequestStream(); try{$stream.Write($bytes,0,$bytes.Length)}finally{$stream.Dispose()}
        $response=[System.Net.HttpWebResponse]$request.GetResponse()
        try { $reader=New-Object IO.StreamReader($response.GetResponseStream(),[Text.Encoding]::UTF8); try{$text=$reader.ReadToEnd()}finally{$reader.Dispose()} } finally {$response.Dispose()}
        $parsed=$text | ConvertFrom-Json -ErrorAction Stop
        $automation.response=$parsed
        if ([bool](Get-ObjectProperty $parsed 'success' $false)) { $automation.validResponse=$true; $automation.status='ready'; break }
        $errors += "返回 JSON 但 success 不为 true：$text"
      } catch { $errors += $_.Exception.Message }
    }
    if (-not $automation.validResponse) { $automation.status='invalid-response'; $automation.error=($errors -join '；') }
  }

  if ($automation.status -eq 'invalid-response') {
    $owners = @($listeners | ForEach-Object { "$($_.processName) PID=$($_.owningProcess)" })
    $isOverwolfOwner = @($listeners | Where-Object { $_.processName -match '(?i)overwolf|^ow-' }).Count -gt 0
    if ($isOverwolfOwner) {
      Add-Finding -Code 'AUTOMATION_INVALID_RESPONSE' -Title '默认 Automation 端口 18765 返回无效结果' -Cause '18765 上的 Overwolf 本机接口没有返回 Firestone2Green 需要的成功 JSON；最新版会安全检查并有限尝试 18766-18770。' -Score 76 -Severity 'warning' -Evidence @($owners + $automation.error) -Solutions @('使用最新版 Firestone2Green 的“一键重启并授权”，程序会先尝试 v0.2.7 已验证的兼容启动方式，再有限尝试备用端口。','无需手工拼接 --automation 参数，也不要反复启动 Firestone；程序会自动选择并验证本次有效端口。','如果全部候选端口仍失败，提交本诊断 JSON 和最新 FirestoneOfflineReport JSON。')
    } else {
      Add-Finding -Code 'DEFAULT_PORT_OCCUPIED_NON_AUTOMATION' -Title '默认 Automation 端口 18765 被其他程序占用' -Cause '18765 当前不是有效的 Overwolf Automation；最新版会安全跳过占用者，只按顺序尝试 18766-18770。' -Score 64 -Severity 'warning' -Blocking $false -Evidence $owners -Solutions @('直接使用最新版 Firestone2Green 的“一键重启并授权”，无需手工选择端口。','不要结束 NetHost.exe、System 或任何不认识的端口占用进程。')
    }
  }
  return [pscustomobject][ordered]@{ processes=$processes; firestoneProcesses=$firestoneProcesses; commandLineAccessError=$commandLineAccessError; automation=[pscustomobject]$automation }
}

function Get-HostsFilePath {
  $windowsRoot = if (-not [string]::IsNullOrWhiteSpace($env:SystemRoot)) { $env:SystemRoot } else { $env:windir }
  $databasePath = ''
  try { $databasePath = [string](Get-ItemProperty -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters' -Name DataBasePath -ErrorAction Stop).DataBasePath } catch {}
  $databasePath = Expand-EnvironmentPath $databasePath
  if ([string]::IsNullOrWhiteSpace($databasePath)) { $databasePath = Join-Path $windowsRoot 'System32\drivers\etc' }
  elseif (-not [IO.Path]::IsPathRooted($databasePath)) { $databasePath = Join-Path $windowsRoot $databasePath }
  $candidate = if ([string]::Equals((Split-Path -Leaf $databasePath), 'hosts', [StringComparison]::OrdinalIgnoreCase)) { $databasePath } else { Join-Path $databasePath 'hosts' }
  try {
    if ([Environment]::Is64BitOperatingSystem -and -not [Environment]::Is64BitProcess) {
      $system32Prefix = (Join-Path $windowsRoot 'System32').TrimEnd('\') + '\'
      if ($candidate.StartsWith($system32Prefix, [StringComparison]::OrdinalIgnoreCase)) {
        $candidate = Join-Path (Join-Path $windowsRoot 'Sysnative') $candidate.Substring($system32Prefix.Length)
      }
    }
  } catch {}
  try { return [IO.Path]::GetFullPath($candidate) } catch { return $candidate }
}

function Get-HostsSnapshot {
  Write-DiagLog '检查 hosts 实际路径、hosts.txt、属性、ACL 和 Firestone 目标条目...'
  $path = Get-HostsFilePath
  $parentPath = try { Split-Path -Parent $path } catch { '' }
  $hostsTxtPath = if ($parentPath) { Join-Path $parentPath 'hosts.txt' } else { '' }
  $exists = Test-Path -LiteralPath $path -PathType Leaf
  $pathIsDirectory = Test-Path -LiteralPath $path -PathType Container
  $parentExists = if ($parentPath) { Test-Path -LiteralPath $parentPath -PathType Container } else { $false }
  $hostsTxtExists = if ($hostsTxtPath) { Test-Path -LiteralPath $hostsTxtPath -PathType Leaf } else { $false }
  $content = ''
  $readError = ''
  $attributes = ''
  $owner = ''
  $access = @()
  if ($exists) {
    try { $content = Get-Content -LiteralPath $path -Raw -ErrorAction Stop } catch { $readError = $_.Exception.Message }
    try { $attributes = [string](Get-Item -LiteralPath $path -Force).Attributes } catch {}
    try {
      $acl = Get-Acl -LiteralPath $path -ErrorAction Stop
      $owner = [string]$acl.Owner
      $access = @($acl.Access | ForEach-Object { [pscustomobject]@{ identity=[string]$_.IdentityReference; rights=[string]$_.FileSystemRights; type=[string]$_.AccessControlType; inherited=[bool]$_.IsInherited } })
    } catch {}
  }
  if ($pathIsDirectory) {
    Add-Finding -Code 'HOSTS_PATH_OCCUPIED_BY_DIRECTORY' -Title 'hosts 路径被同名文件夹占用' -Cause 'Windows 要求 hosts 是一个无扩展名文件，但该路径现在是文件夹，因此程序无法创建或更新 hosts。' -Score 99 -Severity 'critical' -Blocking $true -Evidence @("异常路径：$path") -Solutions @('先关闭所有可能占用该位置的程序。','备份后删除或重命名这个同名文件夹，再由 Firestone2Green 创建 hosts 文件。','正确目标是无扩展名的 hosts 文件，不是 hosts.txt。')
  } elseif ($readError) {
    Add-Finding -Code 'HOSTS_READ_FAILED' -Title '无法读取 hosts 文件' -Cause '当前进程无法读取系统 hosts，通常是安全软件保护、ACL 权限异常或文件被独占。' -Score 78 -Severity 'error' -Evidence @("路径：$path","读取错误：$readError") -Solutions @('点击“管理员重新排查”，确认是否仍然无法读取。','在安全软件中临时关闭 Hosts 保护或把 Firestone2Green 加入允许列表。','如 ACL 被改坏，请恢复系统默认权限，不要给 Everyone 完全控制。')
  } elseif (-not $exists -and $hostsTxtExists) {
    Add-Finding -Code 'HOSTS_TXT_ONLY' -Title '只有 hosts.txt，没有系统实际使用的 hosts' -Cause 'Windows 只读取无扩展名的 hosts 文件；hosts.txt 不会生效，这通常是手工保存文件时被记事本自动加了扩展名。' -Score 6 -Severity 'info' -Evidence @("缺少文件：$path","发现文件：$hostsTxtPath") -Solutions @('用管理员权限运行 Firestone2Green，让程序创建正确的无扩展名 hosts 文件。','如果日志出现 HOSTS_CREATE_FAILED，再检查安全软件保护和 hosts 目录 ACL。')
  } elseif (-not $exists) {
    Add-Finding -Code 'HOSTS_FILE_MISSING' -Title '系统 hosts 文件不存在' -Cause 'Windows 可以在需要时重新创建 hosts，因此单独缺失不一定是故障；只有程序创建失败时才会阻断修复。' -Score 3 -Severity 'info' -Evidence @("目标路径：$path","父目录存在：$parentExists") -Solutions @('先以管理员权限执行一次一键处理。','若仍提示无法创建 hosts，请检查安全软件的 Hosts 保护以及目录权限。')
  }
  if ($exists -and $attributes -match 'ReadOnly') {
    Add-Finding -Code 'HOSTS_READ_ONLY' -Title 'hosts 文件带有只读属性' -Cause '只读属性可能阻止程序更新 hosts，但程序在管理员权限下通常会尝试安全处理。' -Score 4 -Severity 'info' -Evidence @("路径：$path","属性：$attributes") -Solutions @('如果实际写入失败，再检查安全软件保护和 hosts ACL。')
  }
  $entries = @()
  $lineNumber = 0
  foreach ($line in @($content -split "`r?`n")) {
    $lineNumber++
    $trimmed = ([string]$line -replace '#.*$','').Trim()
    if (-not $trimmed) { continue }
    $parts = @($trimmed -split '\s+')
    if ($parts.Count -lt 2) { continue }
    foreach ($hostName in $parts[1..($parts.Count-1)]) {
      if ($hostName -ieq $Script:ProtectedDomain -or $hostName -match '(?i)lambda-url\.us-west-2\.on\.aws$') {
        $entries += [pscustomobject]@{ line=$lineNumber; address=$parts[0]; host=$hostName.ToLowerInvariant(); raw=[string]$line }
      }
    }
  }
  $targetEntries = @($entries | Where-Object { $_.host -ieq $Script:ProtectedDomain })
  $blockingEntries = @($targetEntries | Where-Object { $_.address -in @('0.0.0.0','127.0.0.1','::1') })
  $conflictingEntries = @($targetEntries | Where-Object { $_.address -notin @('0.0.0.0','127.0.0.1','::1') })
  if ($targetEntries.Count -gt 1 -or $conflictingEntries.Count -gt 0) {
    Add-Finding -Code 'HOSTS_DUPLICATE_OR_CONFLICTING' -Title 'hosts 中存在重复或冲突的 Firestone 条目' -Cause '同一域名出现多条记录或指向非阻断地址时，最终解析结果可能不稳定。' -Score 55 -Severity 'warning' -Evidence @($targetEntries | ForEach-Object { "第 $($_.line) 行：$($_.address) $($_.host)" }) -Solutions @('不要手工叠加多组 hosts 规则。','使用最新版 Firestone2Green 恢复网络后再执行一键处理，让程序重新生成唯一的精确条目。')
  }
  return [pscustomobject][ordered]@{
    path=$path; parentPath=$parentPath; parentExists=$parentExists; exists=$exists; pathIsDirectory=$pathIsDirectory; hostsTxtPath=$hostsTxtPath; hostsTxtExists=$hostsTxtExists; size=if($exists){(Get-Item -LiteralPath $path).Length}else{0}; attributes=$attributes; readOnly=($attributes -match 'ReadOnly'); owner=$owner; access=$access; readError=$readError; targetEntries=$targetEntries; blockingEntryCount=$blockingEntries.Count; conflictingEntryCount=$conflictingEntries.Count; protectedDomainBlocked=($blockingEntries.Count -gt 0)
  }
}

function Get-FirewallSnapshot {
  param($Overwolf)
  Write-DiagLog '检查 Firestone2Green 精确域名规则和旧版全程序防火墙阻断...'
  $available = [bool](Get-Command Get-NetFirewallRule -ErrorAction SilentlyContinue)
  $readError = ''
  $rules = @()
  if ($available) {
    try {
      $candidateRules = @(Get-NetFirewallRule -ErrorAction Stop | Where-Object {
        [string]$_.DisplayName -like 'Firestone2Green*' -or [string]$_.Group -like 'Firestone*' -or [string]$_.DisplayGroup -like 'Firestone*'
      })
      foreach ($rule in $candidateRules) {
        $apps = @(); $addresses = @()
        try { $apps = @(Get-NetFirewallApplicationFilter -AssociatedNetFirewallRule $rule -ErrorAction Stop | ForEach-Object { [string]$_.Program }) } catch {}
        try { $addresses = @(Get-NetFirewallAddressFilter -AssociatedNetFirewallRule $rule -ErrorAction Stop | ForEach-Object { [string]$_.RemoteAddress }) } catch {}
        $rules += [pscustomobject][ordered]@{
          name=[string]$rule.DisplayName; group=[string]$rule.Group; displayGroup=[string]$rule.DisplayGroup; enabled=([string]$rule.Enabled -eq 'True'); direction=[string]$rule.Direction; action=[string]$rule.Action; profile=[string]$rule.Profile; programs=$apps; remoteAddresses=$addresses
        }
      }
    } catch { $readError = $_.Exception.Message }
  }
  $exactRules = @($rules | Where-Object { $_.group -eq 'Firestone2Green Exact Domain Block' -or $_.name -like 'Firestone2Green * Domain*' })
  $activeExactRules = @($exactRules | Where-Object { $_.enabled -and $_.direction -eq 'Outbound' -and $_.action -eq 'Block' })
  $broadRules = @($rules | Where-Object { ($_.group -eq 'Firestone Offline Block' -or $_.name -like 'Firestone Offline Block*') -and $_.enabled -and $_.direction -eq 'Outbound' -and $_.action -eq 'Block' })
  $expectedPrograms = @()
  $selectedRoot = if ($Overwolf) { [string](Get-ObjectProperty $Overwolf 'selectedRoot' '') } else { '' }
  if ($selectedRoot -and (Test-Path -LiteralPath $selectedRoot -PathType Container)) {
    try { $expectedPrograms = @(Get-ChildItem -LiteralPath $selectedRoot -Recurse -Filter '*.exe' -File -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName } | Sort-Object -Unique) } catch {}
  }
  $ruleProgramSet = @{}
  $activeRulePrograms = @($activeExactRules | ForEach-Object { $_.programs } | Where-Object { $_ -and $_ -ne 'Any' } | ForEach-Object {
    $expanded = Expand-EnvironmentPath ([string]$_)
    try { $expanded = [IO.Path]::GetFullPath($expanded) } catch {}
    $ruleProgramSet[$expanded.ToLowerInvariant()] = $true
    $expanded
  } | Sort-Object -Unique)
  $missingPrograms = @($expectedPrograms | Where-Object { -not $ruleProgramSet.ContainsKey($_.ToLowerInvariant()) })
  $stalePrograms = @($activeRulePrograms | Where-Object { -not (Test-Path -LiteralPath $_ -PathType Leaf) })
  $coverageKnown = $expectedPrograms.Count -gt 0
  $coverageComplete = (-not $coverageKnown) -or $missingPrograms.Count -eq 0
  $exactProtectionActive = $activeExactRules.Count -gt 0 -and $coverageComplete
  if ($activeExactRules.Count -gt 0 -and $coverageKnown -and -not $coverageComplete) {
    Add-Finding -Code 'EXACT_FIREWALL_COVERAGE_INCOMPLETE' -Title '精确防火墙规则未覆盖当前全部 Overwolf 程序' -Cause 'Overwolf 更新后可能新增或替换了可执行文件；现有规则只覆盖旧程序。hosts 被安全软件保护时，这套防火墙兜底可能无法完整阻断目标域名。' -Score 8 -Severity 'info' -Evidence @("当前程序数：$($expectedPrograms.Count)","规则覆盖数：$($activeRulePrograms.Count)","缺少覆盖：$($missingPrograms.Count)",@($missingPrograms | Select-Object -First 12)) -Solutions @('使用最新版 Firestone2Green 重新执行一键处理，以当前 Overwolf 目录重建精确规则。','Overwolf 每次大版本更新后，如 hosts 无法写入，应重新运行一次修复。')
  }
  if ($stalePrograms.Count -gt 0) {
    Add-Finding -Code 'STALE_FIREWALL_PROGRAM_PATHS' -Title '防火墙规则仍指向已不存在的 Overwolf 程序' -Cause 'Overwolf 更新后旧版本程序被删除，但旧规则没有同步清理。' -Score 5 -Severity 'info' -Evidence $stalePrograms -Solutions @('使用最新版 Firestone2Green 恢复网络后重新执行一键处理，以刷新规则。')
  }
  if ($broadRules.Count -gt 0) {
    Add-Finding -Code 'BROAD_FIREWALL_RULE_ACTIVE' -Title '检测到旧版全程序阻断规则' -Cause '旧版 StrictOffline 或全量规则会阻断 Overwolf 与 Firestone 的所有网络，可能造成数据、更新或登录功能不可用。' -Score 76 -Severity 'error' -Evidence @($broadRules | ForEach-Object { $_.name }) -Solutions @('使用最新版 Firestone2Green 执行“恢复网络”，清理旧版全量阻断。','随后重新执行一键处理，使用 AuthOnlyOnline 精确保护模式。')
  }
  return [pscustomobject][ordered]@{ available=$available; readError=$readError; rules=$rules; exactRules=$exactRules; activeExactRules=$activeExactRules; broadProgramRules=$broadRules; expectedPrograms=$expectedPrograms; activeRulePrograms=$activeRulePrograms; missingPrograms=$missingPrograms; stalePrograms=$stalePrograms; coverageKnown=$coverageKnown; coverageComplete=$coverageComplete; exactProtectionActive=$exactProtectionActive; broadProgramBlockActive=($broadRules.Count -gt 0) }
}

function Test-DnsNameSafe {
  param([string]$Name)
  $result = [ordered]@{ name=$Name; success=$false; addresses=@(); error='' }
  try {
    $addresses = @([Net.Dns]::GetHostAddresses($Name) | ForEach-Object { $_.IPAddressToString } | Sort-Object -Unique)
    $result.addresses = $addresses
    $result.success = ($addresses.Count -gt 0)
  } catch { $result.error = $_.Exception.Message }
  return [pscustomobject]$result
}

function Test-HttpsHeadSafe {
  param([string]$Uri)
  $result = [ordered]@{ uri=$Uri; success=$false; statusCode=0; error=''; proxy='' }
  $response = $null
  try {
    $request = [System.Net.HttpWebRequest][System.Net.WebRequest]::Create([uri]$Uri)
    $request.Method='HEAD'; $request.Timeout=5000; $request.ReadWriteTimeout=5000; $request.AllowAutoRedirect=$true; $request.UserAgent='Firestone2Green-Diagnostics/0.1'
    try { if ($request.Proxy) { $proxyUri=$request.Proxy.GetProxy([uri]$Uri); if($proxyUri){$result.proxy=$proxyUri.AbsoluteUri} } } catch {}
    $response=[System.Net.HttpWebResponse]$request.GetResponse(); $result.statusCode=[int]$response.StatusCode; $result.success=($result.statusCode -ge 200 -and $result.statusCode -lt 500)
  } catch [System.Net.WebException] {
    if ($_.Exception.Response) { try{$result.statusCode=[int]$_.Exception.Response.StatusCode; $result.success=($result.statusCode -ge 200 -and $result.statusCode -lt 500)}catch{} }
    $result.error=$_.Exception.Message
  } catch { $result.error=$_.Exception.Message }
  finally { if($response){$response.Dispose()} }
  return [pscustomobject]$result
}

function Get-NetworkSnapshot {
  Write-DiagLog '检查 DNS、更新站点连通性以及保护是否存在安全兜底...'
  $dns = @(
    (Test-DnsNameSafe 'github.com'),
    (Test-DnsNameSafe 'overwolf.com'),
    (Test-DnsNameSafe $Script:ProtectedDomain)
  )
  $https = Test-HttpsHeadSafe 'https://github.com/'
  $normalDnsFailures = @($dns | Where-Object { $_.name -ne $Script:ProtectedDomain -and -not $_.success })
  if ($normalDnsFailures.Count -ge 2) {
    Add-Finding -Code 'NETWORK_DNS_FAILURE' -Title '系统 DNS 无法解析常用服务域名' -Cause '这不是 Firestone 授权逻辑本身的问题；DNS、代理、VPN 或安全软件网络过滤异常会导致更新检查和 Overwolf 数据请求失败。' -Score 80 -Severity 'error' -Evidence @($normalDnsFailures | ForEach-Object { "$($_.name)：$($_.error)" }) -Solutions @('临时退出异常 VPN/代理后重试。','确认系统时间和 DNS 设置正常；可先重启路由器和电脑。','不要为了修复此问题关闭 Firestone2Green 的精确端点保护。')
  }
  if (-not $https.success -and @($dns | Where-Object {$_.name -eq 'github.com' -and $_.success}).Count -gt 0) {
    Add-Finding -Code 'UPDATE_SITE_HTTPS_BLOCKED' -Title 'GitHub 域名可解析，但 HTTPS 连接失败' -Cause '自动更新检查可能被代理、防火墙、证书检查或网络策略拦截。' -Score 38 -Severity 'warning' -Evidence @("状态码：$($https.statusCode)","错误：$($https.error)","系统代理：$($https.proxy)") -Solutions @('这不会阻止本地修复功能；仅影响更新检查。','检查代理/VPN，或从可信网络手动下载 GitHub Release。')
  }
  return [pscustomobject][ordered]@{ dns=$dns; githubHttps=$https }
}

function Get-ShortcutSnapshot {
  param([string]$Path)
  $result = [ordered]@{ path=$Path; exists=(Test-Path -LiteralPath $Path -PathType Leaf); target=''; arguments=''; workingDirectory=''; error='' }
  if (-not $result.exists) { return [pscustomobject]$result }
  try {
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($Path)
    $result.target = [string]$shortcut.TargetPath
    $result.arguments = [string]$shortcut.Arguments
    $result.workingDirectory = [string]$shortcut.WorkingDirectory
  } catch { $result.error=$_.Exception.Message }
  return [pscustomobject]$result
}

function Get-PersistenceSnapshot {
  Write-DiagLog '检查持续修复计划任务、隐藏启动脚本和桌面快捷方式...'
  $service = $null
  try { $svc=Get-Service -Name Schedule -ErrorAction Stop; $service=[pscustomobject]@{status=[string]$svc.Status;startType=[string]$svc.StartType} } catch {}
  if ($service -and $service.status -ne 'Running') {
    Add-Finding -Code 'TASK_SCHEDULER_DISABLED' -Title 'Windows 任务计划程序服务未运行' -Cause '持续修复和桌面隐藏启动依赖 Task Scheduler；服务停止后任务不会执行。' -Score 90 -Severity 'critical' -Blocking $true -Evidence @("Schedule 服务状态：$($service.status)，启动类型：$($service.startType)") -Solutions @('在 Windows 服务中把“Task Scheduler/任务计划程序”恢复为运行状态。','如果被优化软件禁用，请撤销对应的系统优化。','服务恢复后，移除并重新安装持续修复。')
  }

  $tasks = @()
  foreach ($name in @('Firestone2Green','Firestone2Green Launch')) {
    $entry = [ordered]@{name=$name;exists=$false;state='';enabled=$false;actions=@();userId='';runLevel='';logonType='';error=''}
    try {
      $task=Get-ScheduledTask -TaskName $name -ErrorAction Stop
      $entry.exists=$true; $entry.state=[string]$task.State; $entry.enabled=([string]$task.State -ne 'Disabled'); $entry.userId=[string]$task.Principal.UserId; $entry.runLevel=[string]$task.Principal.RunLevel; $entry.logonType=[string]$task.Principal.LogonType
      foreach ($action in @($task.Actions)) {
        $scriptPath = Get-PowerShellFileArgument ([string]$action.Arguments)
        if ($scriptPath -and -not $Script:TaskScriptPaths.Contains($scriptPath)) { $Script:TaskScriptPaths.Add($scriptPath) }
        $entry.actions += [pscustomobject]@{execute=[string]$action.Execute;arguments=[string]$action.Arguments;workingDirectory=[string]$action.WorkingDirectory;scriptPath=$scriptPath;scriptExists=if($scriptPath){Test-Path -LiteralPath $scriptPath -PathType Leaf}else{$true}}
      }
    } catch { $entry.error=$_.Exception.Message }
    $tasks += [pscustomobject]$entry
  }
  $installedTasks = @($tasks | Where-Object {$_.exists})
  foreach ($task in $installedTasks) {
    $brokenActions = @($task.actions | Where-Object { -not $_.scriptExists -or ([string]::IsNullOrWhiteSpace($_.execute)) })
    if (-not $task.enabled -or $brokenActions.Count -gt 0 -or $task.runLevel -ne 'Highest') {
      $evidence=@("任务：$($task.name)，状态=$($task.state)，权限=$($task.runLevel)")
      $evidence += @($task.actions | ForEach-Object { "$($_.execute) $($_.arguments)；脚本存在=$($_.scriptExists)" })
      Add-Finding -Code 'PERSISTENT_TASK_BROKEN' -Title '持续修复计划任务配置不完整' -Cause '任务被禁用、不是最高权限，或动作引用的脚本已经不存在。' -Score 87 -Severity 'error' -Blocking $true -Evidence $evidence -Solutions @('在最新版 Firestone2Green 中先“移除持续修复”，再“安装持续修复”。','不要手工复制旧的计划任务 XML 或旧脚本。')
    }
  }
  if (($installedTasks.Count -eq 1) -and @($installedTasks | Where-Object {$_.name -eq 'Firestone2Green'}).Count -eq 1) {
    Add-Finding -Code 'PERSISTENT_LAUNCH_TASK_MISSING' -Title '持续修复缺少启动任务' -Cause '只存在监听任务，没有 “Firestone2Green Launch” 任务，桌面隐藏快捷方式无法触发完整启动流程。' -Score 63 -Severity 'warning' -Evidence @('存在 Firestone2Green，但不存在 Firestone2Green Launch') -Solutions @('使用最新版重新安装持续修复，让两个任务和快捷方式成套生成。')
  }

  $vbsPath=Join-Path $env:LOCALAPPDATA 'Firestone2Green\LaunchFirestone2Green.vbs'
  $vbs=[ordered]@{path=$vbsPath;exists=Test-Path -LiteralPath $vbsPath -PathType Leaf;content='';error=''}
  if($vbs.exists){try{$vbs.content=Get-Content -LiteralPath $vbsPath -Raw -ErrorAction Stop}catch{$vbs.error=$_.Exception.Message}}
  $desktop=[Environment]::GetFolderPath('Desktop')
  $shortcutCandidates=@(
    (Join-Path $desktop 'Firestone2Green 启动 Firestone.lnk'),
    (Join-Path $desktop 'Firestone.lnk')
  )
  $shortcuts=@($shortcutCandidates | ForEach-Object { Get-ShortcutSnapshot $_ })
  $f2gShortcut=@($shortcuts | Where-Object { $_.exists -and (($_.target -like '*LaunchFirestone2Green.vbs') -or ($_.arguments -like '*Firestone2Green Launch*')) })
  if (@($tasks | Where-Object {$_.name -eq 'Firestone2Green Launch' -and $_.exists}).Count -gt 0) {
    if (-not $vbs.exists -or $f2gShortcut.Count -eq 0) {
      Add-Finding -Code 'PERSISTENT_SHORTCUT_BROKEN' -Title '持续修复启动任务存在，但隐藏启动入口缺失' -Cause 'Launch 任务已安装，但 VBS 或桌面快捷方式不完整，用户双击旧快捷方式时不会走修复流程。' -Score 61 -Severity 'warning' -Evidence @("VBS 存在=$($vbs.exists)","有效快捷方式数量=$($f2gShortcut.Count)") -Solutions @('移除并重新安装持续修复，让 VBS、Launch 任务和桌面快捷方式重新配套生成。','删除自己手工创建的旧 Firestone 快捷方式，改用程序生成的快捷方式。')
    }
  }
  return [pscustomobject][ordered]@{schedulerService=$service;tasks=$tasks;vbs=[pscustomobject]$vbs;shortcuts=$shortcuts}
}

function Get-ReportSnapshot {
  param([string]$PreferredPath)
  Write-DiagLog '查找并解析最新 FirestoneOfflineReport JSON...'

  # Report discovery must stay bounded. Some users keep large backup trees under
  # %LOCALAPPDATA%\Firestone2Green; an unbounded Get-ChildItem -Recurse can make
  # the diagnostic GUI appear frozen forever.
  $scanStarted = Get-Date
  $maxScanMilliseconds = 6000
  $maxDirectories = 600
  $maxFilesInspected = 12000
  $maxCandidateReports = 1200
  $maxReportBytes = 8MB
  $scan = [ordered]@{
    scanTimedOut = $false
    scannedDirectories = 0
    scannedFiles = 0
    skippedLargeFiles = 0
    skippedReparsePoints = 0
    accessErrors = 0
  }
  $candidates = New-Object System.Collections.Generic.List[object]
  $seen = @{}
  $priorityRoots = @{}

  function Test-ReportScanBudget {
    if ([bool]$scan['scanTimedOut']) { return $false }
    $elapsed = [int]((Get-Date) - $scanStarted).TotalMilliseconds
    if ($elapsed -ge $maxScanMilliseconds -or [int]$scan['scannedDirectories'] -ge $maxDirectories -or [int]$scan['scannedFiles'] -ge $maxFilesInspected -or $candidates.Count -ge $maxCandidateReports) {
      $scan['scanTimedOut'] = $true
      return $false
    }
    return $true
  }

  function Add-ReportFile {
    param([string]$Path,[string]$Source,[int]$Priority)
    if ([string]::IsNullOrWhiteSpace($Path) -or $candidates.Count -ge $maxCandidateReports) { return }
    try { $full = [IO.Path]::GetFullPath($Path) } catch { $scan['accessErrors'] = [int]$scan['accessErrors'] + 1; return }
    $key = $full.ToLowerInvariant()
    if ($seen.ContainsKey($key)) { return }
    $seen[$key] = $true
    try {
      $file = Get-Item -LiteralPath $full -Force -ErrorAction Stop
      if ($file.PSIsContainer) { return }
      if ([int64]$file.Length -gt [int64]$maxReportBytes) {
        $scan['skippedLargeFiles'] = [int]$scan['skippedLargeFiles'] + 1
        return
      }
      $candidates.Add([pscustomobject][ordered]@{
        path = $full
        source = $Source
        priority = $Priority
        lastWriteTime = $file.LastWriteTime
        length = [int64]$file.Length
      })
    } catch {
      $scan['accessErrors'] = [int]$scan['accessErrors'] + 1
    }
  }

  function Test-SkippedReportDirectory {
    param([string]$Path)
    try { $name = [IO.Path]::GetFileName($Path.TrimEnd('\')) } catch { return $true }
    return ($name -like 'backup_*' -or $name -in @('Diagnostics','runtime','hosts-backups','assets'))
  }

  function Scan-ReportTree {
    param([string]$Root,[string]$Source,[int]$Priority,[switch]$SkipPriorityRoots,[int]$MaxDepth=4)
    if ([string]::IsNullOrWhiteSpace($Root)) { return }
    try {
      $rootPath = [IO.Path]::GetFullPath($Root)
      $rootAttributes = [IO.File]::GetAttributes($rootPath)
      if (($rootAttributes -band [IO.FileAttributes]::Directory) -eq 0) { return }
      if (($rootAttributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        $scan['skippedReparsePoints'] = [int]$scan['skippedReparsePoints'] + 1
        return
      }
    } catch {
      return
    }
    if ($MaxDepth -lt 0) { $MaxDepth = 0 }
    $queue = New-Object 'System.Collections.Generic.Queue[object]'
    $queue.Enqueue([pscustomobject]@{ path = $rootPath; depth = 0 })
    while ($queue.Count -gt 0 -and (Test-ReportScanBudget)) {
      $queued = $queue.Dequeue()
      $current = [string]$queued.path
      $depth = [int]$queued.depth
      if (Test-SkippedReportDirectory -Path $current) { continue }
      $scan['scannedDirectories'] = [int]$scan['scannedDirectories'] + 1
      $enumerator = $null
      try {
        $enumerator = [IO.Directory]::EnumerateFileSystemEntries($current).GetEnumerator()
        while (Test-ReportScanBudget) {
          $hasNext = $false
          try { $hasNext = $enumerator.MoveNext() } catch { $scan['accessErrors'] = [int]$scan['accessErrors'] + 1; break }
          if (-not $hasNext) { break }
          $entry = [string]$enumerator.Current
          try {
            $entryAttributes = [IO.File]::GetAttributes($entry)
          } catch {
            $scan['accessErrors'] = [int]$scan['accessErrors'] + 1
            continue
          }
          if (($entryAttributes -band [IO.FileAttributes]::Directory) -ne 0) {
            if (($entryAttributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
              $scan['skippedReparsePoints'] = [int]$scan['skippedReparsePoints'] + 1
              continue
            }
            if (Test-SkippedReportDirectory -Path $entry) { continue }
            if ($depth -ge $MaxDepth) { continue }
            if ($SkipPriorityRoots) {
              try { $entryKey = [IO.Path]::GetFullPath($entry).ToLowerInvariant() } catch { $entryKey = '' }
              if ($entryKey -and $priorityRoots.ContainsKey($entryKey)) { continue }
            }
            $queue.Enqueue([pscustomobject]@{ path = $entry; depth = ($depth + 1) })
            continue
          }
          $scan['scannedFiles'] = [int]$scan['scannedFiles'] + 1
          if ([IO.Path]::GetFileName($entry) -like 'FirestoneOfflineReport_*.json') {
            Add-ReportFile -Path $entry -Source $Source -Priority $Priority
          }
        }
      } catch {
        $scan['accessErrors'] = [int]$scan['accessErrors'] + 1
      } finally {
        try { if ($enumerator -is [IDisposable]) { $enumerator.Dispose() } } catch {}
      }
    }
  }

  function Scan-ReportTopLevel {
    param([string]$Folder,[string]$Source,[int]$Priority)
    if ([string]::IsNullOrWhiteSpace($Folder) -or -not (Test-ReportScanBudget)) { return }
    try {
      $folderAttributes = [IO.File]::GetAttributes($Folder)
      if (($folderAttributes -band [IO.FileAttributes]::Directory) -eq 0) { return }
      if (($folderAttributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        $scan['skippedReparsePoints'] = [int]$scan['skippedReparsePoints'] + 1
        return
      }
    } catch {
      return
    }
    $scan['scannedDirectories'] = [int]$scan['scannedDirectories'] + 1
    $enumerator = $null
    try {
      $enumerator = [IO.Directory]::EnumerateFiles($Folder, 'FirestoneOfflineReport_*.json', [IO.SearchOption]::TopDirectoryOnly).GetEnumerator()
      while (Test-ReportScanBudget) {
        $hasNext = $false
        try { $hasNext = $enumerator.MoveNext() } catch { $scan['accessErrors'] = [int]$scan['accessErrors'] + 1; break }
        if (-not $hasNext) { break }
        $scan['scannedFiles'] = [int]$scan['scannedFiles'] + 1
        Add-ReportFile -Path ([string]$enumerator.Current) -Source $Source -Priority $Priority
      }
    } catch {
      $scan['accessErrors'] = [int]$scan['accessErrors'] + 1
    } finally {
      try { if ($enumerator -is [IDisposable]) { $enumerator.Dispose() } } catch {}
    }
  }

  if ($PreferredPath) { Add-ReportFile -Path $PreferredPath -Source '命令行指定' -Priority 0 }
  $localRoot = Join-Path $env:LOCALAPPDATA 'Firestone2Green'
  $reportRoots = @(
    (Join-Path $localRoot 'FirestoneOfflineReports'),
    (Join-Path $localRoot 'scripts\FirestoneOfflineReports')
  )
  foreach ($reportRoot in $reportRoots) {
    try { $priorityRoots[[IO.Path]::GetFullPath($reportRoot).ToLowerInvariant()] = $true } catch {}
    Scan-ReportTree -Root $reportRoot -Source 'Firestone2Green 报告目录' -Priority 1 -MaxDepth 2
  }
  foreach ($folder in @((Join-Path $env:USERPROFILE 'Downloads'), [Environment]::GetFolderPath('Desktop'))) {
    Scan-ReportTopLevel -Folder $folder -Source '下载/桌面目录' -Priority 3
  }
  # Known report directories and the user's download locations are enough for
  # normal runs. Only search the broader local tree when none of them produced
  # a candidate, and keep that fallback shallow.
  if ($candidates.Count -eq 0 -and (Test-ReportScanBudget)) {
    Scan-ReportTree -Root $localRoot -Source 'Firestone2Green 本地目录' -Priority 2 -SkipPriorityRoots -MaxDepth 3
  }

  $elapsedMilliseconds = [int]((Get-Date) - $scanStarted).TotalMilliseconds
  Write-DiagLog ("报告扫描完成：目录={0}，文件={1}，候选={2}，跳过重解析点={3}，耗时={4}ms，达到限界={5}" -f $scan['scannedDirectories'],$scan['scannedFiles'],$candidates.Count,$scan['skippedReparsePoints'],$elapsedMilliseconds,$scan['scanTimedOut'])
  if ([bool]$scan['scanTimedOut']) {
    Add-Finding -Code 'REPORT_SCAN_LIMIT_REACHED' -Title '历史报告目录较大，已按安全限界结束扫描' -Cause '排查工具只在有限目录数、文件数和时间内查找最新报告，避免窗口无限卡住。' -Score 10 -Severity 'info' -Evidence @("扫描目录：$($scan['scannedDirectories'])","扫描文件：$($scan['scannedFiles'])","耗时：${elapsedMilliseconds}ms") -Solutions @('这不会影响主程序运行；如需分析指定报告，可把 JSON 放到桌面或下载目录后重新排查。')
  }

  $ordered = @($candidates | Sort-Object priority,@{Expression='lastWriteTime';Descending=$true})
  if (-not $PreferredPath) { $ordered = @($candidates | Sort-Object @{Expression='lastWriteTime';Descending=$true},priority) }
  $commonResult = [ordered]@{
    candidates = @($ordered)
    latestPath = ''
    latest = $null
    parseError = ''
    ageDays = $null
    scanTimedOut = [bool]$scan['scanTimedOut']
    scannedDirectories = [int]$scan['scannedDirectories']
    scannedFiles = [int]$scan['scannedFiles']
    skippedLargeFiles = [int]$scan['skippedLargeFiles']
    skippedReparsePoints = [int]$scan['skippedReparsePoints']
    accessErrors = [int]$scan['accessErrors']
    scanElapsedMilliseconds = $elapsedMilliseconds
  }
  if ($ordered.Count -eq 0) { return [pscustomobject]$commonResult }

  $selected = $ordered[0]
  $parsed = $null
  $parseError = ''
  try { $parsed = Get-Content -LiteralPath $selected.path -Raw -Encoding UTF8 -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop } catch { $parseError = $_.Exception.Message }
  if ($parseError) {
    Add-Finding -Code 'LATEST_REPORT_INVALID_JSON' -Title '最新运行报告不是有效 JSON' -Cause '报告写入中断、文件被截断，或文本编码/内容被其他程序改写。' -Score 49 -Severity 'warning' -Evidence @("报告：$($selected.path)","错误：$parseError") -Solutions @('重新执行一次操作并等待日志显示“完成”后再排查。','不要用记事本覆盖原报告；直接发送新生成的 JSON。')
  }
  $commonResult['latestPath'] = $selected.path
  $commonResult['latest'] = $parsed
  $commonResult['parseError'] = $parseError
  $commonResult['ageDays'] = [Math]::Round(((Get-Date) - $selected.lastWriteTime).TotalDays,2)
  return [pscustomobject]$commonResult
}
function Apply-ReportDiagnoses {
  param($Reports,$Processes)
  $report=$Reports.latest
  if($null -eq $report){return}
  $error=[string](Get-ObjectProperty $report 'error' '')
  $errorType=[string](Get-ObjectProperty $report 'errorType' '')
  $errorLine=[string](Get-ObjectProperty $report 'errorLine' '')
  $errorCommand=[string](Get-ObjectProperty $report 'errorCommand' '')
  $launchAttempts=@(Get-ObjectProperty $report 'launchAttempts' @())
  $launchResult=[string](Get-ObjectProperty $report 'launchResult' '')
  $automationAvailable=Get-ObjectProperty $report 'automationAvailable' $null
  $selectedLaunchMode=[string](Get-ObjectProperty $report 'selectedLaunchMode' '')
  $legacyCompatibilityUsed=[bool](Get-ObjectProperty $report 'legacyAutomationCompatibilityUsed' $false)
  $automationLaunchResult=[string](Get-ObjectProperty $report 'automationLaunchResult' '')
  $controlledLegacyLaunch = $legacyCompatibilityUsed -or
    ($selectedLaunchMode -match '(?i)LegacyDirectLauncher') -or
    ($automationLaunchResult -match '(?i)StartedAutomationLegacyDirect')
  $mixedAttempts=@($launchAttempts | Where-Object {
    $argsText=[string](Get-ObjectProperty $_ 'args' '')
    if ([string]::IsNullOrWhiteSpace($argsText)) { $argsText=[string](Get-ObjectProperty $_ 'arguments' '') }
    $description=[string](Get-ObjectProperty $_ 'description' '')
    $isControlledLegacy = $description -match '(?i)旧版兼容|LegacyDirect|兼容参数启动 Firestone \+ Automation' -or
      $selectedLaunchMode -match '(?i)LegacyDirectLauncher'
    if ($isControlledLegacy -or $controlledLegacyLaunch) { return $false }
    return ($argsText -match '(?i)-launchapp\s+' -and $argsText -match '(?i)--automation' -and $argsText -match '(?i)--enable-automation')
  })
  if($mixedAttempts.Count -gt 0 -and -not $controlledLegacyLaunch){
    Add-Finding -Code 'AUTOMATION_MIXED_LAUNCH_ARGUMENTS' -Title '旧版把 automation 参数混入 Firestone 启动命令' -Cause '检测到未受控的旧式混合启动：把 --automation 固定绑在 -launchapp 上，且缺少 pingServer 验证与有限备用端口。v0.2.8 仅在默认端口空闲时把该兼容路径作为受控首选，并在验证失败后继续有限回退。' -Score 100 -Severity 'critical' -Blocking $true -Evidence @(("最新报告：{0}" -f $Reports.latestPath),@($mixedAttempts | ForEach-Object {"启动器：$(Get-ObjectProperty $_ 'launcher' '');参数：$(([string](Get-ObjectProperty $_ 'args' '')) + ([string](Get-ObjectProperty $_ 'arguments' '')))"}),$(if($error){"原错误：$error"}else{''})) -Solutions @('安装包含“兼容启动 + 有限备用端口”修复的最新版。','已安装持续修复时，新版“一键处理”会自动刷新旧监听器。','再执行“一键处理”；报告中应出现 effectiveAutomationPort，或至少为 LaunchOnlyDegraded 而非致命退出。')
  }
  if($error -match '(?i)HOSTS_(WRITE_VERIFY_FAILED|PROTECTION_ACTIVE|FILE_BUSY|CREATE_FAILED)|hosts 写入后校验失败|hosts 更新失败'){
    # Final classification depends on the current hosts/firewall snapshot.
  } elseif($error -match '(?i)automation.*(不可用|未在|timeout|超时)' -and $mixedAttempts.Count -eq 0){
    $portState=$Processes.automation.status
    $evidence=@("最新报告：$($Reports.latestPath)","错误：$error","当前 18765 状态：$portState")
    if($errorLine){$evidence+="错误行：$errorLine"}
    Add-Finding -Code 'AUTOMATION_PORT_CLOSED' -Title 'Firestone 启动流程未获得 automation 接口' -Cause 'Firestone 可能没有通过修复工具启动、Overwolf 启动过慢、runtime 参数未生效，或本机接口被安全策略拦截。' -Score 79 -Severity 'error' -Evidence $evidence -Solutions @('直接用最新版 Firestone2Green 的“一键处理”重试；程序会重新探测 18765-18770，不会结束端口占用进程。','如果 Firestone 能正常打开但报告为 LaunchOnlyDegraded，可继续使用；这表示只跳过本次运行时补授权，不应退出码 1。','如果 Firestone 根本没打开，检查报告中的 launcher 路径和启动参数。')
  }
  if($launchResult -eq 'LaunchOnlyDegraded'){
    Add-Finding -Code 'AUTOMATION_DEGRADED_NOT_FATAL' -Title 'Firestone 已普通启动，automation 本次不可用' -Cause '新版已将这种情况降级为非致命状态；程序可继续打开，但本次运行时补授权被跳过。' -Score 36 -Severity 'warning' -Evidence @("launchResult=$launchResult","automationAvailable=$automationAvailable") -Solutions @('先确认 Firestone 主界面能否正常使用。','需要补授权时，再用“一键处理”重试；程序会重新探测可用 Automation 端口。')
  }
  if($error -and -not $Script:FindingCodes.ContainsKey('AUTOMATION_MIXED_LAUNCH_ARGUMENTS') -and -not $Script:FindingCodes.ContainsKey('AUTOMATION_PORT_CLOSED')){
    $evidence=@("最新报告：$($Reports.latestPath)","错误：$error")
    if($errorType){$evidence+="类型：$errorType"}; if($errorLine){$evidence+="行：$errorLine"}; if($errorCommand){$evidence+="命令：$errorCommand"}
    Add-Finding -Code 'LATEST_REPORT_ERROR' -Title '最新 Firestone2Green 运行报告记录了异常' -Cause '排查工具未把该异常归入更具体的已知模式，需要结合报告中的原始错误继续定位。' -Score 67 -Severity 'error' -Evidence $evidence -Solutions @('先按本报告“最可能原因”和其他高优先级问题处理。','处理后重新运行一次；若仍存在，提交本诊断 JSON 与最新 FirestoneOfflineReport JSON。')
  }
}

function Apply-CombinedDiagnoses {
  param($System,$Hosts,$Firewall,$Reports,$Processes,$Persistence,$RepairScripts)
  $report=$Reports.latest
  $reportError=if($report){[string](Get-ObjectProperty $report 'error' '')}else{''}
  $reportNetworkMode=if($report){[string](Get-ObjectProperty $report 'networkMode' '')}else{''}
  $hostsFailure=$reportError -match '(?i)HOSTS_(WRITE_VERIFY_FAILED|PROTECTION_ACTIVE|FILE_BUSY|CREATE_FAILED)|hosts 写入后校验失败|hosts 更新失败'
  $protected=([bool]$Hosts.protectedDomainBlocked -or [bool]$Firewall.exactProtectionActive)
  if($hostsFailure){
    $securityNames=@($System.securitySoftware | ForEach-Object {$_.name} | Sort-Object -Unique)
    $securityText=if($securityNames.Count){$securityNames -join '、'}else{'未识别到常见安全软件进程'}
    if($Firewall.exactProtectionActive){
      Add-Finding -Code 'HOSTS_PROTECTED_FIREWALL_OK' -Title 'hosts 被保护，但精确域名防火墙兜底已生效' -Cause '安全软件或系统策略阻止修改 hosts；这本身不再需要用户关闭保护，因为 Firestone2Green 已有精确域名防火墙规则接管。' -Score 45 -Severity 'warning' -Evidence @("hosts：$($Hosts.path)","检测到：$securityText","有效精确域名规则：$(@($Firewall.activeExactRules).Count)","原错误：$reportError") -Solutions @('保留安全软件的 Hosts 保护，不要给 hosts 文件添加 Everyone 完全控制权限。','使用 v0.2.8 或更高版本；新版在 hosts 受保护时会自动复用/创建防火墙兜底。','如果主程序仍把此情况当退出码 1，说明仍在运行旧脚本：移除并重装持续修复。')
    } elseif($Hosts.protectedDomainBlocked){
      Add-Finding -Code 'HOSTS_PROTECTED_EXISTING_ENTRY_OK' -Title 'hosts 写入被保护，但所需精确条目已经存在' -Cause '安全软件阻止重复写入，不过当前 hosts 已包含正确阻断条目。' -Score 40 -Severity 'warning' -Evidence @("hosts：$($Hosts.path)","精确阻断条目数量：$($Hosts.blockingEntryCount)","原错误：$reportError") -Solutions @('不要修改 hosts ACL；使用新版让程序识别并复用已有条目。','如果仍报错，移除并重新安装持续修复，避免旧脚本每次强制重写。')
    } else {
      Add-Finding -Code 'HOSTS_AND_FIREWALL_BLOCKED' -Title 'hosts 写入失败，且没有有效防火墙兜底' -Cause '安全软件/系统策略阻止 hosts，同时 Firestone2Green 精确域名防火墙规则也未生效，当前保护链不完整。' -Score 95 -Severity 'critical' -Blocking $true -Evidence @("hosts：$($Hosts.path)","检测到：$securityText","精确防火墙规则：$(@($Firewall.activeExactRules).Count)","原错误：$reportError") -Solutions @('先升级到 v0.2.8 或更高版本并以管理员运行。','先“恢复网络”，再执行“一键处理”，让程序自动建立精确域名防火墙兜底。','不要关闭整套防病毒；如果防火墙创建也被企业策略禁止，只需把 Firestone2Green 的精确出站阻断规则加入允许管理范围。')
    }
  }
  $hasPriorRun=($null -ne $report)
  if($hasPriorRun -and $reportNetworkMode -in @('AuthOnlyOnline','DataOnline') -and -not $protected){
    Add-Finding -Code 'UNPROTECTED_RUNTIME' -Title '报告要求精确保护，但当前 hosts 和防火墙都未生效' -Cause '精确保护条目可能被安全软件还原、被清理工具删除，或旧版在失败后没有成功建立兜底。' -Score 89 -Severity 'critical' -Blocking $true -Evidence @("报告 networkMode=$reportNetworkMode","hosts 精确条目=$($Hosts.blockingEntryCount)","有效精确防火墙规则=$(@($Firewall.activeExactRules).Count)") -Solutions @('使用最新版以管理员运行“一键处理”。','确认日志最终不是 UnprotectedRuntime。','如果持续化已安装，移除并重装，让任务使用新脚本。')
  }
  if($Processes.firestoneProcesses.Count -gt 0 -and $Processes.automation.status -eq 'closed' -and $reportError -match '(?i)automation'){
    Add-Finding -Code 'FIRESTONE_RUNNING_WITHOUT_AUTOMATION' -Title 'Firestone 正在运行，但默认 Automation 端口未监听' -Cause '最新报告同时表明 Automation 不可用；最新版会自动有限检查 18765-18770，并在全部失败时安全进入仅启动降级模式。' -Score 68 -Severity 'warning' -Evidence @("Firestone 相关进程：$($Processes.firestoneProcesses.Count)",'默认端口 18765 无监听') -Solutions @('从最新版 Firestone2Green 执行“一键重启并授权”，程序会自动选择端口。','如果使用持续修复，可继续使用程序生成的 Firestone 快捷方式；一键流程会自动刷新旧监听器。')
  }
}

function Format-FindingText {
  param($Finding,[string]$Prefix='')
  $lines=New-Object System.Collections.Generic.List[string]
  $lines.Add("$Prefix[$($Finding.code)] $($Finding.title)")
  $lines.Add("原因：$($Finding.cause)")
  if(@($Finding.evidence).Count -gt 0){$lines.Add('证据：');foreach($item in @($Finding.evidence)){$lines.Add("  - $item")}}
  if(@($Finding.solutions).Count -gt 0){$lines.Add('解决方法：');$index=0;foreach($item in @($Finding.solutions)){$index++;$lines.Add("  $index. $item")}}
  return @($lines)
}

function Write-DiagnosticReports {
  param($Snapshot)
  if([string]::IsNullOrWhiteSpace($OutputDirectory)){$script:OutputDirectory=Join-Path $env:LOCALAPPDATA 'Firestone2Green\Diagnostics'}
  try{[IO.Directory]::CreateDirectory($OutputDirectory)|Out-Null}catch{$script:OutputDirectory=Join-Path $env:TEMP 'Firestone2Green-Diagnostics';[IO.Directory]::CreateDirectory($OutputDirectory)|Out-Null}
  if([string]::IsNullOrWhiteSpace($RunId)){$script:RunId=(Get-Date).ToString('yyyyMMdd_HHmmss_fff')}
  $safeRunId=($RunId -replace '[^A-Za-z0-9_-]','_')
  $baseName="Firestone2Green_Diagnostic_$safeRunId"
  $jsonPath=Join-Path $OutputDirectory ($baseName+'.json')
  $textPath=Join-Path $OutputDirectory ($baseName+'.txt')
  $latestJson=Join-Path $OutputDirectory 'LatestDiagnostic.json'
  $latestText=Join-Path $OutputDirectory 'LatestDiagnostic.txt'

  $findings=@($Snapshot.findings)
  $primary=$Snapshot.primary
  $text=New-Object System.Collections.Generic.List[string]
  $text.Add('Firestone2Green 问题排查结果')
  $text.Add(('='*58))
  $text.Add("排查工具版本：$($Script:ToolVersion)")
  $text.Add("时间：$($Snapshot.finishedAt)")
  $snapshotSystem=Get-ObjectProperty $Snapshot 'system' $null
  $computer=if($snapshotSystem){[string](Get-ObjectProperty $snapshotSystem 'computer' '未知')}else{'未知'}
  $user=if($snapshotSystem){[string](Get-ObjectProperty $snapshotSystem 'user' '未知')}else{'未知'}
  $elevated=if($snapshotSystem){[bool](Get-ObjectProperty $snapshotSystem 'elevated' $false)}else{$false}
  $text.Add("电脑/用户：$computer / $user")
  $text.Add("管理员：$(if($elevated){'是'}else{'否'})")
  $text.Add('本工具只读取状态，不会修改 hosts、防火墙、计划任务或 Firestone 文件。')
  $text.Add('')
  $text.Add('【最可能原因】')
  foreach($line in @(Format-FindingText $primary)){$text.Add($line)}
  $secondary=@($findings | Where-Object {$_.code -ne $primary.code})
  if($secondary.Count -gt 0){
    $text.Add('');$text.Add('【其他发现】')
    foreach($finding in $secondary){$text.Add('');foreach($line in @(Format-FindingText $finding)){$text.Add($line)}}
  }
  $text.Add('');$text.Add('【关键状态】')
  $snapshotOverwolf=Get-ObjectProperty $Snapshot 'overwolf' $null
  $snapshotFirestone=Get-ObjectProperty $Snapshot 'firestone' $null
  $snapshotProcesses=Get-ObjectProperty $Snapshot 'processes' $null
  $snapshotHosts=Get-ObjectProperty $Snapshot 'hosts' $null
  $snapshotFirewall=Get-ObjectProperty $Snapshot 'firewall' $null
  $snapshotReports=Get-ObjectProperty $Snapshot 'reports' $null
  $overwolfRoot=if($snapshotOverwolf){[string](Get-ObjectProperty $snapshotOverwolf 'selectedRoot' '')}else{''}
  $firestoneVersion=if($snapshotFirestone){[string](Get-ObjectProperty $snapshotFirestone 'selectedVersion' '')}else{''}
  $firestoneIntegrity=if($snapshotFirestone){Get-ObjectProperty $snapshotFirestone 'integrity' $null}else{$null}
  $automation=if($snapshotProcesses){Get-ObjectProperty $snapshotProcesses 'automation' $null}else{$null}
  $hostsEntryCount=if($snapshotHosts){[int](Get-ObjectProperty $snapshotHosts 'blockingEntryCount' 0)}else{0}
  $activeExactRules=if($snapshotFirewall){@(Get-ObjectProperty $snapshotFirewall 'activeExactRules' @())}else{@()}
  $latestReportPath=if($snapshotReports){[string](Get-ObjectProperty $snapshotReports 'latestPath' '')}else{''}
  $text.Add("Overwolf 根目录：$(if($overwolfRoot){$overwolfRoot}else{'未完成检查'})")
  $text.Add("Firestone 版本：$(if($firestoneVersion){$firestoneVersion}else{'未完成检查'})")
  $text.Add("签名完整：$(if($firestoneIntegrity){Get-ObjectProperty $firestoneIntegrity 'ok' '未知'}else{'未知'})")
  $text.Add("默认 automation 18765：$(if($automation){Get-ObjectProperty $automation 'status' '未知'}else{'未知'})")
  $text.Add("hosts 精确条目：$hostsEntryCount")
  $text.Add("精确防火墙规则：$(@($activeExactRules).Count)")
  $text.Add("最新原始报告：$(if($latestReportPath){$latestReportPath}else{'未找到或未完成检查'})")
  $text.Add('');$text.Add('【详细检查日志】');foreach($line in @($Snapshot.log)){$text.Add($line)}
  $text.Add('');$text.Add("完整 JSON：$jsonPath")

  $utf8=New-Object Text.UTF8Encoding($true)
  $json=$Snapshot | ConvertTo-Json -Depth 14
  [IO.File]::WriteAllText($jsonPath,$json,$utf8)
  [IO.File]::WriteAllText($textPath,($text -join [Environment]::NewLine),$utf8)
  [IO.File]::WriteAllText($latestJson,$json,$utf8)
  [IO.File]::WriteAllText($latestText,($text -join [Environment]::NewLine),$utf8)
  return [pscustomobject]@{jsonPath=$jsonPath;textPath=$textPath;directory=$OutputDirectory}
}

try {
  Write-DiagLog "开始 Firestone2Green 全面只读排查，工具版本 $($Script:ToolVersion)"
  $system=Get-SystemSnapshot
  $overwolf=Get-OverwolfSnapshot
  $firestone=Get-FirestoneExtensionSnapshot
  $processes=Get-ProcessAndAutomationSnapshot
  $hosts=Get-HostsSnapshot
  $firewall=Get-FirewallSnapshot $overwolf
  $network=Get-NetworkSnapshot
  $persistence=Get-PersistenceSnapshot
  $repairScripts=@(Get-LocalRepairScriptSnapshot)
  $reports=Get-ReportSnapshot $ReportPath
  Apply-ReportDiagnoses $reports $processes
  Apply-CombinedDiagnoses $system $hosts $firewall $reports $processes $persistence $repairScripts

  $actionableFindings=@($Script:Findings | Where-Object { $_.severity -in @('critical','error','warning') })
  if($actionableFindings.Count -eq 0){
    Add-Finding -Code 'NO_PROBLEM_DETECTED' -Title '未发现明确故障' -Cause 'Overwolf、Firestone 和本机修复链路的已检查项目中，没有发现足以确定为故障主因的异常。' -Score 0 -Severity 'ok' -Evidence @('普通信息项仍保留在“其他发现”中，便于人工核对。') -Solutions @('如果问题仍能复现，请在复现后立即重新排查，并把新生成的 TXT 或 JSON 报告发给维护者。')
  }
  $allSorted=@($Script:Findings | Sort-Object @{Expression='score';Descending=$true},@{Expression='code';Descending=$false})
  $primaryCandidates=@($allSorted | Where-Object { $_.severity -in @('critical','error','warning') })
  $primary=if($primaryCandidates.Count -gt 0){$primaryCandidates[0]}else{@($allSorted | Where-Object {$_.code -eq 'NO_PROBLEM_DETECTED'})[0]}
  $orderedFindings=@($primary)+@($allSorted | Where-Object {$_.code -ne $primary.code})
  $snapshot=[pscustomobject][ordered]@{
    schemaVersion=1
    tool='Firestone2Green Diagnostics'
    toolVersion=$Script:ToolVersion
    startedAt=$Script:StartedAt.ToString('o')
    finishedAt=(Get-Date).ToString('o')
    readOnly=$true
    primary=$primary
    findings=$orderedFindings
    system=$system
    overwolf=$overwolf
    firestone=$firestone
    processes=$processes
    hosts=$hosts
    firewall=$firewall
    network=$network
    persistence=$persistence
    repairScripts=$repairScripts
    reports=$reports
    log=@($Script:Log)
  }
  $paths=Write-DiagnosticReports $snapshot
  Write-Output "F2G_DIAG_PRIMARY_CODE=$($primary.code)"
  Write-Output "F2G_DIAG_PRIMARY_TITLE=$($primary.title)"
  Write-Output "F2G_DIAG_TEXT=$($paths.textPath)"
  Write-Output "F2G_DIAG_JSON=$($paths.jsonPath)"
  exit 0
} catch {
  $fatal=$_.Exception.Message
  Write-Host "F2G_DIAG_FATAL=$fatal"
  Write-Host "F2G_DIAG_FATAL_STACK=$($_.ScriptStackTrace)"
  try{
    Add-Finding -Code 'DIAGNOSTIC_TOOL_FAILURE' -Title '排查工具自身未能完成全部检查' -Cause '某个系统查询返回了未预期错误。' -Score 99 -Severity 'critical' -Blocking $true -Evidence @($fatal,$_.ScriptStackTrace) -Solutions @('点击“管理员重新排查”。','如果仍失败，发送窗口中的错误文本。')
    $orderedFindings=@($Script:Findings | Sort-Object @{Expression='score';Descending=$true})
    $snapshot=[pscustomobject][ordered]@{schemaVersion=1;tool='Firestone2Green Diagnostics';toolVersion=$Script:ToolVersion;startedAt=$Script:StartedAt.ToString('o');finishedAt=(Get-Date).ToString('o');readOnly=$true;primary=$orderedFindings[0];findings=$orderedFindings;fatalError=$fatal;fatalStack=$_.ScriptStackTrace;log=@($Script:Log)}
    $paths=Write-DiagnosticReports $snapshot
    Write-Output "F2G_DIAG_PRIMARY_CODE=DIAGNOSTIC_TOOL_FAILURE"
    Write-Output "F2G_DIAG_PRIMARY_TITLE=排查工具自身未能完成全部检查"
    Write-Output "F2G_DIAG_TEXT=$($paths.textPath)"
    Write-Output "F2G_DIAG_JSON=$($paths.jsonPath)"
  }catch{Write-Error "诊断失败且无法写入报告：$fatal；$($_.Exception.Message)"}
  exit 1
}
