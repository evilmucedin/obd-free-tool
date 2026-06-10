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

- A C++20 compiler (Clang 16+, GCC 13+, or MSVC 2022+).
- CMake >= 3.25.
- [vcpkg](https://vcpkg.io) (used in manifest mode; the CMake preset wires it up).
- `clang-format` for formatting.

```bash
# Clone
git clone https://github.com/evilmucedin/obd-free-tool.git
cd obd-free-tool

# Configure, build, test
cmake --preset default
cmake --build --preset default
ctest --preset default --output-on-failure
```

> If these presets don't exist yet, the build is still being scaffolded — follow
> the structure in `AGENTS.md` and `docs/ARCHITECTURE.md` when adding them.

## Making changes

1. Create a branch: `git checkout -b feature/short-description`.
2. Keep changes small and focused — one concern per PR.
3. Add or update tests. Hardware-touching code must be tested via
   `MockTransport`; nothing in CI may require a physical adapter.
4. Update docs in the same PR when you change layout, commands, or conventions.
5. Format your code: `clang-format -i $(git ls-files '*.cpp' '*.h' '*.hpp')`.

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
