using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using RimworldDuplicateTexturesScanner.Models;
using RimworldDuplicateTexturesScanner.Services.Interfaces;
using RimworldDuplicateTexturesScanner.ViewModels;
using Forms = System.Windows.Forms;

namespace RimworldDuplicateTexturesScanner;

public partial class MainWindow
{
    private readonly IActiveModReader _activeModReader;
    private readonly ITextureConflictScanner _textureConflictScanner;
    private readonly IIgnoredConflictSettingsStore _ignoredConflictSettingsStore;
    private readonly ITexturePreviewProvider _texturePreviewProvider;
    private readonly IRimSortUserRuleEditor _rimSortUserRuleEditor;
    private readonly ObservableCollection<TextureConflictView> _visibleConflicts = [];
    private readonly ObservableCollection<IgnoredModCombinationView> _ignoredModCombinations = [];
    private readonly ObservableCollection<RimSortRuleSummary> _ruleSummaries = [];
    private IReadOnlyList<TextureConflict> _scannedConflicts = [];
    private CancellationTokenSource? _scanCancellation;

    public MainWindow(
        IActiveModReader activeModReader,
        ITextureConflictScanner textureConflictScanner,
        IIgnoredConflictSettingsStore ignoredConflictSettingsStore,
        ITexturePreviewProvider texturePreviewProvider,
        IRimSortUserRuleEditor rimSortUserRuleEditor)
    {
        _activeModReader = activeModReader;
        _textureConflictScanner = textureConflictScanner;
        _ignoredConflictSettingsStore = ignoredConflictSettingsStore;
        _texturePreviewProvider = texturePreviewProvider;
        _rimSortUserRuleEditor = rimSortUserRuleEditor;
        InitializeComponent();
        DuplicateGroups.ItemsSource = _visibleConflicts;
        IgnoredCombinationsListBox.ItemsSource = _ignoredModCombinations;
        RulesListBox.ItemsSource = _ruleSummaries;
        WorkshopPathTextBox.Text = @"F:\SteamLibrary\steamapps\workshop\content\294100";
        LocalModsPathTextBox.Text = @"F:\SteamLibrary\steamapps\common\RimWorld\Mods";
        ConfigPathTextBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "Ludeon Studios", "RimWorld by Ludeon Studios", "Config", "ModsConfig.xml");
        RulesFilePathTextBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RimSort", "dbs", "userRules.json");
        LoadIgnoredModCombinations();
        LoadRimSortRules();
        StatusText.Text = "The scan is limited to mods enabled in ModsConfig.xml.";
    }

    private void BrowseWorkshopButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        dialog.Description = "Select the Steam Workshop RimWorld content folder";
        if (dialog.ShowDialog() == Forms.DialogResult.OK) WorkshopPathTextBox.Text = dialog.SelectedPath;
    }

    private void BrowseLocalModsButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        dialog.Description = "Select the RimWorld local Mods folder";
        if (dialog.ShowDialog() == Forms.DialogResult.OK) LocalModsPathTextBox.Text = dialog.SelectedPath;
    }

    private void BrowseConfigButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new OpenFileDialog();
        dialog.Filter = "RimWorld mod config|ModsConfig.xml|XML files|*.xml";
        dialog.FileName = "ModsConfig.xml";
        dialog.Title = "Select RimWorld ModsConfig.xml";
        if (dialog.ShowDialog() == Forms.DialogResult.OK) ConfigPathTextBox.Text = dialog.FileName;
    }

    private void BrowseRulesButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new OpenFileDialog();
        dialog.Filter = "RimSort user rules|userRules.json|JSON files|*.json";
        dialog.FileName = "userRules.json";
        dialog.Title = "Select RimSort userRules.json";
        if (dialog.ShowDialog() == Forms.DialogResult.OK) RulesFilePathTextBox.Text = dialog.FileName;
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs routedEvent)
    {
        var modLibraryPaths = new[] { WorkshopPathTextBox.Text, LocalModsPathTextBox.Text }
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (modLibraryPaths.Length == 0) { StatusText.Text = "Select an existing Steam Workshop folder, local Mods folder, or both."; return; }
        if (!File.Exists(ConfigPathTextBox.Text)) { StatusText.Text = "Select an existing RimWorld ModsConfig.xml file."; return; }

        IReadOnlySet<string> activePackageIds;
        try { activePackageIds = _activeModReader.ReadPackageIds(ConfigPathTextBox.Text); }
        catch (Exception exception) { StatusText.Text = $"Could not read ModsConfig.xml: {exception.Message}"; return; }
        if (activePackageIds.Count == 0) { StatusText.Text = "ModsConfig.xml does not contain active mods."; return; }

        SetScanControls(false);
        StatusText.Text = "Scanning texture paths…";
        _scanCancellation = new CancellationTokenSource();
        try
        {
            var progress = new Progress<ScanProgress>(scanProgress => StatusText.Text = scanProgress.Message);
            var scanResult = await _textureConflictScanner.ScanAsync(modLibraryPaths, activePackageIds, progress, _scanCancellation.Token);
            _scannedConflicts = scanResult.Conflicts;
            RefreshVisibleConflicts();
            StatusText.Text = $"Scanned {scanResult.TextureCount:N0} textures in {scanResult.ActiveModCount:N0} active mods. Showing {_visibleConflicts.Count:N0} conflicting texture paths.";
        }
        catch (OperationCanceledException) { StatusText.Text = "Scan cancelled."; }
        catch (Exception exception) { StatusText.Text = $"Scan failed: {exception.Message}"; }
        finally
        {
            _scanCancellation.Dispose();
            _scanCancellation = null;
            SetScanControls(true);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => _scanCancellation?.Cancel();

    private void DuplicateGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DuplicateGroups.SelectedItem is not TextureConflictView conflictView)
        {
            CopiesList.ItemsSource = null;
            OrderHint.Text = "";
            StageRuleButton.IsEnabled = false;
            IgnoreCombinationButton.IsEnabled = false;
            return;
        }

        CopiesList.ItemsSource = conflictView.Copies.Select(variant => new TextureVariantView(variant, _texturePreviewProvider.Load(variant.FullPath))).ToList();
        CopiesList.SelectedIndex = -1;
        OrderHint.Text = "Select the mod whose texture you want to keep visible in-game.";
        IgnoreCombinationButton.IsEnabled = true;
    }

    private void CopiesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        StageRuleButton.IsEnabled = CopiesList.SelectedItem is TextureVariantView;
        if (CopiesList.SelectedItem is not TextureVariantView preferredVariantView || DuplicateGroups.SelectedItem is not TextureConflictView conflictView) return;
        var overriddenMods = conflictView.Copies.Where(variant => !string.Equals(variant.PackageId, preferredVariantView.PackageId, StringComparison.OrdinalIgnoreCase)).Select(variant => $"{variant.ModName} ({variant.PackageId})");
        OrderHint.Text = $"Preferred: {preferredVariantView.ModName} ({preferredVariantView.PackageId}). RimWorld normally lets later-loaded mods override earlier ones, so place this mod AFTER: {string.Join(", ", overriddenMods)}.";
    }

    private void IgnoreCombinationButton_Click(object sender, RoutedEventArgs e)
    {
        if (DuplicateGroups.SelectedItem is not TextureConflictView conflictView) return;
        AddIgnoredModCombination(conflictView.Copies.Select(variant => variant.PackageId));
    }

    private void StageRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (CopiesList.SelectedItem is not TextureVariantView preferredVariantView || DuplicateGroups.SelectedItem is not TextureConflictView conflictView) return;
        try
        {
            _rimSortUserRuleEditor.AddLoadAfterRule(preferredVariantView.PackageId, conflictView.Copies.Select(variant => new RimSortLoadAfterTarget(variant.PackageId, variant.ModName)));
            RefreshRuleSummaries();
            RulesStatusText.Text = $"Staged {preferredVariantView.PackageId} after the other providers of {conflictView.DisplayName}.";
        }
        catch (Exception exception) { RulesStatusText.Text = $"Could not stage rule: {exception.Message}"; }
    }

    private void ReloadRulesButton_Click(object sender, RoutedEventArgs e) => LoadRimSortRules();

    private void SaveRulesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _rimSortUserRuleEditor.Save();
            RefreshRuleSummaries();
            RulesStatusText.Text = "Saved staged changes to userRules.json.";
        }
        catch (Exception exception) { RulesStatusText.Text = $"Could not save rules: {exception.Message}"; }
    }

    private void RulesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => RemoveRuleButton.IsEnabled = RulesListBox.SelectedItem is RimSortRuleSummary;

    private void RemoveRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (RulesListBox.SelectedItem is not RimSortRuleSummary selectedRule) return;
        try
        {
            if (!_rimSortUserRuleEditor.RemoveRule(selectedRule.PackageId)) return;
            RefreshRuleSummaries();
            RulesStatusText.Text = $"Staged removal of {selectedRule.PackageId}.";
        }
        catch (Exception exception) { RulesStatusText.Text = $"Could not remove rule: {exception.Message}"; }
    }

    private void IgnoredCombinationsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => RemoveIgnoredCombinationButton.IsEnabled = IgnoredCombinationsListBox.SelectedItem is IgnoredModCombinationView;

    private void RemoveIgnoredCombinationButton_Click(object sender, RoutedEventArgs e)
    {
        if (IgnoredCombinationsListBox.SelectedItem is not IgnoredModCombinationView selectedCombination) return;
        _ignoredModCombinations.Remove(selectedCombination);
        SaveIgnoredModCombinations();
        RefreshVisibleConflicts();
    }

    private void AddIgnoredModCombination(IEnumerable<string> packageIds)
    {
        var combination = NormalizePackageIds(packageIds);
        if (combination.Count < 2 || _ignoredModCombinations.Any(view => HaveSamePackageIds(view.PackageIds, combination))) return;
        _ignoredModCombinations.Add(new IgnoredModCombinationView(combination));
        SaveIgnoredModCombinations();
        RefreshVisibleConflicts();
    }

    private void RefreshVisibleConflicts()
    {
        CopiesList.ItemsSource = null;
        OrderHint.Text = "";
        IgnoreCombinationButton.IsEnabled = false;
        StageRuleButton.IsEnabled = false;
        _visibleConflicts.Clear();
        foreach (var conflict in _scannedConflicts)
        {
            var combination = NormalizePackageIds(conflict.Variants.Select(variant => variant.PackageId));
            if (!_ignoredModCombinations.Any(view => HaveSamePackageIds(view.PackageIds, combination)))
                _visibleConflicts.Add(new TextureConflictView(conflict));
        }
    }

    private void LoadIgnoredModCombinations()
    {
        try
        {
            foreach (var combination in _ignoredConflictSettingsStore.Load().PackageIdCombinations.Select(NormalizePackageIds).Where(combination => combination.Count > 1).DistinctBy(combination => string.Join("|", combination), StringComparer.OrdinalIgnoreCase))
                _ignoredModCombinations.Add(new IgnoredModCombinationView(combination));
        }
        catch (Exception exception) { StatusText.Text = $"Could not load ignored combinations: {exception.Message}"; }
    }

    private void LoadRimSortRules()
    {
        try
        {
            _rimSortUserRuleEditor.Load(RulesFilePathTextBox.Text);
            RefreshRuleSummaries();
            RulesStatusText.Text = _ruleSummaries.Count == 0 ? "No RimSort rules are defined yet." : $"Loaded {_ruleSummaries.Count:N0} RimSort rules.";
        }
        catch (Exception exception)
        {
            _ruleSummaries.Clear();
            SaveRulesButton.IsEnabled = false;
            RulesStatusText.Text = $"Could not load rules: {exception.Message}";
        }
    }

    private void RefreshRuleSummaries()
    {
        _ruleSummaries.Clear();
        foreach (var summary in _rimSortUserRuleEditor.GetRuleSummaries()) _ruleSummaries.Add(summary);
        SaveRulesButton.IsEnabled = _rimSortUserRuleEditor.HasUnsavedChanges;
        RemoveRuleButton.IsEnabled = false;
    }

    private void SaveIgnoredModCombinations()
    {
        try { _ignoredConflictSettingsStore.Save(new IgnoredConflictSettings(_ignoredModCombinations.Select(view => view.PackageIds).ToList())); }
        catch (Exception exception) { StatusText.Text = $"Could not save ignored combinations: {exception.Message}"; }
    }

    private void SetScanControls(bool canStartScan)
    {
        ScanButton.IsEnabled = canStartScan;
        CancelButton.IsEnabled = !canStartScan;
    }

    private static IReadOnlyList<string> NormalizePackageIds(IEnumerable<string> packageIds) =>
    [
        .. packageIds
            .Where(packageId => !string.IsNullOrWhiteSpace(packageId))
            .Select(packageId => packageId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(packageId => packageId, StringComparer.OrdinalIgnoreCase)
    ];

    private static bool HaveSamePackageIds(IReadOnlyList<string> first, IReadOnlyList<string> second) => first.Count == second.Count && first.SequenceEqual(second, StringComparer.OrdinalIgnoreCase);
}
