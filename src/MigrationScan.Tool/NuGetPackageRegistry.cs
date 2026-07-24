using System.Text.Json;
using MigrationScan.Core.Analysis;

namespace MigrationScan.Tool;

/// <summary>
/// Online <see cref="IPackageRegistry"/> backed by the nuget.org search API (used only when
/// <c>--online</c> is passed). Lookups are synchronous and cached; a network failure degrades
/// gracefully to "unknown" with a single diagnostic rather than aborting the scan.
/// </summary>
internal sealed class NuGetPackageRegistry : IPackageRegistry, IDisposable
{
    private const string SearchEndpoint = "https://azuresearch-usnc.nuget.org/query";

    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private readonly Action<string> _onDiagnostic;
    private readonly Dictionary<string, PackageInfo?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _warned;

    public NuGetPackageRegistry(Action<string>? onDiagnostic = null, HttpClient? httpClient = null)
    {
        _onDiagnostic = onDiagnostic ?? (_ => { });
        _ownsHttpClient = httpClient is null;
        _http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MigrationScan");
    }

    public PackageInfo? Lookup(string packageId)
    {
        if (_cache.TryGetValue(packageId, out PackageInfo? cached))
        {
            return cached;
        }

        PackageInfo? info = null;
        try
        {
            string url = $"{SearchEndpoint}?q=packageid:{Uri.EscapeDataString(packageId)}&prerelease=false&semVerLevel=2.0.0";
            string json = _http.GetStringAsync(url).GetAwaiter().GetResult();
            info = ParseSearchResponse(packageId, json);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or IOException)
        {
            if (!_warned)
            {
                _warned = true;
                _onDiagnostic($"online NuGet lookups degraded ({ex.Message}); continuing without package status.");
            }
        }

        _cache[packageId] = info;
        return info;
    }

    /// <summary>
    /// Parses a nuget.org search response for the given package. Returns null when the package
    /// is not present in the results; a non-deprecated match yields a PackageInfo with
    /// <see cref="PackageInfo.IsDeprecated"/> false.
    /// </summary>
    internal static PackageInfo? ParseSearchResponse(string packageId, string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("data", out JsonElement data) || data.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (JsonElement package in data.EnumerateArray())
        {
            string? id = package.TryGetProperty("id", out JsonElement idElement) ? idElement.GetString() : null;
            if (!string.Equals(id, packageId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!package.TryGetProperty("deprecation", out JsonElement deprecation)
                || deprecation.ValueKind != JsonValueKind.Object)
            {
                return new PackageInfo(packageId, IsDeprecated: false, DeprecationMessage: null, AlternatePackage: null);
            }

            string? message = GetString(deprecation, "message");
            string? alternate = deprecation.TryGetProperty("alternatePackage", out JsonElement alt)
                && alt.ValueKind == JsonValueKind.Object
                    ? GetString(alt, "id")
                    : null;

            return new PackageInfo(packageId, IsDeprecated: true, message, alternate);
        }

        return null;
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _http.Dispose();
        }
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
