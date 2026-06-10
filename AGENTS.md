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
decodes vehicle metadata. The codebase is a reusable .NET core library
(`ObdFree.Core`) with **two thin front-ends** on top: a console CLI
(`ObdFree.Cli`, produces the `obd` executable) and a cross-platform desktop GUI
(`ObdFree.Gui`, built with Avalonia). All real logic lives in `ObdFree.Core`; the
front-ends must stay thin and never duplicate it.

## Non-negotiable principle: free & open forever

This tool is, and will **always remain, free and open-source** (Apache-2.0). No
subscriptions, accounts, ads, paywalled features, or telemetry — ever. Do not
add dependencies, services, or features that would compromise this. When in
doubt, choose the option that keeps the tool free, offline-capable, and
auditable.

## Tech stack

- **Language:** C# (latest language version).
- **Runtime:** .NET 10 (`net10.0`), pinned via `global.json`.
- **Cross-platform:** must build and run on **Windows, Linux (incl. Ubuntu), and
  macOS**. Don't use platform-specific APIs without an abstraction + fallback.
- **GUI:** [Avalonia](https://avaloniaui.net/) (MIT) with MVVM via
  `CommunityToolkit.Mvvm`. Chosen over MAUI/WinForms/WPF because it's the only
  free, open-source toolkit that runs on all three OSes. Keep logic in
  `ObdFree.Core` and view models thin and testable.
- **Testing:** xUnit + `coverlet` for coverage. **Heavy test coverage is a
  first-class requirement** — new logic ships with tests.
- **CI/CD:** GitHub Actions (`.github/workflows/`). We lean on GitHub
  infrastructure as much as possible (Actions, artifacts, releases, Dependabot).
- **Quality gates:** warnings-as-errors, .NET analyzers, and `dotnet format` are
  enforced in the build and in CI.

## Repository layout

```
.
├── ObdFree.sln                  # solution
├── global.json                  # pins the .NET SDK
├── Directory.Build.props        # shared MSBuild settings for all projects
├── .editorconfig                # formatting & analyzer style rules
├── src/
│   ├── ObdFree.Core/            # core library: transports, protocol, decoding
│   ├── ObdFree.Cli/             # console CLI (AssemblyName: obd)
│   └── ObdFree.Gui/             # Avalonia desktop GUI (MVVM)
├── tests/
│   ├── ObdFree.Core.Tests/      # xUnit tests (incl. FakeObdTransport)
│   └── ObdFree.Gui.Tests/       # xUnit tests for the GUI view models
├── .github/workflows/ci.yml     # build + test matrix + format check
├── scripts/                     # cross-platform build/test/run/publish helpers
└── docs/                        # human + agent documentation
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for module responsibilities
and data flow, and [`docs/OBD.md`](docs/OBD.md) for the OBD-II domain primer.

## Build & test commands

```bash
dotnet restore                              # restore dependencies
dotnet build                                # build (warnings are errors)
dotnet test                                 # run all tests
dotnet test --collect:"XPlat Code Coverage" # tests + coverage
dotnet format                               # apply formatting
dotnet format --verify-no-changes           # CI formatting gate
dotnet run --project src/ObdFree.Cli        # run the CLI
```

Cross-platform helper scripts live in `scripts/` (`*.sh` for Linux/macOS,
`*.ps1` for Windows): `build`, `test`, `run-cli` / `run-gui` (compile + run each
version), and `publish-cli` / `publish-gui` (self-contained single-file binary
per RID into `artifacts/cli/` and `artifacts/gui/`). On Ubuntu/Debian,
`setup-ubuntu.sh` installs the .NET SDK, adapter packages, and Avalonia GUI
runtime libraries via `apt`. See [`scripts/README.md`](scripts/README.md).

## Conventions

- **Style:** enforced by `.editorconfig` and analyzers. File-scoped namespaces,
  braces always, `System` usings first, 4-space indent (2 for XML/YAML/JSON).
- **Nullable reference types** are enabled everywhere — honor them, no `!` to
  paper over warnings.
- **Naming:** `PascalCase` for types/methods/properties, `camelCase` for locals
  and parameters, `_camelCase` for private fields, `PascalCase` for constants.
- **Async:** suffix async methods with `Async`, accept a
  `CancellationToken` (default it), don't block on `.Result`/`.Wait()`.
- **XML docs:** public members in `ObdFree.Core` are documented
  (`GenerateDocumentationFile` is on; missing docs fail the build). Test projects
  are exempt.
- **No hardware in CI/tests:** anything touching a real adapter sits behind the
  `IObdTransport` abstraction. Tests use `FakeObdTransport`; they must never
  require a physical device.
- **Pure where possible:** decoders (PID/DTC) are pure functions — easy to test
  exhaustively with `[Theory]`/`[InlineData]`.
- **Safety:** code that writes to the vehicle (clearing DTCs, mode 04/08) must be
  explicit, opt-in, and clearly logged. Never make write operations the default.

## Workflow expectations for agents

1. **Read before writing.** Skim `docs/ARCHITECTURE.md` and `docs/OBD.md` before
   changing core logic.
2. **Small, focused changes.** One concern per PR/commit.
3. **Tests alongside code.** New behavior needs tests; transports get faked.
   Aim to keep coverage high — don't merge logic without tests.
4. **Green build locally.** Run `dotnet build`, `dotnet test`, and
   `dotnet format --verify-no-changes` before pushing — CI runs all three on
   Windows, Linux, and macOS.
5. **Keep docs in sync.** If you change the module layout, build commands, or
   conventions, update this file and the relevant `docs/` page in the same change.
6. **Commit messages:** imperative mood, concise subject (`Add ELM327 serial
   transport`), body explains *why* when it isn't obvious.
7. **Don't commit secrets or device-specific paths.** Use config/flags.

## Good first reading order

1. [`README.md`](README.md) — what & why.
2. This file — how we work.
3. [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — where things live.
4. [`docs/OBD.md`](docs/OBD.md) — the protocol domain.
