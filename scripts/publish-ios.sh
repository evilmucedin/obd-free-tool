#!/usr/bin/env bash
# Prepare the iOS app for the Apple App Store: preflight checks, then build a
# signed .ipa archive ready to upload to App Store Connect.
#
# REQUIREMENTS (all Apple-imposed — none can be worked around):
#   - macOS with Xcode installed (`xcode-select -p` must succeed).
#   - .NET iOS workload:  dotnet workload install ios
#   - A paid Apple Developer Program membership ($99/year).
#   - A Distribution signing certificate + an App Store provisioning profile
#     for the bundle id 'io.github.evilmucedin.obdfree'.
#
# SIGNING (set via environment variables for an upload-ready build):
#   APPLE_TEAM_ID        e.g. ABCDE12345
#   CODESIGN_KEY         e.g. "Apple Distribution: Your Name (ABCDE12345)"
#   PROVISIONING_PROFILE provisioning profile name or UUID
#
# Without those, the script does an UNSIGNED build to validate compilation only.
#
# Usage: ./scripts/publish-ios.sh
# Output: ./artifacts/ios/
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

PROJECT="src/ObdFree.iOS/ObdFree.iOS.csproj"
OUT="artifacts/ios"

# --- Preflight ----------------------------------------------------------------
if [ "$(uname -s)" != "Darwin" ]; then
    echo "ERROR: iOS apps can only be built on macOS." >&2
    exit 1
fi

if ! xcode-select -p >/dev/null 2>&1; then
    echo "ERROR: Xcode is not installed. Install it from the App Store, then run:" >&2
    echo "       sudo xcode-select --switch /Applications/Xcode.app" >&2
    exit 1
fi

if ! dotnet workload list 2>/dev/null | grep -qi '^ios'; then
    echo "ERROR: the .NET iOS workload is not installed. Install it with:" >&2
    echo "       dotnet workload install ios" >&2
    exit 1
fi

# --- Build / archive ----------------------------------------------------------
COMMON_ARGS=(
    "$PROJECT"
    --configuration Release
    --framework net10.0-ios
    --runtime ios-arm64
    --output "$OUT"
    -p:ArchiveOnBuild=true
)

if [ -n "${CODESIGN_KEY:-}" ] && [ -n "${PROVISIONING_PROFILE:-}" ]; then
    echo "==> Building a SIGNED App Store archive..."
    dotnet publish "${COMMON_ARGS[@]}" \
        -p:RuntimeIdentifier=ios-arm64 \
        -p:CodesignKey="$CODESIGN_KEY" \
        -p:CodesignProvision="$PROVISIONING_PROFILE" \
        ${APPLE_TEAM_ID:+-p:CodesignTeamId="$APPLE_TEAM_ID"}
    echo ""
    echo "==> Done. Signed .ipa is in $OUT"
    echo "    Upload it to App Store Connect with either:"
    echo "      xcrun altool --upload-app -t ios -f \"$OUT\"/*.ipa -u <apple-id> -p <app-specific-password>"
    echo "    or the Transporter app (https://apps.apple.com/app/transporter/id1450874784)."
else
    echo "==> No signing env vars set — doing an UNSIGNED validation build."
    echo "    (Set CODESIGN_KEY + PROVISIONING_PROFILE for an upload-ready .ipa.)"
    dotnet build "$PROJECT" --configuration Release --framework net10.0-ios
    echo ""
    echo "==> Unsigned build OK. Re-run with signing env vars to produce an .ipa."
fi
