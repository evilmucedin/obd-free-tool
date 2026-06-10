#!/usr/bin/env bash
# Run the full test suite with coverage (Linux / macOS).
# Usage: ./scripts/test.sh [Debug|Release]   (default: Release)
set -euo pipefail

CONFIG="${1:-Release}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "==> Running tests ($CONFIG)..."
dotnet test --configuration "$CONFIG" \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults

echo "==> Tests complete. Coverage in ./TestResults."
