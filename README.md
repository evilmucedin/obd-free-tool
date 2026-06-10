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
- 🩺 `dtc read` — read **stored** (Mode 03) and **pending** (Mode 07) trouble codes.
- 🧹 `dtc clear` — clear trouble codes from memory (Mode 04), with confirmation.
- 🛟 `srs status` / `srs clear` — read and clear **SRS / airbag** codes on
  Toyota/Lexus (UDS over CAN), with a safety warning and confirmation.
- 🖥️ **Two versions:** a console **CLI** (`ObdFree.Cli`) and a desktop **GUI**
  (`ObdFree.Gui`, built with [Avalonia](https://avaloniaui.net/)) — both share the
  same `ObdFree.Core` engine.
- 💻 Cross-platform: **Windows, Linux (incl. Ubuntu), and macOS**.

On the roadmap:

- 📈 Continuous live-data streaming and session logging (CSV / JSON).
- 🚗 VIN decoding and per-vehicle supported-PID discovery.
- 📷 Freeze-frame data and human-readable DTC descriptions.

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
./scripts/build.ps1
./scripts/test.ps1
./scripts/run-cli.ps1 --help
./scripts/run-gui.ps1
./scripts/publish-cli.ps1
./scripts/publish-gui.ps1
```

See [`scripts/README.md`](scripts/README.md) for all targets and supported
platforms.

### Two versions, one engine

Both apps are thin shells over the shared, well-tested `ObdFree.Core` library:

- **CLI** — scriptable, headless, great for automation and CI.
- **GUI** — pick your connection and car make from dropdowns, then click
  **Status**, **Read codes**, or **Clear codes**.

### Talking to an adapter

Connect over USB, Wi-Fi, or Bluetooth and run a command:

```bash
# Adapter status + live-data snapshot (Toyota/Lexus)
dotnet run --project src/ObdFree.Cli -- status --usb /dev/ttyUSB0 --make toyota

# Read stored & pending trouble codes over Wi-Fi (default 192.168.0.10:35000)
dotnet run --project src/ObdFree.Cli -- dtc read --wifi --make lexus

# Clear trouble codes from memory over Bluetooth (asks for confirmation)
dotnet run --project src/ObdFree.Cli -- dtc clear --bluetooth /dev/rfcomm0 --make toyota
```

Connection flags (pick one): `--usb <port>`, `--wifi [host:port]`,
`--bluetooth <port>`, plus optional `--baud <rate>` (default 38400). Classic
Bluetooth ELM327 adapters appear as a serial device, so `--bluetooth` takes the
serial port the OS bound to the adapter.

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
