using System.Xml;
using System.Xml.Linq;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Discovery;

/// <summary>
/// Parses a <c>.csproj</c> as XML (spec §3: no MSBuild evaluation). Handles both
/// SDK-style projects (no XML namespace) and legacy projects (the MSBuild namespace).
/// </summary>
public static class ProjectParser
{
    /// <summary>
    /// Parses the project at <paramref name="projectFilePath"/>.
    /// </summary>
    /// <param name="projectFilePath">Absolute path to the project file.</param>
    /// <param name="relativePath">Repo-relative, forward-slashed path used in output.</param>
    public static DiscoveredProject Parse(string projectFilePath, string relativePath)
    {
        XDocument document = XDocument.Load(projectFilePath, LoadOptions.SetLineInfo);
        XElement root = document.Root
            ?? throw new InvalidDataException($"Project file '{relativePath}' has no root element.");

        XNamespace ns = root.GetDefaultNamespace();
        bool isSdkStyle = root.Attribute("Sdk") is not null;

        return new DiscoveredProject(
            Path: relativePath,
            Name: Path.GetFileNameWithoutExtension(projectFilePath),
            IsSdkStyle: isSdkStyle,
            TargetFramework: ReadTargetFramework(root, ns),
            References: ReadReferences(root, ns),
            RootElementLine: LineOf(root));
    }

    private static string? ReadTargetFramework(XElement root, XNamespace ns)
    {
        // SDK-style: <TargetFramework>net10.0</TargetFramework> or <TargetFrameworks>.
        // Legacy:    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>.
        string? value =
            root.Descendants(ns + "TargetFramework").FirstOrDefault()?.Value
            ?? root.Descendants(ns + "TargetFrameworks").FirstOrDefault()?.Value
            ?? root.Descendants(ns + "TargetFrameworkVersion").FirstOrDefault()?.Value;

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static IReadOnlyList<string> ReadReferences(XElement root, XNamespace ns)
    {
        List<string> references = [];

        foreach (XElement element in root.Descendants(ns + "Reference").Concat(root.Descendants(ns + "PackageReference")))
        {
            string? include = element.Attribute("Include")?.Value;
            if (!string.IsNullOrWhiteSpace(include))
            {
                references.Add(include.Trim());
            }
        }

        return references;
    }

    private static int LineOf(XElement element) =>
        element is IXmlLineInfo lineInfo && lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0;
}
