# Build & run scripts

Cross-platform helper scripts so you don't have to memorize `dotnet` flags.
Every task ships in two flavors:

- `*.sh` — POSIX shell, for **Linux** and **macOS**.
- `*.ps1` — PowerShell, for **Windows** (and any cross-platform `pwsh` host).

All scripts can be run from anywhere; they resolve the repo root themselves.

| Task | Linux / macOS | Windows |
|------|---------------|---------|
| Build the solution | `./scripts/build.sh [Debug\|Release]` | `./scripts/build.ps1 [-Configuration Release]` |
| Run tests + coverage | `./scripts/test.sh [Debug\|Release]` | `./scripts/test.ps1 [-Configuration Release]` |
| Compile **and** run the CLI | `./scripts/run.sh -- <args>` | `./scripts/run.ps1 <args>` |
| Publish a native binary | `./scripts/publish.sh [RID]` | `./scripts/publish.ps1 [-Rid <rid>]` |

## Examples

```bash
# Build, test, and launch the CLI (Linux/macOS)
./scripts/build.sh
./scripts/test.sh
./scripts/run.sh -- --help
```

```powershell
# Same, on Windows
./scripts/build.ps1
./scripts/test.ps1
./scripts/run.ps1 --help
```

## Publishing self-contained binaries

`publish.sh` / `publish.ps1` produce a **self-contained, single-file** executable
(no .NET install required on the target machine) under `artifacts/<RID>/`.

If no Runtime Identifier (RID) is given, the host platform is auto-detected.
Supported RIDs:

| Platform | RID |
|----------|-----|
| Windows x64 | `win-x64` |
| Windows ARM64 | `win-arm64` |
| Linux x64 | `linux-x64` |
| Linux ARM64 | `linux-arm64` |
| macOS Intel | `osx-x64` |
| macOS Apple Silicon | `osx-arm64` |

```bash
# Cross-compile a Windows binary from Linux/macOS
./scripts/publish.sh win-x64
# Auto-detect the current platform
./scripts/publish.sh
```

> The CLI is free and open-source forever — these binaries are too. Ship them
> anywhere.
