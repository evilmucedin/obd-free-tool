#!/usr/bin/env bash
# Compile and launch the desktop GUI (Linux / macOS).
# Usage: ./scripts/run-gui.sh
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "==> Building & launching ObdFree.Gui..."
dotnet run --project src/ObdFree.Gui
