namespace RimworldDuplicateTexturesScanner.Services.Interfaces;

public interface IActiveModReader
{
    IReadOnlySet<string> ReadPackageIds(string configurationPath);
    IReadOnlyList<string> ReadPackageIdsInLoadOrder(string configurationPath);
}
