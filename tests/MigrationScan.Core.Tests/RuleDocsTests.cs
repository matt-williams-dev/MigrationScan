using System.Text;
using MigrationScan.Core.Models;
using MigrationScan.Core.Rules;

namespace MigrationScan.Core.Tests;

/// <summary>
/// Keeps <c>docs/rules/</c> honest against the shipping catalog. Every finding a report emits
/// carries a <c>docsUrl</c>, so a rule without a page, or a page missing from the index, is a
/// broken promise in somebody's report rather than a documentation nicety.
/// </summary>
public class RuleDocsTests
{
    private const string CanonicalDocsPrefix =
        "https://github.com/matt-williams-dev/MigrationScan/blob/main/docs/rules/";

    private static readonly RuleCatalog Catalog = RuleCatalog.LoadDefault();
    private static readonly string DocsDirectory = Path.Combine(Repo.Root, "docs", "rules");
    private static readonly string Index = File.ReadAllText(Path.Combine(DocsDirectory, "README.md"));

    [Fact]
    public void EveryRuleHasADocsPage()
    {
        List<string> missing = [.. Catalog.All
            .Where(rule => !File.Exists(Path.Combine(DocsDirectory, $"{rule.Id}.md")))
            .Select(rule => rule.Id)];

        Assert.True(missing.Count == 0,
            $"No page in docs/rules/ for: {string.Join(", ", missing)}. "
            + "Add one per rule, following the shape of MIG3001.md.");
    }

    [Fact]
    public void EveryRuleAppearsInTheIndex()
    {
        StringBuilder missing = new();
        foreach (RuleMetadata rule in Catalog.All)
        {
            if (!Index.Contains(RowFor(rule), StringComparison.Ordinal))
            {
                missing.AppendLine(RowFor(rule));
            }
        }

        Assert.True(missing.Length == 0,
            "docs/rules/README.md is missing a row, or a row no longer matches the catalog. "
            + $"Add or correct these rows under their category heading:\n{missing}");
    }

    [Fact]
    public void TheIndexListsNoRuleThatTheCatalogDoesNotShip()
    {
        // Catches a rule that was renamed or withdrawn and left behind a dead link.
        IReadOnlySet<string> shipped = Catalog.All.Select(rule => rule.Id).ToHashSet(StringComparer.Ordinal);

        List<string> orphans = [.. Directory
            .EnumerateFiles(DocsDirectory, "MIG*.md")
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Where(id => !shipped.Contains(id))];

        Assert.True(orphans.Count == 0,
            $"docs/rules/ has pages for rules the catalog no longer ships: {string.Join(", ", orphans)}.");
    }

    [Fact]
    public void EveryCategoryHasAHeadingInTheIndex()
    {
        List<string> missing = [.. Catalog.All
            .Select(rule => rule.Category)
            .Distinct(StringComparer.Ordinal)
            .Where(category => !Index.Contains($"## {category}", StringComparison.Ordinal))];

        Assert.True(missing.Count == 0,
            $"docs/rules/README.md has no `## ` heading for: {string.Join(", ", missing)}.");
    }

    [Fact]
    public void DocsUrlsPointAtTheCanonicalGitHubPages()
    {
        // These URLs are a permanent contract with every report already generated: reports are
        // byte-identical across runs and both committed sample reports carry them. Repointing
        // them at a different host invalidates those samples and breaks links in reports that
        // are already out in the world. See docs/rules/README.md, "Adding a rule".
        foreach (RuleMetadata rule in Catalog.All)
        {
            Assert.Equal($"{CanonicalDocsPrefix}{rule.Id}.md", rule.DocsUrl);
        }
    }

    private static string RowFor(RuleMetadata rule) =>
        $"| [{rule.Id}]({rule.Id}.md) | {rule.Title} | {Lower(rule.Severity)} | {TierLabel(rule.Tier)} |";

    private static string Lower(Severity severity) => severity.ToString().ToLowerInvariant();

    private static string TierLabel(ConfidenceTier tier) => tier switch
    {
        ConfidenceTier.Certain => "1 · Certain",
        ConfidenceTier.Probable => "2 · Probable",
        ConfidenceTier.Verified => "3 · Verified",
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unhandled confidence tier."),
    };
}
