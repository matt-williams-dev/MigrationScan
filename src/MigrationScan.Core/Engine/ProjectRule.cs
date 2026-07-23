using MigrationScan.Core.Models;

namespace MigrationScan.Core.Engine;

/// <summary>
/// Base class for rules that inspect the project file, its references, packages, or
/// files on disk (Tier 1, certain). Holds the rule's metadata so subclasses only write
/// detection logic and call <see cref="Report"/>.
/// </summary>
public abstract class ProjectRule : IMigrationRule
{
    protected ProjectRule(RuleMetadata metadata) => Metadata = metadata;

    protected RuleMetadata Metadata { get; }

    public string RuleId => Metadata.Id;

    public abstract IEnumerable<Finding> Analyze(AnalysisContext context);

    /// <summary>Creates a finding attributed to this rule.</summary>
    protected Finding Report(AnalysisContext context, string message, string? file = null, int? line = null) =>
        new(Metadata, message, context.ProjectRelativePath, file ?? context.ProjectRelativePath, line);
}
