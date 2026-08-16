namespace RimworldDuplicateTexturesScanner.Services.Interfaces;

public interface IModManifestReader
{
    ModManifest Read(string modDirectory);
}

public sealed record ModManifest(
    string Name,
    string PackageId,
    IReadOnlyList<string> LoadAfterPackageIds,
    IReadOnlyList<string> LoadBeforePackageIds);
