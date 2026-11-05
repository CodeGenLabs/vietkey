using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VietKey.Core.Config;
using VietKey.Core.Typing;
using VietKey.UI.ViewModels;
using InputMethod = VietKey.Core.Config.InputMethod;

namespace VietKey.UI;

public sealed class MainWindow
{
    public SettingsViewModel ViewModel { get; }
    public event Action? Closed;

    public MainWindow(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
    }

    public void ApplyUiSelections(
        bool vietnameseEnabled,
        InputMethod inputMethod,
        ToggleHotkey toggleHotkey,
        bool modernTone,
        bool spellCheck,
        bool autoRestoreEnglish,
        bool smartSwitch,
        bool browserAutocompleteFix,
        bool enableMacros,
        bool startWithWindows,
        bool hideDialogOnStartup = false)
    {
        ViewModel.Config = ViewModel.Config with
        {
            VietnameseEnabled = vietnameseEnabled,
            InputMethod = inputMethod,
            ToggleHotkey = toggleHotkey,
            ModernTone = modernTone,
            SpellCheck = spellCheck,
            AutoRestoreEnglish = autoRestoreEnglish,
            SmartSwitch = smartSwitch,
            BrowserAutocompleteFix = browserAutocompleteFix,
            EnableMacros = enableMacros,
            StartWithWindows = startWithWindows,
            HideDialogOnStartup = hideDialogOnStartup
        };
    }

    public void CloseAndFlush(string engineExecutablePath = "VietKey.Engine.exe")
    {
        ViewModel.SaveAndSignalReload(engineExecutablePath);
        Closed?.Invoke();
    }
}

public partial class DesktopSettingsWindow : Window
{
    private readonly MainWindow _controller;
    private readonly string _engineExecutablePath;
    private readonly ConfigStore _externalSyncStore = new();
    private readonly DispatcherTimer _externalSyncTimer;
    private readonly VietnameseTransformer _sandboxTransformer = new();
    private bool _suppressEvents;
    private bool _savedOnClose;

    public DesktopSettingsWindow(MainWindow controller, string engineExecutablePath)
    {
        _controller = controller;
        _engineExecutablePath = engineExecutablePath;
        InitializeComponent();
        Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/app-logo.png", UriKind.Absolute));
        LoadFromViewModel();
        WireEvents();

        _externalSyncTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _externalSyncTimer.Tick += OnExternalSyncTick;
        _externalSyncTimer.Start();
    }

    private void OnExternalSyncTick(object? sender, EventArgs e)
    {
        if (_suppressEvents)
        {
            return;
        }

        try
        {
            var diskConfig = _externalSyncStore.Load(out _);
            if (diskConfig.VietnameseEnabled != _controller.ViewModel.Config.VietnameseEnabled ||
                diskConfig.InputMethod != _controller.ViewModel.Config.InputMethod)
            {
                _suppressEvents = true;
                try
                {
                    _controller.ViewModel.Config = _controller.ViewModel.Config with
                    {
                        VietnameseEnabled = diskConfig.VietnameseEnabled,
                        InputMethod = diskConfig.InputMethod
                    };
                    VietnameseToggle.IsChecked = diskConfig.VietnameseEnabled;
                    InputMethodCombo.SelectedIndex = diskConfig.InputMethod == InputMethod.Vni ? 1 : 0;
                    UpdateHeaderBadge(diskConfig.VietnameseEnabled, diskConfig.InputMethod);
                    FooterStatusText.Text = $"Đã chuyển sang {(diskConfig.VietnameseEnabled ? "Tiếng Việt (V)" : "Tiếng Anh (E)")}";
                }
                finally
                {
                    _suppressEvents = false;
                }
            }
        }
        catch
        {
            // Ignore transient file access contention during reload
        }
    }

    private void LoadFromViewModel()
    {
        _suppressEvents = true;
        try
        {
            var cfg = _controller.ViewModel.Config;
            VietnameseToggle.IsChecked = cfg.VietnameseEnabled;
            InputMethodCombo.SelectedIndex = cfg.InputMethod == InputMethod.Vni ? 1 : 0;
            HotkeyCombo.SelectedIndex = cfg.ToggleHotkey == ToggleHotkey.CtrlShift ? 1 : 0;
            ModernToneToggle.IsChecked = cfg.ModernTone;
            SpellCheckToggle.IsChecked = cfg.SpellCheck;
            AutoRestoreEnglishToggle.IsChecked = cfg.AutoRestoreEnglish;
            SmartSwitchToggle.IsChecked = cfg.SmartSwitch;
            BrowserFixToggle.IsChecked = cfg.BrowserAutocompleteFix;
            MacrosToggle.IsChecked = cfg.EnableMacros;
            StartupToggle.IsChecked = cfg.StartWithWindows;
            HideDialogOnStartupToggle.IsChecked = cfg.HideDialogOnStartup;

            UpdateHeaderBadge(cfg.VietnameseEnabled, cfg.InputMethod);
            RefreshMacroList();
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void WireEvents()
    {
        QuickToggleModeButton.Click += (_, _) =>
        {
            VietnameseToggle.IsChecked = !(VietnameseToggle.IsChecked ?? true);
            SyncControlsToViewModel();
            FooterStatusText.Text = $"Đã chuyển sang {(VietnameseToggle.IsChecked == true ? "Tiếng Việt (V)" : "Tiếng Anh (E)")}";
        };

        RoutedEventHandler checkChanged = (_, _) =>
        {
            if (!_suppressEvents)
            {
                SyncControlsToViewModel();
            }
        };

        VietnameseToggle.Checked += checkChanged;
        VietnameseToggle.Unchecked += checkChanged;
        ModernToneToggle.Checked += checkChanged;
        ModernToneToggle.Unchecked += checkChanged;
        SpellCheckToggle.Checked += checkChanged;
        SpellCheckToggle.Unchecked += checkChanged;
        AutoRestoreEnglishToggle.Checked += checkChanged;
        AutoRestoreEnglishToggle.Unchecked += checkChanged;
        SmartSwitchToggle.Checked += checkChanged;
        SmartSwitchToggle.Unchecked += checkChanged;
        BrowserFixToggle.Checked += checkChanged;
        BrowserFixToggle.Unchecked += checkChanged;
        MacrosToggle.Checked += checkChanged;
        MacrosToggle.Unchecked += checkChanged;
        StartupToggle.Checked += checkChanged;
        StartupToggle.Unchecked += checkChanged;
        HideDialogOnStartupToggle.Checked += checkChanged;
        HideDialogOnStartupToggle.Unchecked += checkChanged;

        InputMethodCombo.SelectionChanged += (_, _) =>
        {
            if (!_suppressEvents)
            {
                SyncControlsToViewModel();
            }
        };

        HotkeyCombo.SelectionChanged += (_, _) =>
        {
            if (!_suppressEvents)
            {
                SyncControlsToViewModel();
            }
        };

        AddMacroButton.Click += (_, _) =>
        {
            string trigger = MacroTriggerInput.Text;
            string expansion = MacroExpansionInput.Text;
            if (_controller.ViewModel.TryAddOrUpdateMacro(trigger, expansion))
            {
                MacroTriggerInput.Clear();
                MacroExpansionInput.Clear();
                RefreshMacroList();
                _controller.ViewModel.SaveAndSignalReload(_engineExecutablePath);
                FooterStatusText.Text = $"Đã cập nhật gõ tắt: {trigger.Trim()} -> {expansion}";
            }
            else
            {
                FooterStatusText.Text = "Từ viết tắt không được rỗng hoặc chứa khoảng trắng.";
            }
        };

        RemoveMacroButton.Click += (_, _) =>
        {
            if (MacrosListBox.SelectedItem is string selectedLine)
            {
                int arrowIdx = selectedLine.IndexOf("  →  ", StringComparison.Ordinal);
                if (arrowIdx > 0)
                {
                    string key = selectedLine[..arrowIdx];
                    if (_controller.ViewModel.RemoveMacro(key))
                    {
                        RefreshMacroList();
                        _controller.ViewModel.SaveAndSignalReload(_engineExecutablePath);
                        FooterStatusText.Text = $"Đã xóa gõ tắt: {key}";
                    }
                }
            }
        };

        MacrosListBox.SelectionChanged += (_, _) =>
        {
            if (MacrosListBox.SelectedItem is string selectedLine)
            {
                int arrowIdx = selectedLine.IndexOf("  →  ", StringComparison.Ordinal);
                if (arrowIdx > 0)
                {
                    MacroTriggerInput.Text = selectedLine[..arrowIdx];
                    MacroExpansionInput.Text = selectedLine[(arrowIdx + 5)..];
                }
            }
        };

        LiveSandboxTextBox.PreviewKeyDown += OnSandboxPreviewKeyDown;
        LiveSandboxTextBox.PreviewTextInput += OnSandboxPreviewTextInput;

        ApplyButton.Click += (_, _) =>
        {
            SyncControlsToViewModel();
            FooterStatusText.Text = $"Đã lưu và áp dụng cấu hình lúc {DateTime.Now:HH:mm:ss}";
        };

        SaveAndCloseButton.Click += (_, _) =>
        {
            _externalSyncTimer.Stop();
            SyncControlsToViewModel();
            _savedOnClose = true;
            _controller.CloseAndFlush(_engineExecutablePath);
            Close();
        };

        Closed += (_, _) =>
        {
            _externalSyncTimer.Stop();
            if (!_savedOnClose)
            {
                SyncControlsToViewModel();
                _savedOnClose = true;
                _controller.CloseAndFlush(_engineExecutablePath);
            }
        };
    }

    private void SyncControlsToViewModel()
    {
        bool vietnameseEnabled = VietnameseToggle.IsChecked ?? true;
        InputMethod method = InputMethodCombo.SelectedIndex == 1 ? InputMethod.Vni : InputMethod.Telex;
        ToggleHotkey hotkey = HotkeyCombo.SelectedIndex == 1 ? ToggleHotkey.CtrlShift : ToggleHotkey.AltZ;

        _controller.ApplyUiSelections(
            vietnameseEnabled: vietnameseEnabled,
            inputMethod: method,
            toggleHotkey: hotkey,
            modernTone: ModernToneToggle.IsChecked ?? true,
            spellCheck: SpellCheckToggle.IsChecked ?? true,
            autoRestoreEnglish: AutoRestoreEnglishToggle.IsChecked ?? true,
            smartSwitch: SmartSwitchToggle.IsChecked ?? true,
            browserAutocompleteFix: BrowserFixToggle.IsChecked ?? true,
            enableMacros: MacrosToggle.IsChecked ?? true,
            startWithWindows: StartupToggle.IsChecked ?? false,
            hideDialogOnStartup: HideDialogOnStartupToggle.IsChecked ?? false);

        UpdateHeaderBadge(vietnameseEnabled, method);
        _controller.ViewModel.SaveAndSignalReload(_engineExecutablePath);
    }

    private void UpdateHeaderBadge(bool vietnameseEnabled, InputMethod method)
    {
        string iconUri = vietnameseEnabled
            ? "pack://application:,,,/Assets/icon-v.png"
            : "pack://application:,,,/Assets/icon-e.png";

        var badgeBitmap = new BitmapImage(new Uri(iconUri, UriKind.Absolute));
        ActiveModeIconImage.Source = badgeBitmap;

        StatusTitleText.Text = vietnameseEnabled
            ? "Đang bật chế độ Tiếng Việt (V)"
            : "Đang ở chế độ Tiếng Anh (E)";
        StatusSubtitleText.Text = vietnameseEnabled
            ? $"Kiểu gõ: {method} — Nhẹ, mượt, không độ trễ"
            : "Tạm tắt biến đổi dấu Tiếng Việt";
    }

    private void RefreshMacroList()
    {
        MacrosListBox.Items.Clear();
        foreach (var kvp in _controller.ViewModel.Macros.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            MacrosListBox.Items.Add($"{kvp.Key}  →  {kvp.Value}");
        }
    }

    private static bool IsBackgroundEngineRunning()
    {
        if (Mutex.TryOpenExisting(@"Local\VietKeyEngineMutex", out Mutex? existing))
        {
            existing.Dispose();
            return true;
        }
        return false;
    }

    private static bool IsCurrentThreadForeignIme()
    {
        try
        {
            string iso = InputLanguageManager.Current.CurrentInputLanguage.TwoLetterISOLanguageName;
            return iso is "ja" or "ko" or "zh";
        }
        catch
        {
            return false;
        }
    }

    private void OnSandboxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (IsBackgroundEngineRunning() || IsCurrentThreadForeignIme())
        {
            _sandboxTransformer.Reset();
            return;
        }

        if (e.Key == Key.Back)
        {
            var res = _sandboxTransformer.ProcessBackspace(_controller.ViewModel.Config);
            if (res.Handled)
            {
                ApplyTransformToTextBox(LiveSandboxTextBox, res);
                e.Handled = true;
            }
            return;
        }

        if (e.Key == Key.Space)
        {
            var res = _sandboxTransformer.ProcessKey(' ', _controller.ViewModel.Config, _controller.ViewModel.Macros);
            if (res.Handled)
            {
                ApplyTransformToTextBox(LiveSandboxTextBox, res);
                e.Handled = true;
            }
            return;
        }

        if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End or Key.Return)
        {
            _sandboxTransformer.Reset();
        }
    }

    private void OnSandboxPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (IsBackgroundEngineRunning() || IsCurrentThreadForeignIme() || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        char ch = e.Text[0];
        if (char.IsControl(ch))
        {
            return;
        }

        var res = _sandboxTransformer.ProcessKey(ch, _controller.ViewModel.Config, _controller.ViewModel.Macros);
        if (res.Handled)
        {
            ApplyTransformToTextBox(LiveSandboxTextBox, res);
            e.Handled = true;
        }
    }

    private static void ApplyTransformToTextBox(TextBox box, KeyTransformResult result)
    {
        int caret = box.CaretIndex;
        int removeStart = Math.Max(0, caret - result.BackspaceCount);
        int removeLen = caret - removeStart;
        string current = box.Text ?? string.Empty;
        string updated = current.Remove(removeStart, removeLen).Insert(removeStart, result.ReplacementText);
        box.Text = updated;
        box.CaretIndex = removeStart + result.ReplacementText.Length;
    }
}
