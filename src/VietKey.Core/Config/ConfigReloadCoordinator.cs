namespace VietKey.Core.Config;

public sealed class ConfigReloadCoordinator : IDisposable
{
    public const string DefaultEventName = @"Local\VietKeyConfigReloadEvent";

    private readonly ConfigStore _store;
    private readonly string _eventName;
    private EventWaitHandle? _waitHandle;
    private RegisteredWaitHandle? _registeredWait;
    private volatile AppConfig _currentConfig;
    private volatile IReadOnlyDictionary<string, string> _currentMacros;

    public AppConfig CurrentConfig => _currentConfig;
    public IReadOnlyDictionary<string, string> CurrentMacros => _currentMacros;
    public event Action<AppConfig, IReadOnlyDictionary<string, string>>? ConfigReloaded;

    public ConfigReloadCoordinator(ConfigStore store, string eventName = DefaultEventName)
    {
        _store = store;
        _eventName = eventName;
        _currentConfig = _store.Load(out _);
        _currentMacros = _store.LoadMacros();
    }

    public void StartListening()
    {
        if (_waitHandle is not null)
        {
            return;
        }

        _waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, _eventName);
        _registeredWait = ThreadPool.RegisterWaitForSingleObject(
            _waitHandle,
            static (state, timedOut) =>
            {
                if (!timedOut && state is ConfigReloadCoordinator self)
                {
                    self.ReloadNow();
                }
            },
            this,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    public void ReloadNow()
    {
        var cfg = _store.Load(out _);
        var macros = _store.LoadMacros();
        _currentConfig = cfg;
        _currentMacros = macros;
        ConfigReloaded?.Invoke(cfg, macros);
    }

    public static void SignalReload(string eventName = DefaultEventName)
    {
        try
        {
            using var handle = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
            handle.Set();
        }
        catch
        {
            // Safe to ignore if named event is unsupported in restricted sandbox.
        }
    }

    public void Dispose()
    {
        _registeredWait?.Unregister(null);
        _registeredWait = null;
        _waitHandle?.Dispose();
        _waitHandle = null;
    }
}
