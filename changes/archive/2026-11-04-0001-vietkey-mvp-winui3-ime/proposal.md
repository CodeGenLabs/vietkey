# Proposal — 0001-vietkey-mvp-winui3-ime

## Why

Windows 11 lacks a lightweight, open, maintainable Vietnamese Input Method that combines a modern WinUI 3 Fluent settings interface with a sub-10 MB background typing engine that does not stutter or corrupt text in Chrome address bars, Google Search autocomplete, VS Code, Microsoft Word, and Microsoft Teams. This change builds the complete VietKey MVP using a dual-process C# (.NET 8/9) architecture with zero external server or database dependencies.

## What changes

- Add `VietKey.Core` pure C# library implementing Telex and VNI composition, free diacritic marking, modern (`oà`/`uý`) and classic (`òa`/`úy`) tone placement, phonotactic Vietnamese spellcheck with automatic English raw-key restoration, text expansion macros, and source-generated JSON configuration persistence.
- Add `VietKey.Engine` lightweight Win32 background daemon (`WH_KEYBOARD_LL` + batched Unicode `SendInput` + `Shell_NotifyIconW` system tray + foreground window process cache) with per-app compatibility strategies for Chromium browsers, Electron apps (VS Code, Teams), and Microsoft Word, plus per-application language memory (Smart Switch).
- Add `VietKey.UI` on-demand WinUI 3 desktop settings application for configuring input method (Telex/VNI), tone style, spellcheck/auto-restore, Smart Switch, hotkeys, startup behavior, and macro definitions, signaling `VietKey.Engine` via `Local\VietKeyConfigReloadEvent` and exiting completely when closed to keep background RAM minimal.
- Add `VietKey.Core.Tests` automated unit test suite verifying all requirements and edge-case scenarios.

## Capabilities

- **New:** `docs/system/specs/typing-engine/spec.md` — Telex/VNI syllable composition, free tone/mark placement, double-key undo, spellcheck auto-restore, and macro expansion.
- **New:** `docs/system/specs/os-integration/spec.md` — Win32 low-level keyboard hook, batched `SendInput` injection, Chromium/Electron/Word compatibility handling, and Smart Switch per-app state tracking.
- **New:** `docs/system/specs/settings-and-tray/spec.md` — System tray lifecycle, hotkey switching, local JSON persistence (`%AppData%\VietKey\`), and on-demand WinUI 3 configuration UI.
