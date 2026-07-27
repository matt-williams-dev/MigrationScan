# Woodgrove Bank

A four-project retail banking estate, entirely fictional, built to put every shape a report can
carry into one page. The source is a test fixture in this repository, at
[`tests/fixtures/WoodgroveBanking`](../../../tests/fixtures/WoodgroveBanking), so the scan is
reproducible by anyone who clones the repo.

Every company here is invented: Woodgrove Bank, Litware, Fabrikam, Proseware. That is deliberate.
This output is published, and a report demonstrating a tool should not carry somebody else's
vendor list.

## What the scan reports

| | Cross-platform (`net10.0`) | Windows (`net10.0-windows`) |
| --- | --- | --- |
| Projects scanned | 3 | 3 |
| Findings | 23 (blocker 3 · high 7 · medium 11 · low 2) | 17 (blocker 3 · high 4 · medium 8 · low 2) |
| Estimated effort | 29.3–88 engineer-days | 23.8–72 engineer-days |
| Needs an architectural decision | 1 | 1 |
| Windows lock-in satisfied by target | n/a | 6 |
| Third-party references | 6 distinct | 6 distinct |
| Projects not assessed | 1 | 1 |

Produced by tool version **0.1.4**, report schema **1.6**.

## The five shapes

**An architectural blocker.** `Woodgrove.Web` is WebForms, which has no counterpart on modern
.NET. MIG3001 carries an effort band of its own, because "rewrite this in Razor Pages" is a
decision somebody has to make before anyone can estimate it.

**Work that needs a decision before it can be sized.** `Woodgrove.Domain` writes its statement
archive with BinaryFormatter, removed in .NET 9. Changing the serializer changes the on-disk
format, so every archived file already written is part of the problem.

**Third-party references with no upstream to look up.** A COM type library and a vendor SDK
checked into the repository. Neither has a NuGet page to read, a version to bump, or a changelog
to check. Somebody has to find out whether the vendor still exists.

**A portability gap.** Registry, WMI and P/Invoke in `Woodgrove.Interop` cost nothing while the
target stays on Windows and cost six findings the moment it does not. That difference is the
entire argument for reporting both stances from one scan.

**A project the tool cannot assess.** `Woodgrove.Database` is a `.sqlproj`. MigrationScan does not
read that format, says so, and leaves it out of the estimate rather than quietly implying the
estate is smaller than it is.

## Files

| File | |
| --- | --- |
| `console.txt` | What the tool prints, cross-platform stance |
| `report.md` | The Markdown report, cross-platform stance |
| `report.json` | Machine-readable, redacted, carrying **both** stances |
| `console-windows-stance.txt` | The console read from `net10.0-windows` |
| `report-windows-stance.md` | The Markdown report read from `net10.0-windows` |

`report.md` is also published at [`docs/sample-report.md`](../../sample-report.md), a path older
NuGet package pages link to permanently. See the [index](../README.md) for why that alias exists
and how to regenerate both.
