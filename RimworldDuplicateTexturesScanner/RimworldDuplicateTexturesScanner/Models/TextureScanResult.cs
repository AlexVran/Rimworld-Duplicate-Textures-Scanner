namespace RimworldDuplicateTexturesScanner.Models;

public sealed record TextureScanResult(int ActiveModCount, int TextureCount, IReadOnlyList<TextureConflict> Conflicts);
