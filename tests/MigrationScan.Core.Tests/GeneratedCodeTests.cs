using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

/// <summary>
/// Auto-generated source (WCF/EF proxies, designer files) is skipped — migration regenerates
/// it rather than hand-editing it, and it otherwise floods the results. Surfaced by scanning a
/// real solution whose svcutil Reference.cs produced thousands of findings.
/// </summary>
public class GeneratedCodeTests
{
    [Fact]
    public void GeneratedFilesAreSkippedButHandWrittenFilesAreNot()
    {
        AnalysisResult result = AnalysisHelper.AnalyzeFixture("GeneratedCode", "GeneratedCode.sln");

        // Both Reference.cs (auto-generated header) and Handwritten.cs use the Registry; only the
        // hand-written one is flagged.
        Finding registry = Assert.Single(result.Findings, f => f.Rule.Id == "MIG4002");
        Assert.EndsWith("Handwritten.cs", registry.FilePath);
    }
}
