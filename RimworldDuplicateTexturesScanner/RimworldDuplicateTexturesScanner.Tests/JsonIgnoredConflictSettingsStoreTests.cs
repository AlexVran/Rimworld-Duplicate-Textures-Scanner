using RimworldDuplicateTexturesScanner.Models;
using RimworldDuplicateTexturesScanner.Services;

namespace RimworldDuplicateTexturesScanner.Tests;

[TestFixture]
public sealed class JsonIgnoredConflictSettingsStoreTests
{
    [Test]
    public void Load_ReturnsSavedPackageIdCombinationsInNormalizedOrder()
    {
        using var directory = new TemporaryDirectory();
        var store = new JsonIgnoredConflictSettingsStore(Path.Combine(directory.Path, "settings.json"));
        store.Save(new IgnoredConflictSettings([["birdup.mod", "birdup.continued"]]));

        var settings = store.Load();

        Assert.That(settings.PackageIdCombinations, Has.Count.EqualTo(1));
        Assert.That(settings.PackageIdCombinations[0], Is.EqualTo(["birdup.continued", "birdup.mod"]));
    }

    [Test]
    public void Load_ReturnsEmptySettingsWhenNoSettingsFileExists()
    {
        using var directory = new TemporaryDirectory();
        var store = new JsonIgnoredConflictSettingsStore(Path.Combine(directory.Path, "settings.json"));

        var settings = store.Load();

        Assert.That(settings.PackageIdCombinations, Is.Empty);
    }
}
