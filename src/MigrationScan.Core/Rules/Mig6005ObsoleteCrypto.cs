using MigrationScan.Core.Engine;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// MIG6005 — Obsolete cryptography types. Tier 2 (probable). The concrete
/// <c>*CryptoServiceProvider</c> / <c>*Managed</c> implementation types are obsolete on modern
/// .NET (and some are insecure); use the algorithm base-class factory methods instead.
/// </summary>
public sealed class Mig6005ObsoleteCrypto : SyntaxRule
{
    public const string Id = "MIG6005";

    private static readonly string[] ObsoleteTypes =
    [
        "RNGCryptoServiceProvider", "SHA1Managed", "SHA256Managed", "SHA384Managed", "SHA512Managed",
        "MD5CryptoServiceProvider", "SHA1CryptoServiceProvider", "DESCryptoServiceProvider",
        "TripleDESCryptoServiceProvider", "RC2CryptoServiceProvider", "AesCryptoServiceProvider",
        "RijndaelManaged",
    ];

    public Mig6005ObsoleteCrypto(RuleMetadata metadata) : base(metadata)
    {
    }

    protected override IEnumerable<Finding> AnalyzeSource(SourceFile source, AnalysisContext context)
    {
        var root = source.SyntaxTree.GetRoot();
        foreach (int line in SyntaxScan.IdentifierLines(root, ObsoleteTypes))
        {
            yield return Report(
                context,
                source,
                "Uses an obsolete cryptography implementation type. On modern .NET these are deprecated; " +
                "create algorithms via the base-class factories (e.g. SHA256.Create(), Aes.Create(), " +
                "RandomNumberGenerator) and drop weak algorithms (MD5/SHA1/DES) entirely.",
                line);
        }
    }
}
