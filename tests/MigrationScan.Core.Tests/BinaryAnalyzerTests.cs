using MigrationScan.Core.Analysis;
using MigrationScan.Core.Models;
using MigrationScan.Core.Rules;
using Mono.Cecil;

namespace MigrationScan.Core.Tests;

/// <summary>
/// Binary analysis is tested with in-memory Cecil assemblies, so no real .NET Framework
/// binary is required.
/// </summary>
public class BinaryAnalyzerTests
{
    private static readonly RuleCatalog Catalog = RuleCatalog.LoadDefault();

    private static AssemblyDefinition AssemblyReferencing(params string[] references)
    {
        AssemblyDefinition assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("Sample", new Version(1, 0, 0, 0)), "Sample", ModuleKind.Dll);

        foreach (string reference in references)
        {
            assembly.MainModule.AssemblyReferences.Add(new AssemblyNameReference(reference, new Version(4, 0, 0, 0)));
        }

        return assembly;
    }

    [Fact]
    public void FlagsUnavailableAssemblyReferencesAsVerifiedTier()
    {
        using AssemblyDefinition assembly = AssemblyReferencing("System.Web", "System.Drawing", "Newtonsoft.Json");

        IReadOnlyList<Finding> findings = BinaryAnalyzer.AnalyzeAssembly(assembly, "bin/Sample.dll", Catalog);
        IReadOnlySet<string> ids = findings.Select(f => f.Rule.Id).ToHashSet();

        Assert.Contains("MIG3002", ids); // System.Web
        Assert.Contains("MIG4001", ids); // System.Drawing
        Assert.All(findings, f => Assert.Equal(ConfidenceTier.Verified, f.Rule.Tier)); // Tier 3
    }

    [Fact]
    public void DoesNotFlagAvailableAssemblies()
    {
        using AssemblyDefinition assembly = AssemblyReferencing("System.Runtime", "Newtonsoft.Json", "System.Text.Json");

        Assert.Empty(BinaryAnalyzer.AnalyzeAssembly(assembly, "bin/Sample.dll", Catalog));
    }

    [Fact]
    public void DeduplicatesRepeatedReferences()
    {
        using AssemblyDefinition assembly = AssemblyReferencing("System.Web");
        assembly.MainModule.AssemblyReferences.Add(new AssemblyNameReference("System.Web", new Version(2, 0, 0, 0)));

        Assert.Single(BinaryAnalyzer.AnalyzeAssembly(assembly, "bin/Sample.dll", Catalog), f => f.Rule.Id == "MIG3002");
    }

    [Fact]
    public void FindingsAnchorToTheAssemblyPath()
    {
        using AssemblyDefinition assembly = AssemblyReferencing("System.Messaging");

        Finding finding = Assert.Single(BinaryAnalyzer.AnalyzeAssembly(assembly, "bin/Sample.dll", Catalog));
        Assert.Equal("bin/Sample.dll", finding.FilePath);
        Assert.Equal("MIG3009", finding.Rule.Id);
    }
}
