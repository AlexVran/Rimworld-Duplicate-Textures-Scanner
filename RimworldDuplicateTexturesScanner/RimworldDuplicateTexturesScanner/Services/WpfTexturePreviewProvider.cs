using System.Windows.Media.Imaging;
using RimworldDuplicateTexturesScanner.Services.Interfaces;

namespace RimworldDuplicateTexturesScanner.Services;

public sealed class WpfTexturePreviewProvider : ITexturePreviewProvider
{
    private static readonly HashSet<string> PreviewExtensions = new(StringComparer.OrdinalIgnoreCase) { ".png" };

    public BitmapImage? Load(string texturePath)
    {
        if (!PreviewExtensions.Contains(Path.GetExtension(texturePath))) return null;
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(texturePath);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}
