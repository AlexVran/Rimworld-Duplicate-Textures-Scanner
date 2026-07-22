using RimworldDuplicateTexturesScanner.Models;

namespace RimworldDuplicateTexturesScanner.Services.Interfaces;

public interface ITextureConflictScanner
{
    Task<TextureScanResult> ScanAsync(IReadOnlyCollection<string> modLibraryPaths, IReadOnlySet<string> activePackageIds, IProgress<ScanProgress>? progress, CancellationToken cancellationToken);
}
