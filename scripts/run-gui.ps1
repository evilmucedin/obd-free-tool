#!/usr/bin/env pwsh
# Compile and launch the desktop GUI (Windows / cross-platform PowerShell).
# Usage: ./scripts/run-gui.ps1
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "==> Building & launching ObdFree.Gui..."
dotnet run --project src/ObdFree.Gui
