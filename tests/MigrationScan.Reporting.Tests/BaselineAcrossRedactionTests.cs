using MigrationScan.Core.Analysis;
using MigrationScan.Core.Models;
using MigrationScan.Reporting;

namespace MigrationScan.Reporting.Tests;

/// <summary>
/// Baselines have to survive redaction, and the failure mode is silent in both directions:
/// fingerprints that stop matching suppress nothing (noisy, but visible), while fingerprints that
/// over-match suppress real findings (invisible, and the one that costs money). A redacted report
/// is a legitimate baseline — it is the form a client would send.
/// </summary>
public class BaselineAcrossRedactionTests
{
    private static RuleMetadata Rule(string id) => new(
        Id: id, Title: "t", Category: "c", Severity: Severity.High, Effort: EffortBand.Small,
        Tier: ConfidenceTier.Probable, Remediation: "r", DocsUrl: "https://example.test");

    private static AnalysisResult Sample() => new(
        "net10.0",
        [new DiscoveredProject("App/App.csproj", "App", false, "v4.7.2", 2)],
        [
            new Finding(Rule("MIG4002"), "Accesses the Registry.", "App/App.csproj", "App/Reg.cs", 10),
            new Finding(Rule("MIG6001"), "Uses BinaryFormatter.", "App/App.csproj", "App/Ser.cs", 20),
        ],
        []);

    private static string WriteToTemp(string json)
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void ARedactedReportIsAUsableBaseline()
    {
        string baseline = WriteToTemp(JsonReportWriter.Write(Sample()));
        try
        {
            IReadOnlySet<string> fingerprints = BaselineReader.LoadFingerprints(baseline);

            // Every finding in the fresh (unredacted, in-memory) scan must be recognised. Paths in
            // the baseline are one-way hashes, so this only works because the fingerprint is
            // recorded outright rather than reconstructed from the fields.
            Assert.All(Sample().Findings, f => Assert.Contains(Fingerprints.Of(f), fingerprints));
        }
        finally
        {
            File.Delete(baseline);
        }
    }

    [Fact]
    public void ARedactedAndAnUnredactedBaselineAgree()
    {
        string redacted = WriteToTemp(JsonReportWriter.Write(Sample()));
        string plain = WriteToTemp(JsonReportWriter.Write(Sample(), includePaths: true));
        try
        {
            // Whether the client redacted or not must not change which findings are suppressed.
            Assert.Equal(
                BaselineReader.LoadFingerprints(redacted).OrderBy(x => x, StringComparer.Ordinal),
                BaselineReader.LoadFingerprints(plain).OrderBy(x => x, StringComparer.Ordinal));
        }
        finally
        {
            File.Delete(redacted);
            File.Delete(plain);
        }
    }

    [Fact]
    public void FindingsInDifferentFilesDoNotCollapseToOneIdentity()
    {
        // The expensive failure: two files sharing a fingerprint means baselining one silently
        // suppresses the other, and the report under-reports without saying so.
        string baseline = WriteToTemp(JsonReportWriter.Write(Sample()));
        try
        {
            Assert.Equal(2, BaselineReader.LoadFingerprints(baseline).Count);
        }
        finally
        {
            File.Delete(baseline);
        }
    }

    [Fact]
    public void APre16ReportWithoutAFingerprintFieldStillWorks()
    {
        // Baselines are committed to repositories and outlive tool versions. One captured before
        // fingerprints were recorded has real paths, so reconstructing from the fields is exact.
        string legacy = WriteToTemp("""
            {
              "schemaVersion": "1.4",
              "target": "net10.0",
              "findings": [
                {
                  "ruleId": "MIG4002",
                  "message": "Accesses the Registry.",
                  "project": "App/App.csproj",
                  "file": "App/Reg.cs"
                }
              ]
            }
            """);
        try
        {
            IReadOnlySet<string> fingerprints = BaselineReader.LoadFingerprints(legacy);

            Assert.Contains(Fingerprints.Of("MIG4002", "App/Reg.cs", "Accesses the Registry."), fingerprints);
            Assert.Single(fingerprints);
        }
        finally
        {
            File.Delete(legacy);
        }
    }

    [Fact]
    public void AFingerprintDoesNotDiscloseThePathItWasBuiltFrom()
    {
        // The fingerprint is written into the shareable report, so a plain join of the fields
        // would re-leak the very path the rest of the document redacts.
        string fingerprint = Fingerprints.Of("MIG4002", "App/Secrets/Reg.cs", "Accesses the Registry.");

        Assert.DoesNotContain("App/Secrets/Reg.cs", fingerprint, StringComparison.Ordinal);
        Assert.DoesNotContain("Registry", fingerprint, StringComparison.Ordinal);
    }
}
