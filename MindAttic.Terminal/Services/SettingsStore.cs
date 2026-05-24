using System.Text.Json;
using MindAttic.Terminal.Models;
using MindAttic.Vault.Settings;

namespace MindAttic.Terminal.Services;

/// <summary>
/// Loads and saves AppSettings via MindAttic.Vault. Settings live at
/// %APPDATA%\MindAttic\MindAttic.Terminal\settings.json.
/// </summary>
public sealed class SettingsStore
{
    public const string AppBucket = "MindAttic.Terminal";

    // Legacy file that the original PowerShell scripts read from. Used as a
    // one-time seed source if the Vault settings.json is missing on first run.
    public const string DefaultLegacySettingsPath = @"D:\Projects\MindAttic\settings.json";

    private static readonly JsonSerializerOptions LegacyReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly JsonSettingsStore<AppSettings> store;
    private readonly string? legacySettingsPath;

    public SettingsStore()
        : this(JsonSettingsStore<AppSettings>.ForApp(AppBucket), DefaultLegacySettingsPath)
    {
    }

    public SettingsStore(JsonSettingsStore<AppSettings> store)
        : this(store, legacySettingsPath: null)
    {
    }

    public SettingsStore(JsonSettingsStore<AppSettings> store, string? legacySettingsPath)
    {
        this.store = store;
        this.legacySettingsPath = legacySettingsPath;
    }

    public string SettingsFilePath => store.FilePath;

    public AppSettings Load()
    {
        if (!store.Exists())
            SeedFromLegacyIfPresent();

        return store.Load();
    }

    public void Save(AppSettings settings) => store.Save(settings);

    public AppSettings Update(Action<AppSettings> mutate) => store.Update(mutate);

    private void SeedFromLegacyIfPresent()
    {
        if (string.IsNullOrWhiteSpace(legacySettingsPath)) return;
        if (!File.Exists(legacySettingsPath)) return;

        try
        {
            var raw = File.ReadAllText(legacySettingsPath);
            if (string.IsNullOrWhiteSpace(raw)) return;

            var seed = JsonSerializer.Deserialize<AppSettings>(raw, LegacyReadOptions);
            if (seed is null) return;

            store.Save(seed);
        }
        catch
        {
            // Best-effort seeding. A malformed legacy file falls through to a
            // default-constructed AppSettings — same posture as JsonSettingsStore.Load.
        }
    }
}
