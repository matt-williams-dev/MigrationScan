using MigrationScan.Core.Analysis;
using MigrationScan.Core.Models;

namespace MigrationScan.Reporting.Tests;

public class BaselineReaderTests
{
    [Fact]
    public void RoundTripsFingerprintsFromAJsonReport()
    {
        AnalysisResult result = ReportSample.Build();
        string path = Path.Combine(Path.GetTempPath(), $"baseline-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, JsonReportWriter.Write(result));

        try
        {
            IReadOnlySet<string> fingerprints = BaselineReader.LoadFingerprints(path);

            // Every finding in the report is represented (deduplicated by fingerprint).
            HashSet<string> expected = result.Findings.Select(Fingerprints.Of).ToHashSet();
            Assert.Equal(expected, fingerprints.ToHashSet());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void SuppressingWithItsOwnBaselineRemovesEveryFinding()
    {
        AnalysisResult result = ReportSample.Build();
        string path = Path.Combine(Path.GetTempPath(), $"baseline-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, JsonReportWriter.Write(result));

        try
        {
            IReadOnlySet<string> baseline = BaselineReader.LoadFingerprints(path);
            List<Finding> remaining = result.Findings.Where(f => !baseline.Contains(Fingerprints.Of(f))).ToList();

            Assert.Empty(remaining);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
