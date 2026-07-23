using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MigrationScan.Core.Engine;

/// <summary>
/// Reusable Roslyn syntax queries shared by the Tier 2 rules. All matching is on the
/// syntax tree without a resolved compilation, so results are "probable" (spec §5).
/// </summary>
public static class SyntaxScan
{
    /// <summary>1-based start line of a node.</summary>
    public static int LineOf(SyntaxNode node) =>
        node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    /// <summary>
    /// Lines of <c>using</c> directives whose namespace equals, or is nested under, one of
    /// the given namespaces (e.g. <c>System.Drawing</c> matches <c>System.Drawing.Imaging</c>).
    /// </summary>
    public static IEnumerable<int> UsingNamespaceLines(SyntaxNode root, params string[] namespaces)
    {
        foreach (UsingDirectiveSyntax directive in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            if (directive.Name is null)
            {
                continue;
            }

            string ns = directive.Name.ToString();
            if (namespaces.Any(n => ns == n || ns.StartsWith(n + ".", StringComparison.Ordinal)))
            {
                yield return LineOf(directive);
            }
        }
    }

    /// <summary>Lines where an identifier with one of the given names appears (type or value use).</summary>
    public static IEnumerable<int> IdentifierLines(SyntaxNode root, params string[] names)
    {
        HashSet<string> wanted = new(names, StringComparer.Ordinal);
        foreach (IdentifierNameSyntax identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (wanted.Contains(identifier.Identifier.ValueText))
            {
                yield return LineOf(identifier);
            }
        }
    }

    /// <summary>Lines of member-access expressions <c>{receiver}.{member}</c> (e.g. <c>Encoding.Default</c>).</summary>
    public static IEnumerable<int> MemberAccessLines(SyntaxNode root, string receiver, string member)
    {
        foreach (MemberAccessExpressionSyntax access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (access.Name.Identifier.ValueText == member && ReceiverName(access.Expression) == receiver)
            {
                yield return LineOf(access);
            }
        }
    }

    /// <summary>Invocations of a method named <paramref name="member"/> on <paramref name="receiver"/>.</summary>
    public static IEnumerable<InvocationExpressionSyntax> InvocationsOf(SyntaxNode root, string receiver, string member)
    {
        foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax access
                && access.Name.Identifier.ValueText == member
                && ReceiverName(access.Expression) == receiver)
            {
                yield return invocation;
            }
        }
    }

    /// <summary>Lines of argument-less invocations of a method named <paramref name="member"/> on any receiver.</summary>
    public static IEnumerable<int> ArglessInvocationLines(SyntaxNode root, string member)
    {
        foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.ArgumentList.Arguments.Count == 0
                && invocation.Expression is MemberAccessExpressionSyntax access
                && access.Name.Identifier.ValueText == member)
            {
                yield return LineOf(invocation);
            }
        }
    }

    /// <summary>
    /// Lines of attribute usages whose simple name (ignoring namespace and the
    /// <c>Attribute</c> suffix) is one of the given names.
    /// </summary>
    public static IEnumerable<int> AttributeLines(SyntaxNode root, params string[] names)
    {
        HashSet<string> wanted = new(names, StringComparer.Ordinal);
        foreach (AttributeSyntax attribute in root.DescendantNodes().OfType<AttributeSyntax>())
        {
            if (wanted.Contains(SimpleAttributeName(attribute.Name.ToString())))
            {
                yield return LineOf(attribute);
            }
        }
    }

    private static string ReceiverName(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        _ => string.Empty,
    };

    private static string SimpleAttributeName(string name)
    {
        int lastDot = name.LastIndexOf('.');
        if (lastDot >= 0)
        {
            name = name[(lastDot + 1)..];
        }

        return name.EndsWith("Attribute", StringComparison.Ordinal)
            ? name[..^"Attribute".Length]
            : name;
    }
}
