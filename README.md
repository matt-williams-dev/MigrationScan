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

MigrationScan parses your `.sln` and `.csproj` files as XML and reads your `.cs` files with Roslyn — no MSBuild registration and no Visual Studio required, so it runs the same on Windows, Linux, and macOS. Every finding carries a **confidence tier** so the report is honest about what static analysis can and cannot prove:

| Tier | Name | Source |
| --- | --- | --- |
| 1 | Certain | Project files, `packages.config`, `app.config`, `web.config`, `.sln` (XML, no ambiguity) |
| 2 | Probable | Roslyn syntax trees without a resolved compilation (good recall, some false positives) |
| 3 | Verified | Semantic model or compiled assemblies via Cecil (post-v1) |

## Rules

MigrationScan ships a catalog of stable, never-reused rule IDs grouped by category (project/build, dependencies, blocking frameworks, runtime failures, configuration, serialization/security, data access, globalization). Each rule links to a remediation page under [`/docs/rules`](docs/rules).

### Implemented rules

| ID | Rule | Severity | Tier |
| --- | --- | --- | --- |
| [MIG1001](docs/rules/MIG1001.md) | Non-SDK-style project file | Medium | 1 — Certain |
| [MIG1002](docs/rules/MIG1002.md) | `packages.config` instead of PackageReference | Medium | 1 — Certain |
| [MIG1005](docs/rules/MIG1005.md) | GAC reference (no HintPath) | Medium | 1 — Certain |
| [MIG2001](docs/rules/MIG2001.md) | Package has no version supporting the target framework | High | 1 — Certain |
| [MIG3001](docs/rules/MIG3001.md) | ASP.NET WebForms | Blocker | 1 — Certain |
| [MIG3002](docs/rules/MIG3002.md) | `System.Web` dependency outside WebForms | High | 1 — Certain |
| [MIG3004](docs/rules/MIG3004.md) | WCF service host (server side) | High | 2 — Probable |
| [MIG3005](docs/rules/MIG3005.md) | .NET Remoting | Blocker | 2 — Probable |
| [MIG3010](docs/rules/MIG3010.md) | ASP.NET MVC 5 (`System.Web.Mvc`) | High | 2 — Probable |
| [MIG4001](docs/rules/MIG4001.md) | `System.Drawing.Common` on non-Windows | High | 2 — Probable |
| [MIG4002](docs/rules/MIG4002.md) | Windows Registry access | High | 2 — Probable |
| [MIG4004](docs/rules/MIG4004.md) | `System.DirectoryServices` / Active Directory | High | 2 — Probable |
| [MIG4008](docs/rules/MIG4008.md) | `Thread.Abort` | Medium | 2 — Probable |
| [MIG5001](docs/rules/MIG5001.md) | `ConfigurationManager.AppSettings` usage | Low | 2 — Probable |
| [MIG6001](docs/rules/MIG6001.md) | `BinaryFormatter` (removed in .NET 9) | Blocker | 2 — Probable |
| [MIG6004](docs/rules/MIG6004.md) | Code Access Security attributes | Medium | 2 — Probable |
| [MIG7001](docs/rules/MIG7001.md) | `System.Data.SqlClient` | Medium | 2 — Probable |
| [MIG8002](docs/rules/MIG8002.md) | `Encoding.Default` behavior change | Medium | 2 — Probable |
| [MIG8003](docs/rules/MIG8003.md) | Code-page encoding without provider registration | Medium | 2 — Probable |

More rules land phase by phase; see the [full catalog in the spec](migrationscan-spec.md#6-rule-catalog).

## Usage

```
migrationscan <path> [options]

  <path>                  .sln, .csproj, or directory to scan recursively

  --target <tfm>          Target framework (default: net10.0)
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

### Exit codes

| Code | Meaning |
| --- | --- |
| 0 | No findings above threshold |
| 1 | Findings above `--fail-on` threshold |
| 2 | Analysis error |
| 64 | Bad usage |

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

## Limitations

Static analysis without resolved references cannot see everything, and MigrationScan is honest about that rather than pretending to certainty:

- **Tier 2 findings can be false positives.** A reference to a type named `Registry` might be your own class, not `Microsoft.Win32.Registry`. These are reported as *probable*, never certain.
- **No semantic guarantees without compilation.** Full verification (Tier 3) requires resolved references or compiled binaries, which is post-v1 work.
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
- [ ] **Phase 6** — Post-v1: `--online` lookups, binary analysis, VB.NET, remaining rules

## Open questions

A few decisions from the spec are still open and will be resolved before v1:

- **VB.NET support** — a large share of the legacy estate, significant extra work. Currently deferred to Phase 6.
- **Default target framework** — pinned to `net10.0` (LTS) for now; may float to whatever is current LTS.
- **Schema distribution** — ship a `--json-schema` command vs. publish the schema as a static file.

## License

[Apache-2.0](LICENSE). The patent grant matters for enterprise legal review.
