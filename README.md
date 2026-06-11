# obd-free-tool

An open-source, **always free** tool for talking to your car over OBD-II.

`obd-free-tool` connects to a standard OBD-II adapter (ELM327 family, over
USB / Bluetooth / Wi-Fi) and lets you read live sensor data, diagnostic
trouble codes (DTCs), and vehicle metadata — without subscriptions, accounts,
or paywalls.

> Status: 🚧 **Early development.** APIs, CLI flags, and on-disk formats are not
> yet stable.

## Our promise: free forever, open forever

`obd-free-tool` is and will **always be free and open-source** — no
subscriptions, no accounts, no ads, no paywalled "pro" tier, no telemetry. Your
car's diagnostic data is yours. The Apache-2.0 license guarantees anyone can
use, audit, fork, and build on this tool, forever. That principle drives every
decision in this project.

## Why

Most OBD apps are either locked behind subscriptions, riddled with ads, or
closed-source black boxes. Your car's diagnostic data belongs to you. This
project aims to be a clean, well-documented, permissively licensed (Apache-2.0)
tool that anyone can use, audit, and extend.

Inspired by tools like [ForScan](https://forscan.org/), but free, open-source,
and cross-platform.

## Features

Available now:

- 🔌 Connect to ELM327-compatible adapters over **USB (serial), Wi-Fi (TCP), and
  Bluetooth (SPP)**.
- 📊 `status` — adapter info, battery voltage, and a live-data snapshot
  (RPM, speed, coolant/intake temp, engine load, throttle).
- 🇺🇸 `readiness` — **I/M readiness & MIL status** (Mode 01 PID 01): the emissions
  monitors a **US smog/emissions inspection** checks, with a "likely ready?" verdict.
- 🪪 `vin` — read the **Vehicle Identification Number** (Mode 09 PID 02).
- 🩺 `dtc read` — read **stored** (Mode 03), **pending** (Mode 07), and
  **permanent** (Mode 0A) trouble codes.
- 🧹 `dtc clear` — clear trouble codes from memory (Mode 04), with confirmation.
- 🛟 `srs status` / `srs clear` — read and clear **SRS / airbag** codes on
  Toyota/Lexus (UDS over CAN), with a safety warning and confirmation.
- 🖥️ **Multiple front-ends:** a console **CLI** (`ObdFree.Cli`), a desktop **GUI**
  (`ObdFree.Gui`), and an **iOS app** (`ObdFree.iOS`) — all sharing the same
  Avalonia UI (`ObdFree.App`) and `ObdFree.Core` engine.
- 💻 Cross-platform: **Windows, Linux (incl. Ubuntu), macOS**, and **iOS**.

On the roadmap:

- 📈 Continuous live-data streaming and session logging (CSV / JSON).
- 🚗 VIN decoding and per-vehicle supported-PID discovery.
- 📷 Freeze-frame data and human-readable DTC descriptions.

## Safe vs Professional mode

The app runs in one of two modes, so casual users can't accidentally do harm:

- **Safe** (default) — read-only, standard OBD-II: `status`, `readiness`, `vin`,
  `dtc read`. Nothing that writes to the car.
- **Professional** — unlocks write/advanced features that can be risky:
  `dtc clear`, and all SRS/airbag access (`srs status`, `srs clear`).

The mode is **enforced in the core** (not just hidden in the UI), persisted, and
overridable per run:

```bash
obd config get                        # show current mode + config file path
obd config set mode professional      # persist professional mode
obd dtc clear --usb /dev/ttyUSB0 --mode professional   # one-off override
```

In the GUI, a **Mode** dropdown (top-right) switches modes and persists the
choice; dangerous buttons stay disabled in Safe mode.

## Settings are saved automatically

Your settings persist to a small JSON file under your OS config dir (e.g.
`~/.config/obd-free-tool/config.json`), so the app **resumes with your previous
choices** after a restart. The GUI saves changes as you make them — operating
mode, connection kind, target/endpoint, baud rate, vehicle profile, and adapter
profile. Inspect or tweak it from the CLI:

```bash
obd config get                     # show all saved settings + file path
obd config set mode professional   # persist a setting from the CLI
```

## Tech stack

- **C# / .NET 10** — one codebase, runs everywhere.
- **xUnit** for tests (we care a lot about coverage).
- **GitHub Actions** for CI across all three operating systems.

## Quick start

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
# Build & test everything (both versions + core + tests)
dotnet build
dotnet test

# Run the console CLI
dotnet run --project src/ObdFree.Cli

# Run the desktop GUI
dotnet run --project src/ObdFree.Gui
```

Or use the cross-platform helper scripts (no flags to memorize):

```bash
# Ubuntu/Debian: one-time setup (installs the .NET SDK & deps via apt)
./scripts/setup-ubuntu.sh

# Linux / macOS
./scripts/build.sh              # build everything
./scripts/test.sh               # test + coverage
./scripts/run-cli.sh -- --help  # compile and run the CLI
./scripts/run-gui.sh            # compile and launch the GUI
./scripts/publish-cli.sh        # self-contained CLI in artifacts/cli/
./scripts/publish-gui.sh        # self-contained GUI in artifacts/gui/

# Windows (PowerShell)
./scripts/setup-windows.ps1     # one-shot: install deps, build & run
./scripts/build.ps1
./scripts/test.ps1
./scripts/run-cli.ps1 --help
./scripts/run-gui.ps1
./scripts/publish-cli.ps1
./scripts/publish-gui.ps1
```

See [`scripts/README.md`](scripts/README.md) for all targets and supported
platforms.

### Developing in VS Code

The repo ships a ready-to-use [VS Code](https://code.visualstudio.com/) setup in
[`.vscode/`](.vscode/) — open the folder and you're good to go:

- **Recommended extensions** (`extensions.json`): the C# Dev Kit, .NET runtime
  helper, EditorConfig, and Avalonia tooling. VS Code prompts to install them.
- **Build/test/format tasks** (`tasks.json`): run via *Terminal → Run Task* or
  `Ctrl/Cmd+Shift+B` (default build). Includes `build`, `build (Release)`,
  `test`, `test (coverage)`, and `format` / `format (verify)` — matching the CI
  gates.
- **Debug profiles** (`launch.json`): *Run and Debug* → **CLI (console)** or
  **GUI (desktop)** to launch with breakpoints. Edit `args` in the CLI profile to
  pass flags (e.g. `status --usb /dev/ttyUSB0 --make toyota`).
- **Workspace settings** (`settings.json`): format-on-save, Roslyn analyzers, and
  the `.slnx` solution wired up so everything builds together.

All of these just wrap the same `dotnet` commands and helper scripts, so the
VS Code, CLI, and CI experiences stay identical.

### Developing in Visual Studio

Prefer the full [Visual Studio](https://visualstudio.microsoft.com/) IDE on
Windows? Everything's wired up too:

- **Open the solution** — Visual Studio 2022 (17.14+) opens the `ObdFree.slnx`
  solution natively; no separate `.sln` needed.
- **Required workloads** ([`.vsconfig`](.vsconfig)): when you open the repo,
  Visual Studio prompts to install any missing components (.NET desktop + .NET
  cross-platform build tools, the .NET SDK, NuGet, and Git). The correct .NET 10
  SDK is selected automatically from [`global.json`](global.json).
- **Run/debug profiles** (`Properties/launchSettings.json`): pick the startup
  project and profile from the toolbar — **obd (CLI)** or **obd --help** for the
  console tool, **ObdFree.Gui (desktop)** for the Avalonia app. Edit
  `commandLineArgs` to pass flags (e.g. `status --usb COM3 --make toyota`).
- **Test Explorer** discovers the xUnit tests automatically; **Format Document**
  and analyzers honor the repo `.editorconfig`, matching the CI `dotnet format`
  gate.

Like the VS Code setup, this just drives the same `dotnet` build/test/run, so
all environments stay in sync.

### Front-ends, one engine

Every front-end is a thin shell over the shared, well-tested `ObdFree.Core`
engine and the shared Avalonia UI in `ObdFree.App`:

- **CLI** (`ObdFree.Cli`) — scriptable, headless, great for automation and CI.
- **Desktop GUI** (`ObdFree.Gui`) — Avalonia app for Windows/Linux/macOS.
- **iOS** (`ObdFree.iOS`) — Avalonia iOS app for iPhone/iPad (see below).

### iOS app (Apple App Store)

The iOS app reuses the same UI and engine via [Avalonia iOS](https://avaloniaui.net/).
It builds **only on macOS** with Xcode and the .NET iOS workload
(`dotnet workload install ios`), so it is intentionally kept out of the solution
and CI. Prepare an App Store build with:

```bash
./scripts/publish-ios.sh    # preflight + (signed) .ipa in artifacts/ios/
```

**What you need to publish** (all Apple-imposed):

- A paid **Apple Developer Program** membership (~$99/year).
- A **Distribution certificate** + **App Store provisioning profile** for the
  bundle id `io.github.evilmucedin.obdfree` (pass them to the script via the
  `CODESIGN_KEY` / `PROVISIONING_PROFILE` / `APPLE_TEAM_ID` env vars).

**Adapter support on iOS** — important: iOS does **not** allow third-party apps
to use classic Bluetooth (SPP) or USB serial. So on iOS:

- ✅ **Wi-Fi** ELM327 adapters work today (TCP — `TcpObdTransport`).
- ⏳ **Bluetooth LE** adapters would work but need the not-yet-built BLE transport.
- ❌ USB / classic-Bluetooth dongles are not usable on iOS (OS limitation).

So pick a **Wi-Fi** OBD-II adapter for the iPhone/iPad app.

### Talking to an adapter

Connect over USB, Wi-Fi, or Bluetooth and run a command:

```bash
# Adapter status + live-data snapshot (Toyota/Lexus)
dotnet run --project src/ObdFree.Cli -- status --usb /dev/ttyUSB0 --make toyota

# Will it pass a US smog check? (emissions readiness + MIL)
dotnet run --project src/ObdFree.Cli -- readiness --usb /dev/ttyUSB0

# Read stored & pending trouble codes over Wi-Fi (default 192.168.0.10:35000)
dotnet run --project src/ObdFree.Cli -- dtc read --wifi --make lexus

# Clear trouble codes from memory over Bluetooth (asks for confirmation)
dotnet run --project src/ObdFree.Cli -- dtc clear --bluetooth /dev/rfcomm0 --make toyota
```

Connection flags (pick one): `--usb <port>`, `--wifi [host:port]`,
`--bluetooth <port>`, plus optional `--baud <rate>` (default 38400). Classic
Bluetooth ELM327 adapters appear as a serial device, so `--bluetooth` takes the
serial port the OS bound to the adapter.

### Supported dongles (popular Amazon models)

The tool works with **any ELM327/STN-compatible adapter** over **USB, Wi-Fi, or
classic Bluetooth**. For the popular off-the-shelf dongles, `--dongle <key>`
auto-configures the right endpoint/baud/timing — run `obd dongles` to list them:

```bash
obd dongles                                       # list known dongles
obd status --dongle veepeak-wifi                  # Wi-Fi: endpoint auto-set
obd status --dongle bafx-bt --bluetooth /dev/rfcomm0
obd status --dongle generic-usb --usb /dev/ttyUSB0
```

| Dongle | Link | Works today |
|--------|------|:-----------:|
| BAFX (Bluetooth) | Bluetooth (classic) | ✅ |
| Veepeak Mini WiFi / Bluetooth | Wi-Fi / classic BT | ✅ |
| Vgate iCar Pro (BT 3.0) / WiFi | classic BT / Wi-Fi | ✅ |
| vLinker FS (USB) / MC+ (BT) | USB / classic BT | ✅ |
| Panlong, KOBRA (BT / WiFi) | classic BT / Wi-Fi | ✅ |
| OBDLink LX / MX+ | classic BT (STN) | ✅ |
| Generic ELM327 (USB / WiFi / BT) | USB / Wi-Fi / classic BT | ✅ |
| **Veepeak OBDCheck BLE**, **OBDLink CX**, **Vgate iCar Pro BLE** | Bluetooth **LE** | ⏳ not yet |

> **Bluetooth LE (BLE) dongles aren't supported yet.** Several popular models are
> BLE-only (Veepeak OBDCheck BLE, OBDLink CX, Vgate iCar Pro BLE) — supporting
> them needs a BLE/GATT transport, which is a tracked follow-up. The classic
> Bluetooth, USB, and Wi-Fi versions of these brands work today. If you're buying
> for this tool, a **Wi-Fi** or **classic-Bluetooth** ELM327/STN adapter is the
> safe choice.

### Adapters & Launch tools

Pick an adapter profile to tune reset/timing for your dongle:

- `--adapter standard` (default) — genuine ELM327, STN/OBDLink, well-behaved clones.
- `--adapter launch` — tolerant timing (warm-start reset + delays) for finicky
  clones and **ELM327-compatible Launch Wi-Fi/BT units**.

> **About Launch dongles — read this.** The popular cheap Launch devices
> (**Thinkdiag, Easydiag, Golo, X431/DBSCAR**) are **not** ELM327 — they use
> Launch's **proprietary DBSCAR protocol** and are locked to Launch's own apps,
> so no generic OBD tool (this one included) can drive them. If you connect one,
> the app **detects it and tells you** instead of failing silently. Only
> **ELM327-compatible** adapters work here — including ELM327-style Launch Wi-Fi
> units (use `--adapter launch`). For full third-party compatibility, an OBDLink,
> vLinker, or Vgate adapter is a safe bet.
>
> Note: Bluetooth support is currently **classic SPP** (serial). BLE-only dongles
> aren't supported yet.

### Vehicle profiles (Toyota / Lexus)

Picking your car make selects the right OBD protocol up front, which connects
faster and more reliably than auto-detection:

- `--make toyota` / `--make lexus` → ISO 15765-4 CAN (11-bit, 500k), used by most
  Toyota/Lexus from ~2008+.
- `--make generic` (default) → adapter auto-detects the protocol.
- If you omit `--make`, the CLI asks you to pick interactively.
- Override the protocol for older cars with `--protocol auto|can|iso9141|kwp`.

> Toyota/Lexus is the initial test target. If you hit issues on a specific
> model, please open an issue with the model year and adapter type.

### US emissions & smog checks

OBD-II was mandated in the USA by CARB/EPA primarily for **emissions control**, so
the tool covers the cases a US inspection cares about:

```bash
obd readiness --usb /dev/ttyUSB0   # MIL state + I/M readiness monitors + verdict
obd vin --usb /dev/ttyUSB0         # Vehicle Identification Number
obd dtc read --usb /dev/ttyUSB0    # stored + pending + PERMANENT codes
```

- **Readiness / I/M monitors** (Mode 01 PID 01): shows whether the MIL is on, how
  many DTCs are stored, and which emissions monitors (catalyst, EVAP, O2, EGR, …)
  have completed. Most states require the MIL off and at most one incomplete
  monitor — the tool prints a "likely ready?" verdict (guidance only; rules vary
  by state).
- **Permanent DTCs** (Mode 0A): these can't be cleared by a scan tool or a battery
  disconnect, so they prevent "clear-and-pass" cheating — inspections check them.
- **VIN** (Mode 09): for registration, recalls, and emissions records.

### SRS / airbag (Toyota/Lexus)

The airbag (SRS) system is **not** part of generic OBD-II — it's a separate ECU
reached over **UDS (ISO 14229) on CAN**. This tool can read and clear SRS codes
on Toyota/Lexus:

```bash
# Read SRS/airbag status and codes
obd srs status --usb /dev/ttyUSB0 --make toyota

# Clear SRS codes (asks for explicit confirmation)
obd srs clear --usb /dev/ttyUSB0 --make toyota
```

> ⚠️ **Safety:** clearing SRS codes does **not** repair the fault — only clear
> them *after* the airbag/seat-belt issue has been physically fixed. A faulty
> SRS may not deploy in a crash.
>
> ⚠️ **Experimental:** Toyota/Lexus SRS CAN addresses vary by model and year.
> The defaults are `--srs-tx 7B0 --srs-rx 7B8`; if the module doesn't respond,
> override them with the values for your vehicle. Please validate on the actual
> car and report what works for your model.

> More commands (live streaming, freeze frames, VIN) are on the roadmap. See
> [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

## Documentation

| Doc | What's in it |
|-----|--------------|
| [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) | Module layout, data flow, build system |
| [`docs/OBD.md`](docs/OBD.md) | OBD-II / ELM327 / PID domain primer |
| [`AGENTS.md`](AGENTS.md) | Instructions for AI coding agents (and a good human onboarding read) |
| [`CONTRIBUTING.md`](CONTRIBUTING.md) | Dev setup and contribution workflow |

## Disclaimer

This software talks directly to your vehicle. Reading data is generally safe,
but clearing codes or sending commands can affect vehicle behavior. Use at your
own risk, never operate the tool while driving, and never rely on it for
safety-critical decisions.

## License

[Apache License 2.0](LICENSE).
