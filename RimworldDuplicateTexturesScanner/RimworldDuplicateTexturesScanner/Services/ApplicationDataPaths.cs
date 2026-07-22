using RimworldDuplicateTexturesScanner.Services.Interfaces;

namespace RimworldDuplicateTexturesScanner.Services;

public sealed class ApplicationDataPaths : IApplicationDataPaths
{
    public string IgnoredModCombinationsPath
    {
        get => Path.Combine(field, "ignored-mod-combinations.json");
    } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RimTextureInspector");
}
