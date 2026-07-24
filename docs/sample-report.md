# .NET Framework Migration Assessment

Static analysis of a solution's readiness to move to `net10.0`, produced by MigrationScan.

## Executive summary

- **Projects scanned:** 1
- **Findings:** 6 (blocker 1 · high 1 · medium 3 · low 1)
- **Estimated effort:** 5.5–16 engineer-days, plus 1 item requiring an architectural decision before they can be estimated

> These figures are heuristic planning aids derived from static analysis and are not a quote.

## Blockers

These need an architectural decision before migration can proceed:

- [MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) — `LegacyWebForms/LegacyWebForms.csproj:2` — Project 'LegacyWebForms' is an ASP.NET WebForms application (.aspx/.ascx present). WebForms has no equivalent on modern .NET and needs re-architecting (e.g. to Razor Pages, MVC, or Blazor).

## Findings by project

### `LegacyWebForms/LegacyWebForms.csproj`

Estimated effort: 5.5–16 engineer-days, plus 1 item requiring an architectural decision before they can be estimated

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) | Medium | Certain | Small | `LegacyWebForms/LegacyWebForms.csproj:2` | Project 'LegacyWebForms' uses the legacy non-SDK project format. |
| [MIG1002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1002.md) | Medium | Certain | Small | `LegacyWebForms/packages.config:1` | Project 'LegacyWebForms' declares NuGet dependencies in packages.config rather than PackageReference. |
| [MIG1005](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1005.md) | Medium | Certain | Medium | `LegacyWebForms/LegacyWebForms.csproj:21` | Assembly 'Telerik.Web.UI' is referenced from the GAC (strong-named, no HintPath). The GAC does not exist on modern .NET. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `LegacyWebForms/packages.config:3` | Package 'Microsoft.AspNet.Mvc' has no version that supports net10.0. ASP.NET MVC 5 runs only on .NET Framework. Consider: ASP.NET Core MVC. |
| [MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) | Blocker | Certain | Blocker | `LegacyWebForms/LegacyWebForms.csproj:2` | Project 'LegacyWebForms' is an ASP.NET WebForms application (.aspx/.ascx present). WebForms has no equivalent on modern .NET and needs re-architecting (e.g. to Razor Pages, MVC, or Blazor). |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `LegacyWebForms/AppConfig.cs:8` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |

## Effort breakdown

| Project | Findings | Estimated days | Needs decision |
| --- | --- | --- | --- |
| `LegacyWebForms/LegacyWebForms.csproj` | 6 | 5.5–16 | 1 |
| **Total** | **6** | **5.5–16** | **1** |

_These figures are heuristic planning aids derived from static analysis and are not a quote._

## Remediation guidance

**[MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) — Non-SDK-style project file**

Convert the project to the SDK style (&lt;Project Sdk="Microsoft.NET.Sdk"&gt;). Replace TargetFrameworkVersion with a TargetFramework moniker, move packages.config entries to PackageReference, and let the SDK glob source files instead of listing them. Do this before other migration work: nearly every later step assumes an SDK-style project.

**[MIG1002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1002.md) — packages.config instead of PackageReference**

Migrate packages.config to PackageReference (Visual Studio offers an in-place migration, or run 'dotnet migrate'-style tooling). PackageReference is required for SDK-style projects and gives transitive restore.

**[MIG1005](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1005.md) — GAC reference (no HintPath)**

Replace the GAC reference with a NuGet package or an explicit HintPath to a checked-in assembly. The Global Assembly Cache does not exist on modern .NET, so GAC-resolved dependencies will not load.

**[MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) — Package has no version supporting the target framework**

Replace the package with a version or successor that targets modern .NET, or remove the dependency. See the suggested replacement in the finding message.

**[MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) — ASP.NET WebForms**

WebForms has no counterpart on modern .NET. Plan a re-architecture to Razor Pages, MVC, or Blazor. This is an architectural decision, not a mechanical port.

**[MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) — ConfigurationManager.AppSettings usage**

On modern .NET, either add the System.Configuration.ConfigurationManager package to keep reading app.config, or migrate to Microsoft.Extensions.Configuration (appsettings.json, environment variables, options pattern).

## Methodology & limitations

MigrationScan parses `.sln` and `.csproj` files as XML and reads `.cs` files with Roslyn — no MSBuild or Visual Studio required, and no source code leaves the machine. Every finding carries a **confidence tier**:

- **Tier 1 — Certain:** read directly from project, config, or solution files.
- **Tier 2 — Probable:** matched on the syntax tree without a resolved compilation, so some may be false positives.

Effort figures apply a per-rule range and a flattening occurrence factor, aggregated per project and across the solution. Two things are tracked separately and can differ: **severity** (the *Blockers* section lists the highest-impact findings) and **estimability** (the *Needs decision* count is the subset whose effort is unbounded until an architectural decision is made). A finding can be a severity blocker yet still estimable — for example replacing `BinaryFormatter` is high impact but a bounded change.

_These figures are heuristic planning aids derived from static analysis and are not a quote._
