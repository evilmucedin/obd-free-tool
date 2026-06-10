#!/usr/bin/env pwsh
# Run the full test suite with coverage (Windows / cross-platform PowerShell).
# Usage: ./scripts/test.ps1 [-Configuration Release]
param(
    [string]$Configuration = "Release"
)
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "==> Running tests ($Configuration)..."
dotnet test --configuration $Configuration `
    --collect:"XPlat Code Coverage" `
    --results-directory ./TestResults

Write-Host "==> Tests complete. Coverage in ./TestResults."
