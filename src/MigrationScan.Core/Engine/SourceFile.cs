using Microsoft.CodeAnalysis;

namespace MigrationScan.Core.Engine;

/// <summary>
/// A C# source file parsed into a Roslyn syntax tree. No compilation is performed,
/// so findings drawn from this are Tier 2 (probable) — see spec §5.
/// </summary>
/// <param name="RelativePath">Repo-relative, forward-slashed path used in output.</param>
/// <param name="SyntaxTree">The parsed syntax tree.</param>
public sealed record SourceFile(string RelativePath, SyntaxTree SyntaxTree);
