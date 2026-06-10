# Contributing

Thanks for your interest in `obd-free-tool`! This project is open-source and
free forever — contributions of all sizes are welcome.

## Before you start

- Read [`README.md`](README.md) for the project goals.
- Read [`AGENTS.md`](AGENTS.md) for the tech stack, layout, and conventions
  (it's written for AI agents but is the best human onboarding doc too).
- Skim [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) and
  [`docs/OBD.md`](docs/OBD.md) if you'll touch core logic.

## Development setup

Requirements:

- The [.NET 10 SDK](https://dotnet.microsoft.com/download) (the version is pinned
  in `global.json`).
- Any editor — Visual Studio, VS Code (C# Dev Kit), or Rider all work.

**On Ubuntu/Debian** you can install everything (the .NET SDK and adapter
packages) via apt in one step:

```bash
./scripts/setup-ubuntu.sh
```

```bash
# Clone
git clone https://github.com/evilmucedin/obd-free-tool.git
cd obd-free-tool

# Build, test, run
dotnet build
dotnet test
dotnet run --project src/ObdFree.Cli
```

The project builds and runs on **Windows, Linux (incl. Ubuntu), and macOS**.

## Making changes

1. Create a branch: `git checkout -b feature/short-description`.
2. Keep changes small and focused — one concern per PR.
3. Add or update tests. We care a lot about coverage — new logic ships with
   tests. Hardware-touching code must be tested via `FakeObdTransport`; nothing
   in CI may require a physical adapter.
4. Update docs in the same PR when you change layout, commands, or conventions.
5. Before pushing, make sure all three pass (CI enforces them on every OS):
   ```bash
   dotnet build
   dotnet test
   dotnet format --verify-no-changes
   ```

## Commit & PR guidelines

- Commit messages: imperative mood, concise subject (e.g. `Add ELM327 serial
  transport`); body explains *why* when non-obvious.
- Ensure the build and tests pass locally before opening a PR.
- Describe what changed and why in the PR description; link related issues.

## Code of conduct

Be respectful and constructive. We're here to build a useful tool together.

## License

By contributing, you agree your contributions are licensed under the
[Apache License 2.0](LICENSE).
