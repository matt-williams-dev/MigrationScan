using System.Text.Json;
using MigrationScan.Core.Analysis;

namespace MigrationScan.Reporting;

/// <summary>
/// Loads finding fingerprints from a previously-generated JSON report (spec §9). A baseline
/// is just a JSON report captured earlier (<c>--format json --output baseline.json</c>);
/// findings whose fingerprint is present are suppressed on the next run.
/// </summary>
public static class BaselineReader
{
    /// <summary>Reads the fingerprints of every finding recorded in a JSON report.</summary>
    public static IReadOnlySet<string> LoadFingerprints(string jsonReportPath)
    {
        using FileStream stream = File.OpenRead(jsonReportPath);
        using JsonDocument document = JsonDocument.Parse(stream);

        HashSet<string> fingerprints = new(StringComparer.Ordinal);

        if (!document.RootElement.TryGetProperty("findings", out JsonElement findings)
            || findings.ValueKind != JsonValueKind.Array)
        {
            return fingerprints;
        }

        foreach (JsonElement finding in findings.EnumerateArray())
        {
            // Schema 1.6+ records the fingerprint outright. Preferring it is what makes a
            // *redacted* baseline usable: its paths are one-way hashes, so recomputing from the
            // fields below could never reproduce the original identity.
            if (GetString(finding, "fingerprint") is { Length: > 0 } recorded)
            {
                fingerprints.Add(recorded);
                continue;
            }

            // Pre-1.6 report: reconstruct from the fields, exactly as before. Those reports were
            // never redacted, so the path is present and real.
            string? ruleId = GetString(finding, "ruleId");
            // JSON omits null "file", so fall back to the project path (matches Fingerprints.Of).
            string? file = GetString(finding, "file") ?? GetString(finding, "project");
            string? message = GetString(finding, "message");

            if (ruleId is not null && file is not null && message is not null)
            {
                fingerprints.Add(Fingerprints.Of(ruleId, file, message));
            }
        }

        return fingerprints;
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
