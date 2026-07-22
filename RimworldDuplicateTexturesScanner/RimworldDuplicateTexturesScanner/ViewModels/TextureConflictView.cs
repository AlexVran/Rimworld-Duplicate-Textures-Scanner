using RimworldDuplicateTexturesScanner.Models;

namespace RimworldDuplicateTexturesScanner.ViewModels;

public sealed class TextureConflictView(TextureConflict conflict)
{
    public string DisplayName => conflict.RelativePath;
    public IReadOnlyList<TextureVariant> Copies => conflict.Variants;
    public string ModSummary => string.Join(", ", conflict.Variants.Select(variant => variant.ModName).Distinct().Take(3)) + (conflict.Variants.Select(variant => variant.ModName).Distinct().Skip(3).Any() ? "…" : "");
}
