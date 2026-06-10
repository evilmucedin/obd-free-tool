#!/usr/bin/env bash
# Build the whole solution (Linux / macOS).
# Usage: ./scripts/build.sh [Debug|Release]   (default: Release)
set -euo pipefail

CONFIG="${1:-Release}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "==> Restoring..."
dotnet restore

echo "==> Building ($CONFIG)..."
dotnet build --no-restore --configuration "$CONFIG"

echo "==> Build complete."
