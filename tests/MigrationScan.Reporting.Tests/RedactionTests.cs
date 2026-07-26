using System.Text.Json;
using MigrationScan.Core.Analysis;
using MigrationScan.Core.Models;
using MigrationScan.Reporting;

namespace MigrationScan.Reporting.Tests;

/// <summary>
/// Redaction is a promise made to somebody's security team, so these are written as the claims
/// that promise makes — including the ones about what deliberately survives.
/// </summary>
public class RedactionTests
{
    private static AnalysisResult Sample()
    {
        RuleMetadata rule = new(
            Id: "MIG6001",
            Title: "BinaryFormatter",
            Category: "Serialization and security",
            Severity: Severity.Blocker,
            Effort: EffortBand.Large,
            Tier: ConfidenceTier.Probable,
            Remediation: "Replace with a safe serializer.",
            DocsUrl: "https://example.test/MIG6001");

        DiscoveredProject project = new("Billing.Web/Billing.Web.csproj", "Billing.Web", false, "v4.7.2", 2);

        return new AnalysisResult(
            "net10.0",
            [project],
            [
                new Finding(rule, "Uses BinaryFormatter.", "Billing.Web/Billing.Web.csproj",
                    "Billing.Web/Services/Cache.cs", 42),
                // Second finding in the same file: the id must collapse to the same value.
                new Finding(rule, "Uses BinaryFormatter again.", "Billing.Web/Billing.Web.csproj",
                    "Billing.Web/Services/Cache.cs", 88),
            ],
            [new ScanWarning("Skipped 'Billing.Legacy/Billing.Legacy.csproj': project file not found.",
                "Billing.Legacy/Billing.Legacy.csproj")])
        {
            References =
            [
                new ReferenceRecord(ReferenceKind.VendoredAssembly, "Meridian.Barcode", "4.7.1.0",
                    @"..\libs\Meridian.Barcode.dll", IsFrameworkAssembly: false,
                    "Billing.Web/Billing.Web.csproj", "Billing.Web/Billing.Web.csproj", 18),
                new ReferenceRecord(ReferenceKind.Com, "OrionLabelLib", "1.0",
                    "{9B1A0F21-4C33-4B7E-9E31-2B1A7C4D5E60}", IsFrameworkAssembly: false,
                    "Billing.Web/Billing.Web.csproj", "Billing.Web/Billing.Web.csproj", 24),
                new ReferenceRecord(ReferenceKind.WebService, "PricingService", null,
                    "https://internal-pricing.acme.corp/Pricing.asmx", IsFrameworkAssembly: false,
                    "Billing.Web/Billing.Web.csproj", "Billing.Web/Billing.Web.csproj", 30),
            ],
        };
    }

    private static JsonElement Root(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public void PathsAreRedactedWithoutBeingAskedTo()
    {
        // The whole design rests on this: somebody who reads no documentation still gets the safe
        // file. If the default ever flips, the funnel leaks silently rather than loudly.
        string json = JsonReportWriter.Write(Sample());

        Assert.DoesNotContain("Billing.Web/Services/Cache.cs", json, StringComparison.Ordinal);
        Assert.True(Root(json).GetProperty("redacted").GetBoolean());
    }

    [Fact]
    public void IncludePathsKeepsThem()
    {
        string json = JsonReportWriter.Write(Sample(), includePaths: true);

        Assert.Contains("Billing.Web/Services/Cache.cs", json, StringComparison.Ordinal);
        // Absent rather than false: a report either declares itself redacted or says nothing.
        Assert.False(Root(json).TryGetProperty("redacted", out _));
    }

    [Fact]
    public void FileBecomesFileIdRatherThanBeingRedefined()
    {
        JsonElement redacted = Root(JsonReportWriter.Write(Sample()));
        JsonElement plain = Root(JsonReportWriter.Write(Sample(), includePaths: true));

        JsonElement r = redacted.GetProperty("findings")[0];
        JsonElement p = plain.GetProperty("findings")[0];

        // Never both, in either direction. A consumer that resolves `file` against a repo must
        // never be handed a hash under that name — that would be a breaking change wearing an
        // additive one's clothes.
        Assert.False(r.TryGetProperty("file", out _));
        Assert.StartsWith("f:", r.GetProperty("fileId").GetString());

        Assert.False(p.TryGetProperty("fileId", out _));
        Assert.Equal("Billing.Web/Services/Cache.cs", p.GetProperty("file").GetString());
    }

    [Fact]
    public void TwoFindingsInOneFileShareOneId()
    {
        // Deliberate signal: "seven findings in the same file" is real information for effort
        // estimation, and costs no disclosure.
        JsonElement findings = Root(JsonReportWriter.Write(Sample())).GetProperty("findings");

        Assert.Equal(
            findings[0].GetProperty("fileId").GetString(),
            findings[1].GetProperty("fileId").GetString());
    }

    [Fact]
    public void DifferentFilesGetDifferentIds()
    {
        // The failure that would matter: two files collapsing to one identity silently
        // under-reports through a baseline.
        Assert.NotEqual(Fingerprints.FileId("a/b.cs"), Fingerprints.FileId("a/c.cs"));
    }

    [Fact]
    public void ProjectNamesSurvive()
    {
        // Losing these makes every scope line in a downstream proposal meaningless.
        string json = JsonReportWriter.Write(Sample());

        Assert.Contains("Billing.Web/Billing.Web.csproj", json, StringComparison.Ordinal);
    }

    [Fact]
    public void DependencyIdentitiesSurviveButLocationsDoNot()
    {
        JsonElement references = Root(JsonReportWriter.Write(Sample())).GetProperty("references");

        JsonElement vendored = references[0];
        // You cannot price a control from a vendor that folded without knowing which one it is.
        Assert.Equal("Meridian.Barcode", vendored.GetProperty("name").GetString());
        Assert.Equal("4.7.1.0", vendored.GetProperty("version").GetString());
        // ...but where it sits on disk is not needed to research it.
        Assert.StartsWith("f:", vendored.GetProperty("source").GetString());
    }

    [Fact]
    public void AComGuidIsKeptBecauseItIsIdentityNotLocation()
    {
        JsonElement references = Root(JsonReportWriter.Write(Sample())).GetProperty("references");

        // The same value on every machine that ever registered the component, and the only
        // reliable way to identify one. It discloses nothing about this customer.
        Assert.Equal("{9B1A0F21-4C33-4B7E-9E31-2B1A7C4D5E60}", references[1].GetProperty("source").GetString());
    }

    [Fact]
    public void AServiceEndpointKeepsItsSchemeAndLosesItsHost()
    {
        string json = JsonReportWriter.Write(Sample());
        JsonElement service = Root(json).GetProperty("references")[2];

        Assert.Equal("https://<redacted>", service.GetProperty("source").GetString());
        // Internal DNS names are exactly the sort of thing a security review objects to.
        Assert.DoesNotContain("internal-pricing.acme.corp", json, StringComparison.Ordinal);
        // The service is still named, because it is a component that has to be scoped.
        Assert.Equal("PricingService", service.GetProperty("name").GetString());
    }

    [Fact]
    public void WarningsAreScrubbedInTheProseNotJustTheField()
    {
        // Warning text is the easiest disclosure to miss: it reads like tooling chatter and
        // embeds a path mid-sentence.
        string json = JsonReportWriter.Write(Sample());

        Assert.DoesNotContain("Billing.Legacy/Billing.Legacy.csproj", json, StringComparison.Ordinal);
        JsonElement warning = Root(json).GetProperty("warnings")[0];
        Assert.Contains("f:", warning.GetProperty("message").GetString()!, StringComparison.Ordinal);
        Assert.StartsWith("f:", warning.GetProperty("path").GetString());
    }

    [Fact]
    public void AWarningNamingSeveralProjectsSurvivesRedactionWithItsMeaningIntact()
    {
        // The orphan-projects warning lists several paths in one sentence and carries no single
        // path to swap out. It declares them instead, so each becomes an id and the sentence
        // still says which projects and how many.
        AnalysisResult result = Sample() with
        {
            Warnings =
            [
                new ScanWarning(
                    "2 project(s) are not referenced by any solution: Orphan/Orphan.csproj, Loose/Loose.csproj",
                    Path: null)
                {
                    MentionedPaths = ["Orphan/Orphan.csproj", "Loose/Loose.csproj"],
                },
            ],
        };

        string json = JsonReportWriter.Write(result);

        Assert.DoesNotContain("Orphan/Orphan.csproj", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Loose/Loose.csproj", json, StringComparison.Ordinal);

        JsonElement warning = Assert.Single(Root(json).GetProperty("warnings").EnumerateArray().ToList());
        string message = warning.GetProperty("message").GetString()!;
        Assert.Contains("2 project(s) are not referenced", message, StringComparison.Ordinal);
        // Two distinct ids, so the reader can still tell two projects apart.
        Assert.Equal(2, message.Split("f:").Length - 1);
    }

    [Fact]
    public void AWarningThatCannotBeCleanedIsReplacedRatherThanRemoved()
    {
        // Free text the analyzer passed through, a parser's own error naming a path it found.
        // Nothing declares that path, so the sentence cannot be scrubbed and must not ship.
        // Removing the warning outright is the failure this design exists to prevent: a reader
        // is told to check warnings before trusting coverage, and a silently shorter list reads
        // as a clean scan.
        AnalysisResult result = Sample() with
        {
            Warnings =
            [
                new ScanWarning(@"Skipped: malformed project XML in C:\build\Acme.Payroll\Acme.Payroll.csproj.",
                    Path: null),
            ],
        };

        string json = JsonReportWriter.Write(result);

        Assert.DoesNotContain("Acme.Payroll", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"C:\build", json, StringComparison.OrdinalIgnoreCase);

        JsonElement warning = Assert.Single(Root(json).GetProperty("warnings").EnumerateArray().ToList());
        Assert.Contains("withheld", warning.GetProperty("message").GetString()!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ARedactedReportCarriesAsManyWarningsAsAnUnredactedOne()
    {
        // The invariant behind both tests above, stated once: redaction changes what a warning
        // says, never how many there are. Counting warnings is how somebody decides whether a
        // scan covered the estate.
        AnalysisResult result = Sample() with
        {
            Warnings =
            [
                new ScanWarning("Skipped 'A/A.csproj': project file not found.", "A/A.csproj")
                {
                    MentionedPaths = ["A/A.csproj"],
                },
                new ScanWarning("3 project(s) are not referenced by any solution: B/B.csproj", Path: null)
                {
                    MentionedPaths = ["B/B.csproj"],
                },
                new ScanWarning(@"Skipped: malformed project XML in D:\src\C\C.csproj.", Path: null),
            ],
        };

        int redacted = Root(JsonReportWriter.Write(result)).GetProperty("warnings").GetArrayLength();
        int withPaths = Root(JsonReportWriter.Write(result, includePaths: true))
            .GetProperty("warnings").GetArrayLength();

        Assert.Equal(3, withPaths);
        Assert.Equal(withPaths, redacted);
    }

    [Fact]
    public void NoSourceFilePathSurvivesAnywhereInTheDocument()
    {
        // The blanket claim, asserted against the whole serialized document rather than field by
        // field — a new field carrying a path would slip past a per-field test.
        string json = JsonReportWriter.Write(Sample());

        foreach (string path in new[]
                 {
                     "Billing.Web/Services/Cache.cs",
                     @"..\libs\Meridian.Barcode.dll",
                     "Billing.Legacy/Billing.Legacy.csproj",
                     "internal-pricing.acme.corp",
                 })
        {
            Assert.DoesNotContain(path, json, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void RedactionIsDeterministic()
    {
        // No salt, no clock. Two runs, and two machines, must agree — baselines are committed.
        Assert.Equal(JsonReportWriter.Write(Sample()), JsonReportWriter.Write(Sample()));
    }
}
