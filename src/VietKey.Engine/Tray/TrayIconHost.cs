using System.Runtime.InteropServices;
using VietKey.Core.Config;

namespace VietKey.Engine.Tray;

public sealed class TrayIconHost : IDisposable
{
    public const int CmdToggleLanguage = 1001;
    public const int CmdSelectTelex = 1002;
    public const int CmdSelectVni = 1003;
    public const int CmdOpenSettings = 1004;
    public const int CmdExit = 1005;

    public const uint WM_APP_TRAY = 0x8001;
    public const int WM_LBUTTONUP = 0x0202;
    public const int WM_LBUTTONDBLCLK = 0x0203;
    public const int WM_RBUTTONUP = 0x0205;
    public const int WM_CONTEXTMENU = 0x007B;

    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_MODIFY = 0x00000001;
    private const uint NIM_DELETE = 0x00000002;
    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;

    private nint _hwnd;
    private bool _iconAdded;

    public bool IsVietnameseActive { get; private set; }
    public InputMethod ActiveInputMethod { get; private set; }

    public event Action? ToggleRequested;
    public event Action<InputMethod>? InputMethodChangeRequested;
    public event Action? OpenSettingsRequested;
    public event Action? ContextMenuRequested;
    public event Action? ExitRequested;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATAW
    {
        public uint cbSize;
        public nint hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public nint hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIconW(uint dwMessage, ref NOTIFYICONDATAW lpData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint LoadIconW(nint hInstance, nint lpIconName);

    public TrayIconHost(AppConfig initialConfig)
    {
        IsVietnameseActive = initialConfig.VietnameseEnabled;
        ActiveInputMethod = initialConfig.InputMethod;
    }

    public void AttachToWindow(nint hwnd)
    {
        _hwnd = hwnd;
        var data = CreateIconData();
        _iconAdded = Shell_NotifyIconW(NIM_ADD, ref data);
    }

    public void UpdateState(bool vietnameseEnabled, InputMethod inputMethod)
    {
        IsVietnameseActive = vietnameseEnabled;
        ActiveInputMethod = inputMethod;

        if (_iconAdded && _hwnd != 0)
        {
            var data = CreateIconData();
            Shell_NotifyIconW(NIM_MODIFY, ref data);
        }
    }

    public void HandleMouseMessage(int mouseMessage)
    {
        switch (mouseMessage)
        {
            case WM_LBUTTONUP:
                ToggleRequested?.Invoke();
                break;
            case WM_LBUTTONDBLCLK:
                OpenSettingsRequested?.Invoke();
                break;
            case WM_RBUTTONUP:
            case WM_CONTEXTMENU:
                ContextMenuRequested?.Invoke();
                break;
        }
    }

    public void HandleMenuCommand(int commandId)
    {
        switch (commandId)
        {
            case CmdToggleLanguage:
                ToggleRequested?.Invoke();
                break;
            case CmdSelectTelex:
                InputMethodChangeRequested?.Invoke(InputMethod.Telex);
                break;
            case CmdSelectVni:
                InputMethodChangeRequested?.Invoke(InputMethod.Vni);
                break;
            case CmdOpenSettings:
                OpenSettingsRequested?.Invoke();
                break;
            case CmdExit:
                ExitRequested?.Invoke();
                break;
        }
    }

    public string GetTooltipText() =>
        $"VietKey - {(IsVietnameseActive ? "Tiếng Việt (V)" : "Tiếng Anh (E)")} [{ActiveInputMethod}]";

    private NOTIFYICONDATAW CreateIconData()
    {
        // IDI_APPLICATION (32512) for V, IDI_INFORMATION (32516) for E fallback system icons
        nint iconId = IsVietnameseActive ? 32512 : 32516;
        return new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_APP_TRAY,
            hIcon = LoadIconW(0, iconId),
            szTip = GetTooltipText()
        };
    }

    public void Dispose()
    {
        if (_iconAdded && _hwnd != 0)
        {
            var data = CreateIconData();
            Shell_NotifyIconW(NIM_DELETE, ref data);
            _iconAdded = false;
        }
    }
}
