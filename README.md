# MigrationScan

**Find out what it costs to move a .NET Framework solution to modern .NET.**

MigrationScan tells you what blocks the migration, how much work each item is, and how far to trust each answer. It runs with no network, no account, and no source code leaving your machine.

Download the executable for your platform from the [releases page](../../releases), drop it in the folder you want to assess, and run it:

```console
migrationscan path/to/YourSolution.sln
```

You get one file, `migrationscan-report.json`, plus a summary on screen. No .NET install, no admin rights, no flags to learn. Run it with no arguments and it scans the current directory, so you can drop the executable into a repository root and double-click it.

Already have the .NET 10 SDK? `dotnet tool install -g MigrationScan.Tool` gets you the same tool.

Downloads are signed. Matthew Williams, owner of MW Creative LLC, the parent company of MW Consulting, holds both certificates, so that is the name Windows shows in the SmartScreen prompt and macOS shows when you open the installer. macOS ships as a `.pkg` that installs `migrationscan` into `/usr/local/bin`, because Apple staples a notarization ticket to a package and not to a bare executable, and a stapled ticket means Gatekeeper clears the tool without calling home. Every asset comes with a `.sha256` file, signed or not, to check your download against.

> **Status: early release.** 33 rules across 8 categories, both portability targets, and a redacted report format. Expect the rule catalog to keep growing. See the [roadmap](#roadmap).

📄 **[See a sample Markdown report →](docs/sample-report.md)** is the artifact an engineering manager forwards to a CTO: executive summary, blockers, findings by project, an effort breakdown, and remediation guidance.

📊 **[Or the same report over a real eleven-project estate →](docs/samples/eshop-modernizing/)**, Microsoft's archived eShopModernizing sample: 108 findings and 197 third-party references, read in 1.5 seconds.

## Why this exists

Microsoft [deprecated the .NET Upgrade Assistant](https://learn.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-overview) and now points teams at the GitHub Copilot modernization agent instead. That agent needs a GitHub account with Copilot access, and it works by sending your code to a model service.

For plenty of teams in finance, healthcare, defense and government, routing source through a third party is not a budget question. It is a policy they cannot get an exception to. Those same teams hold the largest .NET Framework estates, which leaves them the least served by the tooling and the most in need of it.

MigrationScan is what you run when the answer has to stay inside the building.

## The promise

- **Offline by default.** The default path makes no network calls. You get network access only by passing `--online`, and only for NuGet package compatibility lookups.
- **No telemetry.** Nothing phones home. No usage collection, no login.
- **Your code stays put.** MigrationScan transmits your source nowhere.
- **Deterministic.** Same input, same output, every run.
- **No AI in the analysis path.** Findings come from static analysis.

## Non-goals

MigrationScan will not:

- Modify your source code
- Perform the upgrade
- Use an LLM anywhere in the analysis path
- Phone home, collect telemetry, or require a login
- Give you a binding cost estimate. Effort figures are planning aids, not a quote.
- Require Visual Studio or MSBuild
- Replace human judgment on architectural decisions

## How it works

MigrationScan parses your `.sln`, `.csproj` and `.vbproj` files as XML, then reads your `.cs` and `.vb` source with Roslyn. It registers no MSBuild and needs no Visual Studio, so it behaves the same on Windows, Linux and macOS.

Every finding carries a **confidence tier**, because static analysis can prove some things and only suspect others:

| Tier | Name | Source |
| --- | --- | --- |
| 1 | Certain | Project files, `packages.config`, `app.config`, `web.config`, `.sln`. XML, no ambiguity. |
| 2 | Probable | Roslyn syntax trees with no resolved compilation. Good recall, some false positives. |
| 3 | Verified | Read from compiled assemblies via Cecil. See [binary analysis](#scanning-compiled-binaries). |

## Reference inventory

Findings tell you what's broken. The **reference inventory** tells you what you depend on: every
NuGet package, GAC and vendored assembly, COM/ActiveX component, project reference and ASMX/WCF
service proxy your projects declare, with versions and where each one resolves from.

Entries carry no severity and no effort, and they never fail a build. They exist because the
expensive unknowns in a migration usually belong to somebody else. A grid control from a vendor
that folded, or a type library nobody ever built for 64-bit, will cost you weeks. Rules catch the
ones matching a known pattern; the inventory hands you the rest of the list to research.

```
Third-party references (10 distinct), inventory only, not counted above:
  • nuget   Newtonsoft.Json 13.0.3
  • gac     Telerik.Web.UI 2015.3.930.45
  • dll     Contoso.Payments 2.1.0.0
  • com     AxInterop.MSCommLib 1.0.0.0
  • com     MSXML2 6.0
  • svc     PricingService
  (Also read, not listed: 1 framework, 1 solution-internal.)
```

The Markdown report renders it as a table with per-component project counts; the JSON exposes it
as a `references` array for scripting. See [the references doc](docs/references.md) for what each
kind covers, the classification judgment calls, and what deliberately isn't collected.

## Rules

MigrationScan ships a catalog of stable, never-reused rule IDs grouped by category (project/build, dependencies, blocking frameworks, runtime failures, configuration, serialization/security, data access, globalization). Each rule links to a remediation page under [`/docs/rules`](docs/rules).

### Implemented rules

| ID | Rule | Severity | Tier |
| --- | --- | --- | --- |
| [MIG1001](docs/rules/MIG1001.md) | Non-SDK-style project file | Medium | 1 Certain |
| [MIG1002](docs/rules/MIG1002.md) | `packages.config` instead of PackageReference | Medium | 1 Certain |
| [MIG1003](docs/rules/MIG1003.md) | Target framework below 4.6.2 | Medium | 1 Certain |
| [MIG1005](docs/rules/MIG1005.md) | GAC reference (no HintPath) | Medium | 1 Certain |
| [MIG1006](docs/rules/MIG1006.md) | COM reference or interop assembly (Windows lock-in) | Medium | 1 Certain |
| [MIG1007](docs/rules/MIG1007.md) | Legacy project type (SSRS, SSIS, setup, Silverlight, Web Site) | High | 1 Certain |
| [MIG1010](docs/rules/MIG1010.md) | Vendored DLL with no source and no NuGet equivalent | High | 1 Certain |
| [MIG2001](docs/rules/MIG2001.md) | Package has no version supporting the target framework | High | 1 Certain |
| [MIG2002](docs/rules/MIG2002.md) | Package marked deprecated on nuget.org (`--online`) | Medium | 1 Certain |
| [MIG3001](docs/rules/MIG3001.md) | ASP.NET WebForms | Blocker | 1 Certain |
| [MIG3002](docs/rules/MIG3002.md) | `System.Web` dependency outside WebForms | High | 1 Certain |
| [MIG3003](docs/rules/MIG3003.md) | ASMX web service | High | 1 Certain |
| [MIG3004](docs/rules/MIG3004.md) | WCF service host (server side) | High | 2 Probable |
| [MIG3005](docs/rules/MIG3005.md) | .NET Remoting | Blocker | 2 Probable |
| [MIG3009](docs/rules/MIG3009.md) | MSMQ (`System.Messaging`) | High | 2 Probable |
| [MIG3010](docs/rules/MIG3010.md) | ASP.NET MVC 5 (`System.Web.Mvc`) | High | 2 Probable |
| [MIG3015](docs/rules/MIG3015.md) | WCF client (`System.ServiceModel`) | Medium | 2 Probable |
| [MIG4001](docs/rules/MIG4001.md) | `System.Drawing.Common` on non-Windows | High | 2 Probable |
| [MIG4002](docs/rules/MIG4002.md) | Windows Registry access | High | 2 Probable |
| [MIG4003](docs/rules/MIG4003.md) | `System.Management` / WMI | High | 2 Probable |
| [MIG4004](docs/rules/MIG4004.md) | `System.DirectoryServices` / Active Directory | High | 2 Probable |
| [MIG4005](docs/rules/MIG4005.md) | `EventLog` | Medium | 2 Probable |
| [MIG4008](docs/rules/MIG4008.md) | `Thread.Abort` | Medium | 2 Probable |
| [MIG4013](docs/rules/MIG4013.md) | P/Invoke to a Windows system DLL (Windows lock-in) | Medium | 2 Probable |
| [MIG5001](docs/rules/MIG5001.md) | `ConfigurationManager.AppSettings` usage | Low | 2 Probable |
| [MIG6001](docs/rules/MIG6001.md) | `BinaryFormatter` (removed in .NET 9) | Blocker | 2 Probable |
| [MIG6004](docs/rules/MIG6004.md) | Code Access Security attributes | Medium | 2 Probable |
| [MIG6005](docs/rules/MIG6005.md) | Obsolete cryptography types | Medium | 2 Probable |
| [MIG7001](docs/rules/MIG7001.md) | `System.Data.SqlClient` | Medium | 2 Probable |
| [MIG7003](docs/rules/MIG7003.md) | `System.Data.OleDb` on non-Windows | Medium | 2 Probable |
| [MIG7006](docs/rules/MIG7006.md) | LINQ to SQL (`System.Data.Linq`) | High | 2 Probable |
| [MIG8002](docs/rules/MIG8002.md) | `Encoding.Default` behavior change | Medium | 2 Probable |
| [MIG8003](docs/rules/MIG8003.md) | Code-page encoding without provider registration | Medium | 2 Probable |

More rules land phase by phase; see the [full catalog in the spec](migrationscan-spec.md#6-rule-catalog).

## Usage

```
migrationscan [path] [options]

  [path]                  .sln, .csproj, .vbproj, .dll/.exe, or directory to scan.
                          Defaults to the current directory.

  --target <tfm>          .NET version to assess against (default: net10.0). Both
                          portability stances always reported. See below.
  --format <fmt>          console | markdown | json | sarif (repeatable). Omit for the
                          default: a console summary plus migrationscan-report.json.
  --output <path>         Output file or directory
  --fail-on <severity>    blocker | high | medium | low
  --online                Allow NuGet.org lookups for package compatibility
  --baseline <path>       Suppress findings present in a baseline file
  --include-paths         Keep real file paths in the JSON (off by default)
```

That is the whole surface, and `--help` is the authority. `--fail-on` looks only at the stance
`--target` names, so carrying the second stance in the report never moves an exit code.

`console` always writes to stdout. For `json`/`markdown`, `--output` may be a **file**
(written as-is for a single format) or a **directory** (receives `report.json` /
`report.md`). When several file formats share one `--output` file path, each is written with
its own extension so they don't overwrite each other.

### Cross-platform vs. staying on Windows

Modern .NET still targets Windows through `net10.0-windows`, where COM interop, P/Invoke to
Win32, the Registry and WMI keep working. Those APIs are **Windows lock-in**. They cost you
something only if you also need to leave Windows.

**You never have to choose, and you never have to scan twice.** Every report covers both stances:

```jsonc
"targets": [
  { "target": "net10.0",         "stance": "crossPlatform", "summary": { /* 7.8–23 days */ } },
  { "target": "net10.0-windows", "stance": "windows",       "summary": { /* 5–15 days  */ } }
]
```

The gap between those two numbers is what portability costs you, and one scan produces it. The
target changes what a finding *costs*, never what the scan found, so the second stance is an
exact re-evaluation of the same analysis.

Your console and Markdown reports show the stance `--target` names, cross-platform by default.
There, Windows lock-in findings drop out of the severity counts, the effort estimate and
`--fail-on`. They still appear under a "satisfied by target" section, flagged
`satisfiedByTarget` in the JSON and suppressed in SARIF, so nothing hides from you. Findings
that break everywhere keep full severity either way: WebForms, `BinaryFormatter`, Remoting,
MVC 5 and the rest.

`--target` picks the .NET *version*, `net8.0` against `net10.0`. The platform axis always
reports both ways. To read the console summary from the Windows stance:

```console
migrationscan MyApp.sln --target net10.0-windows
```

The JSON comes out identical either way. Both stances are in it regardless.

### Exit codes

| Code | Meaning |
| --- | --- |
| 0 | No findings above threshold |
| 1 | Findings above `--fail-on` threshold |
| 2 | Analysis error |
| 64 | Bad usage |

### Online package checks (`--online`)

By default MigrationScan makes **no network calls** and the output is fully deterministic.
Pass `--online` and MigrationScan will ask nuget.org about package status. Today that means
flagging packages the maintainers marked **deprecated** ([MIG2002](docs/rules/MIG2002.md)):

```console
migrationscan . --online
```

Because these findings reflect live nuget.org state, they are not part of the deterministic
default path. When a lookup fails, because you are offline or rate-limited, the scan prints a
warning and carries on without package status instead of failing.

### Scanning compiled binaries

Sometimes you have no source: a third-party component, or an early look at a client's build
output. Point MigrationScan at the compiled assembly instead:

```console
migrationscan path/to/YourApp.dll
```

It reads the assembly with Mono.Cecil and flags references to assemblies that aren't available
on modern .NET (`System.Web`, `System.Drawing`, `System.Management`, `System.Messaging`, …).
Those come back as **Tier 3, Verified**, read from compiled metadata rather than guessed from
syntax. Source-based scanning (a `.sln`/`.csproj`) remains richer; binary analysis is the
fallback for when source isn't on hand.

## Continuous integration

MigrationScan is built for CI: machine-readable output, meaningful [exit codes](#exit-codes),
and no interactive prompts.

### GitHub code scanning

Emit SARIF and upload it. Findings then show up inline on the **Security → Code scanning** tab,
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

The JSON is a stable, versioned, deterministic feed for other tools:
dashboards, portfolio rollups, or your own scoping/estimating layer. Alongside the findings it
carries an effort rollup (`summary.effort` and a per-`projects` breakdown, in engineer-day
ranges) and a `notAssessed` list of non-C#/VB projects (SQL, deployment, …) that need planning
of their own, so coverage gaps stay visible. It also carries the full `references`
inventory, flat and per-project, for feeding a dependency-research workflow. See the
[output schema](docs/schema) for the full shape and consumer notes. Effort figures are
planning aids rather than a quote, so apply your own rates and judgment downstream.

## What's in the report (for your security review)

**The JSON report holds no source file paths.** Every `.cs` and `.vb` location becomes a stable
opaque id, so you can clear the file without anyone reading several thousand lines of JSON. Every
run says so on screen, and every Markdown report ends with a "What this report contains" section.
Project files are the deliberate exception: a report keeps the repo-relative path of each
`.csproj`, because that path is how a project is identified and grouping by project is what makes
a proposal readable downstream.

**It includes:** project paths, line numbers, rule identifiers with their remediation text, and
the names and versions of the dependencies your projects declare. That covers NuGet packages,
referenced assemblies, COM components, web-service endpoints and the Windows system libraries you
call through P/Invoke. A name identifies a component; it says nothing about where it sits on disk.
We keep names because nobody can assess a component without knowing which one it is.

**It does not include:** source file paths, source code, file contents, connection strings,
credentials, configuration values, web-service hosts and URLs, customer or business data, machine
or user names, or anything outside the folder you scanned.

Redaction covers the **JSON**, which is the file you send on. Your console, Markdown report and
SARIF output keep full paths. They stay on your machine, SARIF exists to point at a line in a
file, and hiding paths from your own developers would protect nobody. Add `--include-paths` if
you want the JSON to keep them too.

Two details worth knowing. A `fileId` stays stable, so you can still see that two findings share a
file, which helps when sizing work and discloses nothing. And a redacted report still works as a
`--baseline`, because each finding records its own fingerprint instead of deriving one from the
path.

Each report also records what produced it: the tool version, plus the commit you had checked out
if you scanned a git working tree.

```jsonc
"scan": { "toolVersion": "0.1.0", "commit": "0fc6524d7b26ccd2f1eca0d18d8b3792dc6dc675" }
```

There is deliberately no timestamp: the report is byte-identical for the same input by design,
which is what makes it diffable and baselineable. The commit is the better answer to "is this
scan stale?" anyway, since it names the exact revision assessed.

## Scanning a whole estate

Point MigrationScan at a directory and it assesses everything under it in one pass. Every
solution, every project, one report:

```console
migrationscan C:\code\LegacyEstate
```

Projects are the unit of truth here; solutions just group them. MigrationScan assesses a project
because it exists on disk, so **it still scans a project no solution references**. Those are the
ones that surface halfway through a migration and wreck the plan. You get them in the warnings, so
you can decide whether they belong in scope. A project shared by several solutions gets scanned
once.

MigrationScan skips build output, restored `packages/`, `node_modules` and dot-directories, so it
never mistakes a vendored source tree for your own code.

## Limitations

Static analysis without resolved references cannot see everything, so here is where MigrationScan stops short:

- **Tier 2 findings can be false positives.** A reference to a type named `Registry` might be your own class rather than `Microsoft.Win32.Registry`. MigrationScan reports these as *probable* and never claims certainty.
- **Source scanning has no resolved compilation.** Tier 2 findings come from syntax alone. Scan a compiled binary with `migrationscan YourApp.dll` for Tier 3 confidence, which reads referenced assemblies out of the assembly metadata.
- **Effort figures are heuristic.** Treat them as planning aids, not a quote.
- **The reference inventory covers what your projects *declare*.** It leaves out transitive package dependencies, `<Import>`ed build targets and binding redirects. See [what isn't collected](docs/references.md#what-isnt-collected). Resolving the full package graph would need a restore, and a restore needs the network.
- **Architectural decisions stay yours.** MigrationScan flags what blocks a migration. It will not tell you how to redesign around it.

## Building from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```console
git clone <repo-url>
cd MigrationScan
dotnet build
dotnet test
```

Releases ship from a `v*` tag. See [publishing](docs/PUBLISHING.md) for how, and the
[changelog](CHANGELOG.md) for what changed.

## Roadmap

Development runs in ordered phases. The [spec](migrationscan-spec.md) has the detail.

- [x] **Phase 0.** Foundation: repo, license, CI on Linux, Windows and macOS
- [x] **Phase 1.** Walking skeleton: parse `.sln`/`.csproj`, first rule, console and JSON output
- [x] **Phase 2.** Rule engine for project-file and Roslyn syntax rules, plus the first rule batch
- [x] **Phase 3.** Roslyn syntax rules at Tier 2: 12 runtime and blocking-framework detectors
- [x] **Phase 4.** Effort model and Markdown report, golden-file tested
- [x] **Phase 5.** CI integration: SARIF, `--fail-on` exit codes, `--baseline`
- [x] **Phase 6.** `--online` NuGet deprecation lookups, VB.NET support, Mono.Cecil binary analysis, expanded rule catalog
- [x] **Phase 7.** Redaction: the shared JSON carries opaque ids instead of paths (schema 1.6)

Next up is a wider rule catalog. Open an issue if a pattern in your estate goes unflagged.

## Design decisions worth knowing

- **VB.NET gets the same treatment as C#.** MigrationScan discovers `.vbproj` projects and reads `.vb` source. The syntax queries are language-neutral, so VB picks up the Tier 2 runtime rules too, with VB's case-insensitive matching honoured.
- **The default target stays pinned at `net10.0`** and moves only in a release you can see. A floating default would mean two versions of the tool disagreeing about your codebase for a reason the report never mentions, which would break the determinism promise everything else rests on.
- **The schema ships as a file, not a command.** [`docs/schema`](docs/schema) carries a JSON Schema per minor version, validated against real output in CI. You can link to it, reference it from a `$schema` key, and generate against it.

## License

[Apache-2.0](LICENSE). The patent grant matters for enterprise legal review.
