using System.Runtime.InteropServices;
using VietKey.Engine.Win32;

namespace VietKey.Engine.Hook;

public sealed class LowLevelKeyboardHook : IDisposable
{
    private readonly KeyboardSessionProcessor _processor;
    private readonly NativeMethods.LowLevelKeyboardProc _hookProcDelegate;
    private nint _hookHandle;

    public LowLevelKeyboardHook(KeyboardSessionProcessor processor)
    {
        _processor = processor;
        _hookProcDelegate = HookCallback;
    }

    public void Start()
    {
        if (_hookHandle != 0)
        {
            return;
        }
        _hookHandle = NativeMethods.SetWindowsHookExW(NativeMethods.WH_KEYBOARD_LL, _hookProcDelegate, 0, 0);
    }

    public void Stop()
    {
        if (_hookHandle != 0)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = 0;
        }
    }

    public void RefreshHookToFront()
    {
        Stop();
        Start();
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            if (_processor.PeerArbitrator is { } arbitrator)
            {
                var decision = arbitrator.Evaluate(bypassCache: false);
                if (decision.NewerPeerToRehookPid is int newPeerPid)
                {
                    arbitrator.MarkRehooked(newPeerPid);
                    RefreshHookToFront();
                }
            }

            var info = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            int vk = (int)info.vkCode;
            bool shift = (NativeMethods.GetAsyncKeyState(0x10) & 0x8000) != 0;
            bool ctrl = (NativeMethods.GetAsyncKeyState(0x11) & 0x8000) != 0;
            bool alt = (NativeMethods.GetAsyncKeyState(0x12) & 0x8000) != 0;
            bool win = (NativeMethods.GetAsyncKeyState(0x5B) & 0x8000) != 0 || (NativeMethods.GetAsyncKeyState(0x5C) & 0x8000) != 0;
            bool caps = (NativeMethods.GetKeyState(0x14) & 0x0001) != 0;
            nint hwnd = NativeMethods.GetForegroundWindow();

            if (wParam == NativeMethods.WM_KEYDOWN || wParam == NativeMethods.WM_SYSKEYDOWN)
            {
                bool suppress = _processor.ProcessKeyDown(
                    vk,
                    info.flags,
                    shift,
                    caps,
                    ctrl,
                    alt,
                    win,
                    hwnd);

                if (suppress)
                {
                    return 1;
                }
            }
            else if (wParam == NativeMethods.WM_KEYUP || wParam == NativeMethods.WM_SYSKEYUP)
            {
                if (KeyboardSessionProcessor.IsShiftVirtualKey(vk))
                {
                    shift = false;
                }
                else if (KeyboardSessionProcessor.IsCtrlVirtualKey(vk))
                {
                    ctrl = false;
                }
                else if (KeyboardSessionProcessor.IsAltVirtualKey(vk))
                {
                    alt = false;
                }
                else if (KeyboardSessionProcessor.IsWinVirtualKey(vk))
                {
                    win = false;
                }

                _ = _processor.ProcessKeyUp(
                    vk,
                    info.flags,
                    shift,
                    ctrl,
                    alt,
                    win,
                    hwnd);
            }
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose() => Stop();
}
