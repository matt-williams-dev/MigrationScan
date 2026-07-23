using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG1001 — Non-SDK-style project file.
///
/// SDK-style projects declare an <c>Sdk</c> attribute on the root <c>&lt;Project&gt;</c>
/// element; legacy projects do not. This is a Tier 1 (certain) signal read straight from
/// the project XML. Detection logic only — descriptive metadata lives in rules.json.
/// </summary>
public static class Mig1001NonSdkProject
{
    public const string RuleId = "MIG1001";

    /// <summary>
    /// Returns a finding when the project is non-SDK-style, otherwise null.
    /// </summary>
    public static Finding? Evaluate(DiscoveredProject project, RuleMetadata metadata)
    {
        if (project.IsSdkStyle)
        {
            return null;
        }

        return new Finding(
            Rule: metadata,
            Message: $"Project '{project.Name}' uses the legacy non-SDK project format.",
            ProjectPath: project.Path,
            FilePath: project.Path,
            Line: project.RootElementLine);
    }
}
