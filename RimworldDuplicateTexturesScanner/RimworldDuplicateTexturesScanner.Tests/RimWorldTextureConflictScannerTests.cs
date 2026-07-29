using RimworldDuplicateTexturesScanner.Services;

namespace RimworldDuplicateTexturesScanner.Tests;

[TestFixture]
public sealed class RimWorldTextureConflictScannerTests
{
    [Test]
    public async Task ScanAsync_ReturnsOnlyTexturePathsProvidedByMultipleActiveMods()
    {
        using var directory = new TemporaryDirectory();
        CreateMod(directory, "First", "example.first", "Textures/Shared/Chair.png", "first");
        CreateMod(directory, "Second", "example.second", "Textures/Shared/Chair.png", "second");
        CreateMod(directory, "Inactive", "example.inactive", "Textures/Shared/Chair.png", "inactive");
        CreateMod(directory, "Different", "example.different", "Textures/Shared/Table.png", "different");

        var scanner = new RimWorldTextureConflictScanner(new RimWorldModManifestReader());
        var activePackageIds = new HashSet<string>(["example.first", "example.second", "example.different"], StringComparer.OrdinalIgnoreCase);

        var result = await scanner.ScanAsync([directory.Path], activePackageIds, null, CancellationToken.None);

        var conflict = result.Conflicts.Single();
        Assert.That(conflict.RelativePath, Is.EqualTo("Textures/Shared/Chair.png"));
        Assert.That(conflict.Variants, Has.Count.EqualTo(2));
        Assert.That(conflict.Variants.Select(variant => variant.PackageId).OrderBy(packageId => packageId), Is.EqualTo(["example.first", "example.second"]));
    }

    [Test]
    public async Task ScanAsync_IgnoresDdsFiles()
    {
        using var directory = new TemporaryDirectory();
        CreateMod(directory, "First", "example.first", "Textures/Shared/Chair.dds", "first");
        CreateMod(directory, "Second", "example.second", "Textures/Shared/Chair.dds", "second");

        var scanner = new RimWorldTextureConflictScanner(new RimWorldModManifestReader());
        var activePackageIds = new HashSet<string>(["example.first", "example.second"], StringComparer.OrdinalIgnoreCase);

        var result = await scanner.ScanAsync([directory.Path], activePackageIds, null, CancellationToken.None);

        Assert.That(result.TextureCount, Is.Zero);
        Assert.That(result.Conflicts, Is.Empty);
    }

    [Test]
    public async Task ScanAsync_CombinesActiveModsFromWorkshopAndLocalRoots()
    {
        using var workshop = new TemporaryDirectory();
        using var local = new TemporaryDirectory();
        CreateMod(workshop, "WorkshopMod", "example.workshop", "Textures/Shared/Chair.png", "workshop");
        CreateMod(local, "LocalMod", "example.local", "Textures/Shared/Chair.png", "local");

        var scanner = new RimWorldTextureConflictScanner(new RimWorldModManifestReader());
        var activePackageIds = new HashSet<string>(["example.workshop", "example.local"], StringComparer.OrdinalIgnoreCase);

        var result = await scanner.ScanAsync([workshop.Path, local.Path], activePackageIds, null, CancellationToken.None);

        var conflict = result.Conflicts.Single();
        Assert.That(result.ActiveModCount, Is.EqualTo(2));
        Assert.That(conflict.Variants.Select(variant => variant.PackageId), Is.EquivalentTo(["example.workshop", "example.local"]));
    }

    private static void CreateMod(TemporaryDirectory directory, string folderName, string packageId, string texturePath, string textureContent)
    {
        directory.CreateFile($"{folderName}/About/About.xml", $"<ModMetaData><name>{folderName}</name><packageId>{packageId}</packageId></ModMetaData>");
        directory.CreateFile($"{folderName}/{texturePath}", textureContent);
    }
}
