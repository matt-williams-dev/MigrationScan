# What a real estate looks like through this tool

The [other sample](../woodgrove-banking/) is a fixture: four small projects, built to put one of
everything into a single report. This one is a repository somebody else wrote, scanned as found.

## The estate

[eShopModernizing](https://github.com/dotnet-architecture/eShopModernizing) is Microsoft's
reference sample for moving .NET Framework web apps onto Windows Containers. Seven solutions,
eleven projects: WebForms and MVC storefronts, a WCF service with its WinForms client, and a
containerized variant of each. MIT licensed, archived and read-only, last commit October 2023.

The whole estate took 1.5 seconds, with no configuration, no build, and no network.

## How to read the numbers

eShopModernizing set out to do one specific thing, and its README says so directly: modernize
deployment "with Windows Containers and Azure Cloud", "without having to change the app's
architecture or C# code". Staying on .NET Framework is the design. The sample does exactly what it
set out to do.

MigrationScan measures a different axis, the cost of leaving the framework behind, which this
sample never set out to address. Nothing below is a shortfall against its goals, and none of it is
a defect list. A codebase built to demonstrate legacy patterns produces legacy findings. The tool
and the sample agree with each other.

The reason to scan it anyway: it is a real codebase at real size, written by people solving real
problems, and nobody assembled it to make a scanner look good. Every number here came out of a
repository that existed before this tool did.

## What the scan reports

| | Cross-platform (`net10.0`) | Windows (`net10.0-windows`) |
| --- | --- | --- |
| Projects scanned | 11 | 11 |
| Findings | 108 (blocker 5 · high 54 · medium 29 · low 20) | 103 (blocker 5 · high 49 · medium 29 · low 20) |
| Estimated effort | 88–263.5 engineer-days | 78–238.5 engineer-days |
| Needs an architectural decision | 3 | 3 |
| Windows lock-in satisfied by target | n/a | 5 |
| Third-party references | 197 distinct | 197 distinct |
| Projects not assessed | 7 | 7 |
| Scan warnings | 1 | 1 |

### Leaving Windows costs almost nothing here

Five findings separate the two columns, every one of them `System.Drawing.Common`. This estate
reaches for the Registry, WMI and COM barely at all, so the framework migration is the whole cost
and portability is close to free on top of it.

An estate leaning on those APIs reads the opposite way. The [Woodgrove
fixture](../woodgrove-banking/) drops a quarter of its findings the moment the target is Windows.
Same tool, same two stances, opposite conclusion, which is why one scan reports both rather than
asking you to pick first.

### What the tool did not assess

Seven projects, named rather than passed over: four docker-compose projects (`.dcproj`) and three
Service Fabric projects (`.sfproj`). MigrationScan reads neither format. Those are the deployment
artifacts this sample exists to demonstrate, so the part of the repository most central to its
purpose is the part this tool has least to say about. They carry none of the effort estimate above
and need planning of their own.

One warning also fires: a project no solution in the scan references. Being unreferenced is not a
defect, and the project is still scanned and costed, but whether it belongs in scope is a question
for a human.

## Provenance

Read the JSON as a record of one scan, not as something regenerated on demand. It pins both halves
of what produced it:

```jsonc
"scan": {
  "toolVersion": "0.1.4",
  "commit": "63bc9ec4414d7e281dc9f9d7bdcf70030950457d"
}
```

`commit` is the commit of **eShopModernizing**, the repository that was scanned, not of
MigrationScan. The estate is archived and read-only, so that revision is final and this scan
cannot go out of date against it. `toolVersion` is the MigrationScan build that read it. Rerunning
0.1.4 against `63bc9ec` reproduces every finding here exactly.

## Files

| File | |
| --- | --- |
| `README.md` | This page |
| `console.txt` | What the tool prints, cross-platform stance |
| `report.md` | The Markdown report, cross-platform stance |
| `report.json` | Machine-readable, redacted, carrying **both** stances (336 KB) |
| `console-windows-stance.txt` | The console read from `net10.0-windows` |
| `report-windows-stance.md` | The Markdown report read from `net10.0-windows` |

The Markdown names the unreferenced project; the JSON gives it an opaque id instead. Both report
the same single warning. Redaction changes what a warning says and never how many there are.
