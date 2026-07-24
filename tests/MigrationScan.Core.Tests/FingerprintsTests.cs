using MigrationScan.Core.Analysis;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

public class FingerprintsTests
{
    private static readonly RuleMetadata Rule = new("MIG4002", "t", "c", Severity.High,
        EffortBand.Small, ConfidenceTier.Probable, "r", "https://example.test");

    [Fact]
    public void SameRuleFileAndMessageMatchEvenWhenLineDiffers()
    {
        Finding a = new(Rule, "Accesses the Registry.", "P/P.csproj", "P/Reg.cs", 10);
        Finding b = new(Rule, "Accesses the Registry.", "P/P.csproj", "P/Reg.cs", 42);

        Assert.Equal(Fingerprints.Of(a), Fingerprints.Of(b));
    }

    [Fact]
    public void DifferentMessagesDoNotMatch()
    {
        Finding a = new(Rule, "message one", "P/P.csproj", "P/Reg.cs", 1);
        Finding b = new(Rule, "message two", "P/P.csproj", "P/Reg.cs", 1);

        Assert.NotEqual(Fingerprints.Of(a), Fingerprints.Of(b));
    }

    [Fact]
    public void FallsBackToProjectPathWhenFileIsNull()
    {
        Finding finding = new(Rule, "m", "P/P.csproj", null, null);

        Assert.Equal(Fingerprints.Of("MIG4002", "P/P.csproj", "m"), Fingerprints.Of(finding));
    }
}
