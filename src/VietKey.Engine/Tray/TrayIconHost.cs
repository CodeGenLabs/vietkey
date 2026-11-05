using System.Runtime.InteropServices;
using Microsoft.Win32;
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

    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;
    private const int SM_CXSMICON = 49;
    private const int SM_CYSMICON = 50;

    private const int HORZRES = 8;
    private const int LOGPIXELSX = 88;
    private const int DESKTOPHORZRES = 118;
    private const int TRANSPARENT = 1;
    private const uint DT_CENTER = 0x0001;
    private const uint DT_VCENTER = 0x0004;
    private const uint DT_SINGLELINE = 0x0020;

    private static readonly object FontSync = new();
    private static nint _fontResourceHandle;
    private static nint _pinnedFontMemory;

    private nint _hwnd;
    private bool _iconAdded;
    private bool? _forcedLightTheme;
    private nint _iconVDark;
    private nint _iconEDark;
    private nint _iconVLight;
    private nint _iconELight;

    public bool IsVietnameseActive { get; private set; }
    public InputMethod ActiveInputMethod { get; private set; }
    public bool IsLightThemeActive { get; private set; }
    public int RenderedIconSize { get; private set; }
    public nint ActiveIconHandle => ResolveActiveIconHandle();

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

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool fIcon;
        public uint xHotspot;
        public uint yHotspot;
        public nint hbmMask;
        public nint hbmColor;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIconW(uint dwMessage, ref NOTIFYICONDATAW lpData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint LoadIconW(nint hInstance, nint lpIconName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadImageW(nint hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(nint hIcon);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessDpiAwarenessContext(nint value);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessDPIAware();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindowW(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindowExW(nint hWndParent, nint hWndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern nint GetDC(nint hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(nint hWnd, nint hDC);

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(nint hdc, int nIndex);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(nint hdc, int x, int y);

    [DllImport("kernel32.dll")]
    private static extern int MulDiv(int nNumber, int nNumerator, int nDenominator);

    [DllImport("gdi32.dll")]
    private static extern nint AddFontMemResourceEx(nint pbFont, uint cbFont, nint pdv, out uint pcFonts);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern nint CreateFontW(
        int nHeight,
        int nWidth,
        int nEscapement,
        int nOrientation,
        int fnWeight,
        uint fdwItalic,
        uint fdwUnderline,
        uint fdwStrikeOut,
        uint fdwCharSet,
        uint fdwOutputPrecision,
        uint fdwClipPrecision,
        uint fdwQuality,
        uint fdwPitchAndFamily,
        string lpszFace);

    [DllImport("gdi32.dll")]
    private static extern nint CreateDIBSection(
        nint hdc,
        ref BITMAPINFOHEADER pbmi,
        uint iUsage,
        out nint ppvBits,
        nint hSection,
        uint dwOffset);

    [DllImport("gdi32.dll")]
    private static extern nint CreateCompatibleDC(nint hdc);

    [DllImport("gdi32.dll")]
    private static extern nint SelectObject(nint hdc, nint h);

    [DllImport("gdi32.dll")]
    private static extern int SetBkMode(nint hdc, int mode);

    [DllImport("gdi32.dll")]
    private static extern uint SetTextColor(nint hdc, uint crColor);

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    private static extern int DrawTextA(nint hdc, string lpchText, int cchText, ref RECT lprc, uint format);

    [DllImport("user32.dll")]
    private static extern nint CreateIconIndirect(ref ICONINFO piconinfo);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(nint hdc);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(nint ho);

    public TrayIconHost(AppConfig initialConfig)
    {
        EnsurePerMonitorDpiAwareness();
        IsVietnameseActive = initialConfig.VietnameseEnabled;
        ActiveInputMethod = initialConfig.InputMethod;
        IsLightThemeActive = QuerySystemUsesLightTheme();
        RebuildTrayIcons(GetTargetIconSize());
    }

    public static void EnsurePerMonitorDpiAwareness()
    {
        try
        {
            if (!SetProcessDpiAwarenessContext((nint)(-4)))
            {
                SetProcessDPIAware();
            }
        }
        catch
        {
            // Ignore if already set on current process
        }
    }

    public static int GetTrueScreenDpi()
    {
        nint hdc = GetDC(0);
        if (hdc == 0)
        {
            return 96;
        }

        try
        {
            int logPixelsX = GetDeviceCaps(hdc, LOGPIXELSX);
            int horzRes = GetDeviceCaps(hdc, HORZRES);
            int desktopHorzRes = GetDeviceCaps(hdc, DESKTOPHORZRES);
            if (horzRes > 0 && desktopHorzRes > 0)
            {
                int trueDpi = (int)Math.Round(logPixelsX * (double)desktopHorzRes / horzRes);
                if (trueDpi >= 96)
                {
                    return trueDpi;
                }
            }

            return logPixelsX >= 96 ? logPixelsX : 96;
        }
        finally
        {
            ReleaseDC(0, hdc);
        }
    }

    public static int GetTargetIconSize()
    {
        int dpi = GetTrueScreenDpi();
        int scaled = MulDiv(16, dpi, 96);
        int sm = GetSystemMetrics(SM_CXSMICON);
        int chosen = Math.Max(scaled, sm);
        return Math.Clamp(chosen, 16, 64);
    }

    public static bool QuerySystemUsesLightTheme()
    {
        try
        {
            using var hkcu = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", writable: false);
            if (hkcu?.GetValue("SystemUsesLightTheme") is int userVal && userVal == 1)
            {
                return true;
            }

            using var hklm = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", writable: false);
            if (hklm?.GetValue("SystemUsesLightTheme") is int machineVal && machineVal == 1)
            {
                return true;
            }
        }
        catch
        {
            // Ignore registry access errors
        }

        return IsTrayBackgroundLuminanceLight();
    }

    private static bool IsTrayBackgroundLuminanceLight()
    {
        nint hwndTray = FindWindowW("Shell_TrayWnd", null);
        if (hwndTray == 0)
        {
            return false;
        }

        nint hwndNotify = FindWindowExW(hwndTray, 0, "TrayNotifyWnd", null);
        nint targetHwnd = hwndNotify != 0 ? hwndNotify : hwndTray;
        if (!GetWindowRect(targetHwnd, out RECT rc) || rc.right <= rc.left || rc.bottom <= rc.top)
        {
            return false;
        }

        nint hdc = GetDC(0);
        if (hdc == 0)
        {
            return false;
        }

        try
        {
            uint colorRef = GetPixel(hdc, rc.left + 2, rc.top + 2);
            if (colorRef == 0xFFFFFFFF)
            {
                return false;
            }

            int r = (int)(colorRef & 0xFF);
            int g = (int)((colorRef >> 8) & 0xFF);
            int b = (int)((colorRef >> 16) & 0xFF);
            int luminance = (299 * r + 587 * g + 114 * b) / 1000;
            return luminance > 145;
        }
        finally
        {
            ReleaseDC(0, hdc);
        }
    }

    public void AttachToWindow(nint hwnd)
    {
        _hwnd = hwnd;
        RefreshThemeIfNeeded();
        var data = CreateIconData();
        if (_iconAdded)
        {
            if (!Shell_NotifyIconW(NIM_MODIFY, ref data))
            {
                _iconAdded = Shell_NotifyIconW(NIM_ADD, ref data);
            }
        }
        else
        {
            _iconAdded = Shell_NotifyIconW(NIM_ADD, ref data);
        }
    }

    public void UpdateState(bool vietnameseEnabled, InputMethod inputMethod)
    {
        IsVietnameseActive = vietnameseEnabled;
        ActiveInputMethod = inputMethod;
        IsLightThemeActive = _forcedLightTheme ?? QuerySystemUsesLightTheme();

        if (_iconAdded && _hwnd != 0)
        {
            var data = CreateIconData();
            Shell_NotifyIconW(NIM_MODIFY, ref data);
        }
    }

    public bool RefreshThemeIfNeeded(bool? forceLightTheme = null)
    {
        if (forceLightTheme.HasValue)
        {
            _forcedLightTheme = forceLightTheme.Value;
        }

        bool targetLight = _forcedLightTheme ?? QuerySystemUsesLightTheme();
        int targetSize = GetTargetIconSize();
        bool sizeChanged = targetSize != RenderedIconSize;
        bool themeChanged = targetLight != IsLightThemeActive;

        if (sizeChanged)
        {
            RebuildTrayIcons(targetSize);
        }

        if (!sizeChanged && !themeChanged)
        {
            return false;
        }

        IsLightThemeActive = targetLight;
        if (_iconAdded && _hwnd != 0)
        {
            var data = CreateIconData();
            Shell_NotifyIconW(NIM_MODIFY, ref data);
        }

        return true;
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

    private void RebuildTrayIcons(int size)
    {
        DestroyCustomIcons();
        RenderedIconSize = size;

        if (EnsureFontRegistered())
        {
            _iconVDark = BuildNativeGlyphIcon("v", size, isLightTheme: false);
            _iconEDark = BuildNativeGlyphIcon("e", size, isLightTheme: false);
            _iconVLight = BuildNativeGlyphIcon("v", size, isLightTheme: true);
            _iconELight = BuildNativeGlyphIcon("e", size, isLightTheme: true);
        }

        if (_iconVDark == 0)
        {
            string iconVPath = ResolveIconFilePath("icon-v.ico");
            if (File.Exists(iconVPath))
            {
                _iconVDark = LoadImageW(0, iconVPath, IMAGE_ICON, size, size, LR_LOADFROMFILE);
            }
        }

        if (_iconEDark == 0)
        {
            string iconEPath = ResolveIconFilePath("icon-e.ico");
            if (File.Exists(iconEPath))
            {
                _iconEDark = LoadImageW(0, iconEPath, IMAGE_ICON, size, size, LR_LOADFROMFILE);
            }
        }
    }

    private static bool EnsureFontRegistered()
    {
        lock (FontSync)
        {
            if (_fontResourceHandle != 0)
            {
                return true;
            }

            byte[]? fontBytes = LoadEvFontBytes();
            if (fontBytes is null || fontBytes.Length == 0)
            {
                return false;
            }

            _pinnedFontMemory = Marshal.AllocHGlobal(fontBytes.Length);
            Marshal.Copy(fontBytes, 0, _pinnedFontMemory, fontBytes.Length);
            _fontResourceHandle = AddFontMemResourceEx(_pinnedFontMemory, (uint)fontBytes.Length, 0, out uint count);
            return _fontResourceHandle != 0 && count > 0;
        }
    }

    private static byte[]? LoadEvFontBytes()
    {
        using var stream = typeof(TrayIconHost).Assembly.GetManifestResourceStream("EVFont.ttf");
        if (stream is not null)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        string fontPath = ResolveIconFilePath("EVFont.ttf");
        return File.Exists(fontPath) ? File.ReadAllBytes(fontPath) : null;
    }

    private static nint BuildNativeGlyphIcon(string glyph, int size, bool isLightTheme)
    {
        int margin = Math.Max(1, size / 16);
        int fontHeight = (size - 2 * margin) - 1;
        nint hFont = CreateFontW(fontHeight, 0, 0, 0, 400, 0, 0, 0, 0, 0, 0, 5, 0x10, "EVFont");
        if (hFont == 0)
        {
            return 0;
        }

        nint screenDc = GetDC(0);
        var bmi = new BITMAPINFOHEADER
        {
            biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
            biWidth = size,
            biHeight = size,
            biPlanes = 1,
            biBitCount = 32
        };

        nint hbmColor = CreateDIBSection(screenDc, ref bmi, 0, out nint colorBits, 0, 0);
        nint hdcColor = CreateCompatibleDC(screenDc);
        nint oldColorBmp = SelectObject(hdcColor, hbmColor);

        nint hbmMask = CreateDIBSection(screenDc, ref bmi, 0, out nint maskBits, 0, 0);
        nint hdcMask = CreateCompatibleDC(screenDc);
        nint oldMaskBmp = SelectObject(hdcMask, hbmMask);
        ReleaseDC(0, screenDc);

        try
        {
            nint oldMaskFont = SelectObject(hdcMask, hFont);
            SetBkMode(hdcMask, TRANSPARENT);

            int left = margin;
            int top = margin;
            int right = size - margin;
            int bottom = size - margin;

            ReadOnlySpan<(int dx, int dy)> offsets =
            [
                (-margin, -margin), (-margin, -margin), (0, -margin), (margin, -margin),
                (-margin, 0), (margin, 0),
                (-margin, margin), (0, margin), (0, margin), (margin, margin)
            ];

            uint format = DT_CENTER | DT_VCENTER | DT_SINGLELINE;
            SetTextColor(hdcMask, 0x00FFFFFF);
            foreach (var (dx, dy) in offsets)
            {
                var rc = new RECT
                {
                    left = left + dx,
                    top = top + dy,
                    right = right + dx,
                    bottom = bottom + dy
                };
                DrawTextA(hdcMask, glyph, -1, ref rc, format);
            }

            SetTextColor(hdcMask, 0x00000000);
            var rcCenter = new RECT
            {
                left = left,
                top = top,
                right = right,
                bottom = bottom
            };
            DrawTextA(hdcMask, glyph, -1, ref rcCenter, format);
            SelectObject(hdcMask, oldMaskFont);

            int totalPixels = size * size;
            int[] maskArr = new int[totalPixels];
            int[] colorArr = new int[totalPixels];
            Marshal.Copy(maskBits, maskArr, 0, totalPixels);

            uint borderRgb = isLightTheme ? 0x000000u : 0xFAFAFAu;
            for (int i = 0; i < totalPixels; i++)
            {
                uint m = (uint)maskArr[i];
                int rM = (int)((m >> 16) & 0xFF);
                int gM = (int)((m >> 8) & 0xFF);
                int bM = (int)(m & 0xFF);
                int alpha = ((rM + gM + bM) / 3) - 10;
                if (alpha < 0)
                {
                    alpha = 0;
                }

                colorArr[i] = unchecked((int)(((uint)alpha << 24) | borderRgb));
            }

            Marshal.Copy(colorArr, 0, colorBits, totalPixels);

            var iconInfo = new ICONINFO
            {
                fIcon = true,
                xHotspot = 0,
                yHotspot = 0,
                hbmMask = hbmMask,
                hbmColor = hbmColor
            };
            return CreateIconIndirect(ref iconInfo);
        }
        finally
        {
            SelectObject(hdcColor, oldColorBmp);
            SelectObject(hdcMask, oldMaskBmp);
            DeleteDC(hdcColor);
            DeleteDC(hdcMask);
            DeleteObject(hbmColor);
            DeleteObject(hbmMask);
            DeleteObject(hFont);
        }
    }

    private static string ResolveIconFilePath(string fileName)
    {
        string inAssets = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        if (File.Exists(inAssets))
        {
            return inAssets;
        }

        string direct = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(direct))
        {
            return direct;
        }

        return inAssets;
    }

    private nint ResolveActiveIconHandle()
    {
        if (IsVietnameseActive)
        {
            nint preferredV = IsLightThemeActive && _iconVLight != 0 ? _iconVLight : _iconVDark;
            if (preferredV != 0)
            {
                return preferredV;
            }
        }
        else
        {
            nint preferredE = IsLightThemeActive && _iconELight != 0 ? _iconELight : _iconEDark;
            if (preferredE != 0)
            {
                return preferredE;
            }
        }

        nint fallbackId = IsVietnameseActive ? 32512 : 32516;
        return LoadIconW(0, fallbackId);
    }

    private NOTIFYICONDATAW CreateIconData()
    {
        return new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_APP_TRAY,
            hIcon = ResolveActiveIconHandle(),
            szTip = GetTooltipText()
        };
    }

    private void DestroyCustomIcons()
    {
        if (_iconVDark != 0)
        {
            DestroyIcon(_iconVDark);
            _iconVDark = 0;
        }

        if (_iconEDark != 0)
        {
            DestroyIcon(_iconEDark);
            _iconEDark = 0;
        }

        if (_iconVLight != 0)
        {
            DestroyIcon(_iconVLight);
            _iconVLight = 0;
        }

        if (_iconELight != 0)
        {
            DestroyIcon(_iconELight);
            _iconELight = 0;
        }
    }

    public void Dispose()
    {
        if (_iconAdded && _hwnd != 0)
        {
            var data = CreateIconData();
            Shell_NotifyIconW(NIM_DELETE, ref data);
            _iconAdded = false;
        }

        DestroyCustomIcons();
    }
}
