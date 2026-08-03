using System.Text.Json;
using System.Text.Json.Nodes;
using RimworldDuplicateTexturesScanner.Models;
using RimworldDuplicateTexturesScanner.Services.Interfaces;

namespace RimworldDuplicateTexturesScanner.Services;

public sealed class JsonRimSortUserRuleEditor : IRimSortUserRuleEditor
{
    private JsonObject _document = CreateEmptyDocument();
    private string? _rulesFilePath;

    public bool HasUnsavedChanges { get; private set; }

    public void Load(string filePath)
    {
        var document = ReadDocument(filePath);
        _rulesFilePath = filePath;
        _document = document;
        EnsureRulesNode();
        HasUnsavedChanges = false;
    }

    public void AddLoadAfterRule(string packageId, IEnumerable<RimSortLoadAfterTarget> targets)
    {
        var targetPackageId = packageId.Trim();
        if (string.IsNullOrWhiteSpace(targetPackageId)) return;
        var requiredTargets = targets
            .Where(target => !string.IsNullOrWhiteSpace(target.PackageId) && !string.Equals(target.PackageId, targetPackageId, StringComparison.OrdinalIgnoreCase))
            .GroupBy(target => target.PackageId.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new RimSortLoadAfterTarget(group.Key, group.First().Name.Trim()))
            .ToList();
        if (requiredTargets.Count == 0) return;

        var rule = FindRule(targetPackageId);
        if (rule is null)
        {
            rule = CreateRule(targetPackageId);
            HasUnsavedChanges = true;
        }

        switch (Rules)
        {
            case JsonArray:
                AddArrayLoadAfterTargets(rule, requiredTargets);
                break;
            case JsonObject:
                AddObjectLoadAfterTargets(rule, requiredTargets);
                break;
        }
    }

    public IReadOnlyList<RimSortRuleSummary> GetRuleSummaries() =>
    [
        .. GetRuleSummaries(Rules)
            .OrderBy(summary => summary.DisplayText, StringComparer.OrdinalIgnoreCase)
    ];

    public bool RemoveRule(string packageId)
    {
        var removed = Rules switch
        {
            JsonArray rules => RemoveArrayRule(rules, packageId),
            JsonObject rules => RemoveObjectRule(rules, packageId),
            _ => false
        };
        HasUnsavedChanges |= removed;
        return removed;
    }

    public void Save()
    {
        if (_rulesFilePath is null) throw new InvalidOperationException("Load a RimSort rule file before saving.");
        Directory.CreateDirectory(Path.GetDirectoryName(_rulesFilePath)!);
        File.WriteAllText(_rulesFilePath, _document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        HasUnsavedChanges = false;
    }

    private JsonNode Rules => EnsureRulesNode();

    private static JsonObject ReadDocument(string filePath)
    {
        if (!File.Exists(filePath) || string.IsNullOrWhiteSpace(File.ReadAllText(filePath))) return CreateEmptyDocument();
        return JsonNode.Parse(File.ReadAllText(filePath)) as JsonObject ?? throw new InvalidDataException("RimSort userRules.json must contain a JSON object.");
    }

    private static JsonObject CreateEmptyDocument() => new() { ["rules"] = new JsonArray() };

    private JsonNode EnsureRulesNode()
    {
        if (_document["rules"] is JsonArray or JsonObject) return _document["rules"]!;
        if (_document["rules"] is not null) throw new InvalidDataException("The rules property must be a JSON array or object.");
        var createdRules = new JsonArray();
        _document["rules"] = createdRules;
        return createdRules;
    }

    private JsonObject? FindRule(string packageId) => Rules switch
    {
        JsonArray rules => rules.OfType<JsonObject>().FirstOrDefault(rule => string.Equals(rule["packageId"]?.GetValue<string>(), packageId, StringComparison.OrdinalIgnoreCase)),
        JsonObject rules => rules.FirstOrDefault(property => string.Equals(property.Key, packageId, StringComparison.OrdinalIgnoreCase)).Value as JsonObject,
        _ => null
    };

    private JsonObject CreateRule(string packageId)
    {
        var rule = new JsonObject();
        switch (Rules)
        {
            case JsonArray rules:
                rule["packageId"] = packageId;
                rules.Add(rule);
                break;
            case JsonObject rules:
                rules[packageId] = rule;
                break;
        }
        return rule;
    }

    private static bool RemoveArrayRule(JsonArray rules, string packageId)
    {
        var rule = rules.OfType<JsonObject>().FirstOrDefault(item => string.Equals(item["packageId"]?.GetValue<string>(), packageId, StringComparison.OrdinalIgnoreCase));
        return rule is not null && rules.Remove(rule);
    }

    private static bool RemoveObjectRule(JsonObject rules, string packageId)
    {
        var property = rules.FirstOrDefault(item => string.Equals(item.Key, packageId, StringComparison.OrdinalIgnoreCase));
        return property.Key is not null && rules.Remove(property.Key);
    }

    private static JsonArray GetOrCreateStringArray(JsonObject rule, string propertyName)
    {
        if (rule[propertyName] is JsonArray values) return values;
        if (rule[propertyName] is not null) throw new InvalidDataException($"The {propertyName} property must be a JSON array.");
        var createdValues = new JsonArray();
        rule[propertyName] = createdValues;
        return createdValues;
    }

    private void AddArrayLoadAfterTargets(JsonObject rule, IReadOnlyList<RimSortLoadAfterTarget> targets)
    {
        var loadAfter = GetOrCreateStringArray(rule, "loadTheseAfter");
        var existingPackageIds = ReadStringValues(loadAfter).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var target in targets)
            if (existingPackageIds.Add(target.PackageId))
            {
                loadAfter.Add(target.PackageId);
                HasUnsavedChanges = true;
            }
    }

    private void AddObjectLoadAfterTargets(JsonObject rule, IReadOnlyList<RimSortLoadAfterTarget> targets)
    {
        var loadAfter = GetOrCreateObject(rule, "loadAfter");
        foreach (var target in targets)
            if (!loadAfter.ContainsKey(target.PackageId))
            {
                loadAfter[target.PackageId] = new JsonObject { ["comment"] = string.Empty, ["name"] = target.Name };
                HasUnsavedChanges = true;
            }
    }

    private static JsonObject GetOrCreateObject(JsonObject rule, string propertyName)
    {
        if (rule[propertyName] is JsonObject values) return values;
        if (rule[propertyName] is not null) throw new InvalidDataException($"The {propertyName} property must be a JSON object.");
        var createdValues = new JsonObject();
        rule[propertyName] = createdValues;
        return createdValues;
    }

    private static IEnumerable<string> ReadStringValues(JsonArray values) => values
        .OfType<JsonValue>()
        .Select(value => value.TryGetValue<string>(out var text) ? text : null)
        .Where(text => !string.IsNullOrWhiteSpace(text))!;

    private static IEnumerable<RimSortRuleSummary> GetRuleSummaries(JsonNode rules) => rules switch
    {
        JsonArray array => array.OfType<JsonObject>().Select(rule => CreateSummary(rule, rule["packageId"]?.GetValue<string>() ?? "(rule without packageId)")),
        JsonObject map => map.Where(property => property.Value is JsonObject).Select(property => CreateSummary((JsonObject)property.Value!, property.Key)),
        _ => []
    };

    private static RimSortRuleSummary CreateSummary(JsonObject rule, string packageId)
    {
        var constraints = rule
            .Where(property => property.Key != "packageId")
            .Select(CreateConstraintSummary)
            .Where(summary => summary is not null);
        return new RimSortRuleSummary(packageId, $"{packageId} — {string.Join(" | ", constraints)}");
    }

    private static string? CreateConstraintSummary(KeyValuePair<string, JsonNode?> property) => property.Value switch
    {
        JsonArray values => $"{property.Key}: {string.Join(", ", ReadStringValues(values))}",
        JsonObject values => $"{property.Key}: {string.Join(", ", values.Select(entry => entry.Key))}",
        _ => null
    };
}
