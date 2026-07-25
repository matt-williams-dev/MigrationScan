using MigrationScan.Core.Models;

namespace MigrationScan.Core.Tests;

/// <summary>
/// Covers the reference inventory: every declaration form a legacy project uses to name a
/// dependency, and the classification that tells a vendor DLL apart from a restored package.
/// </summary>
public class ReferenceInventoryTests
{
    private static AnalysisResult Result() => AnalysisHelper.AnalyzeFixture("ReferenceGraph");

    private static ReferenceRecord Reference(AnalysisResult result, string name) =>
        Assert.Single(result.References, r => r.Name == name);

    [Fact]
    public void CatalogsPackageFromPackagesConfigWithItsPackageVersion()
    {
        ReferenceRecord package = Reference(Result(), "Newtonsoft.Json");

        Assert.Equal(ReferenceKind.Package, package.Kind);
        Assert.Equal("13.0.3", package.Version); // the package version, not the 13.0.0.0 assembly version
        Assert.Equal("App/packages.config", package.DeclaredIn);
        Assert.True(package.IsThirdParty);
    }

    [Fact]
    public void DoesNotDuplicateARestoredPackageAsAnAssemblyReference()
    {
        // App.csproj also has a <Reference> with a HintPath into ..\packages\ for Newtonsoft.Json.
        // packages.config is authoritative, so exactly one entry should exist.
        Assert.Single(Result().References, r => r.Name == "Newtonsoft.Json");
    }

    [Fact]
    public void PrefersThePackageEntryOverAGacReferenceOfTheSameName()
    {
        // Contoso.Grid is in packages.config *and* declared as a GAC <Reference> with no
        // HintPath — a common legacy inconsistency. It is one component to research, so the
        // package entry wins; MIG1005 is what reports the GAC resolution.
        ReferenceRecord grid = Reference(Result(), "Contoso.Grid");

        Assert.Equal(ReferenceKind.Package, grid.Kind);
        Assert.Equal("4.5.0", grid.Version);
    }

    [Fact]
    public void CatalogsPackageReferenceFromSdkStyleProject()
    {
        ReferenceRecord package = Reference(Result(), "Serilog");

        Assert.Equal(ReferenceKind.Package, package.Kind);
        Assert.Equal("3.1.1", package.Version);
        Assert.Equal("Shared/Shared.csproj", package.ProjectPath);
    }

    [Fact]
    public void ClassifiesGacAssemblyWithItsStrongNameVersion()
    {
        ReferenceRecord telerik = Reference(Result(), "Telerik.Web.UI");

        Assert.Equal(ReferenceKind.Assembly, telerik.Kind);
        Assert.Equal("2015.3.930.45", telerik.Version);
        Assert.Null(telerik.Source); // resolved from the GAC — nowhere on disk to point at
        Assert.False(telerik.IsFrameworkAssembly);
        Assert.True(telerik.IsThirdParty);
    }

    [Fact]
    public void MarksFrameworkAssemblyAsNotThirdParty()
    {
        ReferenceRecord systemWeb = Reference(Result(), "System.Web");

        Assert.True(systemWeb.IsFrameworkAssembly);
        Assert.False(systemWeb.IsThirdParty);
    }

    [Fact]
    public void ClassifiesCheckedInDllAsVendoredWithForwardSlashedHintPath()
    {
        ReferenceRecord vendored = Reference(Result(), "Contoso.Payments");

        Assert.Equal(ReferenceKind.VendoredAssembly, vendored.Kind);
        Assert.Equal("../libs/Contoso.Payments.dll", vendored.Source);
        Assert.Equal("2.1.0.0", vendored.Version);
    }

    [Fact]
    public void ClassifiesAxInteropWrapperAsCom()
    {
        // aximp emits AxInterop.* for the ActiveX host control — a COM dependency, not a
        // plain third-party assembly that happens to be checked in.
        Assert.Equal(ReferenceKind.Com, Reference(Result(), "AxInterop.MSCommLib").Kind);
    }

    [Fact]
    public void ClassifiesEmbeddedInteropAssemblyAsCom()
    {
        Assert.Equal(ReferenceKind.Com, Reference(Result(), "Microsoft.Office.Interop.Excel").Kind);
    }

    [Fact]
    public void CatalogsComReferenceWithTypeLibraryGuidAndVersion()
    {
        ReferenceRecord com = Reference(Result(), "MSXML2");

        Assert.Equal(ReferenceKind.Com, com.Kind);
        Assert.Equal("6.0", com.Version);
        Assert.Equal("{f5078f18-c551-11d3-89b9-0000f81fe221}", com.Source);
    }

    [Fact]
    public void CatalogsComFileReference()
    {
        ReferenceRecord com = Reference(Result(), "ScannerCtl.tlb");

        Assert.Equal(ReferenceKind.Com, com.Kind);
        Assert.Equal("../libs/ScannerCtl.tlb", com.Source);
    }

    [Fact]
    public void CatalogsProjectReferenceAgainstTheScanRoot()
    {
        ReferenceRecord project = Reference(Result(), "Shared");

        Assert.Equal(ReferenceKind.Project, project.Kind);
        Assert.Equal("Shared/Shared.csproj", project.Source);
        Assert.Equal("App/App.csproj", project.ProjectPath);
        Assert.False(project.IsThirdParty); // this solution's own code, already in scope
    }

    [Fact]
    public void CatalogsAsmxWebReferenceByFolderNameAndUrl()
    {
        ReferenceRecord service = Reference(Result(), "LegacyPricing");

        Assert.Equal(ReferenceKind.WebService, service.Kind);
        Assert.Equal("http://pricing.internal/Legacy.asmx", service.Source);
    }

    [Fact]
    public void ResolvesWcfServiceReferenceEndpointFromTheSvcmap()
    {
        ReferenceRecord service = Reference(Result(), "PricingService");

        Assert.Equal(ReferenceKind.WebService, service.Kind);
        Assert.Equal("http://pricing.internal/PricingService.svc", service.Source);
    }

    [Fact]
    public void CountsDistinctThirdPartyComponentsNotDeclarationSites()
    {
        AnalysisResult result = Result();

        // Telerik, Contoso.Payments, Contoso.Grid, Newtonsoft.Json, Serilog, AxInterop.MSCommLib,
        // Microsoft.Office.Interop.Excel, MSXML2, ScannerCtl.tlb, LegacyPricing, PricingService.
        Assert.Equal(11, result.DistinctThirdPartyCount());
        Assert.DoesNotContain(result.ThirdPartyReferences, r => r.Name == "System.Web");
        Assert.DoesNotContain(result.ThirdPartyReferences, r => r.Kind == ReferenceKind.Project);
    }

    [Fact]
    public void OrdersReferencesDeterministically()
    {
        Assert.Equal(
            Result().References.Select(r => (r.ProjectPath, r.Kind, r.Name)),
            Result().References.Select(r => (r.ProjectPath, r.Kind, r.Name)));

        // Grouped by project, ordinal — App before Shared.
        List<string> projects = Result().References.Select(r => r.ProjectPath).Distinct().ToList();
        Assert.Equal(["App/App.csproj", "Shared/Shared.csproj"], projects);
    }

    [Fact]
    public void ModernCleanSolutionHasNoThirdPartyReferences()
    {
        Assert.Empty(AnalysisHelper.AnalyzeFixture("ModernClean").ThirdPartyReferences);
    }
}
