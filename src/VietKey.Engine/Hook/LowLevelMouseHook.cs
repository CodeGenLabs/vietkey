using VietKey.Engine.Win32;

namespace VietKey.Engine.Hook;

/// <summary>
/// Watches mouse button presses so the composing buffer is dropped when the user clicks to move the
/// caret. Without it, the next tone key would rewrite the previously typed word at the new caret
/// position (deleting unrelated text and re-typing the old word there).
/// </summary>
public sealed class LowLevelMouseHook : IDisposable
{
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_XBUTTONDOWN = 0x020B;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int WM_NCRBUTTONDOWN = 0x00A4;
    private const int WM_NCMBUTTONDOWN = 0x00A7;
    private const int WM_NCXBUTTONDOWN = 0x00AB;

    private readonly Action _onButtonDown;
    private readonly NativeMethods.LowLevelKeyboardProc _hookProcDelegate;
    private nint _hookHandle;

    public LowLevelMouseHook(Action onButtonDown)
    {
        _onButtonDown = onButtonDown;
        _hookProcDelegate = HookCallback;
    }

    public void Start()
    {
        if (_hookHandle != 0)
        {
            return;
        }
        // Same (int, nint, nint) -> nint signature as the keyboard hook procedure
        _hookHandle = NativeMethods.SetWindowsHookExW(WH_MOUSE_LL, _hookProcDelegate, 0, 0);
    }

    public void Stop()
    {
        if (_hookHandle != 0)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = 0;
        }
    }

    public static bool IsButtonDownMessage(nint wParam) =>
        (int)wParam is WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN or WM_XBUTTONDOWN
            or WM_NCLBUTTONDOWN or WM_NCRBUTTONDOWN or WM_NCMBUTTONDOWN or WM_NCXBUTTONDOWN;

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        // Keep this path trivial: it runs for every mouse move system-wide
        if (nCode >= 0 && IsButtonDownMessage(wParam))
        {
            _onButtonDown();
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose() => Stop();
}
