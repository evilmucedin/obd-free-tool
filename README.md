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
- 🧱 Reusable core library (`ObdFree.Core`) with a thin CLI on top.
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
# Build & test
dotnet build
dotnet test

# Run the CLI
dotnet run --project src/ObdFree.Cli
```

Or use the cross-platform helper scripts (no flags to memorize):

```bash
# Ubuntu/Debian: one-time setup (installs the .NET SDK & deps via apt)
./scripts/setup-ubuntu.sh

# Linux / macOS
./scripts/build.sh          # build
./scripts/test.sh           # test + coverage
./scripts/run.sh -- --help  # compile and run
./scripts/publish.sh        # self-contained binary in artifacts/

# Windows (PowerShell)
./scripts/build.ps1
./scripts/test.ps1
./scripts/run.ps1 --help
./scripts/publish.ps1
```

See [`scripts/README.md`](scripts/README.md) for all targets and supported
platforms.

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
