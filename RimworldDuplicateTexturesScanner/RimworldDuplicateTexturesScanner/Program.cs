using Microsoft.Extensions.DependencyInjection;
using RimworldDuplicateTexturesScanner.Services;
using RimworldDuplicateTexturesScanner.Services.Interfaces;

namespace RimworldDuplicateTexturesScanner;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IApplicationDataPaths, ApplicationDataPaths>();
        services.AddSingleton<IActiveModReader, RimWorldActiveModReader>();
        services.AddSingleton<IModManifestReader, RimWorldModManifestReader>();
        services.AddSingleton<ITextureConflictScanner, RimWorldTextureConflictScanner>();
        services.AddSingleton<IIgnoredConflictSettingsStore, JsonIgnoredConflictSettingsStore>();
        services.AddSingleton<ITexturePreviewProvider, WpfTexturePreviewProvider>();

        services.AddTransient<IRimSortUserRuleEditor, JsonRimSortUserRuleEditor>();
        services.AddTransient<MainWindow>();

        using var serviceProvider = services.BuildServiceProvider();
        new System.Windows.Application().Run(serviceProvider.GetRequiredService<MainWindow>());
    }
}
