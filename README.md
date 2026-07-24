# MigrationScan

**A free, deterministic, offline .NET Framework migration assessment tool.**

It answers one question: *how much work is it to move this solution off .NET Framework, and what specifically blocks it?* It runs offline, produces the same output every time, requires no account, and never transmits your source code.

```console
dotnet tool install -g MigrationScan.Tool
migrationscan path/to/YourSolution.sln
```

> **Status: pre-release / under active development.** The foundation is in place (Phase 0). Rules, reports, and the CLI surface are being built out phase by phase — see [Roadmap](#roadmap). The install command above will work once the first release is published to NuGet.

📄 **[See a sample Markdown report →](docs/sample-report.md)** — the shareable artifact an engineering manager forwards to a CTO: executive summary, blockers, findings by project, an effort breakdown, and remediation guidance.

## Why this exists

Microsoft deprecated the .NET Upgrade Assistant in late 2025 and replaced it with the GitHub Copilot app modernization agent. The replacement needs a paid subscription and sends source code to a cloud service. Teams in finance, healthcare, defense, and government cannot do that — and they are the teams sitting on the largest .NET Framework estates. MigrationScan gives those teams an assessment they can run themselves, offline, with nothing leaving the machine.

## The promise

- **Offline by default.** No network calls in the default path. Network access is only available behind an explicit `--online` flag, and only for NuGet package compatibility lookups.
- **No telemetry.** No phoning home, no usage collection, no login.
- **Your code stays put.** Source is never transmitted anywhere.
- **Deterministic.** Same input, same output, every run.
- **No AI in the analysis path.** Findings come from static analysis, not an LLM.

## Non-goals

MigrationScan deliberately does **not**:

- Modify your source code
- Perform the upgrade
- Use AI or an LLM anywhere in the analysis path
- Phone home, collect telemetry, or require a login
- Produce a binding cost estimate — effort figures are heuristic planning aids, not a quote
- Require Visual Studio or MSBuild to be installed
- Replace human judgment on architectural decisions

## How it works

MigrationScan parses your `.sln`, `.csproj`, and `.vbproj` files as XML and reads your `.cs` and `.vb` source with Roslyn — no MSBuild registration and no Visual Studio required, so it runs the same on Windows, Linux, and macOS. Every finding carries a **confidence tier** so the report is honest about what static analysis can and cannot prove:

| Tier | Name | Source |
| --- | --- | --- |
| 1 | Certain | Project files, `packages.config`, `app.config`, `web.config`, `.sln` (XML, no ambiguity) |
| 2 | Probable | Roslyn syntax trees without a resolved compilation (good recall, some false positives) |
| 3 | Verified | Read from compiled assemblies via Cecil — see [binary analysis](#scanning-compiled-binaries) |

## Rules

MigrationScan ships a catalog of stable, never-reused rule IDs grouped by category (project/build, dependencies, blocking frameworks, runtime failures, configuration, serialization/security, data access, globalization). Each rule links to a remediation page under [`/docs/rules`](docs/rules).

### Implemented rules

| ID | Rule | Severity | Tier |
| --- | --- | --- | --- |
| [MIG1001](docs/rules/MIG1001.md) | Non-SDK-style project file | Medium | 1 — Certain |
| [MIG1002](docs/rules/MIG1002.md) | `packages.config` instead of PackageReference | Medium | 1 — Certain |
| [MIG1003](docs/rules/MIG1003.md) | Target framework below 4.6.2 | Medium | 1 — Certain |
| [MIG1005](docs/rules/MIG1005.md) | GAC reference (no HintPath) | Medium | 1 — Certain |
| [MIG1006](docs/rules/MIG1006.md) | COM reference or interop assembly (Windows lock-in) | Medium | 1 — Certain |
| [MIG1007](docs/rules/MIG1007.md) | Legacy project type (SSRS, SSIS, setup, Silverlight, Web Site) | High | 1 — Certain |
| [MIG1010](docs/rules/MIG1010.md) | Vendored DLL with no source and no NuGet equivalent | High | 1 — Certain |
| [MIG2001](docs/rules/MIG2001.md) | Package has no version supporting the target framework | High | 1 — Certain |
| [MIG2002](docs/rules/MIG2002.md) | Package marked deprecated on nuget.org (`--online`) | Medium | 1 — Certain |
| [MIG3001](docs/rules/MIG3001.md) | ASP.NET WebForms | Blocker | 1 — Certain |
| [MIG3002](docs/rules/MIG3002.md) | `System.Web` dependency outside WebForms | High | 1 — Certain |
| [MIG3003](docs/rules/MIG3003.md) | ASMX web service | High | 1 — Certain |
| [MIG3004](docs/rules/MIG3004.md) | WCF service host (server side) | High | 2 — Probable |
| [MIG3005](docs/rules/MIG3005.md) | .NET Remoting | Blocker | 2 — Probable |
| [MIG3009](docs/rules/MIG3009.md) | MSMQ (`System.Messaging`) | High | 2 — Probable |
| [MIG3010](docs/rules/MIG3010.md) | ASP.NET MVC 5 (`System.Web.Mvc`) | High | 2 — Probable |
| [MIG3015](docs/rules/MIG3015.md) | WCF client (`System.ServiceModel`) | Medium | 2 — Probable |
| [MIG4001](docs/rules/MIG4001.md) | `System.Drawing.Common` on non-Windows | High | 2 — Probable |
| [MIG4002](docs/rules/MIG4002.md) | Windows Registry access | High | 2 — Probable |
| [MIG4003](docs/rules/MIG4003.md) | `System.Management` / WMI | High | 2 — Probable |
| [MIG4004](docs/rules/MIG4004.md) | `System.DirectoryServices` / Active Directory | High | 2 — Probable |
| [MIG4005](docs/rules/MIG4005.md) | `EventLog` | Medium | 2 — Probable |
| [MIG4008](docs/rules/MIG4008.md) | `Thread.Abort` | Medium | 2 — Probable |
| [MIG4013](docs/rules/MIG4013.md) | P/Invoke to a Windows system DLL (Windows lock-in) | Medium | 2 — Probable |
| [MIG5001](docs/rules/MIG5001.md) | `ConfigurationManager.AppSettings` usage | Low | 2 — Probable |
| [MIG6001](docs/rules/MIG6001.md) | `BinaryFormatter` (removed in .NET 9) | Blocker | 2 — Probable |
| [MIG6004](docs/rules/MIG6004.md) | Code Access Security attributes | Medium | 2 — Probable |
| [MIG6005](docs/rules/MIG6005.md) | Obsolete cryptography types | Medium | 2 — Probable |
| [MIG7001](docs/rules/MIG7001.md) | `System.Data.SqlClient` | Medium | 2 — Probable |
| [MIG7003](docs/rules/MIG7003.md) | `System.Data.OleDb` on non-Windows | Medium | 2 — Probable |
| [MIG7006](docs/rules/MIG7006.md) | LINQ to SQL (`System.Data.Linq`) | High | 2 — Probable |
| [MIG8002](docs/rules/MIG8002.md) | `Encoding.Default` behavior change | Medium | 2 — Probable |
| [MIG8003](docs/rules/MIG8003.md) | Code-page encoding without provider registration | Medium | 2 — Probable |

More rules land phase by phase; see the [full catalog in the spec](migrationscan-spec.md#6-rule-catalog).

## Usage

```
migrationscan <path> [options]

  <path>                  .sln, .csproj, .vbproj, .dll/.exe, or directory to scan

  --target <tfm>          Target framework (default: net10.0). A -windows TFM
                          (net10.0-windows) treats Windows lock-in findings as satisfied.
  --format <fmt>          console | markdown | json | sarif (repeatable)
  --output <path>         Output file or directory
  --rules <ids>           Include only these rule IDs
  --exclude <ids>         Exclude these rule IDs
  --fail-on <severity>    blocker | high | medium | low
  --online                Allow NuGet.org lookups for package compatibility
  --baseline <path>       Suppress findings present in a baseline file
  --verbosity <level>     quiet | normal | detailed
```

`console` always writes to stdout. For `json`/`markdown`, `--output` may be a **file**
(written as-is for a single format) or a **directory** (receives `report.json` /
`report.md`). When several file formats share one `--output` file path, each is written with
its own extension so they don't overwrite each other.

### Cross-platform vs. staying on Windows (`--target`)

Modern .NET can still target Windows (`net10.0-windows`), where COM interop, P/Invoke to
Win32, the Registry, WMI, and similar continue to work. Those APIs are **Windows lock-in** —
they are only migration cost if you also need to leave Windows. MigrationScan reflects that in
the target:

```console
migrationscan MyApp.sln                          # cross-platform (net10.0) — the loud default
migrationscan MyApp.sln --target net10.0-windows # staying on Windows
```

On a `-windows` target, the Windows lock-in findings are **downgraded**: still listed (under a
"satisfied by target" section, and flagged `satisfiedByTarget` in JSON / suppressed in SARIF),
but excluded from the severity counts, the effort estimate, and `--fail-on`. Gone-everywhere
findings — WebForms, `BinaryFormatter`, Remoting, MVC 5, and the rest — stay at full severity
regardless of target. Run it both ways to see exactly what portability costs:

```console
migrationscan MyApp.sln --format json -o cross-platform.json
migrationscan MyApp.sln --target net10.0-windows --format json -o windows.json
```

### Exit codes

| Code | Meaning |
| --- | --- |
| 0 | No findings above threshold |
| 1 | Findings above `--fail-on` threshold |
| 2 | Analysis error |
| 64 | Bad usage |

### Online package checks (`--online`)

By default MigrationScan makes **no network calls** and the output is fully deterministic.
Passing `--online` opts in to nuget.org lookups for package status — currently flagging
packages the maintainers have marked **deprecated** ([MIG2002](docs/rules/MIG2002.md)):

```console
migrationscan . --online
```

Because these findings reflect live nuget.org state, they are not part of the deterministic
default path. If a lookup fails (offline, rate-limited), the scan degrades gracefully — it
prints a warning and continues without package status rather than failing.

### Scanning compiled binaries

When you don't have the source — a third-party component, or an early look at a client's
build output — point MigrationScan at a compiled assembly:

```console
migrationscan path/to/YourApp.dll
```

It reads the assembly with Mono.Cecil and flags references to assemblies that aren't available
on modern .NET (`System.Web`, `System.Drawing`, `System.Management`, `System.Messaging`, …).
These are **Tier 3 — Verified** findings: read from the compiled metadata rather than inferred
from syntax. Source-based scanning (a `.sln`/`.csproj`) remains richer; binary analysis is the
fallback for when source isn't on hand.

## Continuous integration

MigrationScan is built for CI: machine-readable output, meaningful [exit codes](#exit-codes),
and no interactive prompts.

### GitHub code scanning

Emit SARIF and upload it — findings show up inline on the **Security → Code scanning** tab
and as annotations on pull requests:

```yaml
name: Migration scan
on: [push, pull_request]

permissions:
  contents: read
  security-events: write   # required to upload SARIF

jobs:
  migrationscan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet tool install -g MigrationScan.Tool
      - run: migrationscan . --format sarif --output migrationscan.sarif
      - uses: github/codeql-action/upload-sarif@v3
        with:
          sarif_file: migrationscan.sarif
```

> SARIF file paths are relative to the scan root (the directory or solution you point at), so
> run the scan from the repository root for the annotations to line up with your files.

### Failing the build on regressions

Use `--fail-on` to return exit code `1` when a finding is at least as severe as the threshold:

```console
migrationscan . --fail-on high        # fail on any high or blocker finding
```

### Baselining an existing estate

Adopt the tool on a large legacy codebase without failing on day one: capture a baseline, then
fail only on **new** findings.

```console
migrationscan . --format json --output migrationscan-baseline.json   # once, committed to the repo
migrationscan . --baseline migrationscan-baseline.json --fail-on high # in CI: only new findings count
```

A baseline is just a JSON report captured earlier; findings whose rule, file, and message match
one in the baseline are suppressed. (Line numbers are ignored, so baselined findings survive
unrelated edits that shift lines.)

### Building on the output

The JSON is a stable, versioned, deterministic feed meant to be consumed by other tools —
dashboards, portfolio rollups, or your own scoping/estimating layer. Alongside the findings it
carries an effort rollup (`summary.effort` and a per-`projects` breakdown, in engineer-day
ranges) and a `notAssessed` list of non-C#/VB projects (SQL, deployment, …) that need planning
of their own — so coverage gaps are explicit, not silent. See the
[output schema](docs/schema) for the full shape and consumer notes. Effort figures are
heuristic planning aids, not a quote — apply your own rates and judgment downstream.

## Limitations

Static analysis without resolved references cannot see everything, and MigrationScan is honest about that rather than pretending to certainty:

- **Tier 2 findings can be false positives.** A reference to a type named `Registry` might be your own class, not `Microsoft.Win32.Registry`. These are reported as *probable*, never certain.
- **Source scanning has no resolved compilation.** Tier 2 findings come from syntax alone. For extra confidence you can also scan compiled binaries (Tier 3, via `migrationscan YourApp.dll`), which reads referenced assemblies from the assembly metadata.
- **Effort figures are heuristic.** They are planning aids derived from static analysis, not a quote.
- **Architectural decisions are yours.** MigrationScan flags what blocks a migration; it does not decide how to redesign around it.

## Building from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```console
git clone <repo-url>
cd MigrationScan
dotnet build
dotnet test
```

## Roadmap

Development proceeds in ordered phases (see the spec for detail):

- [x] **Phase 0** — Foundation: repo, license, CI on Linux/Windows/macOS, empty solution
- [x] **Phase 1** — Walking skeleton: parse `.sln`/`.csproj`, first rule (MIG1001), console + JSON output
- [x] **Phase 2** — Rule engine (project-file + Roslyn syntax rules) and the first rule batch
- [x] **Phase 3** — Roslyn syntax rules (Tier 2): 12 runtime/blocking-framework detectors
- [x] **Phase 4** — Effort model and Markdown report (golden-file tested)
- [x] **Phase 5** — CI integration: SARIF, `--fail-on` exit codes, `--baseline`
- [x] **Phase 6** — Post-v1: `--online` NuGet deprecation lookups, VB.NET support (projects + source), Mono.Cecil binary analysis, and an expanded rule catalog (28 rules). Further catalog rules land as needed.

## Open questions

A few decisions from the spec are still open and will be resolved before v1:

- **VB.NET support** — `.vbproj` projects are discovered and their `.vb` source is analyzed by the same rules as C#: the syntax queries are language-neutral, so VB gets the runtime-failure (Tier 2) rules too, honouring VB's case-insensitive matching.
- **Default target framework** — pinned to `net10.0` (LTS) for now; may float to whatever is current LTS.
- **Schema distribution** — ship a `--json-schema` command vs. publish the schema as a static file.

## License

[Apache-2.0](LICENSE). The patent grant matters for enterprise legal review.
