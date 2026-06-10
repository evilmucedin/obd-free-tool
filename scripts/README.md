# Build & run scripts

Cross-platform helper scripts so you don't have to memorize `dotnet` flags.
Every task ships in two flavors:

- `*.sh` — POSIX shell, for **Linux** and **macOS**.
- `*.ps1` — PowerShell, for **Windows** (and any cross-platform `pwsh` host).

The app comes in **two versions** that share the same `ObdFree.Core` engine:

- **CLI** (`ObdFree.Cli`) — the `obd` console tool.
- **GUI** (`ObdFree.Gui`) — an Avalonia desktop app.

All scripts can be run from anywhere; they resolve the repo root themselves.

| Task | Linux / macOS | Windows |
|------|---------------|---------|
| One-time setup (Ubuntu/Debian) | `./scripts/setup-ubuntu.sh` | — |
| Build everything | `./scripts/build.sh [Debug\|Release]` | `./scripts/build.ps1 [-Configuration Release]` |
| Run tests + coverage | `./scripts/test.sh [Debug\|Release]` | `./scripts/test.ps1 [-Configuration Release]` |
| Run the **CLI** | `./scripts/run-cli.sh -- <args>` | `./scripts/run-cli.ps1 <args>` |
| Run the **GUI** | `./scripts/run-gui.sh` | `./scripts/run-gui.ps1` |
| Publish the **CLI** | `./scripts/publish-cli.sh [RID]` | `./scripts/publish-cli.ps1 [-Rid <rid>]` |
| Publish the **GUI** | `./scripts/publish-gui.sh [RID]` | `./scripts/publish-gui.ps1 [-Rid <rid>]` |
| Prepare the **iOS** App Store build | `./scripts/publish-ios.sh` (macOS only) | — |

`build.sh` and `test.sh` cover the whole solution (both apps + the core library
and tests), so there's a single build/test step for everything.

## First-time setup on Ubuntu / Debian

`setup-ubuntu.sh` installs everything via `apt` so a clean machine is ready to
build and run **both** versions:

- base prerequisites (`curl`, `git`, `ca-certificates`, …),
- the **.NET 10 SDK** (`dotnet-sdk-10.0`; falls back to the Microsoft package
  feed if the distro repo doesn't carry it),
- OBD adapter support (`usbutils`, `udev`, `bluez`, `libbluetooth-dev`),
- GUI runtime libraries for Avalonia (`libx11-6`, `libfontconfig1`, `libgl1`, …),
  needed only to *run* the desktop app, and
- adds you to the `dialout` group for USB serial access (`/dev/ttyUSB*`).

```bash
./scripts/setup-ubuntu.sh
# then log out/in once for serial-port group access to take effect
```

It's idempotent — safe to re-run. macOS users install the .NET SDK via the
official installer or Homebrew; Windows users via winget or the .NET installer.

## Examples

```bash
# Linux / macOS
./scripts/build.sh
./scripts/test.sh
./scripts/run-cli.sh -- status --usb /dev/ttyUSB0 --make toyota
./scripts/run-gui.sh
./scripts/publish-cli.sh        # self-contained CLI in artifacts/cli/<rid>/
./scripts/publish-gui.sh        # self-contained GUI in artifacts/gui/<rid>/
```

```powershell
# Windows (PowerShell)
./scripts/build.ps1
./scripts/test.ps1
./scripts/run-cli.ps1 status --usb COM3 --make lexus
./scripts/run-gui.ps1
./scripts/publish-cli.ps1
./scripts/publish-gui.ps1
```

## Publishing self-contained binaries

`publish-cli` / `publish-gui` produce a **self-contained, single-file**
executable (no .NET install required on the target machine) under
`artifacts/cli/<RID>/` and `artifacts/gui/<RID>/` respectively.

If no Runtime Identifier (RID) is given, the host platform is auto-detected.
Supported RIDs:

| Platform | RID |
|----------|-----|
| Windows x64 | `win-x64` |
| Windows ARM64 | `win-arm64` |
| Linux x64 | `linux-x64` |
| Linux ARM64 | `linux-arm64` |
| macOS Intel | `osx-x64` |
| macOS Apple Silicon | `osx-arm64` |

```bash
# Cross-compile a Windows GUI from Linux/macOS
./scripts/publish-gui.sh win-x64
# Auto-detect the current platform
./scripts/publish-cli.sh
```

> Both versions are free and open-source forever — these binaries are too. Ship
> them anywhere.

## iOS App Store build

`publish-ios.sh` runs on **macOS only** and preflights the requirements (Xcode,
the `ios` .NET workload) before building. With signing env vars set it produces
an upload-ready `.ipa` in `artifacts/ios/`; without them it does an unsigned
validation build.

```bash
# Validation build (no signing)
./scripts/publish-ios.sh

# Upload-ready signed build
APPLE_TEAM_ID=ABCDE12345 \
CODESIGN_KEY="Apple Distribution: Your Name (ABCDE12345)" \
PROVISIONING_PROFILE="OBD Free App Store" \
./scripts/publish-ios.sh
```

Publishing to the App Store additionally requires a paid Apple Developer Program
membership. `src/ObdFree.iOS` is **not** in the solution (it only builds on macOS
with the iOS workload), so the regular `build.sh`/`test.sh` don't touch it.
