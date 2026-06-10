# AGENTS.md

Canonical instructions for AI coding agents (Pi, Claude Code, GitHub Copilot,
Cursor, Codex, …) working in this repository. Humans are welcome to read it too —
it doubles as an onboarding guide.

This is the **single source of truth**. Tool-specific files (`CLAUDE.md`,
`.github/copilot-instructions.md`) intentionally just point back here so the
guidance never drifts.

## Project in one paragraph

`obd-free-tool` is an open-source, free-forever tool to communicate with cars
over OBD-II. It connects to ELM327-compatible adapters (USB serial, Bluetooth,
TCP/Wi-Fi), reads live sensor data and Diagnostic Trouble Codes (DTCs), and
decodes vehicle metadata. The codebase is a reusable C++ core library
(`libobd`) with a thin CLI (`obd-cli`) on top.

## Tech stack

- **Language:** C++20.
- **Build:** CMake (>= 3.25) with presets (`CMakePresets.json`).
- **Dependencies:** [vcpkg](https://vcpkg.io) in manifest mode (`vcpkg.json`).
- **Testing:** CTest + a unit-test framework (GoogleTest or Catch2 — see the
  build files once they land).
- **Target platforms:** Linux and macOS first; Windows best-effort.

## Repository layout (target)

```
.
├── CMakeLists.txt          # top-level build
├── CMakePresets.json       # configure/build/test presets
├── vcpkg.json              # dependency manifest
├── src/
│   ├── libobd/             # core library: transports, protocol, PID decoding
│   └── cli/                # obd-cli executable
├── include/obd/            # public headers for libobd
├── tests/                  # unit & integration tests
└── docs/                   # human + agent documentation
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for module responsibilities
and data flow, and [`docs/OBD.md`](docs/OBD.md) for the OBD-II domain primer.

## Build & test commands

```bash
# Configure (pulls deps via vcpkg)
cmake --preset default

# Build
cmake --build --preset default

# Run tests
ctest --preset default --output-on-failure

# Format (run before committing)
clang-format -i $(git ls-files '*.cpp' '*.h' '*.hpp')
```

> If a preset or file referenced above does not exist yet, the project is still
> being scaffolded — create it following the conventions in this document rather
> than inventing a different structure.

## Conventions

- **C++ style:** follow `.clang-format` (to be added; default to LLVM style with
  4-space indent until then). Prefer RAII, `std::expected`/`Result`-style error
  handling over exceptions across API boundaries, and `std::span`/`string_view`
  for non-owning views.
- **Headers:** public API in `include/obd/`, implementation details stay in
  `src/`. Use `#pragma once`.
- **Naming:** `PascalCase` for types, `camelCase` for functions/methods,
  `snake_case` for variables and files, `kPascalCase` for constants.
- **Errors:** never `abort()` or leak adapter handles on error paths. Surface
  transport/protocol failures as typed errors.
- **No hardware in CI:** anything touching a real adapter must sit behind an
  abstraction (`Transport` interface) so it can be mocked. Unit tests must not
  require a physical device.
- **Safety:** code that writes to the vehicle (clearing DTCs, mode 08 tests)
  must be explicit, opt-in, and clearly logged. Never make write operations the
  default.

## Workflow expectations for agents

1. **Read before writing.** Skim `docs/ARCHITECTURE.md` and `docs/OBD.md` before
   changing core logic.
2. **Small, focused changes.** One concern per PR/commit.
3. **Keep docs in sync.** If you change the module layout, build commands, or
   conventions, update this file and the relevant `docs/` page in the same change.
4. **Tests alongside code.** New behavior needs tests; transports get mocked.
5. **Commit messages:** imperative mood, concise subject (`Add ELM327 serial
   transport`), body explains *why* when it isn't obvious.
6. **Don't commit secrets or device-specific paths.** Use config/flags.

## Good first reading order

1. [`README.md`](README.md) — what & why.
2. This file — how we work.
3. [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — where things live.
4. [`docs/OBD.md`](docs/OBD.md) — the protocol domain.
