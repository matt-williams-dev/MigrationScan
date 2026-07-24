using MigrationScan.Core.Discovery;
using MigrationScan.Core.Models;
using MigrationScan.Core.Rules;
using Mono.Cecil;

namespace MigrationScan.Core.Analysis;

/// <summary>
/// Binary analysis via Mono.Cecil (spec §5, Tier 3). For a compiled assembly with no source,
/// reads its referenced assemblies and flags those that are not available on modern .NET.
/// Findings are Tier 3 (Verified) — read from the compiled metadata, not inferred from syntax.
/// </summary>
public static class BinaryAnalyzer
{
    // Referenced assemblies unavailable (or Windows-only) on modern .NET, mapped to the rule
    // that best describes the concern.
    private static readonly IReadOnlyDictionary<string, string> UnavailableAssemblies =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["System.Web"] = Mig3002SystemWeb.Id,
            ["System.Web.Services"] = Mig3003Asmx.Id,
            ["System.Web.Mvc"] = Mig3010AspNetMvc5.Id,
            ["System.Drawing"] = Mig4001SystemDrawingCommon.Id,
            ["System.Management"] = Mig4003Wmi.Id,
            ["System.DirectoryServices"] = Mig4004DirectoryServices.Id,
            ["System.Messaging"] = Mig3009Msmq.Id,
            ["System.Data.Linq"] = Mig7006LinqToSql.Id,
        };

    /// <summary>Analyzes a compiled assembly file, returning a synthetic project and its findings.</summary>
    public static (DiscoveredProject Project, IReadOnlyList<Finding> Findings) Analyze(
        string assemblyPath, string rootDirectory, RuleCatalog? catalog = null)
    {
        RuleCatalog rules = catalog ?? RuleCatalog.LoadDefault();
        string relativePath = PathUtilities.ToRelative(rootDirectory, Path.GetFullPath(assemblyPath));

        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyPath);

        DiscoveredProject project = new(
            Path: relativePath,
            Name: assembly.Name.Name,
            IsSdkStyle: false,
            TargetFramework: null,
            References: [],
            RootElementLine: 0);

        return (project, AnalyzeAssembly(assembly, relativePath, rules));
    }

    /// <summary>
    /// Analyzes an already-loaded assembly. Tests use this with an in-memory
    /// <see cref="AssemblyDefinition"/> so no real .NET Framework binary is required.
    /// </summary>
    internal static IReadOnlyList<Finding> AnalyzeAssembly(
        AssemblyDefinition assembly, string relativePath, RuleCatalog catalog)
    {
        List<Finding> findings = [];
        HashSet<string> reported = new(StringComparer.OrdinalIgnoreCase);

        foreach (ModuleDefinition module in assembly.Modules)
        {
            foreach (AssemblyNameReference reference in module.AssemblyReferences)
            {
                if (!UnavailableAssemblies.TryGetValue(reference.Name, out string? ruleId)
                    || !reported.Add(reference.Name))
                {
                    continue;
                }

                // Tier 3: verified from compiled metadata rather than inferred from syntax.
                RuleMetadata metadata = catalog.Get(ruleId) with { Tier = ConfidenceTier.Verified };
                findings.Add(new Finding(
                    metadata,
                    $"Compiled assembly references '{reference.Name}', which is not available on modern .NET.",
                    relativePath,
                    relativePath,
                    Line: null));
            }
        }

        return findings;
    }
}
