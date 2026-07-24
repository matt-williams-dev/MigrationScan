using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using MigrationScan.Core.Models;

namespace MigrationScan.Reporting;

/// <summary>
/// Renders an <see cref="AnalysisResult"/> as SARIF 2.1.0 (spec §8) so it drops into GitHub
/// code scanning and Azure DevOps with no glue code. Deterministic: no timestamps, stable
/// ordering, LF newlines.
/// </summary>
public static class SarifReportWriter
{
    private const string SchemaUrl = "https://json.schemastore.org/sarif-2.1.0.json";
    private const string InformationUri = "https://github.com/matt-williams-dev/MigrationScan";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Write(AnalysisResult result)
    {
        // Distinct rules that produced findings, ordered by ID; results reference them by index.
        List<RuleMetadata> rules = result.Findings
            .Select(f => f.Rule)
            .DistinctBy(r => r.Id)
            .OrderBy(r => r.Id, StringComparer.Ordinal)
            .ToList();

        Dictionary<string, int> ruleIndex = rules
            .Select((rule, index) => (rule.Id, index))
            .ToDictionary(x => x.Id, x => x.index);

        SarifLog log = new(
            Schema: SchemaUrl,
            Version: "2.1.0",
            Runs:
            [
                new SarifRun(
                    Tool: new SarifTool(new SarifDriver(
                        Name: "MigrationScan",
                        InformationUri: InformationUri,
                        Rules: rules.Select(ToDescriptor).ToList())),
                    Results: result.Findings.Select(f => ToResult(f, ruleIndex)).ToList()),
            ]);

        return JsonSerializer.Serialize(log, SerializerOptions).Replace("\r\n", "\n");
    }

    // SARIF has three result levels; map severity onto them.
    private static string Level(Severity severity) => severity switch
    {
        Severity.Blocker or Severity.High => "error",
        Severity.Medium => "warning",
        Severity.Low => "note",
        _ => "warning",
    };

    private static SarifDescriptor ToDescriptor(RuleMetadata rule) => new(
        Id: rule.Id,
        Name: rule.Title,
        ShortDescription: new SarifText(rule.Title),
        FullDescription: new SarifText(rule.Remediation),
        HelpUri: rule.DocsUrl,
        DefaultConfiguration: new SarifConfiguration(Level(rule.Severity)),
        Properties: new SarifRuleProperties(
            Category: rule.Category,
            Severity: rule.Severity.ToString().ToLowerInvariant(),
            Tier: rule.Tier.ToString().ToLowerInvariant(),
            Effort: rule.Effort.ToString().ToLowerInvariant(),
            Tags: [rule.Category]));

    private static SarifResult ToResult(Finding finding, IReadOnlyDictionary<string, int> ruleIndex) => new(
        RuleId: finding.Rule.Id,
        RuleIndex: ruleIndex[finding.Rule.Id],
        Level: Level(finding.Rule.Severity),
        Message: new SarifText(finding.Message),
        Locations:
        [
            new SarifLocation(new SarifPhysicalLocation(
                ArtifactLocation: new SarifArtifactLocation(finding.FilePath ?? finding.ProjectPath),
                Region: finding.Line is { } line ? new SarifRegion(line) : null)),
        ]);

    // --- SARIF DTOs (a minimal, valid subset of the 2.1.0 schema) ---

    private sealed record SarifLog(
        [property: JsonPropertyName("$schema")] string Schema,
        string Version,
        IReadOnlyList<SarifRun> Runs);

    private sealed record SarifRun(SarifTool Tool, IReadOnlyList<SarifResult> Results);

    private sealed record SarifTool(SarifDriver Driver);

    private sealed record SarifDriver(string Name, string InformationUri, IReadOnlyList<SarifDescriptor> Rules);

    private sealed record SarifDescriptor(
        string Id,
        string Name,
        SarifText ShortDescription,
        SarifText FullDescription,
        string HelpUri,
        SarifConfiguration DefaultConfiguration,
        SarifRuleProperties Properties);

    private sealed record SarifRuleProperties(
        string Category,
        string Severity,
        string Tier,
        string Effort,
        IReadOnlyList<string> Tags);

    private sealed record SarifConfiguration(string Level);

    private sealed record SarifText(string Text);

    private sealed record SarifResult(
        string RuleId,
        int RuleIndex,
        string Level,
        SarifText Message,
        IReadOnlyList<SarifLocation> Locations);

    private sealed record SarifLocation(SarifPhysicalLocation PhysicalLocation);

    private sealed record SarifPhysicalLocation(SarifArtifactLocation ArtifactLocation, SarifRegion? Region);

    private sealed record SarifArtifactLocation(string Uri);

    private sealed record SarifRegion(int StartLine);
}
