using MigrationScan.Core.Models;

namespace MigrationScan.Core.Engine;

/// <summary>
/// Runs a fixed set of rules against one project's <see cref="AnalysisContext"/>.
/// Rules run in ID order so output is deterministic before the final solution-level sort.
/// </summary>
public sealed class RuleEngine
{
    private readonly IReadOnlyList<IMigrationRule> _rules;

    public RuleEngine(IEnumerable<IMigrationRule> rules) =>
        _rules = rules.OrderBy(r => r.RuleId, StringComparer.Ordinal).ToList();

    public IReadOnlyList<IMigrationRule> Rules => _rules;

    public IReadOnlyList<Finding> Analyze(AnalysisContext context)
    {
        List<Finding> findings = [];
        foreach (IMigrationRule rule in _rules)
        {
            findings.AddRange(rule.Analyze(context));
        }

        return findings;
    }
}
