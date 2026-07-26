# Sample scan output

Verbatim output from real MigrationScan runs, committed so that a page, a deck or a design
review can quote the tool without anyone re-running it and getting subtly different numbers.
Nothing here is hand-edited. If a file looks wrong, the fix is to regenerate it, not to correct
it in place.

## What may live here

This repository is public, and so is everything in this directory. A scan output may be
committed here only when it names no real client, no real estate and no real vendor. Two ways
to satisfy that: scan a fixture built from fictional companies, or scan something whose owner
has cleared publication and read the file first. The JSON is redacted by default, meaning it
carries opaque `fileId` values rather than paths, but redaction hides paths and nothing else.
Project names, package names and assembly names survive on purpose, because a report nobody can
read is a report nobody can act on. Read the file before you add it.

## Layout

One directory per scanned estate, with the same five files in each, so a second run drops in
beside the first without anything downstream having to learn a new shape:

| File | |
| --- | --- |
| `console.txt` | What the tool prints, cross-platform stance |
| `report.md` | The Markdown report an engineering manager forwards |
| `report.json` | The machine-readable report, redacted, carrying **both** stances |
| `console-windows-stance.txt` | The console read from `net10.0-windows` |
| `report-windows-stance.md` | The Markdown report read from `net10.0-windows` |

Console and Markdown each show one stance at a time, whichever `--target` names. The JSON holds
both regardless, so anything comparing the two portability costs should read `targets[]` from
the JSON rather than diffing the two Markdown files.

## Runs

### `woodgrove-banking`

Woodgrove Bank, a four-project retail banking estate, entirely fictional. The fixture lives at
[`tests/fixtures/WoodgroveBanking`](../../tests/fixtures/WoodgroveBanking) and exists to put
five things in one report: an architectural blocker, work that needs a decision before anyone
can estimate it, third-party references with no upstream to look up, a portability gap between
the two stances, and a project the tool cannot assess at all.

Produced by tool version **0.1.3**, report schema **1.6**.

| | Cross-platform (`net10.0`) | Windows (`net10.0-windows`) |
| --- | --- | --- |
| Findings | 23 (blocker 3 · high 7 · medium 11 · low 2) | 17 (blocker 3 · high 4 · medium 8 · low 2) |
| Estimated effort | 29.3–88 engineer-days | 23.8–72 engineer-days |
| Needs an architectural decision | 1 | 1 |
| Windows lock-in satisfied by target | n/a | 6 |

The six findings separating the two columns are the Registry, WMI, COM and P/Invoke usage in
`Woodgrove.Interop`. That difference is what portability costs on this estate, and one scan
produces both sides of it.

## Regenerating

From the repository root, with `<name>` the directory under `docs/samples`:

```console
dotnet build src/MigrationScan.Tool -c Release

dotnet run --project src/MigrationScan.Tool -c Release --no-build -- <solution> \
  --format console > docs/samples/<name>/console.txt
dotnet run --project src/MigrationScan.Tool -c Release --no-build -- <solution> \
  --format markdown --format json --output docs/samples/<name>

dotnet run --project src/MigrationScan.Tool -c Release --no-build -- <solution> \
  --target net10.0-windows --format console > docs/samples/<name>/console-windows-stance.txt
dotnet run --project src/MigrationScan.Tool -c Release --no-build -- <solution> \
  --target net10.0-windows --format markdown --output docs/samples/<name>
```

Run the Windows pair second and rename as you go. Both Markdown runs write `report.md`, so the
second overwrites the first unless you move it to `report-windows-stance.md` in between.

Findings are deterministic: same input, same tool version, same output. One field is not.
`scan.commit` records the commit of the working copy that was scanned, so regenerating a sample
whose fixture lives in this repository yields a new commit hash every time, while every finding
stays identical. A diff confined to that line is noise. A diff anywhere else means the tool
changed its mind about an estate that did not change, which is worth reading before committing.
`scan.toolVersion` records which build wrote the file.
