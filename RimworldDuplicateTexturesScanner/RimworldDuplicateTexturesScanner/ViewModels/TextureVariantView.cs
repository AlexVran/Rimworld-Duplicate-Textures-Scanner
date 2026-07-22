using System.Windows.Media.Imaging;
using RimworldDuplicateTexturesScanner.Models;

namespace RimworldDuplicateTexturesScanner.ViewModels;

public sealed class TextureVariantView(TextureVariant variant, BitmapImage? preview)
{
    public string ModName => variant.ModName;
    public string PackageId => variant.PackageId;
    public string RelativePath => variant.RelativePath;
    public string FullPath => variant.FullPath;
    public BitmapImage? Preview { get; } = preview;
}
