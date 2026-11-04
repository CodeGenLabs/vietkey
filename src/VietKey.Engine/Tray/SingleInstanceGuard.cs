namespace VietKey.Engine.Tray;

public sealed class SingleInstanceGuard : IDisposable
{
    public const string DefaultMutexName = @"Local\VietKeyEngineMutex";

    private readonly Mutex _mutex;
    public bool IsFirstInstance { get; }

    public SingleInstanceGuard(string mutexName = DefaultMutexName)
    {
        _mutex = new Mutex(initiallyOwned: true, mutexName, out bool createdNew);
        IsFirstInstance = createdNew;
    }

    public void Dispose()
    {
        if (IsFirstInstance)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Ignore if called from a different thread during shutdown
            }
        }
        _mutex.Dispose();
    }
}
