using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG3003 — ASMX web service. Tier 1 (certain): the presence of an <c>.asmx</c> file
/// identifies a legacy ASP.NET web service, which has no equivalent on modern .NET.
/// </summary>
public sealed class Mig3003Asmx : ProjectRule
{
    public const string Id = "MIG3003";

    public Mig3003Asmx(RuleMetadata metadata) : base(metadata)
    {
    }

    public override IEnumerable<Finding> Analyze(AnalysisContext context)
    {
        if (!context.HasFilesWithExtension(".asmx"))
        {
            yield break;
        }

        yield return Report(
            context,
            $"Project '{context.Project.Name}' exposes an ASMX web service (.asmx present). ASMX has no " +
            "counterpart on modern .NET; re-implement the service as an ASP.NET Core Web API or gRPC.",
            line: context.Project.RootElementLine);
    }
}
