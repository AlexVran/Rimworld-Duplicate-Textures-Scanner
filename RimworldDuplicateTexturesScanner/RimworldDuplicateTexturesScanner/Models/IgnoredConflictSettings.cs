namespace RimworldDuplicateTexturesScanner.Models;

public sealed record IgnoredConflictSettings(IReadOnlyList<IReadOnlyList<string>> PackageIdCombinations);
