using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using MigrationScan.Core.Engine;

namespace MigrationScan.Core.Tests;

/// <summary>
/// The syntax queries are language-neutral (C# and VB) and honour each language's casing
/// rules. These tests parse snippets directly rather than going through a fixture.
/// </summary>
public class SyntaxScanTests
{
    private static SyntaxNode Cs(string code) => CSharpSyntaxTree.ParseText(code).GetRoot();

    private static SyntaxNode Vb(string code) => VisualBasicSyntaxTree.ParseText(code).GetRoot();

    [Fact]
    public void IdentifierLinesMatchInBothLanguages()
    {
        Assert.NotEmpty(SyntaxScan.IdentifierLines(
            Cs("class C { void M() { var k = Registry.LocalMachine; } }"), "Registry"));

        Assert.NotEmpty(SyntaxScan.IdentifierLines(
            Vb("""
               Module M
                   Sub S()
                       Dim k = Registry.LocalMachine
                   End Sub
               End Module
               """), "Registry"));
    }

    [Fact]
    public void VbIdentifierMatchingIsCaseInsensitive()
    {
        // VB is case-insensitive: 'registry' still matches 'Registry'.
        SyntaxNode root = Vb("""
            Module M
                Sub S()
                    Dim k = registry.LocalMachine
                End Sub
            End Module
            """);

        Assert.NotEmpty(SyntaxScan.IdentifierLines(root, "Registry"));
    }

    [Fact]
    public void CSharpIdentifierMatchingIsCaseSensitive()
    {
        // C# is case-sensitive: a lowercase 'registry' variable must NOT match 'Registry'.
        SyntaxNode root = Cs("class C { void M() { var registry = 1; var y = registry; } }");

        Assert.Empty(SyntaxScan.IdentifierLines(root, "Registry"));
    }

    [Fact]
    public void ImportsAndUsingsBothMatchANamespace()
    {
        Assert.NotEmpty(SyntaxScan.UsingNamespaceLines(
            Cs("using System.Web.Mvc; class C {}"), "System.Web.Mvc"));

        Assert.NotEmpty(SyntaxScan.UsingNamespaceLines(
            Vb("""
               Imports System.Web.Mvc
               Module M
               End Module
               """), "System.Web.Mvc"));
    }

    [Fact]
    public void MemberAccessMatchesInBothLanguages()
    {
        Assert.NotEmpty(SyntaxScan.MemberAccessLines(
            Cs("class C { void M() { var d = System.Text.Encoding.Default; } }"), "Encoding", "Default"));

        Assert.NotEmpty(SyntaxScan.MemberAccessLines(
            Vb("""
               Module M
                   Sub S()
                       Dim d = System.Text.Encoding.Default
                   End Sub
               End Module
               """), "Encoding", "Default"));
    }

    [Fact]
    public void CodePageInvocationLiteralIsExtractedInBothLanguages()
    {
        (int, object?) csHit = Assert.Single(
            SyntaxScan.InvocationsWithLiteralArg(
                Cs("class C { void M() { var e = System.Text.Encoding.GetEncoding(1252); } }"),
                "Encoding", "GetEncoding"));
        Assert.Equal(1252, csHit.Item2);

        (int, object?) vbHit = Assert.Single(
            SyntaxScan.InvocationsWithLiteralArg(
                Vb("""
                   Module M
                       Sub S()
                           Dim e = System.Text.Encoding.GetEncoding(1252)
                       End Sub
                   End Module
                   """),
                "Encoding", "GetEncoding"));
        Assert.Equal(1252, vbHit.Item2);
    }
}
