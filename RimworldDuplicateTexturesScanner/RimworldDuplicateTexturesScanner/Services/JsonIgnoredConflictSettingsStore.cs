using System.Text.Json;
using System.Text.Json.Nodes;
using RimworldDuplicateTexturesScanner.Models;
using RimworldDuplicateTexturesScanner.Services.Interfaces;

namespace RimworldDuplicateTexturesScanner.Services;

public sealed class JsonIgnoredConflictSettingsStore(string settingsPath) : IIgnoredConflictSettingsStore
{
    public JsonIgnoredConflictSettingsStore(IApplicationDataPaths applicationDataPaths)
        : this(applicationDataPaths.IgnoredModCombinationsPath)
    {
    }

    public IgnoredConflictSettings Load()
    {
        if (!File.Exists(settingsPath)) return new IgnoredConflictSettings([]);
        var root = JsonNode.Parse(File.ReadAllText(settingsPath))?.AsObject();
        var combinations = root?["packageIdCombinations"]?.AsArray()
            .OfType<JsonArray>()
            .Select(ReadPackageIds)
            .Where(packageIds => packageIds.Count > 1)
            .ToList() ?? [];
        return new IgnoredConflictSettings(combinations);
    }

    public void Save(IgnoredConflictSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(settingsPath, JsonSerializer.Serialize(new { packageIdCombinations = settings.PackageIdCombinations }));
    }

    private static IReadOnlyList<string> ReadPackageIds(JsonArray values) =>
    [
        .. values
            .OfType<JsonValue>()
            .Select(value => value.TryGetValue<string>(out var packageId) ? packageId : null)
            .Where(packageId => !string.IsNullOrWhiteSpace(packageId))
            .Select(packageId => packageId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(packageId => packageId, StringComparer.OrdinalIgnoreCase)
    ];
}
