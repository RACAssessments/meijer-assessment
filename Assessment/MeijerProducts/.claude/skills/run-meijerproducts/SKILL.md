---
name: run-meijerproducts
description: Build, run, and drive the MeijerProducts .NET MAUI Windows app. Use when asked to start MeijerProducts, build it, launch it, take a screenshot of its UI, click a button, or otherwise interact with the running app.
---

MeijerProducts is a .NET MAUI app; this skill builds and launches its Windows target
(`net10.0-windows10.0.19041.0`) as a native desktop window, then drives it via UI Automation
through `.claude/skills/run-meijerproducts/driver.ps1` — no xvfb/tmux needed, this runs directly
on the Windows host.

All paths below are relative to `Assessment/MeijerProducts/` (the project directory), except where
marked as repo-root-relative.

## Prerequisites

.NET SDK with the MAUI Windows workload, verified installed in this environment:

```
> dotnet --version
10.0.302
> dotnet workload list
android        36.1.43/10.0.100
ios            26.5.10284/10.0.100
maccatalyst    26.5.10284/10.0.100
maui-windows   10.0.20/10.0.100
```

If `maui-windows` is missing: `dotnet workload install maui-windows`.

This only works on Windows — `net10.0-windows10.0.19041.0` is a Windows-only target framework
(see `MeijerProducts.csproj`; other targets are `net10.0-android` and `net10.0-ios`, neither of
which produces a locally-launchable desktop window here).

## Build

```powershell
dotnet build MeijerProducts.csproj -f net10.0-windows10.0.19041.0
```

(Run from `Assessment/MeijerProducts/`, or `Assessment/` with the project path prefixed — see
repo `CLAUDE.md`.) The driver's `launch` command runs this for you by default.

## Run (agent path)

Drive the app through `driver.ps1` — every command below was actually run in this environment.
From the repo root:

```powershell
$driver = "Assessment\MeijerProducts\.claude\skills\run-meijerproducts\driver.ps1"

powershell -File $driver launch                          # builds + starts the app, waits for the window
powershell -File $driver screenshot -Out C:\tmp\before.png
powershell -File $driver click -Name "Click me"           # invoke a button by its current text
powershell -File $driver click                            # or omit -Name to click the (sole) app button
powershell -File $driver get-text                         # prints the button's current text
powershell -File $driver screenshot -Out C:\tmp\after.png
powershell -File $driver stop                             # kills the process, clears tracked state
```

Each invocation is a separate `powershell.exe` process, so `launch` persists the app's PID/window
handle to `%TEMP%\meijerproducts-driver-state.json`; every later command reads that file to find
the running window. `stop` removes it.

| command | what it does |
|---|---|
| `launch [-NoBuild] [-Configuration Debug]` | `dotnet build` (unless `-NoBuild`), starts the exe, polls up to 15s for a window handle, saves state |
| `screenshot -Out <path>` | Full virtual-screen PNG via `System.Drawing` (`Screen.CopyFromScreen`) |
| `click [-Name <text>]` | Finds a real app button (window-chrome Minimize/Maximize/Close/Restore excluded) via `System.Windows.Automation`, invokes it |
| `get-text [-Name <text>]` | Same lookup, prints `.Current.Name` (a `Button`'s accessible Name is its visible text) |
| `stop` | `Stop-Process` on the tracked PID, deletes the state file |

Screenshots go wherever `-Out` points — no fixed default, pass an absolute path.

## Run (human path)

```powershell
dotnet build MeijerProducts.csproj -t:Run -f net10.0-windows10.0.19041.0
```

Opens the window interactively; close it or Ctrl-C the terminal to stop. Not useful for an agent —
no return handle to script clicks/screenshots against, use the driver instead.

## Test

No test project exists yet in this repo (see `CLAUDE.md`) — nothing to run here currently.

---

## Gotchas

- **`FindFirst`/`FindAll` for `ControlType.Button` returns window-chrome buttons too** — Minimize,
  Maximize, Close, Restore are all `Button` automation elements and come *before* the app's own
  buttons in document order. A naive "find the first button" grabs Minimize and silently
  minimizes the window instead of clicking anything in the app. `driver.ps1`'s `Find-AppButton`
  explicitly filters these names out — don't remove that filter when extending the driver.
- **`GetWindowRect` + `CopyFromScreen` crop produces a misaligned/offset image** — on this DPI
  setup, cropping to the window's physical-pixel rect doesn't line up with what `CopyFromScreen`
  captures (DPI-awareness mismatch between the two APIs). The driver captures the full virtual
  screen instead (`SystemInformation.VirtualScreen`) rather than trying to crop to the window —
  reliable, at the cost of a bigger image with other windows visible around the app.
- **Non-ASCII characters (em dashes, smart quotes) in a `.ps1` file break the parser** with
  confusing cascading "Unexpected token" errors far below the actual bad character, if the file's
  encoding doesn't match what `powershell.exe` assumes when reading it. Keep string literals in
  this driver ASCII-only.
- **A `Button`'s accessible Name *is* its text**, and this app's counter button's text changes on
  every click ("Click me" → "Clicked 1 time" → "Clicked 2 times" → ...). Matching by `-Name` only
  works for the *first* click unless you track the current text yourself — omit `-Name` to always
  hit the one app button regardless of its current label.

## Troubleshooting

- **`No running app tracked (...) missing. Run the 'launch' command first.`**: any command other
  than `launch` was run without an active tracked process — either `launch` was never called, or
  `stop` already ran. Call `launch` again.
- **`Could not attach to window handle ... - is the app still running?`**: the process behind the
  saved state died (crashed, or closed manually) but the state file is stale. Run `stop` to clear
  it, then `launch` again.
- **`App started (PID ...) but no window appeared within 15s.`**: build likely succeeded but the
  app crashed on startup before showing a window — check `dotnet build ... -t:Run` (human path)
  directly to see the crash output, since `Start-Process` in the driver doesn't surface it.
