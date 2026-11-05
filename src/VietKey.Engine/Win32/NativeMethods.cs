using System.Runtime.InteropServices;
using System.Text;
using VietKey.Core.Injection;

namespace VietKey.Engine.Win32;

public static class NativeMethods
{
    public const int WH_KEYBOARD_LL = 13;
    public const int WM_KEYDOWN = 0x0100;
    public const int WM_KEYUP = 0x0101;
    public const int WM_SYSKEYDOWN = 0x0104;
    public const int WM_SYSKEYUP = 0x0105;

    public const uint WM_IME_CONTROL = 0x0283;
    public const nint IMC_GETCONVERSIONMODE = 0x0001;
    public const nint IMC_GETOPENSTATUS = 0x0005;
    public const uint SMTO_BLOCK = 0x0001;
    public const uint SMTO_ABORTIFHUNG = 0x0002;

    public const int IME_CMODE_ALPHANUMERIC = 0x0000;
    public const int IME_CMODE_NATIVE = 0x0001;
    public const int IME_CMODE_KATAKANA = 0x0002;
    public const int IME_CMODE_FULLSHAPE = 0x0008;
    public const int IME_CMODE_ROMAN = 0x0010;
    public const int ImeNonHalfWidthAlphanumericMask = IME_CMODE_NATIVE | IME_CMODE_KATAKANA | IME_CMODE_FULLSHAPE;

    public const uint INPUT_KEYBOARD = 1;
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint KEYEVENTF_UNICODE = 0x0004;

    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    public delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GUITHREADINFO
    {
        public uint cbSize;
        public uint flags;
        public nint hwndActive;
        public nint hwndFocus;
        public nint hwndCapture;
        public nint hwndMenuOwner;
        public nint hwndMoveSize;
        public nint hwndCaret;
        public RECT rcCaret;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint SetWindowsHookExW(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    public static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

    [DllImport("user32.dll")]
    public static extern nint GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    public static extern short GetKeyState(int nVirtKey);

    [DllImport("imm32.dll")]
    public static extern nint ImmGetDefaultIMEWnd(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint SendMessageTimeoutW(
        nint hWnd,
        uint msg,
        nint wParam,
        nint lParam,
        uint fuFlags,
        uint uTimeout,
        out nint lpdwResult);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern nint OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool QueryFullProcessImageNameW(nint hProcess, uint dwFlags, StringBuilder lpExeName, ref int lpdwSize);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(nint hObject);

    public static bool IsCjkKeyboardLayout(nint hkl)
    {
        int langId = (ushort)((ulong)hkl & 0xFFFF);
        int primaryLang = langId & 0x03FF;
        // 0x11 = LANG_JAPANESE (0x0411), 0x12 = LANG_KOREAN (0x0412), 0x04 = LANG_CHINESE (0x0404, 0x0804, etc.)
        return primaryLang is 0x11 or 0x12 or 0x04;
    }

    public static bool IsCjkConversionActive(nint hkl, nint imeOpenStatus, nint imeConversionMode = 0)
    {
        if (!IsCjkKeyboardLayout(hkl))
        {
            return false;
        }

        // When IME open status is 0 (Closed), the CJK IME is in Half-width Alphanumeric ('A' / Direct Input) mode.
        if (imeOpenStatus == 0)
        {
            return false;
        }

        // When IME is open and reports a non-zero conversion mode (e.g. IME_CMODE_ROMAN = 0x10)
        // where neither Native (Hiragana/Hangul/Hanzi 0x01), Katakana (0x02), nor Full-shape (0x08) is set,
        // it is also in Half-width Alphanumeric ('A') mode -> allow Vietnamese typing.
        if (imeConversionMode != 0 && ((int)imeConversionMode & ImeNonHalfWidthAlphanumericMask) == 0)
        {
            return false;
        }

        return true;
    }

    public static bool IsForeignImeActive(nint foregroundHwnd)
    {
        var guiInfo = new GUITHREADINFO
        {
            cbSize = (uint)Marshal.SizeOf<GUITHREADINFO>()
        };

        if (GetGUIThreadInfo(0, ref guiInfo))
        {
            if (TryQueryWindowCjkState(guiInfo.hwndFocus, out bool focusCjkActive))
            {
                return focusCjkActive;
            }

            if (TryQueryWindowCjkState(guiInfo.hwndActive, out bool activeCjkActive))
            {
                return activeCjkActive;
            }
        }

        if (TryQueryWindowCjkState(foregroundHwnd, out bool fgCjkActive))
        {
            return fgCjkActive;
        }

        return false;
    }

    private static bool TryQueryWindowCjkState(nint hwnd, out bool isCjkConversionActive)
    {
        isCjkConversionActive = false;
        if (hwnd == 0)
        {
            return false;
        }

        uint threadId = GetWindowThreadProcessId(hwnd, out _);
        if (threadId == 0)
        {
            return false;
        }

        nint hkl = GetKeyboardLayout(threadId);
        if (!IsCjkKeyboardLayout(hkl))
        {
            return true;
        }

        nint imeWnd = ImmGetDefaultIMEWnd(hwnd);
        if (imeWnd == 0)
        {
            return false;
        }

        nint openOk = SendMessageTimeoutW(
            imeWnd,
            WM_IME_CONTROL,
            IMC_GETOPENSTATUS,
            0,
            SMTO_ABORTIFHUNG | SMTO_BLOCK,
            30,
            out nint imeOpenStatus);

        if (openOk == 0)
        {
            return false;
        }

        nint imeConversionMode = 0;
        if (imeOpenStatus != 0)
        {
            _ = SendMessageTimeoutW(
                imeWnd,
                WM_IME_CONTROL,
                IMC_GETCONVERSIONMODE,
                0,
                SMTO_ABORTIFHUNG | SMTO_BLOCK,
                30,
                out imeConversionMode);
        }

        isCjkConversionActive = IsCjkConversionActive(hkl, imeOpenStatus, imeConversionMode);
        return true;
    }

    public static string ResolveProcessImageName(nint hwnd)
    {
        if (hwnd == 0)
        {
            return string.Empty;
        }

        _ = GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == 0)
        {
            return string.Empty;
        }

        nint hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (hProcess == 0)
        {
            return string.Empty;
        }

        try
        {
            var sb = new StringBuilder(260);
            int capacity = sb.Capacity;
            if (QueryFullProcessImageNameW(hProcess, 0, sb, ref capacity))
            {
                return sb.ToString(0, capacity);
            }
            return string.Empty;
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }

    public static INPUT[] ToWin32Inputs(ReadOnlySpan<SimulatedKeyEvent> batch)
    {
        var inputs = new INPUT[batch.Length];
        for (int i = 0; i < batch.Length; i++)
        {
            var ev = batch[i];
            uint flags = 0;
            if (ev.IsUnicode)
            {
                flags |= KEYEVENTF_UNICODE;
            }
            if (ev.IsKeyUp)
            {
                flags |= KEYEVENTF_KEYUP;
            }

            inputs[i] = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = ev.IsUnicode ? (ushort)0 : ev.VirtualKey,
                        wScan = ev.IsUnicode ? ev.ScanCode : (ushort)0,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = 0
                    }
                }
            };
        }
        return inputs;
    }

    public static uint DispatchBatch(ReadOnlySpan<SimulatedKeyEvent> batch)
    {
        if (batch.IsEmpty)
        {
            return 0;
        }

        var inputs = ToWin32Inputs(batch);
        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }
}
