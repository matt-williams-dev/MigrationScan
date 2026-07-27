# .NET Framework Migration Assessment

Static analysis of a solution's readiness to move to `net10.0`, produced by MigrationScan.

## Executive summary

- **Projects scanned:** 11
- **Findings:** 108 (blocker 5 · high 54 · medium 29 · low 20)
- **Estimated effort:** 88–263.5 engineer-days, plus 3 items requiring an architectural decision before they can be estimated
- **Projects not assessed:** 7 (listed below, scope separately)
- **Third-party references:** 197 distinct (listed below as inventory, not counted or estimated)

> These figures are heuristic planning aids derived from static analysis and are not a quote.

## Not assessed, scope separately

These projects are part of the solution but are not C#/VB, so their contents were not analyzed. They still need migration planning of their own and are **not** in the effort estimate:

| Project | Type | Location |
| --- | --- | --- |
| eShopModernizedMVC_ServiceFabricApp | SFPROJ project | `ServiceFabric/eShopModernizedMVC-ServiceFabricApp/eShopModernizedMVC_ServiceFabricApp.sfproj` |
| eShopModernizedSQL_ServiceFabricApp | SFPROJ project | `ServiceFabric/eShopModernizedSQL_ServiceFabricApp/eShopModernizedSQL_ServiceFabricApp.sfproj` |
| eShopModernizedWebFormsSF | SFPROJ project | `ServiceFabric/eShopModernizedWebForms-ServiceApp/eShopModernizedWebFormsSF.sfproj` |
| docker-compose | DCPROJ project | `eShopModernizedMVCSolution/docker-compose.dcproj` |
| docker-compose | DCPROJ project | `eShopModernizedNTier/docker-compose.dcproj` |
| docker-compose | DCPROJ project | `eShopModernizedNTier/temp/docker-compose.dcproj` |
| docker-compose | DCPROJ project | `eShopModernizedWebFormsSolution/docker-compose.dcproj` |

## Scan warnings

The following were skipped and are not reflected in the findings below:

- 1 project(s) are not referenced by any solution in the scan and may not be part of a shipping build — confirm whether they are in scope: eShopModernizedNTier/src/eShopWinForms/eShopWinForms.fx.csproj

## Blockers

These need an architectural decision before migration can proceed:

- [MIG6001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG6001.md) · `eShopLegacyMVCSolution/eShopLegacy.Utilities/Serializing.cs:11` · Uses BinaryFormatter, which is removed in .NET 9 and throws when used. Replace it with a safe serializer such as System.Text.Json, MessagePack, or protobuf.
- [MIG6001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG6001.md) · `eShopLegacyMVCSolution/eShopLegacy.Utilities/Serializing.cs:19` · Uses BinaryFormatter, which is removed in .NET 9 and throws when used. Replace it with a safe serializer such as System.Text.Json, MessagePack, or protobuf.
- [MIG3005](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3005.md) · `eShopLegacyMVCSolution/src/eShopLegacyMVC/Controllers/WebApi/BrandsController.cs:7` · Uses .NET Remoting (System.Runtime.Remoting), which was removed from modern .NET. Replace with a supported transport such as gRPC, WCF-on-CoreWCF, or an HTTP API.
- [MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) · `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/eShopLegacyWebForms.csproj:2` · Project 'eShopLegacyWebForms' is an ASP.NET WebForms application (.aspx/.ascx present). WebForms has no equivalent on modern .NET and needs re-architecting (e.g. to Razor Pages, MVC, or Blazor).
- [MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) · `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/eShopModernizedWebForms.csproj:2` · Project 'eShopModernizedWebForms' is an ASP.NET WebForms application (.aspx/.ascx present). WebForms has no equivalent on modern .NET and needs re-architecting (e.g. to Razor Pages, MVC, or Blazor).

## Findings by project

### `eShopLegacyMVCSolution/eShopLegacy.Utilities/eShopLegacy.Utilities.csproj`

Estimated effort: 8.5–26.5 engineer-days

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) | Medium | Certain | Small | `eShopLegacyMVCSolution/eShopLegacy.Utilities/eShopLegacy.Utilities.csproj:2` | Project 'eShopLegacy.Utilities' uses the legacy non-SDK project format. |
| [MIG1003](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1003.md) | Medium | Certain | Small | `eShopLegacyMVCSolution/eShopLegacy.Utilities/eShopLegacy.Utilities.csproj:2` | Project 'eShopLegacy.Utilities' targets .NET Framework 4.6.1 (below 4.6.2). Retarget to at least 4.6.2 before migrating. Earlier versions lack the .NET Standard 2.0 surface that migration relies on. |
| [MIG6001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG6001.md) | Blocker | Probable | Large | `eShopLegacyMVCSolution/eShopLegacy.Utilities/Serializing.cs:11` | Uses BinaryFormatter, which is removed in .NET 9 and throws when used. Replace it with a safe serializer such as System.Text.Json, MessagePack, or protobuf. |
| [MIG6001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG6001.md) | Blocker | Probable | Large | `eShopLegacyMVCSolution/eShopLegacy.Utilities/Serializing.cs:19` | Uses BinaryFormatter, which is removed in .NET 9 and throws when used. Replace it with a safe serializer such as System.Text.Json, MessagePack, or protobuf. |

### `eShopLegacyMVCSolution/eShopPorted/eShopPorted.csproj`

Estimated effort: 7.5–22 engineer-days

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG1003](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1003.md) | Medium | Certain | Small | `eShopLegacyMVCSolution/eShopPorted/eShopPorted.csproj:1` | Project 'eShopPorted' targets .NET Framework 4.6.1 (below 4.6.2). Retarget to at least 4.6.2 before migrating. Earlier versions lack the .NET Standard 2.0 surface that migration relies on. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyMVCSolution/eShopPorted/eShopPorted.csproj:26` | Package 'WebGrease' has no version that supports net10.0. A System.Web bundling dependency with no modern .NET target. Consider: A build-time bundler. |
| [MIG3010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3010.md) | High | Probable | Large | `eShopLegacyMVCSolution/eShopPorted/Controllers/PicController.cs:5` | References ASP.NET MVC 5 (System.Web.Mvc), which runs only on .NET Framework. Migrate to ASP.NET Core MVC (Microsoft.AspNetCore.Mvc). |

### `eShopLegacyMVCSolution/src/eShopLegacyMVC/eShopLegacyMVC.csproj`

Estimated effort: 17.8–52 engineer-days, plus 1 item requiring an architectural decision before they can be estimated

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) | Medium | Certain | Small | `eShopLegacyMVCSolution/src/eShopLegacyMVC/eShopLegacyMVC.csproj:2` | Project 'eShopLegacyMVC' uses the legacy non-SDK project format. |
| [MIG1002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1002.md) | Medium | Certain | Small | `eShopLegacyMVCSolution/src/eShopLegacyMVC/packages.config:1` | Project 'eShopLegacyMVC' declares NuGet dependencies in packages.config rather than PackageReference. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyMVCSolution/src/eShopLegacyMVC/packages.config:18` | Package 'Microsoft.AspNet.Mvc' has no version that supports net10.0. ASP.NET MVC 5 runs only on .NET Framework. Consider: ASP.NET Core MVC. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyMVCSolution/src/eShopLegacyMVC/packages.config:19` | Package 'Microsoft.AspNet.Razor' has no version that supports net10.0. The System.Web-era Razor engine runs only on .NET Framework. Consider: ASP.NET Core Razor. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyMVCSolution/src/eShopLegacyMVC/packages.config:22` | Package 'Microsoft.AspNet.Web.Optimization' has no version that supports net10.0. System.Web bundling/minification runs only on .NET Framework. Consider: A build-time bundler or ASP.NET Core static asset pipeline. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyMVCSolution/src/eShopLegacyMVC/packages.config:23` | Package 'Microsoft.AspNet.WebPages' has no version that supports net10.0. ASP.NET Web Pages runs only on .NET Framework. Consider: ASP.NET Core (Razor Pages). |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyMVCSolution/src/eShopLegacyMVC/packages.config:27` | Package 'Microsoft.Web.Infrastructure' has no version that supports net10.0. A System.Web infrastructure shim with no modern .NET target. Consider: None required on modern .NET. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyMVCSolution/src/eShopLegacyMVC/packages.config:44` | Package 'WebGrease' has no version that supports net10.0. A System.Web bundling dependency with no modern .NET target. Consider: A build-time bundler. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyMVCSolution/src/eShopLegacyMVC/eShopLegacyMVC.csproj:90` | Package 'Microsoft.AspNet.WebApi' has no version that supports net10.0. ASP.NET Web API 2 runs only on .NET Framework. Consider: ASP.NET Core Web API. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyMVCSolution/src/eShopLegacyMVC/eShopLegacyMVC.csproj:96` | Package 'Microsoft.AspNet.WebApi.Core' has no version that supports net10.0. ASP.NET Web API 2 runs only on .NET Framework. Consider: ASP.NET Core Web API. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyMVCSolution/src/eShopLegacyMVC/eShopLegacyMVC.csproj:99` | Package 'Microsoft.AspNet.WebApi.WebHost' has no version that supports net10.0. System.Web-hosted Web API runs only on .NET Framework. Consider: ASP.NET Core Web API. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyMVCSolution/src/eShopLegacyMVC/eShopLegacyMVC.csproj:233` | Package 'WebGrease' has no version that supports net10.0. A System.Web bundling dependency with no modern .NET target. Consider: A build-time bundler. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyMVCSolution/src/eShopLegacyMVC/eShopLegacyMVC.csproj:257` | Package 'Microsoft.Web.Infrastructure' has no version that supports net10.0. A System.Web infrastructure shim with no modern .NET target. Consider: None required on modern .NET. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyMVCSolution/src/eShopLegacyMVC/eShopLegacyMVC.csproj:523` | Package 'Microsoft.AspNet.Mvc' has no version that supports net10.0. ASP.NET MVC 5 runs only on .NET Framework. Consider: ASP.NET Core MVC. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyMVCSolution/src/eShopLegacyMVC/eShopLegacyMVC.csproj:528` | Package 'Microsoft.AspNet.Razor' has no version that supports net10.0. The System.Web-era Razor engine runs only on .NET Framework. Consider: ASP.NET Core Razor. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyMVCSolution/src/eShopLegacyMVC/eShopLegacyMVC.csproj:533` | Package 'Microsoft.AspNet.Web.Optimization' has no version that supports net10.0. System.Web bundling/minification runs only on .NET Framework. Consider: A build-time bundler or ASP.NET Core static asset pipeline. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyMVCSolution/src/eShopLegacyMVC/eShopLegacyMVC.csproj:538` | Package 'Microsoft.AspNet.WebPages' has no version that supports net10.0. ASP.NET Web Pages runs only on .NET Framework. Consider: ASP.NET Core (Razor Pages). |
| [MIG3002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3002.md) | High | Certain | Medium | `eShopLegacyMVCSolution/src/eShopLegacyMVC/eShopLegacyMVC.csproj:199` | Project 'eShopLegacyMVC' references System.Web outside of WebForms. System.Web is not available on modern .NET; the dependent code needs replacing. |
| [MIG3005](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3005.md) | Blocker | Probable | Blocker | `eShopLegacyMVCSolution/src/eShopLegacyMVC/Controllers/WebApi/BrandsController.cs:7` | Uses .NET Remoting (System.Runtime.Remoting), which was removed from modern .NET. Replace with a supported transport such as gRPC, WCF-on-CoreWCF, or an HTTP API. |
| [MIG3010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3010.md) | High | Probable | Large | `eShopLegacyMVCSolution/src/eShopLegacyMVC/App_Start/RouteConfig.cs:1` | References ASP.NET MVC 5 (System.Web.Mvc), which runs only on .NET Framework. Migrate to ASP.NET Core MVC (Microsoft.AspNetCore.Mvc). |
| [MIG3010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3010.md) | High | Probable | Large | `eShopLegacyMVCSolution/src/eShopLegacyMVC/Controllers/Api/CatalogController.cs:1` | References ASP.NET MVC 5 (System.Web.Mvc), which runs only on .NET Framework. Migrate to ASP.NET Core MVC (Microsoft.AspNetCore.Mvc). |
| [MIG3010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3010.md) | High | Probable | Large | `eShopLegacyMVCSolution/src/eShopLegacyMVC/App_Start/FilterConfig.cs:2` | References ASP.NET MVC 5 (System.Web.Mvc), which runs only on .NET Framework. Migrate to ASP.NET Core MVC (Microsoft.AspNetCore.Mvc). |
| [MIG3010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3010.md) | High | Probable | Large | `eShopLegacyMVCSolution/src/eShopLegacyMVC/Controllers/CatalogController.cs:3` | References ASP.NET MVC 5 (System.Web.Mvc), which runs only on .NET Framework. Migrate to ASP.NET Core MVC (Microsoft.AspNetCore.Mvc). |
| [MIG3010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3010.md) | High | Probable | Large | `eShopLegacyMVCSolution/src/eShopLegacyMVC/Controllers/PicController.cs:5` | References ASP.NET MVC 5 (System.Web.Mvc), which runs only on .NET Framework. Migrate to ASP.NET Core MVC (Microsoft.AspNetCore.Mvc). |
| [MIG3010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3010.md) | High | Probable | Large | `eShopLegacyMVCSolution/src/eShopLegacyMVC/Global.asax.cs:15` | References ASP.NET MVC 5 (System.Web.Mvc), which runs only on .NET Framework. Migrate to ASP.NET Core MVC (Microsoft.AspNetCore.Mvc). |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopLegacyMVCSolution/src/eShopLegacyMVC/Models/Infrastructure/CatalogDBInitializer.cs:29` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopLegacyMVCSolution/src/eShopLegacyMVC/Global.asax.cs:68` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopLegacyMVCSolution/src/eShopLegacyMVC/Global.asax.cs:85` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |

### `eShopLegacyNTier/src/eShopWCFService/eShopWCFService.csproj`

Estimated effort: 4.3–14 engineer-days

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) | Medium | Certain | Small | `eShopLegacyNTier/src/eShopWCFService/eShopWCFService.csproj:1` | Project 'eShopWCFService' uses the legacy non-SDK project format. |
| [MIG1002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1002.md) | Medium | Certain | Small | `eShopLegacyNTier/src/eShopWCFService/packages.config:1` | Project 'eShopWCFService' declares NuGet dependencies in packages.config rather than PackageReference. |
| [MIG1003](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1003.md) | Medium | Certain | Small | `eShopLegacyNTier/src/eShopWCFService/eShopWCFService.csproj:1` | Project 'eShopWCFService' targets .NET Framework 4.6.1 (below 4.6.2). Retarget to at least 4.6.2 before migrating. Earlier versions lack the .NET Standard 2.0 surface that migration relies on. |
| [MIG3002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3002.md) | High | Certain | Medium | `eShopLegacyNTier/src/eShopWCFService/eShopWCFService.csproj:66` | Project 'eShopWCFService' references System.Web outside of WebForms. System.Web is not available on modern .NET; the dependent code needs replacing. |
| [MIG3015](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3015.md) | Medium | Probable | Small | `eShopLegacyNTier/src/eShopWCFService/ICatalogService.cs:6` | Uses WCF (System.ServiceModel). On modern .NET the WCF client is supported by adding the System.ServiceModel.* packages (e.g. System.ServiceModel.Http, .NetTcp) and regenerating the proxy; config-based endpoints usually move to code. Server-side hosting instead needs CoreWCF (see MIG3004). |
| [MIG3015](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3015.md) | Medium | Probable | Small | `eShopLegacyNTier/src/eShopWCFService/CatalogService.svc.cs:7` | Uses WCF (System.ServiceModel). On modern .NET the WCF client is supported by adding the System.ServiceModel.* packages (e.g. System.ServiceModel.Http, .NetTcp) and regenerating the proxy; config-based endpoints usually move to code. Server-side hosting instead needs CoreWCF (see MIG3004). |
| [MIG3015](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3015.md) | Medium | Probable | Small | `eShopLegacyNTier/src/eShopWCFService/CatalogServiceClient.cs:9` | Uses WCF (System.ServiceModel). On modern .NET the WCF client is supported by adding the System.ServiceModel.* packages (e.g. System.ServiceModel.Http, .NetTcp) and regenerating the proxy; config-based endpoints usually move to code. Server-side hosting instead needs CoreWCF (see MIG3004). |

### `eShopLegacyNTier/src/eShopWinForms/eShopWinForms.csproj`

Estimated effort: 3–9 engineer-days

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) | Medium | Certain | Small | `eShopLegacyNTier/src/eShopWinForms/eShopWinForms.csproj:2` | Project 'eShopWinForms' uses the legacy non-SDK project format. |
| [MIG1002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1002.md) | Medium | Certain | Small | `eShopLegacyNTier/src/eShopWinForms/packages.config:1` | Project 'eShopWinForms' declares NuGet dependencies in packages.config rather than PackageReference. |
| [MIG4001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG4001.md) | High | Probable | Medium | `eShopLegacyNTier/src/eShopWinForms/Views/CatalogView.cs:5` | Uses System.Drawing. On modern .NET, System.Drawing.Common is supported only on Windows and throws PlatformNotSupportedException elsewhere. Use a cross-platform imaging library (e.g. ImageSharp, SkiaSharp) if the app must run on Linux. |

### `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/eShopLegacyWebForms.csproj`

Estimated effort: 5.3–16.5 engineer-days, plus 1 item requiring an architectural decision before they can be estimated

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) | Medium | Certain | Small | `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/eShopLegacyWebForms.csproj:2` | Project 'eShopLegacyWebForms' uses the legacy non-SDK project format. |
| [MIG1002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1002.md) | Medium | Certain | Small | `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/packages.config:1` | Project 'eShopLegacyWebForms' declares NuGet dependencies in packages.config rather than PackageReference. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/packages.config:25` | Package 'Microsoft.AspNet.Web.Optimization' has no version that supports net10.0. System.Web bundling/minification runs only on .NET Framework. Consider: A build-time bundler or ASP.NET Core static asset pipeline. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/packages.config:29` | Package 'Microsoft.Web.Infrastructure' has no version that supports net10.0. A System.Web infrastructure shim with no modern .NET target. Consider: None required on modern .NET. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/packages.config:46` | Package 'WebGrease' has no version that supports net10.0. A System.Web bundling dependency with no modern .NET target. Consider: A build-time bundler. |
| [MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) | Blocker | Certain | Blocker | `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/eShopLegacyWebForms.csproj:2` | Project 'eShopLegacyWebForms' is an ASP.NET WebForms application (.aspx/.ascx present). WebForms has no equivalent on modern .NET and needs re-architecting (e.g. to Razor Pages, MVC, or Blazor). |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/Models/Infrastructure/CatalogDBInitializer.cs:29` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/Global.asax.cs:53` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/Global.asax.cs:61` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG7001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG7001.md) | Medium | Probable | Small | `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/Services/CatalogService.cs:5` | Uses System.Data.SqlClient, which is in maintenance mode. Switch to Microsoft.Data.SqlClient (note its Encrypt=true default change, see MIG7002). |

### `eShopModernizedMVCSolution/src/eShopModernizedMVC/eShopModernizedMVC.csproj`

Estimated effort: 20.5–60 engineer-days

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) | Medium | Certain | Small | `eShopModernizedMVCSolution/src/eShopModernizedMVC/eShopModernizedMVC.csproj:2` | Project 'eShopModernizedMVC' uses the legacy non-SDK project format. |
| [MIG1002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1002.md) | Medium | Certain | Small | `eShopModernizedMVCSolution/src/eShopModernizedMVC/packages.config:1` | Project 'eShopModernizedMVC' declares NuGet dependencies in packages.config rather than PackageReference. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopModernizedMVCSolution/src/eShopModernizedMVC/packages.config:22` | Package 'Microsoft.AspNet.Mvc' has no version that supports net10.0. ASP.NET MVC 5 runs only on .NET Framework. Consider: ASP.NET Core MVC. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopModernizedMVCSolution/src/eShopModernizedMVC/packages.config:23` | Package 'Microsoft.AspNet.Razor' has no version that supports net10.0. The System.Web-era Razor engine runs only on .NET Framework. Consider: ASP.NET Core Razor. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopModernizedMVCSolution/src/eShopModernizedMVC/packages.config:26` | Package 'Microsoft.AspNet.Web.Optimization' has no version that supports net10.0. System.Web bundling/minification runs only on .NET Framework. Consider: A build-time bundler or ASP.NET Core static asset pipeline. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopModernizedMVCSolution/src/eShopModernizedMVC/packages.config:27` | Package 'Microsoft.AspNet.WebPages' has no version that supports net10.0. ASP.NET Web Pages runs only on .NET Framework. Consider: ASP.NET Core (Razor Pages). |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopModernizedMVCSolution/src/eShopModernizedMVC/packages.config:61` | Package 'Microsoft.Owin.Host.SystemWeb' has no version that supports net10.0. OWIN hosting on System.Web runs only on .NET Framework. Consider: ASP.NET Core hosting. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopModernizedMVCSolution/src/eShopModernizedMVC/packages.config:67` | Package 'Microsoft.Web.Infrastructure' has no version that supports net10.0. A System.Web infrastructure shim with no modern .NET target. Consider: None required on modern .NET. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopModernizedMVCSolution/src/eShopModernizedMVC/packages.config:146` | Package 'WebGrease' has no version that supports net10.0. A System.Web bundling dependency with no modern .NET target. Consider: A build-time bundler. |
| [MIG3002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3002.md) | High | Certain | Medium | `eShopModernizedMVCSolution/src/eShopModernizedMVC/eShopModernizedMVC.csproj:446` | Project 'eShopModernizedMVC' references System.Web outside of WebForms. System.Web is not available on modern .NET; the dependent code needs replacing. |
| [MIG3010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3010.md) | High | Probable | Large | `eShopModernizedMVCSolution/src/eShopModernizedMVC/App_Start/FilterConfig.cs:2` | References ASP.NET MVC 5 (System.Web.Mvc), which runs only on .NET Framework. Migrate to ASP.NET Core MVC (Microsoft.AspNetCore.Mvc). |
| [MIG3010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3010.md) | High | Probable | Large | `eShopModernizedMVCSolution/src/eShopModernizedMVC/Filters/ActionTracerFilter.cs:2` | References ASP.NET MVC 5 (System.Web.Mvc), which runs only on .NET Framework. Migrate to ASP.NET Core MVC (Microsoft.AspNetCore.Mvc). |
| [MIG3010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3010.md) | High | Probable | Large | `eShopModernizedMVCSolution/src/eShopModernizedMVC/Controllers/CatalogController.cs:3` | References ASP.NET MVC 5 (System.Web.Mvc), which runs only on .NET Framework. Migrate to ASP.NET Core MVC (Microsoft.AspNetCore.Mvc). |
| [MIG3010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3010.md) | High | Probable | Large | `eShopModernizedMVCSolution/src/eShopModernizedMVC/App_Start/RouteConfig.cs:5` | References ASP.NET MVC 5 (System.Web.Mvc), which runs only on .NET Framework. Migrate to ASP.NET Core MVC (Microsoft.AspNetCore.Mvc). |
| [MIG3010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3010.md) | High | Probable | Large | `eShopModernizedMVCSolution/src/eShopModernizedMVC/Controllers/AccountController.cs:6` | References ASP.NET MVC 5 (System.Web.Mvc), which runs only on .NET Framework. Migrate to ASP.NET Core MVC (Microsoft.AspNetCore.Mvc). |
| [MIG3010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3010.md) | High | Probable | Large | `eShopModernizedMVCSolution/src/eShopModernizedMVC/Controllers/PicController.cs:9` | References ASP.NET MVC 5 (System.Web.Mvc), which runs only on .NET Framework. Migrate to ASP.NET Core MVC (Microsoft.AspNetCore.Mvc). |
| [MIG3010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3010.md) | High | Probable | Large | `eShopModernizedMVCSolution/src/eShopModernizedMVC/Global.asax.cs:13` | References ASP.NET MVC 5 (System.Web.Mvc), which runs only on .NET Framework. Migrate to ASP.NET Core MVC (Microsoft.AspNetCore.Mvc). |
| [MIG4001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG4001.md) | High | Probable | Medium | `eShopModernizedMVCSolution/src/eShopModernizedMVC/Controllers/PicController.cs:4` | Uses System.Drawing. On modern .NET, System.Drawing.Common is supported only on Windows and throws PlatformNotSupportedException elsewhere. Use a cross-platform imaging library (e.g. ImageSharp, SkiaSharp) if the app must run on Linux. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopModernizedMVCSolution/src/eShopModernizedMVC/App_Start/Startup.Auth.cs:38` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopModernizedMVCSolution/src/eShopModernizedMVC/App_Start/CatalogConfiguration.cs:44` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopModernizedMVCSolution/src/eShopModernizedMVC/App_Start/CatalogConfiguration.cs:52` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopModernizedMVCSolution/src/eShopModernizedMVC/App_Start/CatalogConfiguration.cs:68` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopModernizedMVCSolution/src/eShopModernizedMVC/App_Start/CatalogConfiguration.cs:76` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopModernizedMVCSolution/src/eShopModernizedMVC/App_Start/CatalogConfiguration.cs:84` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopModernizedMVCSolution/src/eShopModernizedMVC/App_Start/CatalogConfiguration.cs:90` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG7001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG7001.md) | Medium | Probable | Small | `eShopModernizedMVCSolution/src/eShopModernizedMVC/App_Start/SqlAccessTokenProvider.cs:3` | Uses System.Data.SqlClient, which is in maintenance mode. Switch to Microsoft.Data.SqlClient (note its Encrypt=true default change, see MIG7002). |

### `eShopModernizedNTier/src/eShopWCFService/eShopWCFService.csproj`

Estimated effort: 4.3–14 engineer-days

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) | Medium | Certain | Small | `eShopModernizedNTier/src/eShopWCFService/eShopWCFService.csproj:1` | Project 'eShopWCFService' uses the legacy non-SDK project format. |
| [MIG1002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1002.md) | Medium | Certain | Small | `eShopModernizedNTier/src/eShopWCFService/packages.config:1` | Project 'eShopWCFService' declares NuGet dependencies in packages.config rather than PackageReference. |
| [MIG1003](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1003.md) | Medium | Certain | Small | `eShopModernizedNTier/src/eShopWCFService/eShopWCFService.csproj:1` | Project 'eShopWCFService' targets .NET Framework 4.6.1 (below 4.6.2). Retarget to at least 4.6.2 before migrating. Earlier versions lack the .NET Standard 2.0 surface that migration relies on. |
| [MIG3002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3002.md) | High | Certain | Medium | `eShopModernizedNTier/src/eShopWCFService/eShopWCFService.csproj:66` | Project 'eShopWCFService' references System.Web outside of WebForms. System.Web is not available on modern .NET; the dependent code needs replacing. |
| [MIG3015](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3015.md) | Medium | Probable | Small | `eShopModernizedNTier/src/eShopWCFService/ICatalogService.cs:6` | Uses WCF (System.ServiceModel). On modern .NET the WCF client is supported by adding the System.ServiceModel.* packages (e.g. System.ServiceModel.Http, .NetTcp) and regenerating the proxy; config-based endpoints usually move to code. Server-side hosting instead needs CoreWCF (see MIG3004). |
| [MIG3015](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3015.md) | Medium | Probable | Small | `eShopModernizedNTier/src/eShopWCFService/CatalogService.svc.cs:7` | Uses WCF (System.ServiceModel). On modern .NET the WCF client is supported by adding the System.ServiceModel.* packages (e.g. System.ServiceModel.Http, .NetTcp) and regenerating the proxy; config-based endpoints usually move to code. Server-side hosting instead needs CoreWCF (see MIG3004). |
| [MIG3015](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3015.md) | Medium | Probable | Small | `eShopModernizedNTier/src/eShopWCFService/CatalogServiceClient.cs:9` | Uses WCF (System.ServiceModel). On modern .NET the WCF client is supported by adding the System.ServiceModel.* packages (e.g. System.ServiceModel.Http, .NetTcp) and regenerating the proxy; config-based endpoints usually move to code. Server-side hosting instead needs CoreWCF (see MIG3004). |

### `eShopModernizedNTier/src/eShopWinForms/eShopWinForms.csproj`

Estimated effort: 2–5 engineer-days

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG4001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG4001.md) | High | Probable | Medium | `eShopModernizedNTier/src/eShopWinForms/Views/CatalogView.cs:5` | Uses System.Drawing. On modern .NET, System.Drawing.Common is supported only on Windows and throws PlatformNotSupportedException elsewhere. Use a cross-platform imaging library (e.g. ImageSharp, SkiaSharp) if the app must run on Linux. |

### `eShopModernizedNTier/src/eShopWinForms/eShopWinForms.fx.csproj`

Estimated effort: 2.5–7 engineer-days

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) | Medium | Certain | Small | `eShopModernizedNTier/src/eShopWinForms/eShopWinForms.fx.csproj:2` | Project 'eShopWinForms.fx' uses the legacy non-SDK project format. |
| [MIG4001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG4001.md) | High | Probable | Medium | `eShopModernizedNTier/src/eShopWinForms/Views/CatalogView.cs:5` | Uses System.Drawing. On modern .NET, System.Drawing.Common is supported only on Windows and throws PlatformNotSupportedException elsewhere. Use a cross-platform imaging library (e.g. ImageSharp, SkiaSharp) if the app must run on Linux. |

### `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/eShopModernizedWebForms.csproj`

Estimated effort: 12.5–37.5 engineer-days, plus 1 item requiring an architectural decision before they can be estimated

| Rule | Severity | Tier | Effort | Location | Issue |
| --- | --- | --- | --- | --- | --- |
| [MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) | Medium | Certain | Small | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/eShopModernizedWebForms.csproj:2` | Project 'eShopModernizedWebForms' uses the legacy non-SDK project format. |
| [MIG1002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1002.md) | Medium | Certain | Small | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/packages.config:1` | Project 'eShopModernizedWebForms' declares NuGet dependencies in packages.config rather than PackageReference. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/packages.config:28` | Package 'Microsoft.AspNet.Web.Optimization' has no version that supports net10.0. System.Web bundling/minification runs only on .NET Framework. Consider: A build-time bundler or ASP.NET Core static asset pipeline. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/packages.config:62` | Package 'Microsoft.Owin.Host.SystemWeb' has no version that supports net10.0. OWIN hosting on System.Web runs only on .NET Framework. Consider: ASP.NET Core hosting. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/packages.config:68` | Package 'Microsoft.Web.Infrastructure' has no version that supports net10.0. A System.Web infrastructure shim with no modern .NET target. Consider: None required on modern .NET. |
| [MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) | High | Certain | Medium | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/packages.config:147` | Package 'WebGrease' has no version that supports net10.0. A System.Web bundling dependency with no modern .NET target. Consider: A build-time bundler. |
| [MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) | Blocker | Certain | Blocker | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/eShopModernizedWebForms.csproj:2` | Project 'eShopModernizedWebForms' is an ASP.NET WebForms application (.aspx/.ascx present). WebForms has no equivalent on modern .NET and needs re-architecting (e.g. to Razor Pages, MVC, or Blazor). |
| [MIG3003](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3003.md) | High | Certain | Large | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/eShopModernizedWebForms.csproj:2` | Project 'eShopModernizedWebForms' exposes an ASMX web service (.asmx present). ASMX has no counterpart on modern .NET; re-implement the service as an ASP.NET Core Web API or gRPC. |
| [MIG4001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG4001.md) | High | Probable | Medium | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/Catalog/PicUploader.asmx.cs:6` | Uses System.Drawing. On modern .NET, System.Drawing.Common is supported only on Windows and throws PlatformNotSupportedException elsewhere. Use a cross-platform imaging library (e.g. ImageSharp, SkiaSharp) if the app must run on Linux. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/App_Start/Startup.Auth.cs:21` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/App_Start/CatalogConfiguration.cs:44` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/App_Start/CatalogConfiguration.cs:52` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/App_Start/CatalogConfiguration.cs:68` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/App_Start/CatalogConfiguration.cs:76` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/App_Start/CatalogConfiguration.cs:84` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG5001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG5001.md) | Low | Probable | Small | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/App_Start/CatalogConfiguration.cs:90` | Reads configuration via ConfigurationManager.AppSettings. On modern .NET this requires the System.Configuration.ConfigurationManager package, or migration to Microsoft.Extensions.Configuration. |
| [MIG7001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG7001.md) | Medium | Probable | Small | `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/App_Start/SqlAccessTokenProvider.cs:3` | Uses System.Data.SqlClient, which is in maintenance mode. Switch to Microsoft.Data.SqlClient (note its Encrypt=true default change, see MIG7002). |

## Effort breakdown

| Project | Findings | Estimated days | Needs decision |
| --- | --- | --- | --- |
| `eShopLegacyMVCSolution/eShopLegacy.Utilities/eShopLegacy.Utilities.csproj` | 4 | 8.5–26.5 | 0 |
| `eShopLegacyMVCSolution/eShopPorted/eShopPorted.csproj` | 3 | 7.5–22 | 0 |
| `eShopLegacyMVCSolution/src/eShopLegacyMVC/eShopLegacyMVC.csproj` | 28 | 17.8–52 | 1 |
| `eShopLegacyNTier/src/eShopWCFService/eShopWCFService.csproj` | 7 | 4.3–14 | 0 |
| `eShopLegacyNTier/src/eShopWinForms/eShopWinForms.csproj` | 3 | 3–9 | 0 |
| `eShopLegacyWebFormsSolution/src/eShopLegacyWebForms/eShopLegacyWebForms.csproj` | 10 | 5.3–16.5 | 1 |
| `eShopModernizedMVCSolution/src/eShopModernizedMVC/eShopModernizedMVC.csproj` | 26 | 20.5–60 | 0 |
| `eShopModernizedNTier/src/eShopWCFService/eShopWCFService.csproj` | 7 | 4.3–14 | 0 |
| `eShopModernizedNTier/src/eShopWinForms/eShopWinForms.csproj` | 1 | 2–5 | 0 |
| `eShopModernizedNTier/src/eShopWinForms/eShopWinForms.fx.csproj` | 2 | 2.5–7 | 0 |
| `eShopModernizedWebFormsSolution/src/eShopModernizedWebForms/eShopModernizedWebForms.csproj` | 17 | 12.5–37.5 | 1 |
| **Total** | **108** | **88–263.5** | **3** |

_These figures are heuristic planning aids derived from static analysis and are not a quote._

## References

Everything the scanned projects declare a dependency on, read from the project files. This is an inventory, not findings: nothing here is counted, estimated, or a build failure. This is the list to research. Check each third-party component for a supported .NET 10 release before committing to a plan.

### Third-party (197 distinct)

| Reference | Kind | Version | Used by | Resolved from |
| --- | --- | --- | --- | --- |
| Antlr | NuGet package | 3.5.0.2 | 5 projects | n/a |
| Antlr3.Runtime | NuGet package | 3.5.0.2 | 3 projects | `../../packages/Antlr.3.5.0.2/lib/Antlr3.Runtime.dll` |
| AspNet.ScriptManager.bootstrap | NuGet package | 4.3.1 | 2 projects | n/a |
| AspNet.ScriptManager.jQuery | NuGet package | 3.3.1, 3.4.1 | 2 projects | n/a |
| Autofac | NuGet package | 4.9.1, 4.9.4, 6.1.0 | 5 projects | n/a |
| Autofac.Extensions.DependencyInjection | NuGet package | 4.4.0 | 1 project | n/a |
| Autofac.Integration.Mvc | NuGet package | 4.0.0.0 | 2 projects | `../../packages/Autofac.Mvc5.4.0.2/lib/net45/Autofac.Integration.Mvc.dll` |
| Autofac.Integration.Web | NuGet package | 4.0.0.0 | 2 projects | `../../packages/Autofac.Web.4.0.0/lib/net45/Autofac.Integration.Web.dll` |
| Autofac.Mvc5 | NuGet package | 4.0.2 | 3 projects | n/a |
| Autofac.Web | NuGet package | 4.0.0 | 2 projects | n/a |
| autofac.webapi2 | NuGet package | 6.0.1 | 1 project | n/a |
| bootstrap | NuGet package | 4.3.1 | 4 projects | n/a |
| EntityFramework | NuGet package | 6.1.3, 6.2.0, 6.3.0, 6.4.4 | 9 projects | n/a |
| EntityFramework.SqlServer | NuGet package | 6.0.0.0 | 7 projects | `../../packages/EntityFramework.6.1.3/lib/net45/EntityFramework.SqlServer.dll`&lt;br&gt;`../../packages/EntityFramework.6.2.0/lib/net45/EntityFramework.SqlServer.dll`&lt;br&gt;`../../packages/EntityFramework.6.3.0/lib/net45/EntityFramework.SqlServer.dll`&lt;br&gt;`../packages/EntityFramework.6.1.3/lib/net45/EntityFramework.SqlServer.dll` |
| jQuery | NuGet package | 3.5.0 | 4 projects | n/a |
| jQuery.Validation | NuGet package | 1.19.4 | 2 projects | n/a |
| log4net | NuGet package | 2.0.10, 2.0.12 | 5 projects | n/a |
| log4net.Appender.Azure | NuGet package | 1.4.3.0 | 2 projects | n/a |
| Microsoft.AI.Agent.Intercept | NuGet package | 2.4.0.0 | 4 projects | `../../packages/Microsoft.ApplicationInsights.Agent.Intercept.2.4.0/lib/net45/Microsoft.AI.Agent.Intercept.dll` |
| Microsoft.AI.DependencyCollector | NuGet package | 2.11.2.0, 2.9.1.0 | 4 projects | `../../packages/Microsoft.ApplicationInsights.DependencyCollector.2.11.2/lib/net45/Microsoft.AI.DependencyCollector.dll`&lt;br&gt;`../../packages/Microsoft.ApplicationInsights.DependencyCollector.2.9.1/lib/net45/Microsoft.AI.DependencyCollector.dll` |
| Microsoft.AI.PerfCounterCollector | NuGet package | 2.11.2.0, 2.9.1.0 | 4 projects | `../../packages/Microsoft.ApplicationInsights.PerfCounterCollector.2.11.2/lib/net45/Microsoft.AI.PerfCounterCollector.dll`&lt;br&gt;`../../packages/Microsoft.ApplicationInsights.PerfCounterCollector.2.9.1/lib/net45/Microsoft.AI.PerfCounterCollector.dll` |
| Microsoft.AI.ServerTelemetryChannel | NuGet package | 2.11.0.0, 2.9.1.0 | 4 projects | `../../packages/Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.2.11.0/lib/net45/Microsoft.AI.ServerTelemetryChannel.dll`&lt;br&gt;`../../packages/Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.2.9.1/lib/net45/Microsoft.AI.ServerTelemetryChannel.dll` |
| Microsoft.AI.ServiceFabric | NuGet package | 2.3.1.0 | 2 projects | `../../packages/Microsoft.ApplicationInsights.ServiceFabric.2.3.1/lib/net45/Microsoft.AI.ServiceFabric.dll` |
| Microsoft.AI.Web | NuGet package | 2.11.2.0, 2.9.1.0 | 4 projects | `../../packages/Microsoft.ApplicationInsights.Web.2.11.2/lib/net45/Microsoft.AI.Web.dll`&lt;br&gt;`../../packages/Microsoft.ApplicationInsights.Web.2.9.1/lib/net45/Microsoft.AI.Web.dll` |
| Microsoft.AI.WindowsServer | NuGet package | 2.11.2.0, 2.9.1.0 | 4 projects | `../../packages/Microsoft.ApplicationInsights.WindowsServer.2.11.2/lib/net45/Microsoft.AI.WindowsServer.dll`&lt;br&gt;`../../packages/Microsoft.ApplicationInsights.WindowsServer.2.9.1/lib/net45/Microsoft.AI.WindowsServer.dll` |
| Microsoft.ApplicationInsights | NuGet package | 2.11.0, 2.9.1 | 4 projects | n/a |
| Microsoft.ApplicationInsights.Agent.Intercept | NuGet package | 2.4.0 | 4 projects | n/a |
| Microsoft.ApplicationInsights.DependencyCollector | NuGet package | 2.11.2, 2.9.1 | 4 projects | n/a |
| Microsoft.ApplicationInsights.Log4NetAppender | NuGet package | 2.11.0 | 1 project | n/a |
| Microsoft.ApplicationInsights.PerfCounterCollector | NuGet package | 2.11.2, 2.9.1 | 4 projects | n/a |
| Microsoft.ApplicationInsights.ServiceFabric | NuGet package | 2.3.1 | 2 projects | n/a |
| Microsoft.ApplicationInsights.TraceListener | NuGet package | 2.11.0 | 2 projects | n/a |
| Microsoft.ApplicationInsights.Web | NuGet package | 2.11.2, 2.9.1 | 4 projects | n/a |
| Microsoft.ApplicationInsights.WindowsServer | NuGet package | 2.11.2, 2.9.1 | 4 projects | n/a |
| Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel | NuGet package | 2.11.0, 2.9.1 | 4 projects | n/a |
| Microsoft.AspNet.FriendlyUrls | NuGet package | 1.0.2 | 2 projects | n/a |
| Microsoft.AspNet.FriendlyUrls.Core | NuGet package | 1.0.2 | 2 projects | n/a |
| Microsoft.AspNet.Mvc | NuGet package | 5.2.7 | 2 projects | n/a |
| Microsoft.AspNet.Razor | NuGet package | 3.2.7 | 2 projects | n/a |
| Microsoft.AspNet.ScriptManager.MSAjax | NuGet package | 5.0.0 | 2 projects | n/a |
| Microsoft.AspNet.ScriptManager.WebForms | NuGet package | 5.0.0 | 2 projects | n/a |
| Microsoft.AspNet.SessionState.SessionStateModule | NuGet package | 1.1.0 | 4 projects | n/a |
| Microsoft.AspNet.TelemetryCorrelation | NuGet package | 1.0.5, 1.0.7 | 4 projects | n/a |
| Microsoft.AspNet.Web.Optimization | NuGet package | 1.1.3 | 4 projects | n/a |
| Microsoft.AspNet.Web.Optimization.WebForms | NuGet package | 1.1.3 | 2 projects | n/a |
| Microsoft.AspNet.WebApi | NuGet package | 5.2.7 | 1 project | n/a |
| Microsoft.AspNet.WebApi.Client | NuGet package | 5.2.3, 5.2.7 | 4 projects | n/a |
| Microsoft.AspNet.WebApi.Core | NuGet package | 5.2.7 | 1 project | n/a |
| Microsoft.AspNet.WebApi.WebHost | NuGet package | 5.2.7 | 1 project | n/a |
| Microsoft.AspNet.WebPages | NuGet package | 3.2.7 | 2 projects | n/a |
| Microsoft.AspNetCore | NuGet package | 2.2.0 | 1 project | n/a |
| Microsoft.AspNetCore.Mvc | NuGet package | 2.2.0 | 1 project | n/a |
| Microsoft.AspNetCore.StaticFiles | NuGet package | 2.2.0 | 1 project | n/a |
| Microsoft.Azure.KeyVault | NuGet package | 3.0.4 | 2 projects | n/a |
| Microsoft.Azure.KeyVault.Core | NuGet package | 3.0.4 | 2 projects | n/a |
| Microsoft.Azure.KeyVault.WebKey | NuGet package | 3.0.4 | 2 projects | n/a |
| Microsoft.Azure.Services.AppAuthentication | NuGet package | 1.3.1 | 2 projects | n/a |
| Microsoft.Bcl.AsyncInterfaces | NuGet package | 1.0.0 | 2 projects | n/a |
| Microsoft.CodeDom.Providers.DotNetCompilerPlatform | NuGet package | 2.0.1 | 4 projects | n/a |
| Microsoft.Configuration.ConfigurationBuilders.Azure | NuGet package | 2.0.0-beta | 2 projects | n/a |
| Microsoft.Configuration.ConfigurationBuilders.Base | NuGet package | 2.0.0-beta | 2 projects | n/a |
| Microsoft.Configuration.ConfigurationBuilders.Environment | NuGet package | 2.0.0-beta | 2 projects | n/a |
| Microsoft.Configuration.ConfigurationBuilders.UserSecrets | NuGet package | 2.0.0-beta | 2 projects | n/a |
| Microsoft.CSharp | NuGet package | 4.7.0 | 1 project | n/a |
| Microsoft.Data.Edm | NuGet package | 5.8.4 | 2 projects | n/a |
| Microsoft.Data.OData | NuGet package | 5.8.4 | 2 projects | n/a |
| Microsoft.Data.Services.Client | NuGet package | 5.8.4 | 2 projects | n/a |
| Microsoft.EntityFrameworkCore | NuGet package | 2.2.6 | 1 project | n/a |
| Microsoft.EntityFrameworkCore.Design | NuGet package | 2.2.6 | 1 project | n/a |
| Microsoft.EntityFrameworkCore.Relational | NuGet package | 2.2.6 | 1 project | n/a |
| Microsoft.EntityFrameworkCore.SqlServer | NuGet package | 2.2.6 | 1 project | n/a |
| Microsoft.Extensions.Configuration | NuGet package | 3.0.0 | 2 projects | n/a |
| Microsoft.Extensions.Configuration.Abstractions | NuGet package | 3.0.0 | 2 projects | n/a |
| Microsoft.Extensions.Configuration.Binder | NuGet package | 3.0.0 | 2 projects | n/a |
| Microsoft.Extensions.Configuration.FileExtensions | NuGet package | 3.0.0 | 2 projects | n/a |
| Microsoft.Extensions.Configuration.Json | NuGet package | 3.0.0 | 2 projects | n/a |
| Microsoft.Extensions.FileProviders.Abstractions | NuGet package | 3.0.0 | 2 projects | n/a |
| Microsoft.Extensions.FileProviders.Physical | NuGet package | 3.0.0 | 2 projects | n/a |
| Microsoft.Extensions.FileSystemGlobbing | NuGet package | 3.0.0 | 2 projects | n/a |
| Microsoft.Extensions.Primitives | NuGet package | 3.0.0 | 2 projects | n/a |
| Microsoft.IdentityModel.Clients.ActiveDirectory | NuGet package | 5.2.4 | 2 projects | n/a |
| Microsoft.IdentityModel.JsonWebTokens | NuGet package | 5.6.0 | 2 projects | n/a |
| Microsoft.IdentityModel.Logging | NuGet package | 5.6.0 | 2 projects | n/a |
| Microsoft.IdentityModel.Protocol.Extensions | NuGet package | 1.0.4.403061554 | 2 projects | n/a |
| Microsoft.IdentityModel.Protocols | NuGet package | 5.6.0 | 2 projects | n/a |
| Microsoft.IdentityModel.Protocols.OpenIdConnect | NuGet package | 5.6.0 | 2 projects | n/a |
| Microsoft.IdentityModel.Tokens | NuGet package | 5.6.0 | 2 projects | n/a |
| Microsoft.jQuery.Unobtrusive.Validation | NuGet package | 3.2.11 | 2 projects | n/a |
| Microsoft.Net.Compilers | NuGet package | 2.10.0, 3.3.1 | 4 projects | n/a |
| Microsoft.NETCore.Platforms | NuGet package | 3.0.0 | 2 projects | n/a |
| Microsoft.Owin | NuGet package | 4.2.2 | 2 projects | n/a |
| Microsoft.Owin.Host.SystemWeb | NuGet package | 4.0.1 | 2 projects | n/a |
| Microsoft.Owin.Security | NuGet package | 4.0.1 | 2 projects | n/a |
| Microsoft.Owin.Security.Cookies | NuGet package | 4.2.2 | 2 projects | n/a |
| Microsoft.Owin.Security.OpenIdConnect | NuGet package | 4.0.1 | 2 projects | n/a |
| Microsoft.Rest.ClientRuntime | NuGet package | 2.3.24 | 2 projects | n/a |
| Microsoft.Rest.ClientRuntime.Azure | NuGet package | 3.3.19 | 2 projects | n/a |
| Microsoft.ScriptManager.MSAjax | NuGet package | 5.0.0.0 | 2 projects | `../../packages/Microsoft.AspNet.ScriptManager.MSAjax.5.0.0/lib/net45/Microsoft.ScriptManager.MSAjax.dll` |
| Microsoft.ScriptManager.WebForms | NuGet package | 5.0.0.0 | 2 projects | `../../packages/Microsoft.AspNet.ScriptManager.WebForms.5.0.0/lib/net45/Microsoft.ScriptManager.WebForms.dll` |
| Microsoft.ServiceBus | NuGet package | 3.0.0.0 | 2 projects | `../../packages/WindowsAzure.ServiceBus.6.0.0/lib/net462/Microsoft.ServiceBus.dll` |
| Microsoft.Web.Infrastructure | NuGet package | 1.0.0.0 | 4 projects | n/a |
| Microsoft.Web.RedisSessionStateProvider | NuGet package | 4.0.1 | 2 projects | n/a |
| Microsoft.Win32.Primitives | NuGet package | 4.3.0 | 2 projects | n/a |
| Microsoft.WindowsAzure.Configuration | NuGet package | 3.0.0.0 | 2 projects | `../../packages/Microsoft.WindowsAzure.ConfigurationManager.3.2.3/lib/net40/Microsoft.WindowsAzure.Configuration.dll` |
| Microsoft.WindowsAzure.ConfigurationManager | NuGet package | 3.2.3 | 2 projects | n/a |
| Microsoft.WindowsAzure.Storage | NuGet package | 9.3.2.0 | 2 projects | `../../packages/WindowsAzure.Storage.9.3.3/lib/net45/Microsoft.WindowsAzure.Storage.dll` |
| Modernizr | NuGet package | 2.8.3 | 4 projects | n/a |
| NETStandard.Library | NuGet package | 2.0.3 | 2 projects | n/a |
| Newtonsoft.Json | NuGet package | 12.0.1, 13.0.2, 6.0.4 | 6 projects | n/a |
| Owin | NuGet package | 1.0 | 2 projects | n/a |
| Pipelines.Sockets.Unofficial | NuGet package | 1.0.7, 2.1.0 | 4 projects | n/a |
| popper.js | NuGet package | 1.14.3 | 4 projects | n/a |
| Respond | NuGet package | 1.4.2 | 4 projects | n/a |
| StackExchange.Redis | NuGet package | 2.0.601 | 2 projects | n/a |
| System.AppContext | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Buffers | NuGet package | 4.4.0, 4.5.0, 4.5.1 | 4 projects | n/a |
| System.Collections | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Collections.Concurrent | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Collections.Immutable | NuGet package | 1.6.0 | 2 projects | n/a |
| System.Collections.NonGeneric | NuGet package | 4.3.0 | 2 projects | n/a |
| System.ComponentModel.EventBasedAsync | NuGet package | 4.3.0 | 2 projects | n/a |
| System.ComponentModel.Primitives | NuGet package | 4.3.0 | 2 projects | n/a |
| System.ComponentModel.TypeConverter | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Console | NuGet package | 4.3.1 | 2 projects | n/a |
| System.Diagnostics.Debug | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Diagnostics.DiagnosticSource | NuGet package | 4.5.1, 4.6.0 | 4 projects | n/a |
| System.Diagnostics.PerformanceCounter | NuGet package | 4.5.0, 4.6.0 | 4 projects | n/a |
| System.Diagnostics.Tools | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Diagnostics.Tracing | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Dynamic.Runtime | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Globalization | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Globalization.Calendars | NuGet package | 4.3.0 | 2 projects | n/a |
| System.IdentityModel.Tokens.Jwt | NuGet package | 5.6.0 | 2 projects | n/a |
| System.IO | NuGet package | 4.3.0 | 2 projects | n/a |
| System.IO.Compression | NuGet package | 4.3.0 | 4 projects | n/a |
| System.IO.Compression.ZipFile | NuGet package | 4.3.0 | 4 projects | n/a |
| System.IO.FileSystem | NuGet package | 4.3.0 | 2 projects | n/a |
| System.IO.FileSystem.Primitives | NuGet package | 4.3.0 | 2 projects | n/a |
| System.IO.Pipelines | NuGet package | 4.5.1, 4.6.0 | 4 projects | n/a |
| System.Linq | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Linq.Expressions | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Linq.Queryable | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Memory | NuGet package | 4.5.1, 4.5.3, 4.5.4 | 4 projects | n/a |
| System.Net.Http | NuGet package | 4.3.4 | 3 projects | n/a |
| System.Net.Http.Formatting | NuGet package | 5.2.3.0 | 1 project | `../packages/Microsoft.AspNet.WebApi.Client.5.2.3/lib/net45/System.Net.Http.Formatting.dll` |
| System.Net.Primitives | NuGet package | 4.3.1 | 2 projects | n/a |
| System.Net.Requests | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Net.Sockets | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Numerics.Vectors | NuGet package | 4.4.0, 4.5.0 | 4 projects | n/a |
| System.ObjectModel | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Reflection | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Reflection.Extensions | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Reflection.Primitives | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Resources.ResourceManager | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Runtime | NuGet package | 4.3.1 | 2 projects | n/a |
| System.Runtime.CompilerServices.Unsafe | NuGet package | 4.5.0, 4.6.0 | 4 projects | n/a |
| System.Runtime.Extensions | NuGet package | 4.3.1 | 2 projects | n/a |
| System.Runtime.Handles | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Runtime.InteropServices | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Runtime.InteropServices.RuntimeInformation | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Runtime.Numerics | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Security.Cryptography.Algorithms | NuGet package | 4.3.1 | 2 projects | n/a |
| System.Security.Cryptography.Encoding | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Security.Cryptography.Primitives | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Security.Cryptography.X509Certificates | NuGet package | 4.3.2 | 2 projects | n/a |
| System.ServiceModel.Duplex | NuGet package | 4.8.0 | 1 project | n/a |
| System.ServiceModel.Http | NuGet package | 4.8.0 | 1 project | n/a |
| System.ServiceModel.NetTcp | NuGet package | 4.8.0 | 1 project | n/a |
| System.ServiceModel.Security | NuGet package | 4.8.0 | 1 project | n/a |
| System.Spatial | NuGet package | 5.8.4 | 2 projects | n/a |
| System.Text.Encoding | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Text.Encoding.Extensions | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Text.Encodings.Web | NuGet package | 4.7.2 | 2 projects | n/a |
| System.Text.Json | NuGet package | 4.6.0 | 2 projects | n/a |
| System.Text.RegularExpressions | NuGet package | 4.3.1 | 2 projects | n/a |
| System.Threading | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Threading.Channels | NuGet package | 4.5.0, 4.6.0 | 4 projects | n/a |
| System.Threading.Tasks | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Threading.Tasks.Dataflow | NuGet package | 4.10.0 | 2 projects | n/a |
| System.Threading.Tasks.Extensions | NuGet package | 4.5.1, 4.5.3 | 4 projects | n/a |
| System.Threading.Tasks.Parallel | NuGet package | 4.3.0 | 2 projects | n/a |
| System.Threading.Timer | NuGet package | 4.3.0 | 2 projects | n/a |
| System.ValueTuple | NuGet package | 4.5.0 | 2 projects | n/a |
| System.Web.Helpers | NuGet package | 3.0.0.0 | 2 projects | `../../packages/Microsoft.AspNet.WebPages.3.2.7/lib/net45/System.Web.Helpers.dll` |
| System.Web.Mvc | NuGet package | 5.2.7.0 | 2 projects | `../../packages/Microsoft.AspNet.Mvc.5.2.7/lib/net45/System.Web.Mvc.dll` |
| System.Web.Optimization | NuGet package | 1.1.0.0 | 4 projects | `../../packages/Microsoft.AspNet.Web.Optimization.1.1.3/lib/net40/System.Web.Optimization.dll` |
| System.Web.Razor | NuGet package | 3.0.0.0 | 2 projects | `../../packages/Microsoft.AspNet.Razor.3.2.7/lib/net45/System.Web.Razor.dll` |
| System.Web.WebPages | NuGet package | 3.0.0.0 | 2 projects | `../../packages/Microsoft.AspNet.WebPages.3.2.7/lib/net45/System.Web.WebPages.dll` |
| System.Web.WebPages.Deployment | NuGet package | 3.0.0.0 | 2 projects | `../../packages/Microsoft.AspNet.WebPages.3.2.7/lib/net45/System.Web.WebPages.Deployment.dll` |
| System.Web.WebPages.Razor | NuGet package | 3.0.0.0 | 2 projects | `../../packages/Microsoft.AspNet.WebPages.3.2.7/lib/net45/System.Web.WebPages.Razor.dll` |
| System.Xml.ReaderWriter | NuGet package | 4.3.1 | 2 projects | n/a |
| System.Xml.XDocument | NuGet package | 4.3.0 | 2 projects | n/a |
| Validation | NuGet package | 2.4.22 | 2 projects | n/a |
| WebGrease | NuGet package | 1.6.0 | 5 projects | n/a |
| WindowsAzure.ServiceBus | NuGet package | 6.0.0 | 2 projects | n/a |
| WindowsAzure.Storage | NuGet package | 9.3.3 | 2 projects | n/a |
| eShopServiceReference | Service proxy | n/a | 2 projects | `Connected Services/eShopServiceReference/Reference.svcmap`&lt;br&gt;`http://localhost:62314/CatalogService.svc` |

### Solution-internal project references

This solution's own code, already in scope. Listed to show the build order dependencies:

| Project | Depends on |
| --- | --- |
| `eShopLegacyMVCSolution/eShopPorted/eShopPorted.csproj` | eShopLegacy.Utilities |
| `eShopLegacyMVCSolution/src/eShopLegacyMVC/eShopLegacyMVC.csproj` | eShopLegacy.Utilities |

_186 framework assembly references were also read (`System.*`, `mscorlib`, WPF, …) and are not listed. They move with the runtime rather than needing research._

## Remediation guidance

**[MIG1001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1001.md) · Non-SDK-style project file**

Convert the project to the SDK style (&lt;Project Sdk="Microsoft.NET.Sdk"&gt;). Replace TargetFrameworkVersion with a TargetFramework moniker, move packages.config entries to PackageReference, and let the SDK glob source files instead of listing them. Do this before other migration work: nearly every later step assumes an SDK-style project.

**[MIG1002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1002.md) · packages.config instead of PackageReference**

Migrate packages.config to PackageReference (Visual Studio offers an in-place migration, or run 'dotnet migrate'-style tooling). PackageReference is required for SDK-style projects and gives transitive restore.

**[MIG1003](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG1003.md) · Target framework below 4.6.2**

Retarget the project to at least .NET Framework 4.6.2 before migrating. Versions below 4.6.2 lack full .NET Standard 2.0 support, which the migration path relies on. This is usually a low-risk retarget-and-rebuild step.

**[MIG2001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG2001.md) · Package has no version supporting the target framework**

Replace the package with a version or successor that targets modern .NET, or remove the dependency. See the suggested replacement in the finding message.

**[MIG3001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md) · ASP.NET WebForms**

WebForms has no counterpart on modern .NET. Plan a re-architecture to Razor Pages, MVC, or Blazor. This is an architectural decision, not a mechanical port.

**[MIG3002](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3002.md) · System.Web dependency outside WebForms**

System.Web is not available on modern .NET. Replace the dependent code: HttpContext usage moves to Microsoft.AspNetCore.Http, HttpUtility to System.Web.HttpUtility's modern equivalents or System.Net.WebUtility.

**[MIG3003](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3003.md) · ASMX web service**

ASMX web services have no counterpart on modern .NET. Re-implement the service as an ASP.NET Core Web API (or gRPC). Clients that consumed the ASMX endpoint need updating too.

**[MIG3005](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3005.md) · .NET Remoting**

.NET Remoting was removed entirely and has no modern equivalent. Replace it with a supported transport: gRPC, WCF on CoreWCF, or an HTTP API. This needs an architectural decision.

**[MIG3010](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3010.md) · ASP.NET MVC 5 (System.Web.Mvc)**

ASP.NET MVC 5 (System.Web.Mvc) runs only on .NET Framework. Migrate to ASP.NET Core MVC (Microsoft.AspNetCore.Mvc). Controllers and views port with edits; routing, filters, and DI wiring change.

**[MIG3015](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3015.md) · WCF client (System.ServiceModel)**

WCF client usage is supported on modern .NET via the System.ServiceModel.* packages (System.ServiceModel.Http, .NetTcp, .Primitives). Add them, regenerate the service reference, and move config-based endpoint/binding settings to code. Server-side hosting is not supported and needs CoreWCF (see MIG3004).

**[MIG4001](https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG4001.md) · System.Drawing.Common on non-Windows**

System.Drawing.Common is Windows-only on modern .NET and throws PlatformNotSupportedException elsewhere. If the app must run on Linux, switch to a cross-platform imaging library such as ImageSharp or SkiaSharp.

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
