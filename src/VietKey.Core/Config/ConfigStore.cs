using System.Text;
using System.Text.Json;

namespace VietKey.Core.Config;

public sealed class ConfigStore
{
    public const string StoreMutexName = @"Local\VietKeyConfigStoreMutex";

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
        bool reset = false;
        AppConfig result = ExecuteWithStoreLock(() =>
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    reset = true;
                    SaveCore(AppConfig.Default);
                    return AppConfig.Default;
                }

                string json = ReadAllTextShared(ConfigFilePath);
                var parsed = JsonSerializer.Deserialize(json, VietKeyJsonContext.Default.AppConfig);
                if (parsed is null)
                {
                    reset = true;
                    SaveCore(AppConfig.Default);
                    return AppConfig.Default;
                }

                return parsed;
            }
            catch
            {
                reset = true;
                try
                {
                    SaveCore(AppConfig.Default);
                }
                catch
                {
                    // Ignore secondary write failure when falling back in memory
                }
                return AppConfig.Default;
            }
        });

        wasResetToDefault = reset;
        return result;
    }

    public void Save(AppConfig config)
    {
        ExecuteWithStoreLock(() => SaveCore(config));
    }

    private void SaveCore(AppConfig config)
    {
        string json = JsonSerializer.Serialize(config, VietKeyJsonContext.Default.AppConfig);
        WriteAllTextAtomic(ConfigFilePath, json);
    }

    public Dictionary<string, string> LoadMacros()
    {
        return ExecuteWithStoreLock(() =>
        {
            try
            {
                if (!File.Exists(MacrosFilePath))
                {
                    return new Dictionary<string, string>(StringComparer.Ordinal);
                }

                string json = ReadAllTextShared(MacrosFilePath);
                var dict = JsonSerializer.Deserialize(json, VietKeyJsonContext.Default.DictionaryStringString);
                return dict is not null
                    ? new Dictionary<string, string>(dict, StringComparer.Ordinal)
                    : new Dictionary<string, string>(StringComparer.Ordinal);
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }
        });
    }

    public void SaveMacros(IReadOnlyDictionary<string, string> macros)
    {
        ExecuteWithStoreLock(() =>
        {
            var serializable = macros as Dictionary<string, string>
                ?? new Dictionary<string, string>(macros, StringComparer.Ordinal);
            string json = JsonSerializer.Serialize(serializable, VietKeyJsonContext.Default.DictionaryStringString);
            WriteAllTextAtomic(MacrosFilePath, json);
        });
    }

    private static string ReadAllTextShared(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(fs, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private void WriteAllTextAtomic(string targetPath, string content)
    {
        Directory.CreateDirectory(ConfigDirectory);
        string tempPath = $"{targetPath}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(tempPath, content, Encoding.UTF8);
            for (int attempt = 0; attempt < 6; attempt++)
            {
                try
                {
                    File.Move(tempPath, targetPath, overwrite: true);
                    return;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    Thread.Sleep(5 * (attempt + 1));
                }
            }

            using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            using var writer = new StreamWriter(fs, Encoding.UTF8);
            writer.Write(content);
            writer.Flush();
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // Ignore temporary file cleanup errors
            }
        }
    }

    private static void ExecuteWithStoreLock(Action action)
    {
        ExecuteWithStoreLock(() =>
        {
            action();
            return true;
        });
    }

    private static T ExecuteWithStoreLock<T>(Func<T> action)
    {
        using var mutex = new Mutex(initiallyOwned: false, StoreMutexName);
        bool acquired = false;
        try
        {
            try
            {
                acquired = mutex.WaitOne(TimeSpan.FromSeconds(2));
            }
            catch (AbandonedMutexException)
            {
                acquired = true;
            }

            return action();
        }
        finally
        {
            if (acquired)
            {
                try
                {
                    mutex.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                    // Ignore if not owned on current thread
                }
            }
        }
    }
}
