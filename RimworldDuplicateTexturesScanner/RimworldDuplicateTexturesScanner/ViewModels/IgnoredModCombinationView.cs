namespace RimworldDuplicateTexturesScanner.ViewModels;

public sealed class IgnoredModCombinationView(IReadOnlyList<string> packageIds)
{
    public IReadOnlyList<string> PackageIds { get; } = packageIds;
    public string DisplayText => string.Join(" + ", PackageIds);
}
