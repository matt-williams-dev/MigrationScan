using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG8003 — Code-page encoding without provider registration. Tier 2 (probable).
/// Flags <c>Encoding.GetEncoding(...)</c> for a code page; Unicode names are ignored.
/// </summary>
public sealed class Mig8003CodePageEncoding : SyntaxRule
{
    public const string Id = "MIG8003";

    // Encodings available on modern .NET without CodePagesEncodingProvider.
    private static readonly HashSet<string> AlwaysAvailable = new(StringComparer.OrdinalIgnoreCase)
    {
        "utf-8", "utf8", "utf-16", "utf-16le", "utf-16be", "unicode", "utf-32", "us-ascii", "ascii",
    };

    public Mig8003CodePageEncoding(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        foreach (InvocationExpressionSyntax invocation in SyntaxScan.InvocationsOf(root, "Encoding", "GetEncoding"))
        {
            ArgumentSyntax? argument = invocation.ArgumentList.Arguments.FirstOrDefault();
            if (argument is null || IsAlwaysAvailableName(argument.Expression))
            {
                continue;
            }

            yield return Report(
                context,
                source,
                "Calls Encoding.GetEncoding for a code page. Modern .NET does not register code-page encodings " +
                "by default; register CodePagesEncodingProvider.Instance first or the call throws.",
                SyntaxScan.LineOf(invocation));
        }
    }

    private static bool IsAlwaysAvailableName(ExpressionSyntax expression) =>
        expression is LiteralExpressionSyntax { Token.Value: string name }
        && AlwaysAvailable.Contains(name.Trim());
}
