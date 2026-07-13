[CmdletBinding()]
param(
    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'
$repo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $repo 'src\Firestone2Green.Wpf\Firestone2Green.Wpf.csproj'
$bootstrapperSource = Join-Path $repo 'src\Firestone2Green.Bootstrapper\Program.cs'
$icon = Join-Path $repo 'assets\app.ico'
$bootstrapperManifest = Join-Path $repo 'src\Firestone2Green.Bootstrapper\app.manifest'
$dotnet = 'C:\Users\15206\.dotnet\dotnet.exe'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$corePublish = Join-Path $repo 'tmp\wpf-framework-dependent'
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $repo 'tmp\wpf-release' }
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
$allowedRoot = [System.IO.Path]::GetFullPath((Join-Path $repo 'tmp')) + [System.IO.Path]::DirectorySeparatorChar

if (-not (Test-Path -LiteralPath $dotnet)) { throw "找不到 .NET SDK: $dotnet" }
if (-not (Test-Path -LiteralPath $csc)) { throw "找不到 .NET Framework C# 编译器: $csc" }
if (-not (Test-Path -LiteralPath $bootstrapperManifest)) { throw "找不到启动器清单: $bootstrapperManifest" }
if (-not $output.StartsWith($allowedRoot, [System.StringComparison]::OrdinalIgnoreCase)) { throw "输出目录必须位于 tmp 下: $output" }

foreach ($dir in @($corePublish, $output)) {
    $full = [System.IO.Path]::GetFullPath($dir)
    if (-not $full.StartsWith($allowedRoot, [System.StringComparison]::OrdinalIgnoreCase)) { throw "拒绝清理非 tmp 路径: $full" }
    if (Test-Path -LiteralPath $full) { Remove-Item -LiteralPath $full -Recurse -Force }
    New-Item -ItemType Directory -Path $full -Force | Out-Null
}

$env:HTTP_PROXY = 'http://127.0.0.1:7897'
$env:HTTPS_PROXY = 'http://127.0.0.1:7897'
$env:ALL_PROXY = 'http://127.0.0.1:7897'

& $dotnet publish $project -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true -p:DebugType=embedded -o $corePublish --nologo
if ($LASTEXITCODE -ne 0) { throw "WPF 主程序发布失败: $LASTEXITCODE" }

$payload = Join-Path $corePublish 'Firestone2Green.Wpf.exe'
if (-not (Test-Path -LiteralPath $payload)) { throw "找不到 WPF 单文件主程序: $payload" }

$finalExe = Join-Path $output 'Firestone2Green_v0.2.5.exe'
$cscArgs = @(
    '/nologo',
    '/target:winexe',
    '/platform:x64',
    '/optimize+',
    "/out:$finalExe",
    "/win32icon:$icon",
    "/win32manifest:$bootstrapperManifest",
    '/reference:System.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll',
    "/resource:$payload,Firestone2Green.Wpf.exe",
    $bootstrapperSource
)
& $csc @cscArgs
if ($LASTEXITCODE -ne 0) { throw "启动器编译失败: $LASTEXITCODE" }

$files = @(Get-ChildItem -LiteralPath $output -File)
if ($files.Count -ne 1 -or $files[0].Name -ne 'Firestone2Green_v0.2.5.exe') {
    throw "最终输出不是单 EXE: $($files.Name -join ', ')"
}

$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($finalExe)
[pscustomobject]@{
    Output = $finalExe
    SizeMiB = [Math]::Round($files[0].Length / 1MB, 2)
    FileVersion = $version.FileVersion
    ProductVersion = $version.ProductVersion
    SHA256 = (Get-FileHash -LiteralPath $finalExe -Algorithm SHA256).Hash
} | Format-List
