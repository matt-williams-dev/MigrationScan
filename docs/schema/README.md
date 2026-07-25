# JSON output schema

`migrationscan <path> --format json` emits a stable, versioned document. It is the
intended integration point: downstream tooling (dashboards, portfolio rollups, scoping
and estimating tools) should consume this rather than parsing the console or Markdown.

- **Deterministic.** Same input → byte-identical output. No timestamps or machine-specific
  data. Paths are repo-relative and forward-slashed.
- **Versioned.** `schemaVersion` follows semver-style rules: additive, backward-compatible
  changes bump the minor version; a breaking change would bump the major.

## Current version: `1.5`

Each version is additive and backward-compatible:

- `1.1` — added the effort rollup (`summary.effort` and the `projects` array).
- `1.2` — added `summary.projectsNotAssessed` and the `notAssessed` array (non-C#/VB projects
  the scan could not analyze, which still need migration planning).
- `1.3` — added portability awareness: `finding.platform`, `finding.satisfiedByTarget`, and
  `summary.windowsLockInSatisfied`.
- `1.4` — added the `references` inventory and `summary.thirdPartyReferences`.
- `1.5` — added the `targets` array: both portability stances in one document, so a single
  scan answers both "what does it cost to modernize?" and "what does portability add?".

Consumers written against an earlier version keep working unchanged.

## Shape

```jsonc
{
  "schemaVersion": "1.1",
  "target": "net10.0",                 // framework the scan assessed against
  "summary": {
    "projectsScanned": 2,
    "totalFindings": 7,
    "findingsBySeverity": { "blocker": 2, "high": 0, "medium": 2, "low": 3 },
    "effort": {                        // heuristic, solution-wide
      "minDays": 6.8,                  // engineer-days, low end
      "maxDays": 22,                   // engineer-days, high end
      "needsDecision": 1               // blocking issues excluded from the day range;
                                       // they need an architectural decision first
    },
    "projectsNotAssessed": 1,          // count of non-C#/VB projects not analyzed
    "windowsLockInSatisfied": 3,       // omitted on a cross-platform target
    "thirdPartyReferences": 12         // distinct components, not declaration sites
  },
  "projects": [                        // per-project rollup, ordered by path
    {
      "path": "Shop.Web/Shop.Web.csproj",
      "findingCount": 5,
      "effort": { "minDays": 1.3, "maxDays": 5, "needsDecision": 1 }
    }
  ],
  "targets": [                         // both portability stances, from one scan
    {
      "target": "net10.0",
      "stance": "crossPlatform",
      "default": true,                 // the stance the root target/summary/projects describe;
                                       // omitted on the other one
      "summary": { /* same shape as summary above */ },
      "projects": [ /* same shape as projects above */ ]
    },
    {
      "target": "net10.0-windows",
      "stance": "windows",
      "satisfiedPlatform": "windows",  // this stance satisfies findings whose platform matches;
                                       // omitted on the cross-platform stance
      "summary": { /* ... */ },
      "projects": [ /* ... */ ]
    }
  ],
  "findings": [
    {
      "ruleId": "MIG3001",
      "title": "ASP.NET WebForms",
      "category": "Blocking frameworks",
      "severity": "blocker",           // blocker | high | medium | low
      "tier": "certain",               // certain | probable | verified
      "effort": "blocker",             // trivial | small | medium | large | blocker
      "message": "Project 'Shop.Web' is an ASP.NET WebForms application (.aspx present).",
      "project": "Shop.Web/Shop.Web.csproj",
      "file": "Shop.Web/Shop.Web.csproj", // omitted when a finding has no file
      "line": 2,                          // omitted when not line-specific
      "remediation": "Re-architect to Razor Pages, MVC, or Blazor.",
      "docsUrl": "https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/MIG3001.md"
    }
  ],
  "notAssessed": [                     // non-C#/VB projects the scan could not analyze
    {
      "name": "Shop.Database",
      "path": "Shop.Database/Shop.Database.sqlproj",
      "projectType": "SQL Server database project",
      "reason": "Not a C#/VB project; its contents were not analyzed and must be scoped separately."
    }
  ],
  "references": [                      // dependency inventory — not findings
    {
      "kind": "package",               // package | assembly | vendoredAssembly | com | project | webService
      "name": "Newtonsoft.Json",
      "version": "13.0.3",             // omitted when the declaration carries no version
      "source": "libs/Contoso.dll",    // HintPath, project path, COM type-lib GUID, or service URL;
                                       // omitted when the declaration points nowhere (e.g. a GAC reference)
      "isFrameworkAssembly": false,    // true only for <Reference> to System.*, mscorlib, WPF, …
      "isThirdParty": true,            // external to both the framework and this solution
      "project": "Shop.Web/Shop.Web.csproj",
      "declaredIn": "Shop.Web/packages.config",
      "line": 4                        // omitted when not line-specific
    }
  ],
  "warnings": [                        // always present (may be empty)
    { "message": "Skipped 'X.csproj': project file not found.", "path": "X.csproj" }
  ]
}
```

## Notes for consumers

- **Effort is a planning aid, not a quote.** The day ranges come from per-rule bands and a
  flattening occurrence factor; they are deliberately heuristic. Apply your own rates,
  velocity, risk, and calibration downstream.
- **`needsDecision` vs. severity `blocker` are different axes.** `needsDecision` counts
  findings whose effort is unbounded until an architectural decision is made; a finding can
  be a severity `blocker` yet still estimable (bounded effort). Don't conflate the two.
- **Tier matters.** `probable` (Tier 2) findings are matched on syntax without a resolved
  compilation and may include false positives — discount or verify before acting on them.
- **Portfolio rollups:** scan each solution/repo separately and aggregate the `projects`
  arrays; the occurrence factor is scoped per project by design.
- **`targets` carries both stances; `findings` is not duplicated.** A stance satisfies exactly
  the findings whose `platform` equals its `satisfiedPlatform`, so the active set for a stance is
  `findings.filter(f => f.platform !== stance.satisfiedPlatform)`. Only `summary` and `projects`
  differ between stances — the findings, references, not-assessed projects and warnings are
  identical either way, because the target changes what a finding *costs*, never what was found.
  Use `targets` for the portability comparison rather than scanning twice.
- **The root is still the requested target.** `target`, `summary` and `projects` at the document
  root describe whatever `--target` asked for (cross-platform by default), unchanged from 1.4.
  The matching `targets` entry is flagged `default: true`. A pre-1.5 consumer can ignore
  `targets` entirely and behave exactly as before.
- **`references` is inventory, not findings.** Entries have no severity, no effort, and never
  affect `--fail-on`. The array is flat and per-project — one entry per declaration site — so a
  package used by six projects appears six times. Group on `(kind, name)` for a solution-wide
  view; `summary.thirdPartyReferences` is already that count. See
  [the references doc](../references.md) for what each kind covers and what isn't collected.
