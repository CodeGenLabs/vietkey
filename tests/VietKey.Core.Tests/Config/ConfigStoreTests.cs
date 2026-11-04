using VietKey.Core.Config;
using Xunit;

namespace VietKey.Core.Tests.Config;

public sealed class ConfigStoreTests : IDisposable
{
    private readonly string _tempDir;

    public ConfigStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "VietKeyTests_" + Guid.NewGuid().ToString("N"));
    }

    // @covers REQ-settings-2
    [Fact]
    public void Load_WhenFileDoesNotExist_ReturnsAndWritesDefaultConfig()
    {
        var store = new ConfigStore(_tempDir);

        var config = store.Load(out bool wasReset);

        Assert.True(wasReset);
        Assert.Equal(InputMethod.Telex, config.InputMethod);
        Assert.True(config.VietnameseEnabled);
        Assert.True(config.ModernTone);
        Assert.True(config.SpellCheck);
        Assert.True(config.AutoRestoreEnglish);
        Assert.True(config.SmartSwitch);
        Assert.True(config.BrowserAutocompleteFix);
        Assert.Equal(ToggleHotkey.AltZ, config.ToggleHotkey);
        Assert.True(File.Exists(store.ConfigFilePath));
    }

    // @covers REQ-settings-2
    [Fact]
    public void Load_WhenFileContainsCorruptedJson_FallsBackToSafeDefaults()
    {
        Directory.CreateDirectory(_tempDir);
        var store = new ConfigStore(_tempDir);
        File.WriteAllText(store.ConfigFilePath, "{ invalid json !!!");

        var config = store.Load(out bool wasReset);

        Assert.True(wasReset);
        Assert.Equal(AppConfig.Default, config);
        var reloaded = store.Load(out bool wasResetAgain);
        Assert.False(wasResetAgain);
        Assert.Equal(AppConfig.Default, reloaded);
    }

    // @covers REQ-settings-2
    [Fact]
    public void SaveAndLoad_RoundTripsConfigurationAndMacros()
    {
        var store = new ConfigStore(_tempDir);
        var custom = AppConfig.Default with
        {
            InputMethod = InputMethod.Vni,
            ModernTone = false,
            ToggleHotkey = ToggleHotkey.CtrlShift
        };

        store.Save(custom);
        var loaded = store.Load(out bool wasReset);

        Assert.False(wasReset);
        Assert.Equal(custom, loaded);

        var macros = new Dictionary<string, string>
        {
            ["vn"] = "Việt Nam",
            ["hcm"] = "Thành phố Hồ Chí Minh"
        };
        store.SaveMacros(macros);
        var loadedMacros = store.LoadMacros();
        Assert.Equal(2, loadedMacros.Count);
        Assert.Equal("Việt Nam", loadedMacros["vn"]);
    }

    // @covers REQ-settings-2
    [Fact]
    public async Task ConfigReloadCoordinator_WhenSignaled_ReloadsUpdatedConfigWithin50Ms()
    {
        var store = new ConfigStore(_tempDir);
        string eventName = @"Local\VietKeyTestReload_" + Guid.NewGuid().ToString("N");
        using var coordinator = new ConfigReloadCoordinator(store, eventName);
        var tcs = new TaskCompletionSource<AppConfig>(TaskCreationOptions.RunContinuationsAsynchronously);
        coordinator.ConfigReloaded += (cfg, _) => tcs.TrySetResult(cfg);
        coordinator.StartListening();

        var updated = AppConfig.Default with { InputMethod = InputMethod.Vni, VietnameseEnabled = false };
        store.Save(updated);
        ConfigReloadCoordinator.SignalReload(eventName);

        var reloaded = await tcs.Task.WaitAsync(TimeSpan.FromMilliseconds(500));
        Assert.Equal(InputMethod.Vni, reloaded.InputMethod);
        Assert.False(reloaded.VietnameseEnabled);
        Assert.Equal(updated, coordinator.CurrentConfig);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
