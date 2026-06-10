#!/usr/bin/env pwsh
# Publish a self-contained, single-file desktop GUI for a target platform.
# (Run on Windows or any cross-platform PowerShell host.)
#
# Usage: ./scripts/publish-gui.ps1 [-Rid <rid>]
#   Rid is a .NET Runtime Identifier. If omitted, the host platform is detected.
#   Supported: win-x64 win-arm64 linux-x64 linux-arm64 osx-x64 osx-arm64
#
# Output: ./artifacts/gui/<rid>/
param(
    [string]$Rid
)
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if (-not $Rid) {
    $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
    $arch = if ($arch -eq "x64") { "x64" } elseif ($arch -in @("arm64")) { "arm64" } else { $arch }
    if ($IsWindows) { $os = "win" }
    elseif ($IsLinux) { $os = "linux" }
    elseif ($IsMacOS) { $os = "osx" }
    else { throw "Unsupported host OS." }
    $Rid = "$os-$arch"
}

$out = "artifacts/gui/$Rid"

Write-Host "==> Publishing ObdFree.Gui for $Rid -> $out"
dotnet publish src/ObdFree.Gui `
    --configuration Release `
    --runtime $Rid `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --output $out

Write-Host "==> Done. App is in $out (look for 'ObdFree.Gui' or 'ObdFree.Gui.exe')."
