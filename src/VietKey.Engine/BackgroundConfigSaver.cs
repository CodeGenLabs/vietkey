using VietKey.Core.Config;

namespace VietKey.Engine;

/// <summary>
/// Persists config off the keyboard-hook thread. <see cref="ConfigStore.Save"/> waits on a
/// cross-process mutex (up to 2s) and touches the disk; doing that inside the low-level hook
/// callback (or on the message-loop thread that services it) can exceed LowLevelHooksTimeout,
/// after which Windows silently removes the hook and Vietnamese typing stops for good.
/// Latest write wins; saves never run concurrently.
/// </summary>
public sealed class BackgroundConfigSaver
{
    private readonly Action<AppConfig> _save;
    private readonly object _gate = new();
    private AppConfig? _pending;
    private bool _workerRunning;

    public BackgroundConfigSaver(ConfigStore store)
        : this(store.Save)
    {
    }

    public BackgroundConfigSaver(Action<AppConfig> save)
    {
        _save = save;
    }

    public void Enqueue(AppConfig config)
    {
        lock (_gate)
        {
            _pending = config;
            if (_workerRunning)
            {
                return;
            }
            _workerRunning = true;
        }

        ThreadPool.UnsafeQueueUserWorkItem(_ => Drain(), null);
    }

    private void Drain()
    {
        while (true)
        {
            AppConfig? next;
            lock (_gate)
            {
                next = _pending;
                _pending = null;
                if (next is null)
                {
                    _workerRunning = false;
                    return;
                }
            }

            try
            {
                _save(next);
            }
            catch
            {
                // Persisting the toggle state is best-effort; never take the engine down for it
            }
        }
    }
}
