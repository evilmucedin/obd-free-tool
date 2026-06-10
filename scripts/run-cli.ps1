#!/usr/bin/env pwsh
# Compile and run the console CLI in one step (Windows / cross-platform PowerShell).
# Any arguments are forwarded to the app.
# Usage: ./scripts/run-cli.ps1 [<app args>]
#   e.g. ./scripts/run-cli.ps1 --help
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$AppArgs
)
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "==> Building & running ObdFree.Cli..."
dotnet run --project src/ObdFree.Cli -- @AppArgs
