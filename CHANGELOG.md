# Changelog

All notable changes to MigrationScan are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

The **report schema** carries its own version, separate from the tool. See
[`docs/schema`](docs/schema/README.md). A schema minor bump is additive and never forces a
consumer change.

## [Unreleased]

### Added

- **An index of the rule catalog at [`docs/rules/README.md`](docs/rules/README.md).** All 33 rules
  grouped by their eight categories, each row carrying the title, severity and confidence tier,
  linked to its page. Every finding in a report already points at one of those pages; until now
  there was no way in from the top, so the only route to a rule you had not already hit was
  guessing its id. The page also explains the three axes people conflate, severity, confidence
  tier and effort band, in one place rather than a paragraph per report.

- **`RuleDocsTests` keeps the docs honest against the catalog.** A rule cannot ship without a
  page, without a row in the index, or with a row whose title, severity or tier has drifted from
  the catalog, and a withdrawn rule cannot leave a dead page behind. The failure message names
  the exact row to add. It also pins every `docsUrl` to its canonical GitHub page, because those
  URLs are a permanent contract with every report already generated and both committed sample
  reports carry them.

## [0.1.5]

A readable summary on screen, and a Windows download that is one file. Same rules, same findings,
report schema stays at **1.6**. Markdown and JSON output are byte-identical to 0.1.4.

### Changed

- **The console summary prints as an aligned block.** The counts, the effort range, what was left
  unassessed and what is third-party used to arrive as prose spread over several lines, so the
  numbers a reader wants first were the numbers hardest to find. They now sit in a fixed-width
  block that fits 80 columns.

- **Both stances are priced on screen.** A scan has always costed staying on Windows and going
  cross-platform from the same findings, but the console showed one of them, so the second was
  reachable only by opening the JSON. A `Staying on Windows` row now carries the other total and
  says where the difference sits, because six findings in one project is an afternoon and six
  across six projects is a planning problem. It inverts to `Going cross-platform` when `--target`
  names the Windows side, and says so plainly when the two price the same.

- **One closing line instead of three.** The old ending printed the report path, a sentence saying
  the report prices both options, and the redaction note. The block prices both options now, so
  the middle line said nothing new and the redaction claim appeared twice.

### Added

- **The Windows executable ships bare as well as zipped.** `migrationscan-win-x64.exe` and
  `migrationscan-win-arm64.exe` are attached to the release alongside the existing `.zip`. The
  pitch is one file you double-click, and a zip made that download, unpack, then run. The zip
  stays for anyone whose mail or proxy rules refuse a bare `.exe`. Both hold the same signed
  binary and are covered by the same `.sha256`.

### Fixed

- **Release verification matched the wrong checksum file.** With two assets sharing one `.sha256`,
  the check read the first line rather than the line naming the file in hand, so a mismatch on the
  bare executable could pass.

## [0.1.4]

Fixes a redacted report quietly carrying fewer warnings than the scan produced. Same rules, same
findings, report schema stays at **1.6**.

### Fixed

- **A redacted JSON report dropped warnings the console and Markdown showed.** Redaction
  substituted only the single path a warning carried, then discarded any warning whose text still
  named a path. The warning listing projects no solution references names several in one sentence
  and carries none of them, so it vanished from the JSON while appearing everywhere else.

  Warnings are how you learn a scan's coverage was incomplete. A redacted report reaching a
  colleague without a "this project failed to load" warning invites them to scope against partial
  coverage with nothing on the page suggesting anything was held back, which is what redaction
  exists to prevent rather than cause.

  A warning now declares the paths it spells out and redaction replaces each with an id, so the
  warning survives saying how many projects and which ones. Anything still naming a path
  afterwards is replaced by a placeholder rather than removed, so a redacted report always carries
  as many warnings as an unredacted one.

## [0.1.3]

Signed downloads on Windows and macOS, and a build for Intel Macs. The scanner itself is
untouched: same rules, same findings, same report schema at **1.6**.

### Added

- **Signed downloads.** Matthew Williams, owner of MW Creative LLC, the parent company of MW
  Consulting, signs the Windows and macOS releases. That is the name Windows shows in the
  SmartScreen prompt and macOS shows when you open the installer. Windows goes through Azure
  Artifact Signing; macOS uses a Developer ID pair, with Apple notarizing the result. Every
  asset still ships a `.sha256`, signed or not.

- **A build for Intel Macs.** `osx-x64` joins the release alongside `osx-arm64`.

### Changed

- **macOS ships a `.pkg` instead of a `.tar.gz`.** Apple staples a notarization ticket to a
  package and not to a bare executable, and without a stapled ticket Gatekeeper asks Apple's
  servers about the binary the first time you run it. Plenty of the machines this tool is meant
  for have nowhere to send that question. The package installs `migrationscan` into
  `/usr/local/bin`.

## [0.1.2]

Corrects a factual error about a competing tool. No change to how the scanner behaves; report
schema stays at **1.6**.

### Fixed

- **The "Why this exists" section made a false claim.** It said the GitHub Copilot modernization
  agent, which replaced the deprecated .NET Upgrade Assistant, "needs a paid subscription".
  Microsoft's install documentation lists the prerequisite as "GitHub Copilot subscription (paid
  or free)" in all four supported environments, so a free tier works. The claim appears to have
  been true when that agent launched in late 2025 and is not true now.

  The section now states what is verifiable, links Microsoft's deprecation notice so you can
  check it, and rests on the point that actually matters to the teams this tool is for: code
  going to a model service is a policy problem no budget solves.

  A package readme is embedded at build time and nuget.org holds published versions immutable, so
  0.1.0 and 0.1.1 keep the wrong text. This release is the correction.

## [0.1.1]

No change to how the tool behaves. Report schema stays at **1.6**, and output is byte-identical
to 0.1.0 apart from the version it records.

### Changed

- Rewrote the package description, the readme and the text the tool prints into your console and
  Markdown report. A readme is embedded in the package at build time and nuget.org holds versions
  immutable, so 0.1.0's page keeps the old wording. This release is what corrects it.
- Updated the GitHub Actions the release workflow uses: `checkout` to v7, `setup-dotnet` to v6,
  `upload-artifact` to v7, `download-artifact` to v8. Clears the Node 20 deprecation warning on
  every run.

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

[Unreleased]: https://github.com/matt-williams-dev/MigrationScan/compare/v0.1.4...HEAD
[0.1.4]: https://github.com/matt-williams-dev/MigrationScan/releases/tag/v0.1.4
[0.1.3]: https://github.com/matt-williams-dev/MigrationScan/releases/tag/v0.1.3
[0.1.2]: https://github.com/matt-williams-dev/MigrationScan/releases/tag/v0.1.2
[0.1.1]: https://github.com/matt-williams-dev/MigrationScan/releases/tag/v0.1.1
[0.1.0]: https://github.com/matt-williams-dev/MigrationScan/releases/tag/v0.1.0
