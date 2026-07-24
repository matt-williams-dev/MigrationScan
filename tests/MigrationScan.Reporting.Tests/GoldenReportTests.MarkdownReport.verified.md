# .NET Framework Migration Assessment

Static analysis of a solution's readiness to move to `net10.0`, produced by MigrationScan.

## Executive summary

- **Projects scanned:** 2
- **Findings:** 7 (blocker 2 · high 0 · medium 2 · low 3)
- **Estimated effort:** 6.8–22 engineer-days, plus 1 item requiring an architectural decision before they can be estimated
- **Projects not assessed:** 1 (see below — scope separately)

> These figures are heuristic planning aids derived from static analysis and are not a quote.

## Not assessed — scope separately

These projects are part of the solution but are not C#/VB, so their contents were not analyzed. They still need migration planning of their own and are **not** in the effort estimate:

| Project | Type | Location |
| --- | --- | --- |
| Shop.Database | SQL Server database project | `Shop.Database/Shop.Database.sqlproj` |

## Scan warnings

The following were skipped and are not reflected in the findings below:

- Skipped 'Shop.Legacy/Shop.Legacy.csproj': project file not found (referenced by the solution but missing on disk).

## Blockers

These need an architectural decision before migration can proceed:

- [MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) — `Shop.Web/Shop.Web.csproj:2` — Project 'Shop.Web' is an ASP.NET WebForms application (.aspx present).
- [MIG6001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG6001.md) — `Shop.Core/Cache.cs:33` — Uses BinaryFormatter, which is removed in .NET 9.

## Findings by project

### `Shop.Core/Shop.Core.csproj`

Estimated effort: 5.5–17 engineer-days

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG6001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG6001.md) | Blocker | Probable | Large | `Shop.Core/Cache.cs:33` | Uses BinaryFormatter, which is removed in .NET 9. |
| [MIG7001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG7001.md) | Medium | Probable | Small | `Shop.Core/Db.cs:5` | Uses System.Data.SqlClient, which is in maintenance mode. |

### `Shop.Web/Shop.Web.csproj`

Estimated effort: 1.3–5 engineer-days, plus 1 item requiring an architectural decision before they can be estimated

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) | Medium | Certain | Small | `Shop.Web/Shop.Web.csproj:2` | Project 'Shop.Web' uses the legacy non-SDK project format. |
| [MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) | Blocker | Certain | Blocker | `Shop.Web/Shop.Web.csproj:2` | Project 'Shop.Web' is an ASP.NET WebForms application (.aspx present). |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `Shop.Web/Default.aspx.cs:14` | Reads configuration via ConfigurationManager.AppSettings. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `Shop.Web/Global.asax.cs:20` | Reads configuration via ConfigurationManager.AppSettings. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `Shop.Web/Settings.cs:8` | Reads configuration via ConfigurationManager.AppSettings. |

## Effort breakdown

| Project | Findings | Estimated days | Needs decision |
| --- | --- | --- | --- |
| `Shop.Core/Shop.Core.csproj` | 2 | 5.5–17 | 0 |
| `Shop.Web/Shop.Web.csproj` | 5 | 1.3–5 | 1 |
| **Total** | **7** | **6.8–22** | **1** |

_These figures are heuristic planning aids derived from static analysis and are not a quote._

## Remediation guidance

**[MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) — Non-SDK-style project file**

Convert the project to the SDK style.

**[MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) — ASP.NET WebForms**

Re-architect to Razor Pages, MVC, or Blazor.

**[MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) — ConfigurationManager.AppSettings usage**

Add System.Configuration.ConfigurationManager or migrate to Microsoft.Extensions.Configuration.

**[MIG6001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG6001.md) — BinaryFormatter**

Replace with a safe serializer such as System.Text.Json.

**[MIG7001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG7001.md) — System.Data.SqlClient**

Switch to Microsoft.Data.SqlClient.

## Methodology & limitations

MigrationScan parses `.sln` and `.csproj` files as XML and reads `.cs` files with Roslyn — no MSBuild or Visual Studio required, and no source code leaves the machine. Every finding carries a **confidence tier**:

- **Tier 1 — Certain:** read directly from project, config, or solution files.
- **Tier 2 — Probable:** matched on the syntax tree without a resolved compilation, so some may be false positives.

Effort figures apply a per-rule range and a flattening occurrence factor, aggregated per project and across the solution. Two things are tracked separately and can differ: **severity** (the *Blockers* section lists the highest-impact findings) and **estimability** (the *Needs decision* count is the subset whose effort is unbounded until an architectural decision is made). A finding can be a severity blocker yet still estimable — for example replacing `BinaryFormatter` is high impact but a bounded change.

_These figures are heuristic planning aids derived from static analysis and are not a quote._
