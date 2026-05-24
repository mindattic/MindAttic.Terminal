using MindAttic.Console.Models;
using MindAttic.Console.Services;
using MindAttic.Vault.Settings;
using NUnit.Framework;

namespace MindAttic.Console.Tests;

[TestFixture]
public sealed class SettingsStoreTests
{
    private string tempRoot = "";
    private SettingsStore subject = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "MindAttic.Console.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var store = new JsonSettingsStore<AppSettings>(Path.Combine(tempRoot, "settings"));
        subject = new SettingsStore(store);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(tempRoot, recursive: true); }
        catch { }
    }

    [Test]
    public void Load_returns_defaults_when_vault_is_empty()
    {
        var settings = subject.Load();

        Assert.That(settings, Is.Not.Null);
        Assert.That(settings.Projects, Is.Empty);
        Assert.That(settings.AgentProviders, Is.Empty);
    }

    [Test]
    public void Save_persists_settings_round_trip()
    {
        var input = new AppSettings
        {
            Provider = "Claude",
            Projects =
            {
                new Project { Name = "Alpha", Path = @"C:\a", Provider = "Claude" },
                new Project { Name = "Beta",  Path = @"C:\b" }
            }
        };

        subject.Save(input);
        var roundTripped = subject.Load();

        Assert.That(roundTripped.Provider, Is.EqualTo("Claude"));
        Assert.That(roundTripped.Projects, Has.Count.EqualTo(2));
        Assert.That(roundTripped.Projects[0].Name, Is.EqualTo("Alpha"));
    }

    [Test]
    public void Load_seeds_from_legacy_file_when_vault_is_empty()
    {
        var legacyPath = Path.Combine(tempRoot, "legacy-settings.json");
        File.WriteAllText(legacyPath, """
        {
            "Provider": "Claude",
            "AgentProviders": [
                { "Key": "Claude", "Name": "Claude Code", "RunCommand": "claude" }
            ],
            "Projects": [
                { "Name": "Alpha", "Path": "C:\\a", "Provider": "Claude" }
            ]
        }
        """);

        var freshStore = new JsonSettingsStore<AppSettings>(Path.Combine(tempRoot, "fresh-settings"));
        var seeded = new SettingsStore(freshStore, legacyPath);

        var loaded = seeded.Load();

        Assert.That(loaded.Provider, Is.EqualTo("Claude"));
        Assert.That(loaded.Projects, Has.Count.EqualTo(1));
        Assert.That(loaded.AgentProviders, Has.Count.EqualTo(1));
    }
}
