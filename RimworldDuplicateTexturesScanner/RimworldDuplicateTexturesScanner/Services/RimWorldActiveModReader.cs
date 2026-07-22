using System.Xml.Linq;
using RimworldDuplicateTexturesScanner.Services.Interfaces;

namespace RimworldDuplicateTexturesScanner.Services;

public sealed class RimWorldActiveModReader : IActiveModReader
{
    public IReadOnlySet<string> ReadPackageIds(string configurationPath) => XDocument.Load(configurationPath)
        .Descendants("activeMods")
        .Elements("li")
        .Select(element => element.Value.Trim())
        .Where(packageId => !string.IsNullOrWhiteSpace(packageId))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
