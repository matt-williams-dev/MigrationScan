# Rule catalog

The 33 rules MigrationScan ships, grouped by category. Every finding in a report carries its rule id and a link back to the page for that rule, so this index is the same set of pages a report points at.

Each page covers what the rule detects, why it blocks or complicates a migration, how to fix it, and any known false positives.

## How to read a rule

Three things get conflated and they are not the same axis.

**Severity** is impact: what happens if you ship without addressing it. 3 blocker, 13 high, 16 medium, 1 low.

**Confidence tier** is how the finding was detected, and therefore how far to trust it.

| Tier | Source | What it means |
| --- | --- | --- |
| 1 · Certain | Project XML, `packages.config`, `app.config`, `web.config`, `.sln` | No ambiguity. It is there. |
| 2 · Probable | Roslyn syntax trees without a resolved compilation | Good recall, some false positives. `Registry` might be your own class. |
| 3 · Verified | Semantic model or compiled assembly metadata | Confirmed against resolved references. |

Of the 33 rules, 12 report at tier 1 and 21 at tier 2. A tier 2 finding that turns out wrong costs nothing. A tier 2 finding presented as certain costs your trust, so the tier is on every finding in every output format.

**Effort band** is estimability, not size. Some rules are a bounded afternoon; some are unbounded until somebody makes a decision, and those are counted separately and left unpriced rather than folded in with a guess.

## Categories

| Category | Rules |
| --- | --- |
| [Blocking frameworks](#blocking-frameworks) | 8 |
| [Project and build](#project-and-build) | 7 |
| [Runtime failures](#runtime-failures) | 7 |
| [Data access](#data-access) | 3 |
| [Serialization and security](#serialization-and-security) | 3 |
| [Dependencies](#dependencies) | 2 |
| [Globalization and encoding](#globalization-and-encoding) | 2 |
| [Configuration](#configuration) | 1 |

## Blocking frameworks

Frameworks with no counterpart on modern .NET. Each one needs an architectural decision before an estimate means anything.

| Rule | What it detects | Severity | Confidence |
| --- | --- | --- | --- |
| [MIG3001](MIG3001.md) | ASP.NET WebForms | blocker | 1 · Certain |
| [MIG3002](MIG3002.md) | System.Web dependency outside WebForms | high | 1 · Certain |
| [MIG3003](MIG3003.md) | ASMX web service | high | 1 · Certain |
| [MIG3004](MIG3004.md) | WCF service host (server side) | high | 2 · Probable |
| [MIG3005](MIG3005.md) | .NET Remoting | blocker | 2 · Probable |
| [MIG3009](MIG3009.md) | MSMQ (System.Messaging) | high | 2 · Probable |
| [MIG3010](MIG3010.md) | ASP.NET MVC 5 (System.Web.Mvc) | high | 2 · Probable |
| [MIG3015](MIG3015.md) | WCF client (System.ServiceModel) | medium | 2 · Probable |

## Project and build

Project file shape, target frameworks and package management. Usually the first work in any migration, and usually the cheapest.

| Rule | What it detects | Severity | Confidence |
| --- | --- | --- | --- |
| [MIG1001](MIG1001.md) | Non-SDK-style project file | medium | 1 · Certain |
| [MIG1002](MIG1002.md) | packages.config instead of PackageReference | medium | 1 · Certain |
| [MIG1003](MIG1003.md) | Target framework below 4.6.2 | medium | 1 · Certain |
| [MIG1005](MIG1005.md) | GAC reference (no HintPath) | medium | 1 · Certain |
| [MIG1006](MIG1006.md) | COM reference or interop assembly | medium | 1 · Certain |
| [MIG1007](MIG1007.md) | Legacy project type (SSRS, SSIS, setup, Silverlight, Web Site) | high | 1 · Certain |
| [MIG1010](MIG1010.md) | Vendored DLL with no source and no NuGet equivalent | high | 1 · Certain |

## Runtime failures

Code that compiles against modern .NET and then throws when it runs. The most expensive category to find late.

| Rule | What it detects | Severity | Confidence |
| --- | --- | --- | --- |
| [MIG4001](MIG4001.md) | System.Drawing.Common on non-Windows | high | 2 · Probable |
| [MIG4002](MIG4002.md) | Windows Registry access | high | 2 · Probable |
| [MIG4003](MIG4003.md) | System.Management / WMI | high | 2 · Probable |
| [MIG4004](MIG4004.md) | System.DirectoryServices / Active Directory | high | 2 · Probable |
| [MIG4005](MIG4005.md) | EventLog | medium | 2 · Probable |
| [MIG4008](MIG4008.md) | Thread.Abort | medium | 2 · Probable |
| [MIG4013](MIG4013.md) | P/Invoke to a Windows system DLL | medium | 2 · Probable |

## Data access

Data providers and ORMs whose modern equivalents changed defaults or dropped support.

| Rule | What it detects | Severity | Confidence |
| --- | --- | --- | --- |
| [MIG7001](MIG7001.md) | System.Data.SqlClient | medium | 2 · Probable |
| [MIG7003](MIG7003.md) | System.Data.OleDb on non-Windows | medium | 2 · Probable |
| [MIG7006](MIG7006.md) | LINQ to SQL (System.Data.Linq) | high | 2 · Probable |

## Serialization and security

Serializers and cryptography that were removed or made unsafe, and the APIs that silently changed behaviour.

| Rule | What it detects | Severity | Confidence |
| --- | --- | --- | --- |
| [MIG6001](MIG6001.md) | BinaryFormatter | blocker | 2 · Probable |
| [MIG6004](MIG6004.md) | Code Access Security attributes | medium | 2 · Probable |
| [MIG6005](MIG6005.md) | Obsolete cryptography types | medium | 2 · Probable |

## Dependencies

Packages and assemblies that have no modern build, or whose replacement is a different package entirely.

| Rule | What it detects | Severity | Confidence |
| --- | --- | --- | --- |
| [MIG2001](MIG2001.md) | Package has no version supporting the target framework | high | 1 · Certain |
| [MIG2002](MIG2002.md) | Package marked deprecated on nuget.org | medium | 1 · Certain |

## Globalization and encoding

Culture, encoding and calendar behaviour that changed between .NET Framework and modern .NET.

| Rule | What it detects | Severity | Confidence |
| --- | --- | --- | --- |
| [MIG8002](MIG8002.md) | Encoding.Default behavior change | medium | 2 · Probable |
| [MIG8003](MIG8003.md) | Code-page encoding without provider registration | medium | 2 · Probable |

## Configuration

Configuration APIs that need a package or a migration to the modern configuration model.

| Rule | What it detects | Severity | Confidence |
| --- | --- | --- | --- |
| [MIG5001](MIG5001.md) | ConfigurationManager.AppSettings usage | low | 2 · Probable |

## Adding a rule

Rule metadata lives in [`src/MigrationScan.Core/Data/rules.json`](../../src/MigrationScan.Core/Data/rules.json), not in C#. A new rule needs an entry there, a page in this directory, and a row in the table above. `RuleDocsTests` fails if any of the three is missing, and names what to add.

The `docsUrl` in the catalog points at this directory on GitHub and is a permanent contract with every report already generated. Reports are byte-identical across runs, and both committed sample reports carry these URLs, so changing where they point invalidates both. Leave them alone.
