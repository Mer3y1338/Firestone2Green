[CmdletBinding()]
param(
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot '..\dist\Firestone2Green问题排查.exe'
}
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sourceDirectory = Join-Path $projectRoot 'tools\Firestone2Green.Diagnostics'
$programPath = Join-Path $sourceDirectory 'Program.cs'
$diagnosticScriptPath = Join-Path $sourceDirectory 'Firestone2Green.Diagnostics.ps1'
$manifestPath = Join-Path $sourceDirectory 'app.manifest'
$iconPath = Join-Path $projectRoot 'assets\app.ico'

$cscCandidates = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
)
$cscPath = $cscCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (-not $cscPath) {
    throw '未找到 Windows 自带的 .NET Framework C# 编译器 csc.exe。'
}

foreach ($requiredPath in @($programPath, $diagnosticScriptPath, $manifestPath, $iconPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "构建所需文件不存在：$requiredPath"
    }
}

$resolvedOutputPath = [IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $resolvedOutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$arguments = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    '/optimize+',
    '/warn:4',
    "/out:$resolvedOutputPath",
    "/win32manifest:$manifestPath",
    "/win32icon:$iconPath",
    "/resource:$diagnosticScriptPath,Firestone2Green.Diagnostics.ps1",
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll',
    $programPath
)

& $cscPath @arguments
if ($LASTEXITCODE -ne 0) {
    throw "诊断工具编译失败，csc.exe 退出码：$LASTEXITCODE"
}

$file = Get-Item -LiteralPath $resolvedOutputPath
$hash = Get-FileHash -LiteralPath $resolvedOutputPath -Algorithm SHA256
Write-Host "已生成：$($file.FullName)"
Write-Host ("大小：{0:N0} bytes" -f $file.Length)
Write-Host "SHA256：$($hash.Hash)"
