#!/usr/bin/env bash
# Publish a self-contained, single-file desktop GUI for a target platform.
# (Run on Linux / macOS.)
#
# Usage: ./scripts/publish-gui.sh [RID]
#   RID is a .NET Runtime Identifier. If omitted, the host platform is detected.
#   Supported: linux-x64 linux-arm64 osx-x64 osx-arm64 win-x64 win-arm64
#
# Output: ./artifacts/gui/<RID>/
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

detect_rid() {
    local os arch
    case "$(uname -s)" in
        Linux)  os="linux" ;;
        Darwin) os="osx" ;;
        *)      echo "Unsupported host OS: $(uname -s)" >&2; exit 1 ;;
    esac
    case "$(uname -m)" in
        x86_64|amd64)  arch="x64" ;;
        arm64|aarch64) arch="arm64" ;;
        *)             echo "Unsupported host arch: $(uname -m)" >&2; exit 1 ;;
    esac
    echo "${os}-${arch}"
}

RID="${1:-$(detect_rid)}"
OUT="artifacts/gui/${RID}"

echo "==> Publishing ObdFree.Gui for ${RID} -> ${OUT}"
dotnet publish src/ObdFree.Gui \
    --configuration Release \
    --runtime "$RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    --output "$OUT"

echo "==> Done. App is in ${OUT} (look for 'ObdFree.Gui' or 'ObdFree.Gui.exe')."
