using System.Windows.Media.Imaging;

namespace RimworldDuplicateTexturesScanner.Services.Interfaces;

public interface ITexturePreviewProvider
{
    BitmapImage? Load(string texturePath);
}
