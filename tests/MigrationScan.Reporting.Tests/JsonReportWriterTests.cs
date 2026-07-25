using System.Text.Json;
using MigrationScan.Core.Models;
using MigrationScan.Reporting;

namespace MigrationScan.Reporting.Tests;

public class JsonReportWriterTests
{
    private static AnalysisResult SampleResult()
    {
        RuleMetadata rule = new(
            Id: "MIG1001",
            Title: "Non-SDK-style project file",
            Category: "Project and build",
            Severity: Severity.Medium,
            Effort: EffortBand.Small,
            Tier: ConfidenceTier.Certain,
            Remediation: "Convert to the SDK style.",
            DocsUrl: "https://example.test/MIG1001");

        Finding finding = new(
            Rule: rule,
            Message: "Project 'Legacy' uses the legacy non-SDK project format.",
            ProjectPath: "Legacy/Legacy.csproj",
            FilePath: "Legacy/Legacy.csproj",
            Line: 2);

        DiscoveredProject project = new(
            Path: "Legacy/Legacy.csproj",
            Name: "Legacy",
            IsSdkStyle: false,
            TargetFramework: "v4.7.2",
            RootElementLine: 2);

        return new AnalysisResult("net10.0", [project], [finding], [])
        {
            NotAssessed =
            [
                new NotAssessedProject("Shop.Database", "Shop.Database/Shop.Database.sqlproj",
                    "SQL Server database project", "Not a C#/VB project; must be scoped separately."),
            ],
            References =
            [
                new ReferenceRecord(ReferenceKind.Assembly, "System.Web", null, null,
                    IsFrameworkAssembly: true, "Legacy/Legacy.csproj", "Legacy/Legacy.csproj", 8),
                new ReferenceRecord(ReferenceKind.Package, "Newtonsoft.Json", "13.0.3", null,
                    IsFrameworkAssembly: false, "Legacy/Legacy.csproj", "Legacy/packages.config", 3),
            ],
        };
    }

    [Fact]
    public void ProducesValidJsonWithExpectedSchema()
    {
        string json = JsonReportWriter.Write(SampleResult());

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.Equal("1.5", root.GetProperty("schemaVersion").GetString());
        Assert.Equal("net10.0", root.GetProperty("target").GetString());

        JsonElement summary = root.GetProperty("summary");
        Assert.Equal(1, summary.GetProperty("projectsScanned").GetInt32());
        Assert.Equal(1, summary.GetProperty("totalFindings").GetInt32());
        Assert.Equal(1, summary.GetProperty("findingsBySeverity").GetProperty("medium").GetInt32());
        Assert.Equal(0, summary.GetProperty("findingsBySeverity").GetProperty("blocker").GetInt32());

        // Effort rollup (schema 1.1): one Small finding -> 0.5–2 days, nothing needing a decision.
        JsonElement effort = summary.GetProperty("effort");
        Assert.Equal(0.5, effort.GetProperty("minDays").GetDouble());
        Assert.Equal(2, effort.GetProperty("maxDays").GetDouble());
        Assert.Equal(0, effort.GetProperty("needsDecision").GetInt32());

        JsonElement project = Assert.Single(root.GetProperty("projects").EnumerateArray().ToList());
        Assert.Equal("Legacy/Legacy.csproj", project.GetProperty("path").GetString());
        Assert.Equal(1, project.GetProperty("findingCount").GetInt32());
        Assert.Equal(2, project.GetProperty("effort").GetProperty("maxDays").GetDouble());

        JsonElement finding = Assert.Single(root.GetProperty("findings").EnumerateArray().ToList());
        Assert.Equal("MIG1001", finding.GetProperty("ruleId").GetString());
        Assert.Equal("medium", finding.GetProperty("severity").GetString());
        Assert.Equal("certain", finding.GetProperty("tier").GetString());
        Assert.Equal("small", finding.GetProperty("effort").GetString());
        Assert.Equal("Legacy/Legacy.csproj", finding.GetProperty("project").GetString());
        Assert.Equal(2, finding.GetProperty("line").GetInt32());

        // Not-assessed projects (schema 1.2): structured entry + a summary count.
        Assert.Equal(1, summary.GetProperty("projectsNotAssessed").GetInt32());
        JsonElement notAssessed = Assert.Single(root.GetProperty("notAssessed").EnumerateArray().ToList());
        Assert.Equal("Shop.Database", notAssessed.GetProperty("name").GetString());
        Assert.Equal("SQL Server database project", notAssessed.GetProperty("projectType").GetString());
        Assert.EndsWith(".sqlproj", notAssessed.GetProperty("path").GetString());

        // Reference inventory (schema 1.4): flat and per-project, with the third-party subset
        // counted in the summary. The framework assembly is present in the array but not counted.
        Assert.Equal(1, summary.GetProperty("thirdPartyReferences").GetInt32());
        List<JsonElement> references = root.GetProperty("references").EnumerateArray().ToList();
        Assert.Equal(2, references.Count);

        JsonElement frameworkReference = references[0];
        Assert.Equal("assembly", frameworkReference.GetProperty("kind").GetString());
        Assert.Equal("System.Web", frameworkReference.GetProperty("name").GetString());
        Assert.True(frameworkReference.GetProperty("isFrameworkAssembly").GetBoolean());
        Assert.False(frameworkReference.GetProperty("isThirdParty").GetBoolean());

        JsonElement package = references[1];
        Assert.Equal("package", package.GetProperty("kind").GetString());
        Assert.Equal("13.0.3", package.GetProperty("version").GetString());
        Assert.Equal("Legacy/packages.config", package.GetProperty("declaredIn").GetString());
        Assert.True(package.GetProperty("isThirdParty").GetBoolean());

        // The warnings array is always present (empty here) for schema stability.
        Assert.Equal(JsonValueKind.Array, root.GetProperty("warnings").ValueKind);
        Assert.Empty(root.GetProperty("warnings").EnumerateArray());
    }

    [Fact]
    public void OutputIsDeterministic()
    {
        string first = JsonReportWriter.Write(SampleResult());
        string second = JsonReportWriter.Write(SampleResult());

        Assert.Equal(first, second);
    }

    /// <summary>
    /// The sample plus one Windows lock-in finding (a Registry read), so the two portability
    /// stances have genuinely different costs to assert against.
    /// </summary>
    private static AnalysisResult SampleWithWindowsLockIn()
    {
        RuleMetadata registry = new(
            Id: "MIG4002",
            Title: "Windows Registry access",
            Category: "Runtime failures",
            Severity: Severity.High,
            Effort: EffortBand.Medium,
            Tier: ConfidenceTier.Probable,
            Remediation: "Move the setting into configuration.",
            DocsUrl: "https://example.test/MIG4002") { Platform = RulePlatform.Windows };

        AnalysisResult sample = SampleResult();
        return sample with
        {
            Findings =
            [
                .. sample.Findings,
                new Finding(
                    Rule: registry,
                    Message: "Reads HKEY_LOCAL_MACHINE at startup.",
                    ProjectPath: "Legacy/Legacy.csproj",
                    FilePath: "Legacy/Startup.cs",
                    Line: 42),
            ],
        };
    }

    [Fact]
    public void CarriesBothPortabilityStancesInOneDocument()
    {
        // Written at the cross-platform target, which is what a customer's default run produces.
        string json = JsonReportWriter.Write(SampleWithWindowsLockIn().ForTarget("net10.0"));

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        List<JsonElement> targets = root.GetProperty("targets").EnumerateArray().ToList();
        Assert.Equal(2, targets.Count);

        JsonElement cross = targets[0];
        Assert.Equal("net10.0", cross.GetProperty("target").GetString());
        Assert.Equal("crossPlatform", cross.GetProperty("stance").GetString());
        // The document was written at this stance, so this is the one the root describes.
        Assert.True(cross.GetProperty("default").GetBoolean());
        // Nothing is satisfied cross-platform, so the field is absent rather than null or "".
        Assert.False(cross.TryGetProperty("satisfiedPlatform", out _));

        JsonElement windows = targets[1];
        Assert.Equal("net10.0-windows", windows.GetProperty("target").GetString());
        Assert.Equal("windows", windows.GetProperty("stance").GetString());
        Assert.False(windows.TryGetProperty("default", out _));
        Assert.Equal("windows", windows.GetProperty("satisfiedPlatform").GetString());

        // Staying on Windows drops the Registry finding from the counts and the effort.
        Assert.Equal(2, cross.GetProperty("summary").GetProperty("totalFindings").GetInt32());
        Assert.Equal(1, windows.GetProperty("summary").GetProperty("totalFindings").GetInt32());
        Assert.Equal(1, windows.GetProperty("summary").GetProperty("windowsLockInSatisfied").GetInt32());
        Assert.False(cross.GetProperty("summary").TryGetProperty("windowsLockInSatisfied", out _));

        Assert.True(
            cross.GetProperty("summary").GetProperty("effort").GetProperty("maxDays").GetDouble() >
            windows.GetProperty("summary").GetProperty("effort").GetProperty("maxDays").GetDouble(),
            "portability should cost more than staying on Windows when a lock-in finding is present");

        // Per-project rollups are per stance too — the estimator apportions against these.
        Assert.Equal(2, cross.GetProperty("projects")[0].GetProperty("findingCount").GetInt32());
        Assert.Equal(1, windows.GetProperty("projects")[0].GetProperty("findingCount").GetInt32());

        // The findings array is shared, not duplicated per stance: a stance satisfies exactly the
        // findings whose platform matches its satisfiedPlatform, so one copy stays the single truth.
        List<JsonElement> findings = root.GetProperty("findings").EnumerateArray().ToList();
        Assert.Equal(2, findings.Count);
        Assert.Equal("windows", findings[1].GetProperty("platform").GetString());
    }

    [Fact]
    public void RootStillDescribesTheRequestedTargetForOlderConsumers()
    {
        // A 1.4 consumer reads only the root. Whatever --target was asked for must still be what
        // the root reports, or adding `targets` would silently change existing integrations.
        string json = JsonReportWriter.Write(SampleWithWindowsLockIn().ForTarget("net10.0-windows"));

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.Equal("net10.0-windows", root.GetProperty("target").GetString());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("totalFindings").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("windowsLockInSatisfied").GetInt32());

        // ...and `default` moves to the stance the root actually describes.
        List<JsonElement> targets = root.GetProperty("targets").EnumerateArray().ToList();
        Assert.False(targets[0].TryGetProperty("default", out _));
        Assert.True(targets[1].GetProperty("default").GetBoolean());
    }
}
