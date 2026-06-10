# GitHub Copilot / VS Code instructions

This repository keeps a single source of truth for coding-agent and contributor
guidance in **[`../AGENTS.md`](../AGENTS.md)**.

When generating or reviewing code here, follow `AGENTS.md` for:

- Tech stack (C++20, CMake + vcpkg, CTest).
- Repository layout (`src/libobd`, `src/cli`, `include/obd`, `tests`).
- Coding conventions (naming, error handling, header layout, formatting).
- Safety rules (vehicle-write operations are explicit and opt-in; no real
  hardware required for tests — use the `Transport` abstraction).

Other useful docs: [`../README.md`](../README.md),
[`../docs/ARCHITECTURE.md`](../docs/ARCHITECTURE.md),
[`../docs/OBD.md`](../docs/OBD.md).
