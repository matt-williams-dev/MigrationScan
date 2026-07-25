# Changelog

All notable changes to MigrationScan are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

The **report schema** carries its own version, separate from the tool. See
[`docs/schema`](docs/schema/README.md). A schema minor bump is additive and never forces a
consumer change.

## [Unreleased]

## [0.1.0] · first public release

Report schema **1.6**.

### Added

- **The JSON report holds no file paths.** Each becomes a stable opaque id, so you can clear a
  report without a security review reading the whole file. `file` is now omitted and `fileId`
  carries the id. We did not redefine `file`, because a field holding a path one day and a hash
  the next breaks every consumer that resolves it. Project names and dependency names survive on
  purpose: nobody can assess a component without knowing which one it is. `--include-paths` opts
  out.

  Console, Markdown and SARIF keep full paths. They stay on your machine, SARIF exists to point
  at a line in a file, and hiding paths from your own developers protects nobody.
- **`finding.fingerprint`**, so a redacted report still works as a `--baseline`. Each finding
  records its identity rather than deriving one from the path, which a one-way hash cannot give
  back.
- **A machine-readable schema**, [`docs/schema/migrationscan-1.6.schema.json`](docs/schema/migrationscan-1.6.schema.json),
  validated against real output in CI.

- **One scan reports both portability stances.** The `targets` array carries a cross-platform
  (`net10.0`) and a Windows (`net10.0-windows`) view of the same analysis, each with its own
  summary and per-project effort rollup. You never scan twice to price portability, and nothing
  downstream reconciles two files that differ by one flag.
- **A default run needs no options.** `migrationscan <path>` prints a summary and writes
  `migrationscan-report.json`. With no arguments it scans the current directory and waits for a
  keypress before closing, so you can drop the executable into a repository root and double-click
  it.
- **Self-contained executables** for win-x64, win-arm64, linux-x64 and osx-arm64 on the releases
  page, each with a SHA-256 checksum. No .NET SDK, no install, no admin rights.
- **Whole-estate directory scanning.** Point at a directory and MigrationScan finds every
  solution and every project beneath it. It assesses projects no solution references, and lists
  them in the warnings so you can rule them in or out.
- **A statement of what the report contains**, on the console and as a section in the Markdown
  report, for the security review you face before sending one on.
- **Scan provenance:** `scan.toolVersion`, plus `scan.commit` when you scan a git working copy.
  No timestamp, because byte-identical output for identical input is what makes reports diffable
  and baselineable.

### Fixed

- **Directory scans no longer hide non-C#/VB projects.** Scanning a directory rather than a
  `.sln` used to drop every `.sqlproj`, `.wixproj` and `.rptproj`, so none reached the
  `notAssessed` list and MIG1007 never fired. Only solution-mode scans were complete. If you ran
  a directory scan before this release, it understated your coverage. Rescan.
- Project discovery skips `bin`, `obj`, `packages`, `node_modules` and dot-directories, so a
  vendored source tree no longer gets assessed as your own code.
- A project referenced by several solutions gets scanned once rather than once per solution.

### Changed

- `--target` picks the .NET *version* to assess against. The platform axis always reports both
  ways. `--fail-on` still looks only at the stance `--target` names, so the added stance leaves
  exit codes alone.
- The README no longer documents `--rules`, `--exclude` or `--verbosity`, which were never
  implemented.

[Unreleased]: https://github.com/matt-williams-dev/MigrationScan/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/matt-williams-dev/MigrationScan/releases/tag/v0.1.0
