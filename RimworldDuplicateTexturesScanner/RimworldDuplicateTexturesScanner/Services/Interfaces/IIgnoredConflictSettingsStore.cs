using RimworldDuplicateTexturesScanner.Models;

namespace RimworldDuplicateTexturesScanner.Services.Interfaces;

public interface IIgnoredConflictSettingsStore
{
    IgnoredConflictSettings Load();
    void Save(IgnoredConflictSettings settings);
}
