using System.Text.Json;
using Json.Schema;
using MigrationScan.Reporting;

namespace MigrationScan.Reporting.Tests;

/// <summary>
/// The report schema is a published contract, and MigrationScope depends on it. These validate
/// real output against the schema file consumers are told to trust — which is what turns
/// "additive and backward-compatible" from a claim in a README into something that fails a build.
/// </summary>
public class SchemaContractTests
{
    private static readonly JsonSchema Schema = JsonSchema.FromFile(SchemaPath(JsonReportWriter.SchemaVersion));

    private static readonly EvaluationOptions Options = new()
    {
        // Report every violation rather than stopping at the first, so a broken change is fixed
        // in one pass instead of one field per run.
        OutputFormat = OutputFormat.List,
        RequireFormatValidation = true,
    };

    private static string SchemaPath(string version)
    {
        for (DirectoryInfo? d = new(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            string candidate = Path.Combine(d.FullName, "docs", "schema", $"migrationscan-{version}.schema.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            $"No schema file for version {version}. Every schema bump must ship its own " +
            $"docs/schema/migrationscan-<version>.schema.json — that file is the published contract.");
    }

    private static void AssertValid(string json)
    {
        EvaluationResults results = Schema.Evaluate(JsonDocument.Parse(json).RootElement, Options);
        if (results.IsValid)
        {
            return;
        }

        IEnumerable<string> problems = (results.Details ?? [])
            .Where(d => d.Errors is { Count: > 0 })
            .SelectMany(d => d.Errors!.Select(e => $"  {d.InstanceLocation}: {e.Value}"))
            .Distinct();

        Assert.Fail(
            $"Report does not match docs/schema/migrationscan-{JsonReportWriter.SchemaVersion}.schema.json:\n"
            + string.Join('\n', problems));
    }

    [Fact]
    public void ASchemaFileExistsForTheCurrentVersion()
    {
        // Guards the release process rather than the output: bumping SchemaVersion without adding
        // the matching file would leave consumers validating against a stale contract.
        Assert.True(File.Exists(SchemaPath(JsonReportWriter.SchemaVersion)));
    }

    [Fact]
    public void TheDefaultRedactedReportMatchesTheSchema() =>
        AssertValid(JsonReportWriter.Write(ReportSample.Build()));

    [Fact]
    public void AReportWithPathsMatchesTheSchema() =>
        AssertValid(JsonReportWriter.Write(ReportSample.Build(), includePaths: true));

    [Fact]
    public void AWindowsTargetReportMatchesTheSchema() =>
        AssertValid(JsonReportWriter.Write(ReportSample.BuildWindowsTarget()));

    [Fact]
    public void TheSchemaRejectsAFindingCarryingBothFileAndFileId()
    {
        // The one rule that keeps the 1.6 bump additive rather than breaking: a redacted report
        // *replaces* `file` with `fileId`. Emitting both would mean `file` sometimes holds a path
        // and sometimes a hash, which is the breaking change this design exists to avoid.
        string json = """
            {
              "schemaVersion": "1.6",
              "target": "net10.0",
              "summary": {
                "projectsScanned": 1, "totalFindings": 1,
                "findingsBySeverity": { "blocker": 0, "high": 0, "medium": 1, "low": 0 },
                "effort": { "minDays": 0.5, "maxDays": 2, "needsDecision": 0 }
              },
              "projects": [],
              "findings": [{
                "ruleId": "MIG1001", "title": "t", "category": "c",
                "severity": "medium", "tier": "certain", "effort": "small",
                "message": "m", "project": "P/P.csproj",
                "file": "P/A.cs", "fileId": "f:0123456789abcdef"
              }],
              "warnings": []
            }
            """;

        EvaluationResults results = Schema.Evaluate(JsonDocument.Parse(json).RootElement, Options);

        Assert.False(results.IsValid, "the schema must not accept both `file` and `fileId` on one finding");
    }

    [Fact]
    public void TheSchemaRejectsAnUnknownRuleIdShape()
    {
        // Rule ids are a stable, never-reused namespace. A typo that reaches a published report
        // is a broken docs link for whoever reads it.
        string json = JsonReportWriter.Write(ReportSample.Build()).Replace("\"MIG1001\"", "\"NOTARULE\"");

        Assert.False(Schema.Evaluate(JsonDocument.Parse(json).RootElement, Options).IsValid);
    }
}
