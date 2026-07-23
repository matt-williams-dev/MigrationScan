# MigrationScan — working constraints

This file keeps the core constraints in context across sessions. Full detail lives in
`migrationscan-spec.md`. If anything here conflicts with the spec, the spec wins — update
this file to match.

## Design principles (spec §3)

- **Deterministic.** Same input, same output, every run. No network calls in the default path.
- **Offline by default.** Network access only behind an explicit `--online` flag, and only for
  NuGet package compatibility lookups.
- **No MSBuild dependency.** Legacy projects use non-SDK-style `.csproj` files. Parse `.sln` and
  `.csproj` as XML instead of registering MSBuild / requiring Visual Studio.
- **Tiered confidence.** Full semantic analysis is impossible without resolved references. Be
  honest about that in the output rather than pretending to certainty (see tiers below).
- **Rules as data.** Keep the API catalog and package compatibility lists in JSON files, not
  hardcoded C#. Contributors add rules without touching the engine.
- **CI-friendly.** Machine-readable output, meaningful exit codes, no interactive prompts.

## Analysis tiers (spec §5)

Every finding carries a confidence tier derived from how it was detected:

- **Tier 1 — Certain.** From project files, `packages.config`, `app.config`, `web.config`,
  `.sln`. XML parsing, no ambiguity.
- **Tier 2 — Probable.** From Roslyn syntax trees without a resolved compilation. Good recall,
  some false positives (e.g. `Registry` could be someone's own class).
- **Tier 3 — Verified.** From the semantic model when references resolve, or from compiled
  assemblies in `bin/` via Cecil. Phase 6 work.

Report the tier on every finding. A Tier 2 finding labelled "probable" that turns out wrong
costs nothing. A Tier 2 finding presented as certain costs the user's trust.

## Notes for Claude Code (spec §14)

- Follow the phases in order. Finish and test a phase before starting the next.
- Write tests alongside each rule, not after.
- Do not add NuGet dependencies beyond spec §4 without asking. Approved set: Roslyn
  (`Microsoft.CodeAnalysis.CSharp`), `System.Xml.Linq`, `Mono.Cecil` (phase 6),
  `System.CommandLine`, xUnit + `Verify`.
- Keep rule metadata in JSON data files. The C# should contain detection logic only.
- Every finding needs a confidence tier. Never present a syntax-only match as certain.
- Prefer boring, readable code. Somebody is going to read this repo to decide whether to hire
  the author.
- When a rule's detection is ambiguous, say so in the rule's docs page rather than silently
  guessing.

## Build / test

- Target `net10.0`, C# 14, nullable enabled, warnings as errors (see `Directory.Build.props`).
- `dotnet build` and `dotnet test` from the repo root.
- No network access in tests, ever — including `--online` tests. Mock the NuGet client.
