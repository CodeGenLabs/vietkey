using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        bool startWithWindows)
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
            StartWithWindows = startWithWindows
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
    private readonly VietnameseTransformer _sandboxTransformer = new();
    private bool _suppressEvents;
    private bool _savedOnClose;

    public DesktopSettingsWindow(MainWindow controller, string engineExecutablePath)
    {
        _controller = controller;
        _engineExecutablePath = engineExecutablePath;
        InitializeComponent();
        LoadFromViewModel();
        WireEvents();
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
            _controller.ViewModel.SaveAndSignalReload(_engineExecutablePath);
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
            _controller.ViewModel.SaveAndSignalReload(_engineExecutablePath);
            FooterStatusText.Text = $"Đã lưu và áp dụng cấu hình lúc {DateTime.Now:HH:mm:ss}";
        };

        SaveAndCloseButton.Click += (_, _) =>
        {
            SyncControlsToViewModel();
            _savedOnClose = true;
            _controller.CloseAndFlush(_engineExecutablePath);
            Close();
        };

        Closed += (_, _) =>
        {
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
            startWithWindows: StartupToggle.IsChecked ?? false);

        UpdateHeaderBadge(vietnameseEnabled, method);
    }

    private void UpdateHeaderBadge(bool vietnameseEnabled, InputMethod method)
    {
        ModeBadgeText.Text = vietnameseEnabled ? "V" : "E";
        ModeBadgeBorder.Background = new SolidColorBrush(
            vietnameseEnabled ? Color.FromRgb(217, 45, 32) : Color.FromRgb(29, 78, 216));
        StatusSubtitleText.Text = vietnameseEnabled
            ? $"Bộ gõ Tiếng Việt nhẹ, mượt, không độ trễ — Đang bật Tiếng Việt ({method})"
            : "Bộ gõ Tiếng Việt nhẹ, mượt, không độ trễ — Chế độ Tiếng Anh (E)";
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

    private void OnSandboxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (IsBackgroundEngineRunning())
        {
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
        if (IsBackgroundEngineRunning() || string.IsNullOrEmpty(e.Text))
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
