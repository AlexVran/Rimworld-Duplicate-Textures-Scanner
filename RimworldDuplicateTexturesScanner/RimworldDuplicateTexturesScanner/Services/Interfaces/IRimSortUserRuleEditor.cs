using RimworldDuplicateTexturesScanner.Models;

namespace RimworldDuplicateTexturesScanner.Services.Interfaces;

public interface IRimSortUserRuleEditor
{
    bool HasUnsavedChanges { get; }
    void Load(string rulesFilePath);
    void AddLoadAfterRule(string packageId, IEnumerable<RimSortLoadAfterTarget> targets);
    bool RemoveRule(string packageId);
    IReadOnlyList<RimSortRuleSummary> GetRuleSummaries();
    void Save();
}
