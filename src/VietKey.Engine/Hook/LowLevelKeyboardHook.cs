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

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0 && (wParam == NativeMethods.WM_KEYDOWN || wParam == NativeMethods.WM_SYSKEYDOWN))
        {
            var info = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            bool shift = (NativeMethods.GetKeyState(0x10) & 0x8000) != 0;
            bool ctrl = (NativeMethods.GetKeyState(0x11) & 0x8000) != 0;
            bool alt = (NativeMethods.GetKeyState(0x12) & 0x8000) != 0;
            bool win = (NativeMethods.GetKeyState(0x5B) & 0x8000) != 0 || (NativeMethods.GetKeyState(0x5C) & 0x8000) != 0;
            bool caps = (NativeMethods.GetKeyState(0x14) & 0x0001) != 0;
            nint hwnd = NativeMethods.GetForegroundWindow();

            bool suppress = _processor.ProcessKeyDown(
                (int)info.vkCode,
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

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose() => Stop();
}
