# .NET Framework Migration Assessment

Static analysis of a solution's readiness to move to `net10.0-windows`, produced by MigrationScan.

## Executive summary

- **Projects scanned:** 3
- **Findings:** 17 (blocker 3 · high 4 · medium 8 · low 2)
- **Estimated effort:** 23.8–72 engineer-days, plus 1 item requiring an architectural decision before they can be estimated
- **Windows lock-in satisfied by `net10.0-windows`:** 6 (supported on this target, listed below and not counted or estimated)
- **Projects not assessed:** 1 (listed below, scope separately)
- **Third-party references:** 6 distinct (listed below as inventory, not counted or estimated)

> These figures are heuristic planning aids derived from static analysis and are not a quote.

## Not assessed, scope separately

These projects are part of the solution but are not C#/VB, so their contents were not analyzed. They still need migration planning of their own and are **not** in the effort estimate:

| Project | Type | Location |
| --- | --- | --- |
| Woodgrove.Database | SQL Server database project | `Woodgrove.Database/Woodgrove.Database.sqlproj` |

## Satisfied by target `net10.0-windows`

These are Windows lock-in APIs (COM, P/Invoke, Registry, WMI, …). They are fully supported when targeting `net10.0-windows`, so they are **not** migration cost here and are excluded from the findings, counts, and effort below. They would become work only if the migration also had to run off Windows:

| Rule | Location | Detail |
| --- | --- | --- |
| [MIG1006](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1006.md) | `Woodgrove.Interop/Woodgrove.Interop.csproj:22` | Project 'Woodgrove.Interop' has a COM reference ('FabrikamImagingLib'). COM interop works on modern .NET only when targeting Windows (net-windows); it is a Windows lock-in and unavailable elsewhere. |
| [MIG4002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG4002.md) | `Woodgrove.Interop/TerminalProfile.cs:15` | Accesses the Windows Registry (Microsoft.Win32.Registry). The registry does not exist on non-Windows platforms; the call throws PlatformNotSupportedException there. |
| [MIG4002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG4002.md) | `Woodgrove.Interop/TerminalProfile.cs:23` | Accesses the Windows Registry (Microsoft.Win32.Registry). The registry does not exist on non-Windows platforms; the call throws PlatformNotSupportedException there. |
| [MIG4003](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG4003.md) | `Woodgrove.Interop/BranchHostInventory.cs:2` | Uses WMI (System.Management). On modern .NET this is Windows-only (via the System.Management package) and throws PlatformNotSupportedException elsewhere. |
| [MIG4013](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG4013.md) | `Woodgrove.Interop/ChequeScannerInterop.cs:12` | P/Invokes the Windows system library 'kernel32.dll' via [DllImport]. This works on modern .NET only when targeting Windows (net-windows); it is a Windows lock-in and won't run cross-platform. |
| [MIG4013](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG4013.md) | `Woodgrove.Interop/ChequeScannerInterop.cs:16` | P/Invokes the Windows system library 'advapi32.dll' via [DllImport]. This works on modern .NET only when targeting Windows (net-windows); it is a Windows lock-in and won't run cross-platform. |

## Blockers

These need an architectural decision before migration can proceed:

- [MIG6001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG6001.md) · `Woodgrove.Domain/StatementArchive.cs:21` · Uses BinaryFormatter, which is removed in .NET 9 and throws when used. Replace it with a safe serializer such as System.Text.Json, MessagePack, or protobuf.
- [MIG6001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG6001.md) · `Woodgrove.Domain/StatementArchive.cs:29` · Uses BinaryFormatter, which is removed in .NET 9 and throws when used. Replace it with a safe serializer such as System.Text.Json, MessagePack, or protobuf.
- [MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) · `Woodgrove.Web/Woodgrove.Web.csproj:2` · Project 'Woodgrove.Web' is an ASP.NET WebForms application (.aspx/.ascx present). WebForms has no equivalent on modern .NET and needs re-architecting (e.g. to Razor Pages, MVC, or Blazor).

## Findings by project

### `Woodgrove.Domain/Woodgrove.Domain.csproj`

Estimated effort: 11–33.5 engineer-days

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) | Medium | Certain | Small | `Woodgrove.Domain/Woodgrove.Domain.csproj:2` | Project 'Woodgrove.Domain' uses the legacy non-SDK project format. |
| [MIG1002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1002.md) | Medium | Certain | Small | `Woodgrove.Domain/packages.config:1` | Project 'Woodgrove.Domain' declares NuGet dependencies in packages.config rather than PackageReference. |
| [MIG3002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3002.md) | High | Certain | Medium | `Woodgrove.Domain/Woodgrove.Domain.csproj:19` | Project 'Woodgrove.Domain' references System.Web outside of WebForms. System.Web is not available on modern .NET; the dependent code needs replacing. |
| [MIG6001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG6001.md) | Blocker | Probable | Large | `Woodgrove.Domain/StatementArchive.cs:21` | Uses BinaryFormatter, which is removed in .NET 9 and throws when used. Replace it with a safe serializer such as System.Text.Json, MessagePack, or protobuf. |
| [MIG6001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG6001.md) | Blocker | Probable | Large | `Woodgrove.Domain/StatementArchive.cs:29` | Uses BinaryFormatter, which is removed in .NET 9 and throws when used. Replace it with a safe serializer such as System.Text.Json, MessagePack, or protobuf. |
| [MIG7001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG7001.md) | Medium | Probable | Small | `Woodgrove.Domain/LedgerGateway.cs:3` | Uses System.Data.SqlClient, which is in maintenance mode. Switch to Microsoft.Data.SqlClient (note its Encrypt=true default change, see MIG7002). |

### `Woodgrove.Interop/Woodgrove.Interop.csproj`

Estimated effort: 6–19 engineer-days

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) | Medium | Certain | Small | `Woodgrove.Interop/Woodgrove.Interop.csproj:2` | Project 'Woodgrove.Interop' uses the legacy non-SDK project format. |
| [MIG1002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1002.md) | Medium | Certain | Small | `Woodgrove.Interop/packages.config:1` | Project 'Woodgrove.Interop' declares NuGet dependencies in packages.config rather than PackageReference. |
| [MIG1010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1010.md) | High | Certain | Large | `Woodgrove.Interop/Woodgrove.Interop.csproj:34` | Project 'Woodgrove.Interop' references a vendored assembly ('Litware.ChequeScanner') from a checked-in path, not a NuGet package. Confirm it runs on modern .NET, since many such assemblies are Framework-only or ActiveX/COM with no supported successor. |

### `Woodgrove.Web/Woodgrove.Web.csproj`

Estimated effort: 6.8–19.5 engineer-days, plus 1 item requiring an architectural decision before they can be estimated

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) | Medium | Certain | Small | `Woodgrove.Web/Woodgrove.Web.csproj:2` | Project 'Woodgrove.Web' uses the legacy non-SDK project format. |
| [MIG1002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1002.md) | Medium | Certain | Small | `Woodgrove.Web/packages.config:1` | Project 'Woodgrove.Web' declares NuGet dependencies in packages.config rather than PackageReference. |
| [MIG1005](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1005.md) | Medium | Certain | Medium | `Woodgrove.Web/Woodgrove.Web.csproj:21` | Assembly 'Litware.Web.Controls' is referenced from the GAC (strong-named, no HintPath). The GAC does not exist on modern .NET. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `Woodgrove.Web/packages.config:3` | Package 'Microsoft.AspNet.Mvc' has no version that supports net10.0-windows. ASP.NET MVC 5 runs only on .NET Framework. Consider: ASP.NET Core MVC. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `Woodgrove.Web/packages.config:4` | Package 'Microsoft.AspNet.Web.Optimization' has no version that supports net10.0-windows. System.Web bundling/minification runs only on .NET Framework. Consider: A build-time bundler or ASP.NET Core static asset pipeline. |
| [MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) | Blocker | Certain | Blocker | `Woodgrove.Web/Woodgrove.Web.csproj:2` | Project 'Woodgrove.Web' is an ASP.NET WebForms application (.aspx/.ascx present). WebForms has no equivalent on modern .NET and needs re-architecting (e.g. to Razor Pages, MVC, or Blazor). |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `Woodgrove.Web/AccountPortalConfig.cs:8` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `Woodgrove.Web/AccountPortalConfig.cs:11` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |

## Effort breakdown

| Project | Findings | Estimated days | Needs decision |
| --- | --- | --- | --- |
| `Woodgrove.Domain/Woodgrove.Domain.csproj` | 6 | 11–33.5 | 0 |
| `Woodgrove.Interop/Woodgrove.Interop.csproj` | 3 | 6–19 | 0 |
| `Woodgrove.Web/Woodgrove.Web.csproj` | 8 | 6.8–19.5 | 1 |
| **Total** | **17** | **23.8–72** | **1** |

_These figures are heuristic planning aids derived from static analysis and are not a quote._

## References

Everything the scanned projects declare a dependency on, read from the project files. This is an inventory, not findings: nothing here is counted, estimated, or a build failure. This is the list to research. Check each third-party component for a supported .NET 10 release before committing to a plan.

### Third-party (6 distinct)

| Reference | Kind | Version | Used by | Resolved from |
| --- | --- | --- | --- | --- |
| Litware.Web.Controls | NuGet package | 2018.2.611.40 | 1 project | n/a |
| Microsoft.AspNet.Mvc | NuGet package | 5.2.9 | 1 project | n/a |
| Microsoft.AspNet.Web.Optimization | NuGet package | 1.1.3 | 1 project | n/a |
| Proseware.Json | NuGet package | 9.1.2 | 3 projects | n/a |
| Litware.ChequeScanner | Vendored DLL | 3.0.0.0 | 1 project | `libs/Litware.ChequeScanner.dll` |
| FabrikamImagingLib | COM / ActiveX | 3.0 | 1 project | `{7c2f4a10-0000-0000-0000-0000000000a1}` |

### Solution-internal project references

This solution's own code, already in scope. Listed to show the build order dependencies:

| Project | Depends on |
| --- | --- |
| `Woodgrove.Web/Woodgrove.Web.csproj` | Woodgrove.Domain |

_11 framework assembly references were also read (`System.*`, `mscorlib`, WPF, …) and are not listed. They move with the runtime rather than needing research._

## Remediation guidance

**[MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) · Non-SDK-style project file**

Convert the project to the SDK style (&lt;Project Sdk="Microsoft.NET.Sdk"&gt;). Replace TargetFrameworkVersion with a TargetFramework moniker, move packages.config entries to PackageReference, and let the SDK glob source files instead of listing them. Do this before other migration work: nearly every later step assumes an SDK-style project.

**[MIG1002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1002.md) · packages.config instead of PackageReference**

Migrate packages.config to PackageReference (Visual Studio offers an in-place migration, or run 'dotnet migrate'-style tooling). PackageReference is required for SDK-style projects and gives transitive restore.

**[MIG1005](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1005.md) · GAC reference (no HintPath)**

Replace the GAC reference with a NuGet package or an explicit HintPath to a checked-in assembly. The Global Assembly Cache does not exist on modern .NET, so GAC-resolved dependencies will not load.

**[MIG1010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1010.md) · Vendored DLL with no source and no NuGet equivalent**

A third-party assembly referenced from a checked-in path rather than a NuGet package. Assess it per assembly: confirm it loads on modern .NET (many Framework-only or ActiveX/COM assemblies do not), find a supported successor or NuGet package, or plan a replacement. If the vendor is defunct and there is no equivalent, this can block the migration.

**[MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) · Package has no version supporting the target framework**

Replace the package with a version or successor that targets modern .NET, or remove the dependency. See the suggested replacement in the finding message.

**[MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) · ASP.NET WebForms**

WebForms has no counterpart on modern .NET. Plan a re-architecture to Razor Pages, MVC, or Blazor. This is an architectural decision, not a mechanical port.

**[MIG3002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3002.md) · System.Web dependency outside WebForms**

System.Web is not available on modern .NET. Replace the dependent code: HttpContext usage moves to Microsoft.AspNetCore.Http, HttpUtility to System.Web.HttpUtility's modern equivalents or System.Net.WebUtility.

**[MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) · ConfigurationManager.AppSettings usage**

On modern .NET, either add the System.Configuration.ConfigurationManager package to keep reading app.config, or migrate to Microsoft.Extensions.Configuration (appsettings.json, environment variables, options pattern).

**[MIG6001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG6001.md) · BinaryFormatter**

BinaryFormatter is removed in .NET 9 and throws when invoked (it was also a well-known security risk). Replace it with a safe serializer: System.Text.Json, MessagePack, or protobuf. Changing serialization format may require a data migration.

**[MIG7001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG7001.md) · System.Data.SqlClient**

System.Data.SqlClient is in maintenance mode. Switch to Microsoft.Data.SqlClient, and review the Encrypt=true default change in 4.0+ that can break existing connection strings (see MIG7002).

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
