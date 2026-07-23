using MigrationScan.Core.Models;

namespace MigrationScan.Core.Engine;

/// <summary>
/// A single migration rule. Detection logic only — descriptive metadata lives in the
/// rule catalog (spec §14). Rules come in two shapes (project-file and syntax), both
/// implementing this interface; see <see cref="ProjectRule"/> and <see cref="SyntaxRule"/>.
/// </summary>
public interface IMigrationRule
{
    /// <summary>Stable rule ID, e.g. <c>MIG1002</c>.</summary>
    string RuleId { get; }

    /// <summary>Returns the findings this rule produces for the given project.</summary>
    IEnumerable<Finding> Analyze(AnalysisContext context);
}
