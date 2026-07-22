using RimworldDuplicateTexturesScanner.Services;

namespace RimworldDuplicateTexturesScanner.Tests;

[TestFixture]
public sealed class RimWorldActiveModReaderTests
{
    [Test]
    public void ReadPackageIds_ReturnsAllActivePackageIdsWithoutCaseDuplicates()
    {
        using var directory = new TemporaryDirectory();
        var configurationPath = directory.CreateFile("ModsConfig.xml", """
            <ModsConfigData>
              <activeMods>
                <li>Example.First</li>
                <li>example.first</li>
                <li>Example.Second</li>
              </activeMods>
            </ModsConfigData>
            """);

        var packageIds = new RimWorldActiveModReader().ReadPackageIds(configurationPath);

        Assert.That(packageIds, Has.Count.EqualTo(2));
        Assert.That(packageIds.Contains("example.first", StringComparer.OrdinalIgnoreCase), Is.True);
        Assert.That(packageIds.Contains("example.second", StringComparer.OrdinalIgnoreCase), Is.True);
    }
}
