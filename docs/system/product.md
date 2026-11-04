# Product intent

## What this is for, and who for

VietKey is for Windows 11 users—especially developers, knowledge workers, and office professionals—who switch rapidly between Vietnamese and English across browsers (Chrome, Edge, Google Search), Electron apps (VS Code, Teams, Slack), and Office suites (Word, Excel). Today, users rely on legacy tools like UniKey (dated UI, occasional Chromium address-bar glitches) or closed-source forks like EVKey. VietKey provides a modern WinUI 3 experience with a rock-solid, low-RAM background engine and transparent, maintainable C# architecture.

## Reference products

- **EVKey / OpenKey** — Yardstick for modern Vietnamese typing behaviour:
  - Free diacritic/tone marking anywhere in the syllable, automatic tone repositioning as more vowels arrive, Vietnamese spellcheck with instant restoration of non-Vietnamese words (e.g., typing `window`, `function`, `restore`), Chromium/Google Search autocomplete glitch mitigation, per-application language state memory (Smart Switch), and macro expansion.
- **UniKey (4.x)** — Yardstick for lightweight resource footprint, fast startup, and standard Telex/VNI key mapping semantics.

## Non-goals

- **Legacy 8-bit/16-bit Vietnamese encodings** (TCVN3/ABC, VNI Windows, Vietware, VIQR): Only precomposed Unicode (UTF-16/UTF-8) is supported in MVP to keep the engine simple and bug-free.
- **Cloud sync, telemetry, AI autocomplete, or database engines** (SQLite, LocalDB): Everything runs offline from flat JSON files in `%AppData%\VietKey`.
- **TSF (Text Services Framework) in-proc COM DLL registration**: Avoided deliberately to keep installation portable, maintenance simple, and prevent third-party host process crashes.

## Deferred

- **Legacy charset conversion (Clipboard converter TCVN3 <-> Unicode)** — Deferred until users explicitly need legacy document migration.
- **Compound Unicode (Unicode tổ hợp / NFD) output mode** — Deferred unless specific legacy software requires decomposed combining marks.
- **MSI/MSIX Store packaging & code-signing pipeline** — Deferred until after the portable dual-executable MVP is verified.
