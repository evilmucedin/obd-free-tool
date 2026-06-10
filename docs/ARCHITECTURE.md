# Architecture

> Forward-looking design doc. Some pieces described here do not exist yet; this
> is the shape we are building toward. Keep it in sync as the code lands.

## Goals

- A small, dependency-light **core library** (`ObdFree.Core`) reusable from a
  CLI, a future GUI, or other tools.
- Hardware access fully abstracted so protocol logic is testable without a
  physical adapter.
- Clear separation between **transport** (bytes in/out), **protocol** (ELM327 /
  OBD framing), and **decoding** (raw PID bytes → typed values).
- Runs identically on Windows, Linux, and macOS.

## Solution layout

```
ObdFree.sln
├── src/ObdFree.Core      class library — all the logic
│   ├── Transport/        IObdTransport + serial/tcp/bluetooth impls
│   ├── Protocol/         ELM327 command/response, OBD modes & PIDs
│   ├── Diagnostics/      DTC decoding (DiagnosticTroubleCode)
│   └── Pids/             PID value decoders (PidDecoders, PidValue)
├── src/ObdFree.Cli       console app (AssemblyName: obd)
└── tests/ObdFree.Core.Tests   xUnit tests + FakeObdTransport
```

## Layered overview

```
+-------------------------------------------------------------+
|                       ObdFree.Cli                           |
|   argument parsing, output formatting (table/CSV/JSON)      |
+-------------------------------------------------------------+
|                       ObdFree.Core                          |
|                                                             |
|   Session        high-level API: connect, query, read DTCs  |
|     |                                                       |
|   Protocol       ELM327 command/response, OBD modes/PIDs    |
|     |                                                       |
|   Decode         raw bytes -> typed values (units, scaling) |
|     |                                                       |
|   Transport      IObdTransport: open/close/SendCommandAsync |
+-------------------------------------------------------------+
        |                  |                   |
   SerialTransport   BluetoothTransport   TcpTransport
   (USB serial)      (RFCOMM)             (Wi-Fi ELM327)
                          |
                    FakeObdTransport  (tests, replay)
```

## Modules

### Transport (`Transport/IObdTransport.cs`)
Async abstraction: `OpenAsync`, `CloseAsync`, `SendCommandAsync`, `IsOpen`,
`IAsyncDisposable`. Concrete implementations:
- **SerialObdTransport** — USB/serial (e.g. `/dev/ttyUSB0`, `COM3`). Also serves
  classic **Bluetooth (SPP)** adapters, which every OS exposes as a serial
  device (`/dev/rfcomm0`, a COM port, etc.) — so `ObdConnection.Bluetooth(...)`
  builds a serial transport. This keeps Bluetooth cross-platform with no native
  deps (the same approach ForScan takes with the BT COM port on Windows).
- **TcpObdTransport** — Wi-Fi ELM327 adapters (default `192.168.0.10:35000`).
- **StreamObdTransport** — shared base implementing the ELM327 line protocol
  (write command + `\r`, read until the `>` prompt) over any `Stream`.
- **FakeObdTransport** — scripted request/response map for unit tests and
  session replay. **All protocol tests run on this.** (Lives in the test project.)

### Protocol (`Protocol/`)
- ELM327 AT command handling (reset, echo off, protocol auto-detect, headers).
- OBD-II request framing by **mode** (01 live data, 03 stored DTCs, 04 clear,
  09 vehicle info, …) and **PID**.
- Response parsing, error/`NO DATA`/`?` handling, multi-frame (ISO-TP) assembly.

### Diagnostics (`Diagnostics/`)
- `DiagnosticTroubleCode.Decode(byte a, byte b)` → canonical `P0133`-style code.
- Pure, exhaustively unit-tested.

### Pids (`Pids/`)
- `PidDecoders` — pure functions mapping raw bytes to `PidValue { double Value;
  string Unit }` (RPM, speed, coolant temp, load, throttle, …).
- No I/O; trivial to test with `[Theory]`/`[InlineData]`.

### Vehicles (`Vehicles/`)
- `ObdProtocol` — enum whose values match the ELM327 `ATSP` code, so
  `ToSetProtocolCommand()` yields `ATSP6` etc. `TryParse` accepts friendly names
  (`can`, `iso9141`, …) or raw digits.
- `VehicleProfile` + `VehicleProfiles` — per-make tuning. The key knob is the
  preferred protocol. **Toyota** and **Lexus** (shared platform) default to
  ISO 15765-4 CAN 11-bit/500k; **Generic** uses auto-detect. `ObdSession` applies
  the profile's protocol during `ConnectAsync`, and the CLI's `--make` /
  `--protocol` flags (with an interactive prompt) select it.

### Session (`ObdSession`)
The public high-level entry point the CLI/GUI use (implemented):
- `ConnectAsync()` — open transport + ELM327 init (`ATZ`, `ATE0`, `ATL0`,
  `ATS0`, `ATH0`, `ATSP0`), returns adapter identity.
- `GetStatusAsync()` — adapter identity, battery voltage (`ATRV`), and a snapshot
  of every catalog parameter that responds.
- `ReadParameterAsync(def)` — one live PID value.
- `ReadStoredCodesAsync()` (Mode 03) / `ReadPendingCodesAsync()` (Mode 07).
- `ClearCodesAsync()` (Mode 04) — write; the CLI gates it behind a confirmation.

### CLI (`ObdFree.Cli`)
Thin layer: parse args/flags, build the right transport, drive a session,
format output (human table, CSV, JSON), handle logging.

## Data flow (reading live RPM)

1. CLI builds a `SerialTransport("/dev/ttyUSB0")` and a session.
2. `ConnectAsync` runs ELM327 init (`ATZ`, `ATE0`, `ATSP0`, …).
3. `ReadAsync(Pid.EngineRpm)` → Protocol sends `010C`.
4. Transport returns ASCII hex; Protocol strips framing → raw bytes A, B.
5. `PidDecoders.EngineRpm(A, B)` applies `((A*256)+B)/4` → `825 rpm`.
6. CLI prints it.

## Build & tooling

- **.NET 10** (`net10.0`), SDK pinned in `global.json`.
- **Directory.Build.props** centralizes shared MSBuild settings: nullable on,
  implicit usings, warnings-as-errors, analyzers, XML docs.
- **.editorconfig** drives formatting and code-style rules; `dotnet format`
  enforces them.
- **GitHub Actions** (`.github/workflows/ci.yml`): restore → build → test on a
  `ubuntu/windows/macos` matrix, plus a formatting gate.

## Testing strategy

- **Unit tests** for Decode (pure) and Protocol (driven by `FakeObdTransport`).
- **Recorded sessions:** capture real adapter traffic into fixtures, replay via
  `FakeObdTransport` for regression tests.
- Coverage collected via `coverlet`; CI never touches real hardware.

## Open questions / TODO

- CLI argument-parsing library (System.CommandLine vs hand-rolled).
- Error model (exceptions vs a `Result`-style type across the public API).
- Bluetooth strategy per-platform.
- Config file format for saved adapters/profiles.
