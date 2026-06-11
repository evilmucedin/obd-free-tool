#!/usr/bin/env pwsh
# One-shot setup for Windows: install dependencies, build, and run obd-free-tool.
#
# Installs the .NET 10 SDK (and Git, if missing) via winget, then builds the
# whole solution and launches the app. Mirrors scripts/setup-ubuntu.sh.
#
# Usage:
#   ./scripts/setup-windows.ps1                 # install deps, build, run the CLI (--help)
#   ./scripts/setup-windows.ps1 -App gui        # ... and launch the desktop GUI instead
#   ./scripts/setup-windows.ps1 -App none       # install deps + build only, don't run
#   ./scripts/setup-windows.ps1 -SkipInstall    # skip dependency install (just build & run)
#   ./scripts/setup-windows.ps1 -Configuration Debug
#   ./scripts/setup-windows.ps1 -App cli -- status --usb COM3 --make toyota
#
# Re-runnable (idempotent): existing, up-to-date dependencies are left alone.
param(
    [ValidateSet("cli", "gui", "none")]
    [string]$App = "cli",
    [string]$Configuration = "Release",
    [switch]$SkipInstall,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$AppArgs
)
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

# .NET SDK feature band we target (kept in sync with global.json).
$DotnetWingetId = "Microsoft.DotNet.SDK.10"

function Test-Command([string]$Name) {
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function Install-WithWinget([string]$Id, [string]$DisplayName) {
    if (-not (Test-Command winget)) {
        Write-Warning "winget is not available, so '$DisplayName' can't be installed automatically."
        Write-Warning "Install it from https://dotnet.microsoft.com/download (or 'App Installer' from the Microsoft Store), then re-run."
        return $false
    }
    Write-Host "==> Installing $DisplayName via winget ($Id)..."
    winget install --id $Id --exact --silent `
        --accept-source-agreements --accept-package-agreements
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "winget exited with code $LASTEXITCODE while installing $DisplayName."
        return $false
    }
    return $true
}

function Update-PathFromMachine {
    # Pick up tools installed in this session without requiring a new shell.
    $machine = [Environment]::GetEnvironmentVariable("Path", "Machine")
    $user = [Environment]::GetEnvironmentVariable("Path", "User")
    $env:Path = @($machine, $user, "$env:ProgramFiles\dotnet") -join ";"
}

if (-not $SkipInstall) {
    if (-not (Test-Command winget)) {
        Write-Warning "winget (the Windows Package Manager) was not found."
        Write-Warning "Get it via 'App Installer' in the Microsoft Store, or install the .NET 10 SDK"
        Write-Warning "manually from https://dotnet.microsoft.com/download and re-run with -SkipInstall."
    }

    # .NET SDK: install if missing or older than 10.x.
    $needsDotnet = $true
    if (Test-Command dotnet) {
        $sdks = & dotnet --list-sdks 2>$null
        if ($sdks -match '^\s*10\.') {
            Write-Host "==> .NET 10 SDK already installed; skipping."
            $needsDotnet = $false
        }
    }
    if ($needsDotnet) {
        [void](Install-WithWinget $DotnetWingetId ".NET 10 SDK")
        Update-PathFromMachine
    }

    # Git is handy for contributing; install only if missing.
    if (-not (Test-Command git)) {
        [void](Install-WithWinget "Git.Git" "Git")
        Update-PathFromMachine
    }

    Write-Host ""
    Write-Host "==> Note on OBD-II adapters:"
    Write-Host "      USB ELM327 dongles need their serial driver (e.g. CH340/CP210x/FTDI)."
    Write-Host "      Windows Update usually supplies these; otherwise install the vendor driver."
    Write-Host "      Bluetooth (RFCOMM) adapters: pair via Windows Settings, then use the COM port."
    Write-Host ""
}

if (-not (Test-Command dotnet)) {
    Write-Error "'dotnet' is not on PATH. Open a new terminal (so PATH refreshes) and re-run, or install the .NET 10 SDK manually."
    exit 1
}

Write-Host "==> .NET SDK in use:"
dotnet --version

Write-Host "==> Restoring & building ($Configuration)..."
dotnet build ObdFree.slnx --configuration $Configuration

switch ($App) {
    "cli" {
        # Default to --help so a bare setup run shows something useful.
        if (-not $AppArgs -or $AppArgs.Count -eq 0) { $AppArgs = @("--help") }
        Write-Host "==> Running the console CLI..."
        dotnet run --project src/ObdFree.Cli --configuration $Configuration --no-build -- @AppArgs
    }
    "gui" {
        Write-Host "==> Launching the desktop GUI..."
        dotnet run --project src/ObdFree.Gui --configuration $Configuration --no-build
    }
    "none" {
        Write-Host "==> Build complete. Run the app with:"
        Write-Host "      ./scripts/run-cli.ps1 --help"
        Write-Host "      ./scripts/run-gui.ps1"
    }
}
