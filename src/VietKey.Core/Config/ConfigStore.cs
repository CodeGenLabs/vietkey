using System.Text.Json;

namespace VietKey.Core.Config;

public sealed class ConfigStore
{
    public string ConfigDirectory { get; }
    public string ConfigFilePath => Path.Combine(ConfigDirectory, "config.json");
    public string MacrosFilePath => Path.Combine(ConfigDirectory, "macros.json");

    public ConfigStore(string? configDirectory = null)
    {
        ConfigDirectory = configDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VietKey");
    }

    public AppConfig Load(out bool wasResetToDefault)
    {
        wasResetToDefault = false;
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                wasResetToDefault = true;
                Save(AppConfig.Default);
                return AppConfig.Default;
            }

            string json = File.ReadAllText(ConfigFilePath);
            var parsed = JsonSerializer.Deserialize(json, VietKeyJsonContext.Default.AppConfig);
            if (parsed is null)
            {
                wasResetToDefault = true;
                Save(AppConfig.Default);
                return AppConfig.Default;
            }

            return parsed;
        }
        catch
        {
            wasResetToDefault = true;
            try
            {
                Save(AppConfig.Default);
            }
            catch
            {
                // Ignore secondary write failure when falling back in memory
            }
            return AppConfig.Default;
        }
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(ConfigDirectory);
        string tempPath = ConfigFilePath + ".tmp";
        string json = JsonSerializer.Serialize(config, VietKeyJsonContext.Default.AppConfig);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, ConfigFilePath, overwrite: true);
    }

    public Dictionary<string, string> LoadMacros()
    {
        try
        {
            if (!File.Exists(MacrosFilePath))
            {
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }

            string json = File.ReadAllText(MacrosFilePath);
            var dict = JsonSerializer.Deserialize(json, VietKeyJsonContext.Default.DictionaryStringString);
            return dict is not null
                ? new Dictionary<string, string>(dict, StringComparer.Ordinal)
                : new Dictionary<string, string>(StringComparer.Ordinal);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    public void SaveMacros(IReadOnlyDictionary<string, string> macros)
    {
        Directory.CreateDirectory(ConfigDirectory);
        string tempPath = MacrosFilePath + ".tmp";
        var serializable = macros as Dictionary<string, string>
            ?? new Dictionary<string, string>(macros, StringComparer.Ordinal);
        string json = JsonSerializer.Serialize(serializable, VietKeyJsonContext.Default.DictionaryStringString);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, MacrosFilePath, overwrite: true);
    }
}
