using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using MigrationScan.Core.Models;

namespace MigrationScan.Core.Rules;

/// <summary>
/// The set of rule definitions, loaded from JSON data (spec §14). Detection logic
/// lives in the individual rule classes; this only holds their metadata.
/// </summary>
public sealed class RuleCatalog
{
    private const string ResourceSuffix = "Data.rules.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly IReadOnlyDictionary<string, RuleMetadata> _rules;

    private RuleCatalog(IReadOnlyDictionary<string, RuleMetadata> rules) => _rules = rules;

    /// <summary>All rule definitions, ordered by ID.</summary>
    public IReadOnlyCollection<RuleMetadata> All => _rules.Values.OrderBy(r => r.Id, StringComparer.Ordinal).ToList();

    /// <summary>Loads the catalog embedded in this assembly.</summary>
    public static RuleCatalog LoadDefault()
    {
        Assembly assembly = typeof(RuleCatalog).Assembly;
        string resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded rule catalog '{ResourceSuffix}' was not found.");

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not open embedded rule catalog '{resourceName}'.");

        RuleFile file = JsonSerializer.Deserialize<RuleFile>(stream, SerializerOptions)
            ?? throw new InvalidOperationException("Rule catalog deserialized to null.");

        return FromRules(file.Rules);
    }

    /// <summary>Builds a catalog from an explicit rule list. Useful in tests.</summary>
    public static RuleCatalog FromRules(IEnumerable<RuleMetadata> rules)
    {
        Dictionary<string, RuleMetadata> byId = new(StringComparer.Ordinal);
        foreach (RuleMetadata rule in rules)
        {
            if (!byId.TryAdd(rule.Id, rule))
            {
                throw new InvalidOperationException($"Duplicate rule ID '{rule.Id}' in catalog. Rule IDs must be unique.");
            }
        }

        return new RuleCatalog(byId);
    }

    /// <summary>Gets a rule's metadata by ID, throwing if it is not defined.</summary>
    public RuleMetadata Get(string ruleId) =>
        _rules.TryGetValue(ruleId, out RuleMetadata? rule)
            ? rule
            : throw new KeyNotFoundException($"No rule metadata defined for '{ruleId}'.");

    private sealed record RuleFile(IReadOnlyList<RuleMetadata> Rules);
}
