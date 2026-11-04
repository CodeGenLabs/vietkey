using System.Diagnostics;

namespace VietKey.Engine.Hook;

public readonly record struct PeerImeProcessInfo(
    int ProcessId,
    string ProcessName,
    DateTime? StartTimeUtc);

public readonly record struct PeerImeDecision(
    bool ShouldYieldToOlderPeer,
    bool HasNewerPeer,
    int? NewerPeerToRehookPid);

public sealed class PeerImeArbitrator
{
    private static readonly string[] PeerExeNames = ["EVKey64", "EVKey32", "EVKey"];
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMilliseconds(250);

    private readonly DateTime _selfStartTimeUtc;
    private readonly Func<IReadOnlyList<PeerImeProcessInfo>> _peerScanner;
    private readonly HashSet<int> _olderPeerPids = [];

    private DateTime _lastScanUtc = DateTime.MinValue;
    private PeerImeDecision _cachedDecision;
    private int _lastRehookedPeerPid;

    public PeerImeArbitrator(
        DateTime? selfStartTimeUtc = null,
        Func<IReadOnlyList<PeerImeProcessInfo>>? peerScanner = null)
    {
        _selfStartTimeUtc = selfStartTimeUtc ?? GetCurrentProcessStartTimeUtc();
        _peerScanner = peerScanner ?? ScanRunningEvKeyProcesses;

        // Snapshot any peer IME processes already running at VietKey startup
        foreach (var peer in _peerScanner())
        {
            if (!peer.StartTimeUtc.HasValue || peer.StartTimeUtc.Value <= _selfStartTimeUtc)
            {
                _olderPeerPids.Add(peer.ProcessId);
            }
        }
    }

    public PeerImeDecision Evaluate(bool bypassCache = false)
    {
        DateTime now = DateTime.UtcNow;
        if (!bypassCache && (now - _lastScanUtc) < CacheDuration)
        {
            return _cachedDecision;
        }

        _lastScanUtc = now;
        var peers = _peerScanner();
        if (peers.Count == 0)
        {
            _olderPeerPids.Clear();
            _lastRehookedPeerPid = 0;
            _cachedDecision = new PeerImeDecision(false, false, null);
            return _cachedDecision;
        }

        var activePids = new HashSet<int>();
        bool hasOlderPeer = false;
        int maxNewerPid = 0;

        foreach (var peer in peers)
        {
            activePids.Add(peer.ProcessId);
            bool isOlder = _olderPeerPids.Contains(peer.ProcessId)
                || (peer.StartTimeUtc.HasValue && peer.StartTimeUtc.Value < _selfStartTimeUtc);

            if (isOlder)
            {
                _olderPeerPids.Add(peer.ProcessId);
                hasOlderPeer = true;
            }
            else if (peer.ProcessId > maxNewerPid)
            {
                maxNewerPid = peer.ProcessId;
            }
        }

        _olderPeerPids.RemoveWhere(pid => !activePids.Contains(pid));

        if (hasOlderPeer)
        {
            _cachedDecision = new PeerImeDecision(
                ShouldYieldToOlderPeer: true,
                HasNewerPeer: false,
                NewerPeerToRehookPid: null);
            return _cachedDecision;
        }

        int? rehookPid = (maxNewerPid != 0 && maxNewerPid != _lastRehookedPeerPid)
            ? maxNewerPid
            : null;

        _cachedDecision = new PeerImeDecision(
            ShouldYieldToOlderPeer: false,
            HasNewerPeer: maxNewerPid != 0,
            NewerPeerToRehookPid: rehookPid);
        return _cachedDecision;
    }

    public void MarkRehooked(int peerPid)
    {
        _lastRehookedPeerPid = peerPid;
        _cachedDecision = _cachedDecision with { NewerPeerToRehookPid = null };
    }

    private static DateTime GetCurrentProcessStartTimeUtc()
    {
        try
        {
            using var current = Process.GetCurrentProcess();
            return current.StartTime.ToUniversalTime();
        }
        catch
        {
            return DateTime.UtcNow;
        }
    }

    public static IReadOnlyList<PeerImeProcessInfo> ScanRunningEvKeyProcesses()
    {
        var list = new List<PeerImeProcessInfo>();
        foreach (string name in PeerExeNames)
        {
            Process[]? procs = null;
            try
            {
                procs = Process.GetProcessesByName(name);
                foreach (var proc in procs)
                {
                    try
                    {
                        DateTime? startTimeUtc = null;
                        try
                        {
                            startTimeUtc = proc.StartTime.ToUniversalTime();
                        }
                        catch
                        {
                            // Elevated process may deny StartTime access; PID is still captured
                        }

                        list.Add(new PeerImeProcessInfo(proc.Id, name, startTimeUtc));
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                }
            }
            catch
            {
                if (procs is not null)
                {
                    foreach (var p in procs)
                    {
                        p.Dispose();
                    }
                }
            }
        }

        return list;
    }
}
