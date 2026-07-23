using MigrationScan.Core.Models;

namespace MigrationScan.Core.Engine;

/// <summary>
/// Base class for rules that match patterns in Roslyn syntax trees without a resolved
/// compilation (Tier 2, probable — spec §5). The base iterates the project's source
/// files; subclasses analyze one file at a time.
/// </summary>
public abstract class SyntaxRule : IMigrationRule
{
    protected SyntaxRule(RuleMetadata metadata) => Metadata = metadata;

    protected RuleMetadata Metadata { get; }

    public string RuleId => Metadata.Id;

    public IEnumerable<Finding> Analyze(AnalysisContext context)
    {
        foreach (SourceFile source in context.SourceFiles)
        {
            foreach (Finding finding in AnalyzeSource(source, context))
            {
                yield return finding;
            }
        }
    }

    /// <summary>Analyzes a single source file.</summary>
    protected abstract IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context);

    /// <summary>
    /// Creates a finding that belongs to the project but anchors to a location in the
    /// given source file.
    /// </summary>
    protected Finding Report(AnalysisContext context, SourceFile source, string message, int? line) =>
        new(Metadata, message, context.ProjectRelativePath, source.RelativePath, line);
}
