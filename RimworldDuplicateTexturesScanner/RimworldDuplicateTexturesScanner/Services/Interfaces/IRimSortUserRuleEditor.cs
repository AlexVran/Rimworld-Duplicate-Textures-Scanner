using RimworldDuplicateTexturesScanner.Models;

namespace RimworldDuplicateTexturesScanner.Services.Interfaces;

public interface IRimSortUserRuleEditor
{
    bool HasUnsavedChanges { get; }
    void Load(string rulesFilePath);
    void AddLoadAfterRule(string packageId, IEnumerable<RimSortLoadAfterTarget> targets);
    bool RemoveRule(string packageId);
    bool RemoveAllRules();
    IReadOnlyList<RimSortRuleSummary> GetRuleSummaries();
    void Save();
}
