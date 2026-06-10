#!/usr/bin/env pwsh
# Build the whole solution (Windows / cross-platform PowerShell).
# Usage: ./scripts/build.ps1 [-Configuration Release]
param(
    [string]$Configuration = "Release"
)
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "==> Restoring..."
dotnet restore

Write-Host "==> Building ($Configuration)..."
dotnet build --no-restore --configuration $Configuration

Write-Host "==> Build complete."
