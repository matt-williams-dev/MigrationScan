# Changelog

All notable changes to MigrationScan are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

The **report schema** is versioned separately from the tool — see
[`docs/schema`](docs/schema/README.md). A schema minor bump is always additive and never
requires a consumer change.

## [Unreleased]

## [0.1.0] — first public release

Report schema **1.5**.

### Added

- **One scan reports both portability stances.** The JSON report carries a `targets` array with
  a cross-platform (`net10.0`) and a Windows (`net10.0-windows`) view of the same analysis, each
  with its own summary and per-project effort rollup. Nobody has to scan twice to price
  portability, and nothing downstream has to reconcile two files that differ in one flag.
- **A default run needs no options.** `migrationscan <path>` prints a console summary and writes
  `migrationscan-report.json`. With no arguments at all it scans the current directory and waits
  for a keypress before closing, so the executable can be dropped into a repository root and
  double-clicked.
- **Self-contained executables** for win-x64, win-arm64, linux-x64 and osx-arm64, published on
  the releases page with SHA-256 checksums. No .NET SDK, no install, no admin rights.
- **Whole-estate directory scanning.** Pointing at a directory discovers every solution and
  every project beneath it. Projects no solution references are still assessed, and reported in
  the warnings so they can be confirmed as in or out of scope.
- **A statement of what the report contains**, in the console and as a section in the Markdown
  report, for the security review before a report is shared.
- **Scan provenance** — `scan.toolVersion`, and `scan.commit` when the scanned tree is a git
  working copy. No timestamp: byte-identical output for identical input is what makes reports
  diffable and baselineable.

### Fixed

- **Directory scans no longer hide non-C#/VB projects.** Scanning a directory rather than a
  `.sln` silently dropped every `.sqlproj`, `.wixproj`, `.rptproj` and similar, so they never
  reached the `notAssessed` list and MIG1007 never fired. Only solution-mode scans were
  complete. Reports produced by directory scans before this release understate coverage.
- Project discovery now skips `bin`, `obj`, `packages`, `node_modules` and dot-directories, so a
  vendored source tree is not assessed as if it were your own code.
- A project referenced by several solutions is scanned once rather than once per solution.

### Changed

- `--target` selects the .NET *version* to assess against; the platform axis is always reported
  both ways. `--fail-on` is still evaluated against the stance `--target` names, so exit codes
  are unaffected by the added stance.
- The README no longer documents `--rules`, `--exclude` or `--verbosity`, which were never
  implemented.

[Unreleased]: https://github.com/matt-williams-dev/MigrationScan/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/matt-williams-dev/MigrationScan/releases/tag/v0.1.0
