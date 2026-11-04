# User Flow — 0001-vietkey-mvp-winui3-ime

## 1. Entry Points

1. **Background Launch (`VietKey.Engine.exe`):**
   - Started either manually by the user or automatically at Windows 11 sign-in via `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
   - Places a native System Tray icon (`V` in accent red/blue when Vietnamese mode is active, `E` in neutral monochrome when English mode is active) in the Windows 11 notification area.
2. **Global Hotkey (`Alt+Z` or `Ctrl+Shift`):**
   - Toggles between `V` (Vietnamese) and `E` (English) modes from any focused application without stealing focus or displaying a modal window.
3. **System Tray Interactions:**
   - **Left-click on Tray Icon:** Immediately toggles `V` <-> `E`.
   - **Right-click on Tray Icon:** Opens the native Win32 context menu with:
     - `Bật Tiếng Việt (V) / Tiếng Anh (E)` (checkable toggle)
     - `Kiểu gõ: Telex` (radio item)
     - `Kiểu gõ: VNI` (radio item)
     - `Bảng điều khiển (WinUI 3)...` -> Spawns `VietKey.UI.exe`
     - `Thoát (Exit)` -> Unhooks `WH_KEYBOARD_LL`, removes tray icon, and terminates `VietKey.Engine.exe`.

## 2. Screens & States in `VietKey.UI.exe` (WinUI 3 Settings Window)

When the user clicks `Bảng điều khiển (WinUI 3)...` in the tray menu (or double-clicks the tray icon), `VietKey.Engine.exe` launches `VietKey.UI.exe`. If `VietKey.UI.exe` is already running, its existing window is brought to the foreground via `SetForegroundWindow`.

The WinUI 3 window uses a compact single-window layout (Mica backdrop, 560x640 px) with two tabs/sections:

### Screen A: Cài đặt chung (General & Typing Settings)
- **Controls:**
  - **Chế độ gõ (Input Mode):** ToggleSwitch (`Tiếng Việt` / `Tiếng Anh`).
  - **Kiểu gõ (Input Method):** RadioButtons / ComboBox (`Telex`, `VNI`).
  - **Phím chuyển nhanh (Toggle Hotkey):** RadioButtons (`Alt + Z`, `Ctrl + Shift`).
  - **Đặt dấu thanh (Tone Placement):** ToggleSwitch (`Kiểu mới: hoà, thuý` vs `Kiểu cũ: hòa, thúy`).
  - **Kiểm tra chính tả & Tự khôi phục từ tiếng Anh:** ToggleSwitch (`SpellCheck` + `AutoRestoreEnglish`).
  - **Tự nhớ trạng thái Anh/Việt theo ứng dụng (Smart Switch):** ToggleSwitch (`SmartSwitch`).
  - **Sửa lỗi gợi ý trình duyệt (Chrome, Edge, Google Search):** ToggleSwitch (`BrowserAutocompleteFix`).
  - **Khởi động cùng Windows:** ToggleSwitch (reads/writes `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\VietKey`).
- **States:**
  - **Loading:** Synchronous read of `%AppData%\VietKey\config.json` on startup (< 5 ms); controls render immediately in their current state.
  - **Corrupted/Missing Config:** Automatically resets to safe defaults and shows a subtle InfoBar: *"Cấu hình đã được khôi phục về mặc định."*
  - **Save Error (e.g., disk full or permission denied):** Displays an inline WinUI 3 `InfoBar` (`Severity="Error"`) with the exact file path and error message.
  - **Engine Not Running Warning:** If `Local\VietKeyEngineMutex` is not detected, shows an `InfoBar` (`Severity="Warning"`) with a button `Khởi động bộ gõ ngầm` to start `VietKey.Engine.exe`.

### Screen B: Gõ tắt (Macros)
- **Controls:**
  - Text boxes for `Từ viết tắt` (Trigger, e.g., `vn`) and `Nội dung thay thế` (Expansion, e.g., `Việt Nam`) + `Thêm / Cập nhật` button.
  - List of configured macros with a `Xóa` (Delete) button per row.
  - Master toggle `Bật gõ tắt (Enable Macros)`.
- **States:**
  - **Empty State:** When no macros exist, displays centered muted caption: *"Chưa có từ gõ tắt nào. Thêm từ viết tắt ở khung phía trên (ví dụ: 'vn' -> 'Việt Nam')."*
  - **Validation Error:** If `Từ viết tắt` is empty or contains whitespace, disables the `Thêm` button and highlights the input field.

## 3. Exit & Memory Reclamation Flow

- When the user clicks the Close (`X`) button on `VietKey.UI.exe`:
  1. Pending changes are atomically written to `%AppData%\VietKey\config.json` and `macros.json`.
  2. `Local\VietKeyConfigReloadEvent` is signaled so `VietKey.Engine.exe` reloads the new settings in memory.
  3. `VietKey.UI.exe` exits completely (`Application.Current.Exit()`), releasing 100% of WinUI 3 / XAML / DirectComposition RAM back to the OS.
