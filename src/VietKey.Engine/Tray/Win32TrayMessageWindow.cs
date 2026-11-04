using System.Runtime.InteropServices;
using VietKey.Core.Config;

namespace VietKey.Engine.Tray;

public sealed class Win32TrayMessageWindow : IDisposable
{
    private const uint WM_COMMAND = 0x0111;
    private const uint WM_DESTROY = 0x0002;
    private const uint MF_STRING = 0x00000000;
    private const uint MF_CHECKED = 0x00000008;
    private const uint MF_SEPARATOR = 0x00000800;
    private const uint TPM_RIGHTBUTTON = 0x0002;
    private const uint TPM_RETURNCMD = 0x0100;
    private const uint TPM_NONOTIFY = 0x0080;

    private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEXW
    {
        public uint cbSize;
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public nint hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public POINT pt;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassExW(ref WNDCLASSEXW lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(
        uint dwExStyle,
        string lpClassName,
        string lpWindowName,
        uint dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        nint hWndParent,
        nint hMenu,
        nint hInstance,
        nint lpParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint DefWindowProcW(nint hWnd, uint uMsg, nint wParam, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetMessageW(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint DispatchMessageW(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll")]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenuW(nint hMenu, uint uFlags, nuint uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(nint hMenu);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenu(
        nint hMenu,
        uint uFlags,
        int x,
        int y,
        int nReserved,
        nint hWnd,
        nint prcRect);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandleW(string? lpModuleName);

    private readonly TrayIconHost _tray;
    private readonly WndProcDelegate _wndProcDelegate;
    private nint _hwnd;

    public nint Handle => _hwnd;

    public Win32TrayMessageWindow(TrayIconHost tray)
    {
        _tray = tray;
        _wndProcDelegate = WndProc;
        _tray.ContextMenuRequested += ShowContextMenu;
        _tray.ExitRequested += () => PostQuitMessage(0);
    }

    public void CreateAndAttach()
    {
        if (_hwnd != 0)
        {
            return;
        }

        string className = "VietKeyTrayMsgWnd_" + Environment.ProcessId;
        nint hInstance = GetModuleHandleW(null);

        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
            hInstance = hInstance,
            lpszClassName = className
        };

        RegisterClassExW(ref wc);

        _hwnd = CreateWindowExW(
            0,
            className,
            "VietKey Tray Host",
            0,
            0, 0, 0, 0,
            0,
            0,
            hInstance,
            0);

        if (_hwnd != 0)
        {
            _tray.AttachToWindow(_hwnd);
        }
    }

    public void RunMessageLoop()
    {
        while (GetMessageW(out MSG msg, 0, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessageW(ref msg);
        }
    }

    private void ShowContextMenu()
    {
        if (_hwnd == 0)
        {
            return;
        }

        nint hMenu = CreatePopupMenu();
        if (hMenu == 0)
        {
            return;
        }

        try
        {
            uint vFlag = _tray.IsVietnameseActive ? MF_STRING | MF_CHECKED : MF_STRING;
            AppendMenuW(hMenu, vFlag, TrayIconHost.CmdToggleLanguage, "Chế độ Tiếng Việt (V)");
            AppendMenuW(hMenu, MF_SEPARATOR, 0, null);

            uint telexFlag = _tray.ActiveInputMethod == InputMethod.Telex ? MF_STRING | MF_CHECKED : MF_STRING;
            uint vniFlag = _tray.ActiveInputMethod == InputMethod.Vni ? MF_STRING | MF_CHECKED : MF_STRING;
            AppendMenuW(hMenu, telexFlag, TrayIconHost.CmdSelectTelex, "Kiểu gõ Telex");
            AppendMenuW(hMenu, vniFlag, TrayIconHost.CmdSelectVni, "Kiểu gõ VNI");
            AppendMenuW(hMenu, MF_SEPARATOR, 0, null);

            AppendMenuW(hMenu, MF_STRING, TrayIconHost.CmdOpenSettings, "Bảng điều khiển VietKey...");
            AppendMenuW(hMenu, MF_SEPARATOR, 0, null);
            AppendMenuW(hMenu, MF_STRING, TrayIconHost.CmdExit, "Thoát VietKey");

            GetCursorPos(out POINT pt);
            SetForegroundWindow(_hwnd);

            int cmd = TrackPopupMenu(
                hMenu,
                TPM_RIGHTBUTTON | TPM_RETURNCMD | TPM_NONOTIFY,
                pt.X,
                pt.Y,
                0,
                _hwnd,
                0);

            if (cmd > 0)
            {
                _tray.HandleMenuCommand(cmd);
            }
        }
        finally
        {
            DestroyMenu(hMenu);
        }
    }

    private nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == TrayIconHost.WM_APP_TRAY)
        {
            int mouseMsg = (int)(lParam & 0xFFFF);
            _tray.HandleMouseMessage(mouseMsg);
            return 0;
        }

        if (msg == WM_COMMAND)
        {
            int cmdId = (int)(wParam & 0xFFFF);
            _tray.HandleMenuCommand(cmdId);
            return 0;
        }

        if (msg == WM_DESTROY)
        {
            PostQuitMessage(0);
            return 0;
        }

        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hwnd != 0)
        {
            DestroyWindow(_hwnd);
            _hwnd = 0;
        }
    }
}
