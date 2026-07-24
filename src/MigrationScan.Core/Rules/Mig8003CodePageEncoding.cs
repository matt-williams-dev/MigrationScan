using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG8003 — Code-page encoding without provider registration. Tier 2 (probable).
/// Flags <c>Encoding.GetEncoding(...)</c> for a code page; Unicode names/numbers are ignored.
/// </summary>
public sealed class Mig8003CodePageEncoding : SyntaxRule
{
    public const string Id = "MIG8003";

    // Encodings available on modern .NET without CodePagesEncodingProvider, by name...
    private static readonly HashSet<string> AlwaysAvailableNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "utf-8", "utf8", "utf-16", "utf-16le", "utf-16be", "unicode", "utf-32", "us-ascii", "ascii",
    };

    // ...and by code-page number (utf-16, utf-16BE, utf-32, utf-32BE, us-ascii, latin1, utf-8).
    private static readonly HashSet<int> AlwaysAvailableCodePages =
        [1200, 1201, 12000, 12001, 20127, 28591, 65001];

    public Mig8003CodePageEncoding(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        foreach ((int line, object? literal) in SyntaxScan.InvocationsWithLiteralArg(root, "Encoding", "GetEncoding"))
        {
            if (IsAlwaysAvailable(literal))
            {
                continue;
            }

            yield return Report(
                context,
                source,
                "Calls Encoding.GetEncoding for a code page. Modern .NET does not register code-page encodings " +
                "by default; register CodePagesEncodingProvider.Instance first or the call throws.",
                line);
        }
    }

    // A literal argument for an always-available encoding — by name ("utf-8") or code-page
    // number (65001). A non-literal argument (null) is not classified and is flagged to be safe.
    private static bool IsAlwaysAvailable(object? literal) => literal switch
    {
        string name => AlwaysAvailableNames.Contains(name.Trim()),
        int codePage => AlwaysAvailableCodePages.Contains(codePage),
        long codePage => AlwaysAvailableCodePages.Contains((int)codePage),
        _ => false,
    };
}
