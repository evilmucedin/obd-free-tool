# Architecture

> Forward-looking design doc. Some pieces described here do not exist yet; this
> is the shape we are building toward. Keep it in sync as the code lands.

## Goals

- A small, dependency-light **core library** (`libobd`) that is reusable from a
  CLI, a future GUI, or other tools.
- Hardware access fully abstracted so the protocol logic is testable without a
  physical adapter.
- Clear separation between **transport** (bytes in/out), **protocol** (ELM327 /
  OBD framing), and **decoding** (raw PID bytes → typed values).

## Layered overview

```
+-------------------------------------------------------------+
|                          obd-cli                            |  src/cli
|   argument parsing, output formatting (table/CSV/JSON)      |
+-------------------------------------------------------------+
|                          libobd                             |  src/libobd
|                                                             |
|   Session        high-level API: connect, query, read DTCs  |
|     |                                                       |
|   Protocol       ELM327 command/response, OBD modes/PIDs    |
|     |                                                       |
|   Decode         raw bytes -> typed values (units, scaling) |
|     |                                                       |
|   Transport      abstract byte stream (read/write/timeout)  |
+-------------------------------------------------------------+
        |                  |                   |
   SerialTransport   BluetoothTransport   TcpTransport
   (USB /dev/tty)    (RFCOMM)             (Wi-Fi ELM327)
                          |
                    MockTransport  (tests, replay)
```

## Modules

### Transport (`include/obd/transport.hpp`)
Abstract interface: open/close, timed `read`/`write`, line-oriented helpers.
Concrete implementations:
- **SerialTransport** — USB/serial (e.g. `/dev/ttyUSB0`, `COM3`).
- **TcpTransport** — Wi-Fi ELM327 adapters (default `192.168.0.10:35000`).
- **BluetoothTransport** — RFCOMM (platform-dependent; may be phased in later).
- **MockTransport** — scripted request/response pairs for unit tests and
  session replay. **All protocol tests run on this.**

### Protocol (`src/libobd/protocol/`)
- ELM327 AT command handling (reset, echo off, protocol auto-detect, headers).
- OBD-II request framing by **mode** (01 live data, 03 stored DTCs, 04 clear,
  09 vehicle info, …) and **PID**.
- Response parsing, error/`NO DATA`/`?` handling, multi-frame (ISO-TP) assembly.

### Decode (`src/libobd/decode/`)
- Table of known PIDs: id, name, byte length, formula, unit.
- Pure functions: `(raw bytes) -> PidValue{ double value; std::string unit }`.
- Easy to unit-test; no I/O.

### Session (`include/obd/session.hpp`)
The public high-level entry point the CLI/GUI use:
- `connect(Transport&)`, negotiate protocol, query supported PIDs.
- `read(pid)`, `stream({pids...}, callback)`, `readDtcs()`, `clearDtcs()`,
  `readVin()`.

### CLI (`src/cli/`)
Thin layer: parse args/flags, build the right `Transport`, drive a `Session`,
format output (human table, CSV, JSON), handle logging.

## Data flow (reading live RPM)

1. CLI builds a `SerialTransport("/dev/ttyUSB0")` and a `Session`.
2. `Session::connect` runs ELM327 init (`ATZ`, `ATE0`, `ATSP0`, …).
3. `Session::read(Pid::EngineRpm)` → Protocol sends `010C`.
4. Transport returns ASCII hex; Protocol strips framing → raw bytes.
5. Decode applies the formula `((A*256)+B)/4` → `825.0` with unit `rpm`.
6. CLI prints it.

## Build system

- **CMake** with `CMakePresets.json` (a `default` preset for configure/build/test).
- **vcpkg** manifest mode (`vcpkg.json`) for dependencies; toolchain wired via the
  preset.
- Targets: `obd` (library), `obd-cli` (executable), `obd-tests` (CTest).

## Testing strategy

- **Unit tests** for Decode (pure) and Protocol (driven by `MockTransport`).
- **Recorded sessions:** capture real adapter traffic into fixtures, replay via
  `MockTransport` for regression tests.
- CI never touches real hardware.

## Open questions / TODO

- Pick the unit-test framework (GoogleTest vs Catch2).
- Decide error model (`std::expected` vs a custom `Result`).
- Bluetooth strategy per-platform.
- Config file format for saved adapters/profiles.
