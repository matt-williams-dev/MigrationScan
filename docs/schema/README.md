# JSON output schema

`migrationscan <path> --format json` emits a stable, versioned document. It is the
intended integration point: downstream tooling (dashboards, portfolio rollups, scoping
and estimating tools) should consume this rather than parsing the console or Markdown.

- **Deterministic.** Same input → byte-identical output. No timestamps or machine-specific
  data. Paths are repo-relative and forward-slashed.
- **Versioned.** `schemaVersion` follows semver-style rules: additive, backward-compatible
  changes bump the minor version; a breaking change would bump the major.

## Current version: `1.1`

`1.1` added the effort rollup (`summary.effort` and the `projects` array) over `1.0`. It is
additive — consumers written against `1.0` (which read `summary` counts, `findings`, and
`warnings`) keep working unchanged.

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
    }
  },
  "projects": [                        // per-project rollup, ordered by path
    {
      "path": "Shop.Web/Shop.Web.csproj",
      "findingCount": 5,
      "effort": { "minDays": 1.3, "maxDays": 5, "needsDecision": 1 }
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
