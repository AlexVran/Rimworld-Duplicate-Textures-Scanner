using RimworldDuplicateTexturesScanner.Models;
using RimworldDuplicateTexturesScanner.Services.Interfaces;

namespace RimworldDuplicateTexturesScanner.Services;

public sealed class RimWorldTextureConflictScanner(IModManifestReader manifestReader) : ITextureConflictScanner
{
    private static readonly HashSet<string> TextureExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png" };

    public Task<TextureScanResult> ScanAsync(IReadOnlyCollection<string> modLibraryPaths, IReadOnlySet<string> activePackageIds, IProgress<ScanProgress>? progress, CancellationToken cancellationToken) =>
        Task.Run(() => Scan(modLibraryPaths, activePackageIds, progress, cancellationToken), cancellationToken);

    private TextureScanResult Scan(IReadOnlyCollection<string> modLibraryPaths, IReadOnlySet<string> activePackageIds, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        var variantsByRelativePath = new Dictionary<string, List<TextureVariant>>(StringComparer.OrdinalIgnoreCase);
        var scannedPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var activeModCount = 0;
        var textureCount = 0;

        foreach (var modLibraryPath in modLibraryPaths)
        foreach (var modDirectory in EnumerateModDirectories(modLibraryPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var manifest = manifestReader.Read(modDirectory);
            if (!activePackageIds.Contains(manifest.PackageId)) continue;
            if (!scannedPackageIds.Add(manifest.PackageId)) continue;
            activeModCount++;

            foreach (var textureDirectory in Directory.EnumerateDirectories(modDirectory, "Textures", SearchOption.AllDirectories))
            foreach (var texturePath in EnumerateFiles(textureDirectory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TextureExtensions.Contains(Path.GetExtension(texturePath))) continue;
                textureCount++;
                var relativePath = Path.GetRelativePath(modDirectory, texturePath).Replace('\\', '/');
                if (!variantsByRelativePath.TryGetValue(relativePath, out var variants))
                {
                    variants = [];
                    variantsByRelativePath.Add(relativePath, variants);
                }
                variants.Add(new TextureVariant(manifest.Name, manifest.PackageId, relativePath, texturePath));
            }

            if (activeModCount % 10 == 0)
                progress?.Report(new ScanProgress($"Indexed {textureCount:N0} texture paths in {activeModCount:N0} active mods…"));
        }

        var conflicts = variantsByRelativePath.Values
            .Select(variants => variants.OrderBy(variant => variant.ModName).ToList())
            .Where(variants => variants.Select(variant => variant.PackageId).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .Select(variants => new TextureConflict(variants[0].RelativePath, variants))
            .OrderBy(conflict => conflict.RelativePath)
            .ToList();

        return new TextureScanResult(activeModCount, textureCount, conflicts);
    }

    private static IEnumerable<string> EnumerateModDirectories(string modLibraryPath)
    {
        if (File.Exists(Path.Combine(modLibraryPath, "About", "About.xml"))) yield return modLibraryPath;
        foreach (var directory in Directory.EnumerateDirectories(modLibraryPath))
            if (File.Exists(Path.Combine(directory, "About", "About.xml"))) yield return directory;
    }

    private static string[] EnumerateFiles(string directoryPath)
    {
        try
        {
            return [.. Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }
}
