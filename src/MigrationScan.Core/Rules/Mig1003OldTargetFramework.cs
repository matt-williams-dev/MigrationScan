using System.Text.RegularExpressions;
using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG1003 — Target framework below 4.6.2. Tier 1 (certain): read from the project's declared
/// target framework. Below 4.6.2 lacks the .NET Standard 2.0 support and API surface that make
/// migration tractable, so it should be retargeted upward first.
/// </summary>
public sealed partial class Mig1003OldTargetFramework : ProjectRule
{
    public const string Id = "MIG1003";

    private static readonly Version Minimum = new(4, 6, 2);

    public Mig1003OldTargetFramework(RuleMetadata metadata) : base(metadata)
    {
    }

    public override IEnumerable<Finding> Analyze(AnalysisContext context)
    {
        if (TryParseFrameworkVersion(context.Project.TargetFramework, out Version? version) && version < Minimum)
        {
            yield return Report(
                context,
                $"Project '{context.Project.Name}' targets .NET Framework {version} (below 4.6.2). " +
                "Retarget to at least 4.6.2 before migrating — earlier versions lack the .NET Standard 2.0 " +
                "surface that migration relies on.",
                line: context.Project.RootElementLine);
        }
    }

    /// <summary>
    /// Parses a .NET Framework target (<c>v4.5.2</c>, <c>net472</c>, …) into a Version. Modern
    /// monikers (<c>net5.0+</c>, <c>netcoreapp*</c>, <c>netstandard*</c>) are not Framework and
    /// return false.
    /// </summary>
    internal static bool TryParseFrameworkVersion(string? targetFramework, out Version? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            return false;
        }

        string value = targetFramework.Trim();

        // Legacy form: v4.7.2 / v4.5 / v3.5
        Match legacy = LegacyVersion().Match(value);
        if (legacy.Success)
        {
            return TryVersion(legacy.Groups["v"].Value, out version);
        }

        // SDK Framework moniker: net472 / net48 / net45 (net + digits, no dot). net5+ has a dot.
        Match moniker = FrameworkMoniker().Match(value);
        if (moniker.Success)
        {
            string digits = moniker.Groups["d"].Value; // e.g. "472", "48", "35"
            string dotted = string.Join('.', digits.ToCharArray());
            return TryVersion(dotted, out version);
        }

        return false; // net5.0+, netcoreapp*, netstandard*, unknown -> not Framework
    }

    private static bool TryVersion(string text, out Version? version)
    {
        // Version.TryParse needs at least major.minor; pad "4" -> "4.0".
        string normalized = text.Contains('.') ? text : text + ".0";
        bool ok = Version.TryParse(normalized, out Version? parsed);
        version = parsed;
        return ok;
    }

    [GeneratedRegex(@"^v(?<v>\d+(\.\d+){1,3})$", RegexOptions.IgnoreCase)]
    private static partial Regex LegacyVersion();

    [GeneratedRegex(@"^net(?<d>[1-9]\d?\d?)$", RegexOptions.IgnoreCase)]
    private static partial Regex FrameworkMoniker();
}
