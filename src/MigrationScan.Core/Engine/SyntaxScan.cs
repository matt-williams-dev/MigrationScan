using Microsoft.CodeAnalysis;
using CS = Microsoft.CodeAnalysis.CSharp.Syntax;
using VB = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace MigrationScan.Core.Engine;

/// <summary>
/// Reusable Roslyn syntax queries shared by the Tier 2 rules. Language-neutral: each query
/// handles both C# and VB syntax nodes, so a single rule detects the pattern in either
/// language. Matching honours language casing rules (VB identifiers are case-insensitive).
/// All matching is on the syntax tree without a resolved compilation, so results are
/// "probable" (spec §5).
/// </summary>
public static class SyntaxScan
{
    /// <summary>1-based start line of a node.</summary>
    public static int LineOf(SyntaxNode node) =>
        node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    /// <summary>
    /// Lines of imports (<c>using</c> / <c>Imports</c>) whose namespace equals, or is nested
    /// under, one of the given namespaces (e.g. <c>System.Drawing</c> matches nested ones).
    /// </summary>
    public static IEnumerable<int> UsingNamespaceLines(SyntaxNode root, params string[] namespaces)
    {
        StringComparison cmp = Casing(root);
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            if (ImportedNamespace(node) is { } ns
                && namespaces.Any(n => ns.Equals(n, cmp) || ns.StartsWith(n + ".", cmp)))
            {
                yield return LineOf(node);
            }
        }
    }

    /// <summary>
    /// Lines that reference one of the given namespaces, whether via an import directive or a
    /// fully-qualified name (e.g. <c>System.Data.SqlClient.SqlConnection</c> with no import).
    /// </summary>
    public static IEnumerable<int> NamespaceUsageLines(SyntaxNode root, params string[] namespaces)
    {
        StringComparison cmp = Casing(root);
        foreach (int line in UsingNamespaceLines(root, namespaces))
        {
            yield return line;
        }

        foreach (SyntaxNode node in root.DescendantNodes())
        {
            if (QualifiedName(node) is { } text && namespaces.Any(n => text.StartsWith(n + ".", cmp)))
            {
                yield return LineOf(node);
            }
        }
    }

    /// <summary>
    /// The first line that references one of the given namespaces, or null. Namespace rules use
    /// this to report once per file rather than once per occurrence (a heavily-qualified file
    /// would otherwise produce hundreds of identical findings).
    /// </summary>
    public static int? FirstNamespaceUsageLine(SyntaxNode root, params string[] namespaces)
    {
        foreach (int line in NamespaceUsageLines(root, namespaces))
        {
            return line;
        }

        return null;
    }

    /// <summary>Lines where an identifier with one of the given names appears (type or value use).</summary>
    public static IEnumerable<int> IdentifierLines(SyntaxNode root, params string[] names)
    {
        StringComparison cmp = Casing(root);
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            if (IdentifierName(node) is { } id && names.Any(n => id.Equals(n, cmp)))
            {
                yield return LineOf(node);
            }
        }
    }

    /// <summary>Lines of member-access expressions <c>{receiver}.{member}</c> (e.g. <c>Encoding.Default</c>).</summary>
    public static IEnumerable<int> MemberAccessLines(SyntaxNode root, string receiver, string member)
    {
        StringComparison cmp = Casing(root);
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            if (MemberAccess(node) is var (rcv, mbr) && mbr is not null
                && mbr.Equals(member, cmp) && rcv.Equals(receiver, cmp))
            {
                yield return LineOf(node);
            }
        }
    }

    /// <summary>Lines of argument-less invocations of a method named <paramref name="member"/> on any receiver.</summary>
    public static IEnumerable<int> ArglessInvocationLines(SyntaxNode root, string member)
    {
        StringComparison cmp = Casing(root);
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            if (Invocation(node) is { Member: { } m, ArgumentCount: 0 } && m.Equals(member, cmp))
            {
                yield return LineOf(node);
            }
        }
    }

    /// <summary>
    /// Invocations of <c>{receiver}.{member}(arg, …)</c> with at least one argument, yielding
    /// each invocation's line and the value of its first argument if that argument is a literal
    /// (a string or a number), otherwise null.
    /// </summary>
    public static IEnumerable<(int Line, object? FirstArgumentLiteral)> InvocationsWithLiteralArg(
        SyntaxNode root, string receiver, string member)
    {
        StringComparison cmp = Casing(root);
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            if (Invocation(node) is { Member: { } m, Receiver: { } r, ArgumentCount: > 0 } info
                && m.Equals(member, cmp) && r.Equals(receiver, cmp))
            {
                yield return (LineOf(node), info.FirstArgumentLiteral);
            }
        }
    }

    /// <summary>
    /// Lines of attribute usages whose simple name (ignoring namespace and the
    /// <c>Attribute</c> suffix) is one of the given names.
    /// </summary>
    public static IEnumerable<int> AttributeLines(SyntaxNode root, params string[] names)
    {
        StringComparison cmp = Casing(root);
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            if (AttributeName(node) is { } name && names.Any(n => name.Equals(n, cmp)))
            {
                yield return LineOf(node);
            }
        }
    }

    // --- language-neutral node extraction ---

    private static StringComparison Casing(SyntaxNode node) =>
        node.Language == LanguageNames.VisualBasic ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static string? ImportedNamespace(SyntaxNode node) => node switch
    {
        CS.UsingDirectiveSyntax u => u.Name?.ToString(),
        VB.SimpleImportsClauseSyntax v => v.Name?.ToString(),
        _ => null,
    };

    private static string? QualifiedName(SyntaxNode node) => node switch
    {
        CS.QualifiedNameSyntax q => q.ToString(),
        VB.QualifiedNameSyntax q => q.ToString(),
        _ => null,
    };

    private static string? IdentifierName(SyntaxNode node) => node switch
    {
        CS.IdentifierNameSyntax i => i.Identifier.ValueText,
        VB.IdentifierNameSyntax i => i.Identifier.ValueText,
        _ => null,
    };

    private static (string Receiver, string? Member) MemberAccess(SyntaxNode node) => node switch
    {
        CS.MemberAccessExpressionSyntax m => (NameOf(m.Expression), m.Name.Identifier.ValueText),
        VB.MemberAccessExpressionSyntax m => (NameOf(m.Expression), m.Name.Identifier.ValueText),
        _ => (string.Empty, null),
    };

    private static InvocationInfo? Invocation(SyntaxNode node)
    {
        switch (node)
        {
            case CS.InvocationExpressionSyntax c:
            {
                (string receiver, string? member) = MemberAccess(c.Expression);
                var args = c.ArgumentList.Arguments;
                object? literal = args.Count > 0 && args[0].Expression is CS.LiteralExpressionSyntax lit ? lit.Token.Value : null;
                return new InvocationInfo(receiver, member, args.Count, literal);
            }

            case VB.InvocationExpressionSyntax v:
            {
                (string receiver, string? member) = MemberAccess(v.Expression);
                if (v.ArgumentList is null)
                {
                    return new InvocationInfo(receiver, member, 0, null);
                }

                var args = v.ArgumentList.Arguments;
                object? literal = args.Count > 0 && args[0] is VB.SimpleArgumentSyntax { Expression: VB.LiteralExpressionSyntax lit }
                    ? lit.Token.Value
                    : null;
                return new InvocationInfo(receiver, member, args.Count, literal);
            }

            default:
                return null;
        }
    }

    private static string NameOf(SyntaxNode? expression) => expression switch
    {
        CS.IdentifierNameSyntax i => i.Identifier.ValueText,
        CS.MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText,
        VB.IdentifierNameSyntax i => i.Identifier.ValueText,
        VB.MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText,
        _ => string.Empty,
    };

    private static string? AttributeName(SyntaxNode node) => node switch
    {
        CS.AttributeSyntax a => SimpleAttributeName(a.Name.ToString()),
        VB.AttributeSyntax a => SimpleAttributeName(a.Name.ToString()),
        _ => null,
    };

    private static string SimpleAttributeName(string name)
    {
        int lastDot = name.LastIndexOf('.');
        if (lastDot >= 0)
        {
            name = name[(lastDot + 1)..];
        }

        return name.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase)
            ? name[..^"Attribute".Length]
            : name;
    }

    private readonly record struct InvocationInfo(string Receiver, string? Member, int ArgumentCount, object? FirstArgumentLiteral);
}
