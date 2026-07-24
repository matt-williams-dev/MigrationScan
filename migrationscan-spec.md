# MigrationScan

**A free, deterministic, offline .NET Framework migration assessment tool.**

Working name. Confirm NuGet ID availability before committing to it.

- CLI command: `migrationscan`
- NuGet package: `MigrationScan.Tool`
- Install: `dotnet tool install -g MigrationScan.Tool`
- License: Apache-2.0 (patent grant matters for enterprise legal review)

---

## 1. Why this exists

Microsoft deprecated the .NET Upgrade Assistant in late 2025 and replaced it with the GitHub Copilot app modernization agent. The replacement needs a paid subscription and sends source code to a cloud service. Teams in finance, healthcare, defense, and government cannot do that, and they are the teams sitting on the largest .NET Framework estates.

MigrationScan answers one question: **how much work is it to move this solution off .NET Framework, and what specifically blocks it?**

It runs offline, produces the same output every time, requires no account, and never transmits source code.

## 2. Non-goals

State these in the README. They keep scope controlled and set expectations.

- Does not modify source code
- Does not perform the upgrade
- Does not use AI or an LLM anywhere in the analysis path
- Does not phone home, collect telemetry, or require a login
- Does not produce a binding cost estimate. Effort figures are heuristic planning aids
- Does not require Visual Studio or MSBuild to be installed
- Does not replace human judgment on architectural decisions

## 3. Design principles

**Deterministic.** Same input, same output, every run. No network calls in the default path.

**Offline by default.** Network access only behind an explicit `--online` flag, and only for NuGet package compatibility lookups.

**No MSBuild dependency.** Legacy projects use non-SDK-style `.csproj` files. Evaluating them properly needs MSBuild registration and usually a Visual Studio install, which kills cross-platform use. Parse `.sln` and `.csproj` as XML instead.

**Tiered confidence.** Full semantic analysis is impossible without resolved references. Be honest about that in the output rather than pretending to certainty. See section 5.

**Rules as data.** Keep the API catalog and package compatibility lists in JSON files, not hardcoded C#. Contributors can then add rules without touching the engine.

**CI-friendly.** Machine-readable output, meaningful exit codes, no interactive prompts.

## 4. Technology

| Concern | Choice |
| --- | --- |
| Target framework | `net10.0` |
| Language | C# 14, nullable enabled, warnings as errors |
| Source analysis | Roslyn (`Microsoft.CodeAnalysis.CSharp`) |
| Project/solution parsing | `System.Xml.Linq`, hand-rolled `.sln` parser |
| Binary analysis (phase 6) | `Mono.Cecil` |
| CLI | `System.CommandLine` |
| Testing | xUnit + `Verify` for golden-file report tests |
| CI | GitHub Actions, matrix across ubuntu/windows/macos |

Ask before adding any dependency beyond this list.

## 5. Analysis tiers

Every finding carries a confidence level derived from how it was detected.

**Tier 1 — Certain.** Derived from project files, `packages.config`, `app.config`, `web.config`, `.sln`. XML parsing, no ambiguity. Example: this project uses `packages.config`.

**Tier 2 — Probable.** Derived from Roslyn syntax trees without a resolved compilation. Matches using directives, type names, method invocation patterns. Good recall, some false positives, since `Registry` could be somebody's own class. Example: this file references `Microsoft.Win32.Registry`.

**Tier 3 — Verified.** Derived from semantic model when references resolve, or from compiled assemblies in `bin/` via Cecil. Phase 6 work.

Report the tier on every finding. A Tier 2 finding that says "probable" and turns out wrong costs you nothing. A Tier 2 finding presented as certain costs you the user's trust.

### 5a. Portability awareness

Not every finding is a problem for every migration. Modern .NET can target Windows (`net10.0-windows`), where COM interop, P/Invoke to Win32, the Registry, WMI, and similar continue to work. These are **Windows lock-in**: only migration cost if the app must also leave Windows. That is distinct from **gone-everywhere** APIs — WebForms, `BinaryFormatter`, .NET Remoting, MVC 5 — which are removed on modern .NET regardless of target.

Each rule declares a `platform`: `any` (the default — gone everywhere) or `windows` (a Windows lock-in). When the scan `--target` is a Windows TFM, `windows` findings are **satisfied**: still reported (so scope isn't hidden), but excluded from the severity counts, the effort estimate, and `--fail-on`. This lets one codebase yield two honest numbers — "modernize, stay on Windows" vs. "go cross-platform" — instead of overstating cost for the many teams that are Windows-only. When we can't statically prove an API works on `net-windows` (e.g. a vendored assembly of unknown provenance), the rule stays `any` rather than giving false comfort.

## 6. Rule catalog

Rule IDs are stable and never reused. Each rule declares: ID, title, category, severity, effort band, tier, remediation guidance, and a docs link.

### MIG1xxx — Project and build

| ID | Rule |
| --- | --- |
| MIG1001 | Non-SDK-style project file |
| MIG1002 | `packages.config` instead of PackageReference |
| MIG1003 | Target framework below 4.6.2 |
| MIG1004 | Custom MSBuild targets or post-build steps |
| MIG1005 | GAC reference (no HintPath) |
| MIG1006 | COM reference or interop assembly |
| MIG1007 | Legacy project type GUID (Silverlight, Web Site, `.vdproj`, SSIS, SSRS) |
| MIG1008 | Assembly binding redirects in config |
| MIG1009 | Mixed-language solution (VB.NET, C++/CLI) |
| MIG1010 | Vendored DLL with no source and no NuGet equivalent |

### MIG2xxx — Dependencies

| ID | Rule |
| --- | --- |
| MIG2001 | Package has no version supporting the target framework |
| MIG2002 | Package marked deprecated on nuget.org (`--online` only) |
| MIG2003 | Microsoft Enterprise Library |
| MIG2004 | Windows-only commercial component (Crystal Reports, legacy Telerik WebForms, Infragistics, ActiveReports) |
| MIG2005 | P/Invoke to a Windows system library |
| MIG2006 | `Microsoft.VisualBasic` compatibility layer |

### MIG3xxx — Blocking frameworks

| ID | Rule |
| --- | --- |
| MIG3001 | ASP.NET WebForms (`.aspx`, `.ascx`, `System.Web.UI`) |
| MIG3002 | `System.Web` dependency outside WebForms |
| MIG3003 | ASMX web service |
| MIG3004 | WCF service host (server side, needs CoreWCF or rewrite) |
| MIG3005 | .NET Remoting |
| MIG3006 | Windows Workflow Foundation |
| MIG3007 | `AppDomain` creation or cross-domain calls |
| MIG3008 | Code Access Security attributes |
| MIG3009 | MSMQ (`System.Messaging`) |
| MIG3010 | ASP.NET MVC 5 (`System.Web.Mvc`) |
| MIG3011 | `Global.asax`, `IHttpModule`, `IHttpHandler` |
| MIG3012 | Membership / Role / Profile providers |
| MIG3013 | Setup project or ClickOnce deployment |
| MIG3014 | Silverlight |

### MIG4xxx — Compiles fine, fails at runtime

The highest-value category. These pass the build and throw in production on Linux.

| ID | Rule |
| --- | --- |
| MIG4001 | `System.Drawing.Common` on non-Windows |
| MIG4002 | `Microsoft.Win32.Registry` |
| MIG4003 | `System.Management` / WMI |
| MIG4004 | `System.DirectoryServices` / Active Directory |
| MIG4005 | `EventLog` |
| MIG4006 | `PerformanceCounter` |
| MIG4007 | `WindowsIdentity` impersonation |
| MIG4008 | `Thread.Abort` (throws `PlatformNotSupportedException`) |
| MIG4009 | Hardcoded path separators, drive letters, UNC paths |
| MIG4010 | Case-insensitive filesystem assumption |
| MIG4011 | `Process.Start` relying on the old `UseShellExecute` default |
| MIG4012 | `System.IO.Ports` serial access |
| MIG4013 | P/Invoke to `user32`, `kernel32`, `advapi32` |
| MIG4014 | Windows-only `Environment.SpecialFolder` values |
| MIG4015 | `System.Configuration.Install` |

### MIG5xxx — Configuration

| ID | Rule |
| --- | --- |
| MIG5001 | `ConfigurationManager.AppSettings` usage |
| MIG5002 | Custom `ConfigurationSection` classes |
| MIG5003 | `web.config` transforms |
| MIG5004 | Connection strings using integrated security or legacy providers |
| MIG5005 | Encrypted config sections (`aspnet_regiis`) or `machineKey` |
| MIG5006 | IIS-specific `system.webServer` settings |

### MIG6xxx — Serialization and security

| ID | Rule |
| --- | --- |
| MIG6001 | `BinaryFormatter` (removed in .NET 9, hard blocker) |
| MIG6002 | `SoapFormatter` / `NetDataContractSerializer` |
| MIG6003 | `ISerializable` implementations depending on `BinaryFormatter` |
| MIG6004 | CAS attributes (`SecurityPermission`, `PermissionSet`) |
| MIG6005 | Obsolete crypto (`RNGCryptoServiceProvider`, `SHA1Managed`, `*CryptoServiceProvider`) |
| MIG6006 | ViewState or `MachineKey` dependency |

### MIG7xxx — Data access

| ID | Rule |
| --- | --- |
| MIG7001 | `System.Data.SqlClient` (superseded by `Microsoft.Data.SqlClient`) |
| MIG7002 | `Microsoft.Data.SqlClient` 4.0+ `Encrypt=true` default change breaking existing connection strings |
| MIG7003 | `System.Data.OleDb` on non-Windows |
| MIG7004 | ODBC driver dependency |
| MIG7005 | Unmanaged `Oracle.DataAccess` (ODP.NET) |
| MIG7006 | LINQ to SQL (`System.Data.Linq`) |
| MIG7007 | `DataSet` / `DataTable` binary serialization |

### MIG8xxx — Globalization and encoding

Underreported and expensive to debug after the fact.

| ID | Rule |
| --- | --- |
| MIG8001 | Culture-sensitive string comparison affected by the ICU/NLS switch |
| MIG8002 | `Encoding.Default` behavior change (ANSI on Framework, UTF-8 on modern) |
| MIG8003 | Codepage encoding without `CodePagesEncodingProvider` registration |
| MIG8004 | `DateTime` parsing with implicit culture |
| MIG8005 | Sort-order dependent logic |

**For v1, implement roughly 20 rules well.** Prioritize: MIG1001, MIG1002, MIG1005, MIG2001, MIG3001, MIG3002, MIG3004, MIG3005, MIG3010, MIG4001, MIG4002, MIG4004, MIG4008, MIG5001, MIG6001, MIG6004, MIG7001, MIG7002, MIG8002, MIG8003.

## 7. Effort model

Each finding maps to an effort band with a stated assumption.

| Band | Range | Meaning |
| --- | --- | --- |
| Trivial | under 0.5 day | Mechanical, often a find-and-replace |
| Small | 0.5 to 2 days | Localized change, low risk |
| Medium | 2 to 5 days | Touches multiple files, needs testing |
| Large | 5 to 15 days | Subsystem rework |
| Blocker | Unbounded | Needs an architectural decision before estimating |

Aggregate per project and per solution. Multiply by an occurrence factor that flattens out (fifty `ConfigurationManager` calls is not fifty times the work of one). Report a range, not a point estimate.

The report must carry this sentence near the total: *these figures are heuristic planning aids derived from static analysis and are not a quote.*

## 8. Output

**Console.** Summary table. Counts by severity, top blockers, aggregate effort range.

**Markdown** (`--format markdown`). The shareable artifact. An engineering manager forwards this to a CTO. Structure: executive summary, blockers, findings by project, effort breakdown, methodology and limitations. Make it look good. This file is the marketing.

**JSON** (`--format json`). Stable documented schema, versioned.

**SARIF** (`--format sarif`). Drops into GitHub code scanning and Azure DevOps with no glue code.

**Exit codes.**

| Code | Meaning |
| --- | --- |
| 0 | No findings above threshold |
| 1 | Findings above `--fail-on` threshold |
| 2 | Analysis error |
| 64 | Bad usage |

## 9. CLI surface

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

## 10. Repo structure

```
/src
  MigrationScan.Core/           Engine, rule abstractions, models
    Discovery/                  .sln and .csproj parsing
    Rules/                      Rule implementations by category
    Data/                       JSON rule data, package catalogs
    Effort/                     Scoring model
  MigrationScan.Reporting/      Markdown, JSON, SARIF writers
  MigrationScan.Tool/           CLI entry point, packaged as dotnet tool
/tests
  MigrationScan.Core.Tests/
  MigrationScan.Reporting.Tests/
  fixtures/                     Sample legacy solutions
/docs
  rules/                        One page per rule with remediation detail
  schema/                       JSON output schema
```

## 11. Development phases

Work through these in order. Do not scaffold everything up front.

### Phase 0 — Foundation
Repo, Apache-2.0 license, README skeleton, `.editorconfig`, GitHub Actions building and testing on ubuntu, windows, and macos. Solution with the three source projects and two test projects, all empty.

*Done when:* CI is green on all three platforms.

### Phase 1 — Walking skeleton
Parse a `.sln`, enumerate `.csproj` files, parse each as XML, extract target framework and references. Implement MIG1001 (non-SDK-style project) only. Console and JSON output.

*Done when:* `migrationscan tests/fixtures/LegacyWebForms/LegacyWebForms.sln` prints one finding and emits valid JSON.

### Phase 2 — Rule engine
Define `IMigrationRule`, `AnalysisContext`, `Finding`. Split rules into project-file rules and syntax rules. Wire Roslyn syntax tree parsing for `.cs` files. Load rule metadata from JSON. Implement the Tier 1 rules: MIG1002, MIG1005, MIG2001, MIG3001, MIG3002, MIG5001.

*Done when:* each rule has a passing positive fixture and a negative fixture that produces no finding.

### Phase 3 — Syntax rules
Implement the Tier 2 rules: MIG3004, MIG3005, MIG3010, MIG4001, MIG4002, MIG4004, MIG4008, MIG6001, MIG6004, MIG7001, MIG8002, MIG8003. Each reports file and line. Confidence tier on every finding.

*Done when:* full rule set runs against all fixtures with no false negatives, and known false positives are documented.

### Phase 4 — Effort model and Markdown report
Implement banding and aggregation. Build the Markdown writer. Golden-file tests via Verify.

*Done when:* the report reads well enough to send to a client without editing.

### Phase 5 — CI integration
SARIF writer, exit codes, `--fail-on`, `--baseline`. Document a GitHub Actions usage example in the README.

*Done when:* the SARIF output renders in GitHub code scanning.

### Phase 6 — Post-v1
Optional `--online` NuGet compatibility lookups. Binary analysis via Mono.Cecil for solutions without full source. VB.NET support. Remaining rules from the catalog.

## 12. Testing

**Fixtures.** Build small but realistic legacy solutions under `tests/fixtures`. At minimum: a WebForms app, a WCF service, a WinForms app with COM interop, a class library with `packages.config`, and one clean modern solution that should produce zero findings. That last one guards against false positives, which will damage the tool's reputation faster than missing rules will.

**Rule tests.** Positive and negative fixture per rule. Assert on rule ID, file, and line.

**Report tests.** Golden files through Verify. Any report change becomes a visible diff in review.

**No network in tests.** Ever, including `--online` tests. Mock the NuGet client.

## 13. README requirements

The README does double duty as documentation and as the thing that convinces a stranger you know what you are doing. It needs:

- One-sentence description and the install command in the first screen
- A sample Markdown report, or a link to one, above the fold
- The offline and no-telemetry promise, stated plainly
- The non-goals from section 2
- A rules table linking to `/docs/rules`
- A CI usage example
- A limitations section that admits what static analysis cannot see

## 14. Notes for Claude Code

- Follow the phases in order. Finish and test a phase before starting the next.
- Write tests alongside each rule, not after.
- Do not add NuGet dependencies beyond section 4 without asking.
- Keep rule metadata in JSON data files. The C# should contain detection logic only.
- Every finding needs a confidence tier. Never present a syntax-only match as certain.
- Prefer boring, readable code. Somebody is going to read this repo to decide whether to hire the author.
- When a rule's detection is ambiguous, say so in the rule's docs page rather than silently guessing.

Consider adding a `CLAUDE.md` at the repo root containing sections 3, 5, and 14, so the constraints stay in context across sessions.

## 15. Open questions

1. Confirm `MigrationScan` is free on NuGet, or pick an alternative.
2. VB.NET support. Large share of the legacy estate, significant extra work. Defer to phase 6 or skip?
3. Default target: .NET 10 LTS, or let it float to whatever is current LTS?
4. Ship a `--json-schema` command that emits the output schema, or publish the schema as a static file?
