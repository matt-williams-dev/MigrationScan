# Sample scan output

Verbatim output from real MigrationScan runs, committed so a page, a deck or a design review can
quote the tool without anyone re-running it and getting different numbers. Nothing here is
hand-edited. If a file looks wrong, regenerate it rather than correcting it in place.

## Runs

| Estate | What it shows |
| --- | --- |
| [`woodgrove-banking`](woodgrove-banking/) | A fictional four-project bank, built as a fixture to carry every shape a report can produce |
| [`eshop-modernizing`](eshop-modernizing/) | Microsoft's archived eShopModernizing sample, eleven projects, scanned as found |

One is constructed and one is not, on purpose. A fixture can be made to demonstrate everything at
once and proves nothing about scale. A repository somebody else wrote proves the opposite and
demonstrates whatever it happens to contain.

## What may live here

This repository is public, and so is everything in this directory. A scan output belongs here only
when it names no real client and no real estate. Two ways to satisfy that: scan a fixture built
from fictional companies, or scan a public repository whose licence permits it, having read the
output first.

Redaction removes source file paths and nothing else. Project paths, package names, assembly names
and vendor names all survive by design, because a report nobody can read is a report nobody can
act on. The console and Markdown outputs keep full paths regardless of redaction. Read the files
before you add them.

## Layout

One directory per estate, each holding a narrative and the run it describes:

| File | |
| --- | --- |
| `README.md` | What the estate is and what the numbers mean |
| `console.txt` | What the tool prints, cross-platform stance |
| `report.md` | The Markdown report, cross-platform stance |
| `report.json` | Machine-readable, redacted, carrying **both** stances |
| `console-windows-stance.txt` | The console read from `net10.0-windows` |
| `report-windows-stance.md` | The Markdown report read from `net10.0-windows` |

Console and Markdown each show one stance, whichever `--target` names. The JSON holds both
regardless, so anything comparing the two portability costs should read `targets[]` from the JSON
rather than diffing two Markdown files.

## The one legacy path

[`docs/sample-report.md`](../sample-report.md) is the Woodgrove Markdown report published a second
time, under the path it has always had. The NuGet package pages for 0.1.0 and 0.1.1 link to it,
and nuget.org holds published versions immutable, so that link can never be corrected. Moving the
file would break it permanently.

It is an alias, not a second home: the content is generated from the same run, and the
regeneration below writes both. Everything new goes under `docs/samples/<estate>/`.

## Regenerating

From the repository root, with `<name>` the directory under `docs/samples`:

```console
dotnet build src/MigrationScan.Tool -c Release

dotnet run --project src/MigrationScan.Tool -c Release --no-build -- <target> \
  --format console > docs/samples/<name>/console.txt
dotnet run --project src/MigrationScan.Tool -c Release --no-build -- <target> \
  --target net10.0-windows --format console > docs/samples/<name>/console-windows-stance.txt

# Both Markdown runs write report.md, so take the Windows one first and rename it before the
# cross-platform run overwrites it.
dotnet run --project src/MigrationScan.Tool -c Release --no-build -- <target> \
  --target net10.0-windows --format markdown --output docs/samples/<name>
mv docs/samples/<name>/report.md docs/samples/<name>/report-windows-stance.md
dotnet run --project src/MigrationScan.Tool -c Release --no-build -- <target> \
  --format markdown --format json --output docs/samples/<name>
```

For Woodgrove, refresh the legacy alias as well. It carries a short preamble above the generated
report, so append rather than overwrite:

```console
head -n 12 docs/sample-report.md > /tmp/preamble.md
cat /tmp/preamble.md docs/samples/woodgrove-banking/report.md > docs/sample-report.md
```

Findings are deterministic: same input, same tool version, same output. One field is not.
`scan.commit` records the commit of the working copy that was scanned, so regenerating a sample
whose fixture lives in this repository yields a new hash every time while every finding stays
identical. A diff confined to that line is noise. A diff anywhere else means the tool changed its
mind about an estate that did not change, which is worth reading before committing.
`scan.toolVersion` records which build wrote the file.
