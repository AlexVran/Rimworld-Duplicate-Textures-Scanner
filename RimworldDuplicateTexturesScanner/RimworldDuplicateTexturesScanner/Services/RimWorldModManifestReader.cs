using System.Xml.Linq;
using RimworldDuplicateTexturesScanner.Services.Interfaces;

namespace RimworldDuplicateTexturesScanner.Services;

public sealed class RimWorldModManifestReader : IModManifestReader
{
    public ModManifest Read(string modDirectory)
    {
        try
        {
            var manifest = XDocument.Load(Path.Combine(modDirectory, "About", "About.xml")).Root;
            var packageId = manifest?.Element("packageId")?.Value.Trim();
            var name = manifest?.Element("name")?.Value.Trim();
            return new ModManifest(
                string.IsNullOrWhiteSpace(name) ? Path.GetFileName(modDirectory) : name,
                string.IsNullOrWhiteSpace(packageId) ? "(no packageId)" : packageId,
                ReadPackageIds(manifest, "loadAfter"),
                ReadPackageIds(manifest, "loadBefore"));
        }
        catch
        {
            return new ModManifest(Path.GetFileName(modDirectory), "(unreadable About.xml)", [], []);
        }
    }

    private static IReadOnlyList<string> ReadPackageIds(XElement? manifest, string elementName)
    {
        if (manifest?.Element(elementName) is not { } element) return [];
        return
        [
            .. element.Elements("li")
                .Select(item => item.Value.Trim())
                .Where(packageId => !string.IsNullOrWhiteSpace(packageId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
        ];
    }
}
