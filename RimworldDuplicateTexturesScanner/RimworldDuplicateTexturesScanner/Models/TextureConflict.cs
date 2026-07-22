namespace RimworldDuplicateTexturesScanner.Models;

public sealed record TextureConflict(string RelativePath, IReadOnlyList<TextureVariant> Variants);
