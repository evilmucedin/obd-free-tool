# obd-free-tool

An open-source, **always free** tool for talking to your car over OBD-II.

`obd-free-tool` connects to a standard OBD-II adapter (ELM327 family, over
USB / Bluetooth / Wi-Fi) and lets you read live sensor data, diagnostic
trouble codes (DTCs), and vehicle metadata — without subscriptions, accounts,
or paywalls.

> Status: 🚧 **Early development.** APIs, CLI flags, and on-disk formats are not
> yet stable.

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
- 🧱 Reusable core library (`libobd`) with a thin CLI on top.
- 📝 Log sessions for later analysis (CSV / JSON).

## Quick start

> These commands assume the tooling described in
> [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md). They will work once the first
> build lands.

```bash
# Configure & build (CMake + vcpkg)
cmake --preset default
cmake --build --preset default

# Run against a USB adapter
./build/obd-cli --port /dev/ttyUSB0 live rpm speed coolant_temp

# Read trouble codes
./build/obd-cli --port /dev/ttyUSB0 dtc read
```

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
