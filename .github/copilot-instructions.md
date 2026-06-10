# GitHub Copilot / VS Code instructions

This repository keeps a single source of truth for coding-agent and contributor
guidance in **[`../AGENTS.md`](../AGENTS.md)**.

When generating or reviewing code here, follow `AGENTS.md` for:

- Tech stack (C# / .NET 10, xUnit, GitHub Actions; cross-platform Windows/Linux/macOS).
- Repository layout (`src/ObdFree.Core`, `src/ObdFree.Cli`, `tests/ObdFree.Core.Tests`).
- Coding conventions (file-scoped namespaces, nullable enabled, naming, async, `.editorconfig`).
- Testing: heavy coverage is required; new logic ships with xUnit tests.
- Safety rules (vehicle-write operations are explicit and opt-in; no real
  hardware in tests — use the `IObdTransport` abstraction / `FakeObdTransport`).

Other useful docs: [`../README.md`](../README.md),
[`../docs/ARCHITECTURE.md`](../docs/ARCHITECTURE.md),
[`../docs/OBD.md`](../docs/OBD.md).
