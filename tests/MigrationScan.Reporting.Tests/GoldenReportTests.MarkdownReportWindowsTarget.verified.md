# .NET Framework Migration Assessment

Static analysis of a solution's readiness to move to `net10.0-windows`, produced by MigrationScan.

## Executive summary

- **Projects scanned:** 1
- **Findings:** 2 (blocker 1 · high 0 · medium 1 · low 0)
- **Estimated effort:** 0.5–2 engineer-days, plus 1 item requiring an architectural decision before they can be estimated
- **Windows lock-in satisfied by `net10.0-windows`:** 3 (supported on this target, listed below and not counted or estimated)
- **Third-party references:** 2 distinct (listed below as inventory, not counted or estimated)

> These figures are heuristic planning aids derived from static analysis and are not a quote.

## Satisfied by target `net10.0-windows`

These are Windows lock-in APIs (COM, P/Invoke, Registry, WMI, …). They are fully supported when targeting `net10.0-windows`, so they are **not** migration cost here and are excluded from the findings, counts, and effort below. They would become work only if the migration also had to run off Windows:

| Rule | Location | Detail |
| --- | --- | --- |
| [MIG1006](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1006.md) | `Scan.App/Scan.App.csproj:12` | Project 'Scan.App' has a COM reference ('RANGERLib'). |
| [MIG4002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG4002.md) | `Scan.App/Settings.cs:8` | Uses Microsoft.Win32.Registry. |
| [MIG4002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG4002.md) | `Scan.App/Startup.cs:19` | Uses Microsoft.Win32.Registry. |

## Blockers

These need an architectural decision before migration can proceed:

- [MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) · `Scan.App/Scan.App.csproj:2` · Project 'Scan.App' is an ASP.NET WebForms application (.aspx present).

## Findings by project

### `Scan.App/Scan.App.csproj`

Estimated effort: 0.5–2 engineer-days, plus 1 item requiring an architectural decision before they can be estimated

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) | Blocker | Certain | Blocker | `Scan.App/Scan.App.csproj:2` | Project 'Scan.App' is an ASP.NET WebForms application (.aspx present). |
| [MIG7001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG7001.md) | Medium | Probable | Small | `Scan.App/Db.cs:5` | Uses System.Data.SqlClient, which is in maintenance mode. |

## Effort breakdown

| Project | Findings | Estimated days | Needs decision |
| --- | --- | --- | --- |
| `Scan.App/Scan.App.csproj` | 2 | 0.5–2 | 1 |
| **Total** | **2** | **0.5–2** | **1** |

_These figures are heuristic planning aids derived from static analysis and are not a quote._

## References

Everything the scanned projects declare a dependency on, read from the project files. This is an inventory, not findings: nothing here is counted, estimated, or a build failure. This is the list to research. Check each third-party component for a supported .NET 10 release before committing to a plan.

### Third-party (2 distinct)

| Reference | Kind | Version | Used by | Resolved from |
| --- | --- | --- | --- | --- |
| AxInterop.RANGERLib | COM / ActiveX | 1.0.0.0 | 1 project | `libs/AxInterop.RANGERLib.dll` |
| RANGERLib | COM / ActiveX | 1.0 | 1 project | `{8b3a1e60-0000-0000-0000-000000000001}` |

## Remediation guidance

**[MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) · ASP.NET WebForms**

Re-architect to Razor Pages, MVC, or Blazor.

**[MIG7001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG7001.md) · System.Data.SqlClient**

Switch to Microsoft.Data.SqlClient.

## Methodology & limitations

MigrationScan parses `.sln` and `.csproj` files as XML and reads `.cs` files with Roslyn. no MSBuild or Visual Studio required, and no source code leaves the machine. Every finding carries a **confidence tier**:

- **Tier 1, Certain:** read directly from project, config, or solution files.
- **Tier 2, Probable:** matched on the syntax tree without a resolved compilation, so some may be false positives.

Effort figures apply a per-rule range and a flattening occurrence factor, aggregated per project and across the solution. Two things are tracked separately and can differ: **severity** (the *Blockers* section lists the highest-impact findings) and **estimability** (the *Needs decision* count is the subset whose effort is unbounded until an architectural decision is made). A finding can be a severity blocker yet still estimable. Replacing `BinaryFormatter` is high impact but a bounded change.

_These figures are heuristic planning aids derived from static analysis and are not a quote._
## What this report contains

For the security review before you send this on.

**It includes:**

- Project paths, as they appear in your solution: the repo-relative location of each .csproj or .vbproj. A project keeps its path where a source file does not, because the path is how a project is identified, and findings grouped by project are what make the report readable to somebody scoping the work.
- Line numbers of the code that matched a rule.
- Rule identifiers, titles and their remediation text. These read the same in every scan.
- Names and versions of the dependencies your projects declare: NuGet packages, referenced assemblies, COM components, web-service endpoints, and the Windows system libraries you call through P/Invoke. A name identifies a component; it does not say where it sits on disk. We keep names because nobody can assess a component without knowing which one it is.

**It does not include:**

- Source file paths. Each becomes a stable opaque id, so you can still see that two findings share a file without the report naming that file.
- Source code, and any part of the contents of any file.
- Connection strings, credentials, secrets and configuration values.
- Web-service hosts and URLs. Only the scheme survives.
- Customer, business or personal data of any kind.
- Machine names, user names and environment details.
- Anything outside the folder you scanned.

Redaction covers the JSON report, which is the file you send on. Your console output, this Markdown report and the SARIF output all keep full paths. They stay on your machine, SARIF exists to point at a line in a file, and hiding paths from your own developers would protect nobody. Add --include-paths if you want the JSON to keep them too.
