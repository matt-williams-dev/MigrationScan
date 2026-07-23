using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG1001 — Non-SDK-style project file.
///
/// SDK-style projects declare an <c>Sdk</c> attribute on the root <c>&lt;Project&gt;</c>
/// element; legacy projects do not. Tier 1 (certain), read straight from the project XML.
/// </summary>
public sealed class Mig1001NonSdkProject : ProjectRule
{
    public const string Id = "MIG1001";

    public Mig1001NonSdkProject(RuleMetadata metadata) : base(metadata)
    {
    }

    public override IEnumerable<Finding> Analyze(AnalysisContext context)
    {
        if (context.Project.IsSdkStyle)
        {
            yield break;
        }

        yield return Report(
            context,
            $"Project '{context.Project.Name}' uses the legacy non-SDK project format.",
            line: context.Project.RootElementLine);
    }
}
