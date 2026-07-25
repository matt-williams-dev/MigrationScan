# Reference inventory

Alongside its findings, MigrationScan catalogs every dependency each scanned project declares.
This is **inventory, not findings**: entries carry no severity and no effort, they are excluded
from the counts and the estimate, and they never trip `--fail-on`.

It exists because the expensive unknowns in a .NET Framework migration are usually not in your
code — they're in somebody else's. A grid control from a vendor that folded in 2016, a COM
component with no 64-bit build, an ASMX proxy pointing at a service nobody owns any more. Rules
can only flag the ones with a known pattern. The inventory gives you the whole list so you can
research it.

It appears in the console summary, in the Markdown report under **References**, and in the JSON
report as the `references` array (schema 1.4+).

## What gets collected

Everything is read from project XML, so every entry is Tier 1 (certain). Nothing is resolved on
disk: a reference to a file that no longer exists is still catalogued, because a dangling
dependency is exactly what a scoping exercise needs to see.

| Kind | Read from | Notes |
| --- | --- | --- |
| `package` | `packages.config`, `<PackageReference>` | Version is the *package* version |
| `assembly` | `<Reference>` with no `<HintPath>` | Resolves from the framework or the GAC |
| `vendoredAssembly` | `<Reference>` with a `<HintPath>` to a checked-in DLL | The ones with no upstream |
| `com` | `<COMReference>`, `<COMFileReference>`, `Interop.*` / `AxInterop.*`, `<EmbedInteropTypes>` | COM and ActiveX |
| `project` | `<ProjectReference>` | The solution-internal build graph |
| `webService` | `<WebReferenceUrl>` (ASMX), `.svcmap` (WCF) | Records the endpoint URL |

A binary scan (`migrationscan Foo.dll`) has no project file, so its inventory comes from the
compiled metadata instead: assembly references with their bound versions, and nothing else —
metadata carries no hint paths or package identity.

## Judgment calls

Three places where the tool decides something rather than reporting it raw. Each is visible in
the output, but worth knowing about.

**A `<Reference>` is dropped when a package of the same name is already declared.** Legacy
projects routinely declare both, and sometimes inconsistently — a `packages.config` entry
alongside a `<Reference>` that resolves from the GAC. It is still one component to research, and
the package entry is the better record: it carries the package version (`13.0.3`) rather than the
assembly version (`13.0.0.0`). Where the assembly actually resolves from is reported as a
*finding* — [MIG1005](rules/MIG1005.md) for a GAC reference, [MIG1010](rules/MIG1010.md) for a
vendored one — which is the right place for it. With no matching package the reference is always
kept.

**Framework assemblies are excluded from the third-party tables**, and counted in a note instead.
The test is prefix-based (`System*`, `mscorlib`, `Microsoft.CSharp`, WPF assemblies, …), the same
one MIG1005 uses. It is deliberately generous: a rare `System.*` name misfiled as framework costs
less than flooding every report with the BCL. The full list is still in the JSON, flagged with
`isFrameworkAssembly`.

**`AxInterop.Foo` and `Interop.Foo` are classified as COM, not as vendored DLLs**, even though
they are checked-in assemblies with a hint path. They're generated wrappers — `tlbimp` and
`aximp` output — so the real dependency is the COM/ActiveX component behind them, which is what
you'd research. The same applies to any reference with `<EmbedInteropTypes>true</EmbedInteropTypes>`.

## What isn't collected

Stated so the inventory isn't mistaken for exhaustive:

- **`<Import Project="…" />`** — custom `.targets` and `.props` files, including vendor build
  extensions restored from packages. These are build infrastructure rather than runtime
  dependencies, and a migration usually rewrites them wholesale.
- **`licenses.licx`** — design-time licences for ActiveX and licensed WinForms controls. Every
  assembly named in a `.licx` is also a `<Reference>` in the same project, so reading it would
  only duplicate rows already in the table.
- **Native DLLs called via P/Invoke** — these live in source, not in the project file, so they
  would be Tier 2 rather than Tier 1. Windows system DLLs are already flagged by
  [MIG4013](rules/MIG4013.md); a bespoke native DLL shows up there as a non-finding.
- **Transitive package dependencies** — only what a project declares. Resolving the full graph
  means a restore, which needs the network (spec §3: offline by default).
- **Assembly binding redirects** in `app.config` / `web.config`.

## Using it

The Markdown table is the artifact to hand somebody. For research at scale, group the JSON on
`(kind, name)` and work the list:

```bash
migrationscan MySolution.sln --format json --output scan.json
jq -r '.references[] | select(.isThirdParty) | [.kind, .name, .version] | @tsv' scan.json | sort -u
```

The ones to look at first are `vendoredAssembly` and `com`: a NuGet package can be checked
against nuget.org in seconds, but a checked-in DLL or a registered type library means finding out
whether the thing still exists.
