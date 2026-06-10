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

## Features (planned)

- 🔌 Connect to ELM327-compatible adapters over serial (USB), Bluetooth, and TCP/Wi-Fi.
- 📊 Read live data (RPM, speed, coolant temp, fuel trim, O2 sensors, …).
- 🩺 Read & clear Diagnostic Trouble Codes (DTCs) with human-readable descriptions.
- 🚗 Decode VIN and supported PIDs per vehicle.
- 🧱 Reusable core library (`ObdFree.Core`) with a thin CLI on top.
- 📝 Log sessions for later analysis (CSV / JSON).
- 💻 Cross-platform: **Windows, Linux (incl. Ubuntu), and macOS**.

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

> The CLI is an early scaffold — adapter commands (live data, DTC read/clear)
> are being built out. See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the
> planned design.

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
