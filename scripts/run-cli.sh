#!/usr/bin/env bash
# Compile and run the console CLI in one step (Linux / macOS).
# Any arguments after the script are forwarded to the app.
# Usage: ./scripts/run-cli.sh [-- <app args>]
#   e.g. ./scripts/run-cli.sh --help
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "==> Building & running ObdFree.Cli..."
dotnet run --project src/ObdFree.Cli -- "$@"
