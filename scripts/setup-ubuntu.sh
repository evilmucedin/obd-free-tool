#!/usr/bin/env bash
# Set up an Ubuntu/Debian machine to build and run obd-free-tool.
# Installs everything via apt: build prerequisites, the .NET 10 SDK, and the
# packages needed to talk to USB/Bluetooth OBD-II adapters.
#
# Usage: ./scripts/setup-ubuntu.sh
#
# Re-runnable (idempotent). Uses sudo for apt; will prompt if not already root.
set -euo pipefail

if ! command -v apt-get >/dev/null 2>&1; then
    echo "ERROR: this script is for Ubuntu/Debian (apt-based) systems." >&2
    echo "On macOS use Homebrew; on Windows use winget/the .NET installer." >&2
    exit 1
fi

SUDO=""
if [ "$(id -u)" -ne 0 ]; then
    SUDO="sudo"
fi

# .NET SDK channel we target (kept in sync with global.json).
DOTNET_SDK_PKG="dotnet-sdk-10.0"

echo "==> Updating apt package lists..."
$SUDO apt-get update -y

echo "==> Installing base prerequisites..."
$SUDO apt-get install -y --no-install-recommends \
    ca-certificates \
    curl \
    wget \
    gnupg \
    apt-transport-https \
    git

# Packages useful for talking to OBD-II adapters:
#   - usbutils / udev: USB serial adapter enumeration
#   - bluez / libbluetooth-dev: Bluetooth (RFCOMM) ELM327 adapters
echo "==> Installing OBD adapter support packages..."
$SUDO apt-get install -y --no-install-recommends \
    usbutils \
    udev \
    bluez \
    libbluetooth-dev || echo "WARN: some optional adapter packages were unavailable; continuing."

# Runtime libraries the Avalonia desktop GUI needs on Linux (X11, fonts, GL).
# Only required to *run* the GUI; the CLI needs none of these.
echo "==> Installing GUI (Avalonia) runtime libraries..."
$SUDO apt-get install -y --no-install-recommends \
    libx11-6 \
    libice6 \
    libsm6 \
    libfontconfig1 \
    libgl1 \
    libicu-dev || echo "WARN: some GUI runtime libraries were unavailable; the CLI will still work."

install_dotnet_from_microsoft_feed() {
    echo "==> Falling back to Microsoft package feed for the .NET SDK..."
    # shellcheck disable=SC1091
    source /etc/os-release
    local ver="${VERSION_ID:-22.04}"
    wget -q "https://packages.microsoft.com/config/ubuntu/${ver}/packages-microsoft-prod.deb" \
        -O /tmp/packages-microsoft-prod.deb
    $SUDO dpkg -i /tmp/packages-microsoft-prod.deb
    rm -f /tmp/packages-microsoft-prod.deb
    $SUDO apt-get update -y
    $SUDO apt-get install -y "$DOTNET_SDK_PKG"
}

echo "==> Installing the .NET SDK ($DOTNET_SDK_PKG)..."
if ! $SUDO apt-get install -y "$DOTNET_SDK_PKG"; then
    install_dotnet_from_microsoft_feed
fi

# Give the current user access to serial devices (/dev/ttyUSB*, /dev/ttyACM*)
# without sudo. Takes effect on next login.
if getent group dialout >/dev/null 2>&1; then
    if ! id -nG "${USER:-$(id -un)}" | tr ' ' '\n' | grep -qx dialout; then
        echo "==> Adding ${USER:-$(id -un)} to the 'dialout' group (serial access)..."
        $SUDO usermod -aG dialout "${USER:-$(id -un)}"
        echo "    NOTE: log out and back in for serial access to take effect."
    fi
fi

echo ""
echo "==> Verifying the .NET SDK installation..."
if command -v dotnet >/dev/null 2>&1; then
    dotnet --info | sed -n '1,12p'
else
    echo "WARN: 'dotnet' not found on PATH yet — open a new shell and re-check." >&2
fi

echo ""
echo "==> Setup complete. Next steps:"
echo "      ./scripts/build.sh"
echo "      ./scripts/test.sh"
echo "      ./scripts/run-cli.sh -- --help   # console version"
echo "      ./scripts/run-gui.sh             # desktop GUI version"
