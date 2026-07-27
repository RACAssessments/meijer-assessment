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

powershell -File $driver launch                            # builds + starts the app, waits for the window
powershell -File $driver screenshot -Out C:\tmp\list.png
# ...click a product row by hand to reach the detail screen - see Gotchas...
powershell -File $driver click -Name "Add to list"          # invoke a button by its text
powershell -File $driver click                              # or omit -Name to click the (sole) app button
powershell -File $driver get-text -Name "Add to list"       # prints the button's current text
powershell -File $driver screenshot -Out C:\tmp\share.png
powershell -File $driver stop                               # kills the process, clears tracked state
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

Two xUnit projects, both run from `Assessment/`:

```powershell
dotnet test MeijerProducts.Tests/MeijerProducts.Tests.csproj          # MAUI ViewModels/services/converters
dotnet test MeijerProducts.Api.Tests/MeijerProducts.Api.Tests.csproj  # API endpoints, mapping, seeding
```

Run them per-project, not solution-wide — a solution-level `dotnet test` drags the MAUI head into
the build graph and builds all its target frameworks, which fails without the Android workload.

Neither suite needs the app or the API running. Unit tests are not a substitute for the driver
here: `LocationService`/`ShareService` are deliberately untested (they only delegate to MAUI
Essentials statics), so the share flow is verified by launching the app and driving it.

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
- **A `Button`'s accessible Name *is* its text** — that's what `-Name` matches against. The only
  real `Button` in this app is `"Add to list"` on the detail screen, and its label is static, so
  `-Name "Add to list"` is stable. Omitting `-Name` also works while it stays the sole button.
- **`click` cannot navigate from the product list to the detail screen.** List rows are a
  `TapGestureRecognizer` on a `Grid` inside the `CollectionView.ItemTemplate` (see
  `Views/ProductListPage.xaml`), not `Button`s — they expose no `InvokePattern`, so `Find-AppButton`
  will never match them. Click a row by hand, then resume driving. Scripting it needs a synthesized
  mouse click; if you build that, note the next two bullets, both hit during the #26 verification
  pass.
- **`GetClickablePoint()` throws for this app's `Label` elements**, even with the window
  foregrounded and the element plainly visible. Fall back to the centre of
  `element.Current.BoundingRectangle`, which works. Raising the window first is still necessary —
  the point is in screen coordinates, and a synthesized click lands on whatever is topmost.
- **UI Automation does not expose `CollectionView.EmptyView` text.** Enumerating descendants of the
  app window returns the page title and nothing else when the list is empty, even while the error
  message is plainly rendered on screen. Verify empty/error states with `screenshot`, not by
  dumping the automation tree — the tree will make a working error state look like a broken one.
- **The Windows share flyout is invisible to `screenshot`.** It renders in a separate process
  (an unnamed `ApplicationFrameWindow`), so `CopyFromScreen` captures a blank white panel where the
  sheet is, and the flyout's contents aren't reachable from the app window's automation tree
  either. To confirm a share fired, check the app's own state instead — e.g. whether the location
  fallback message appeared.
- **Running `driver.ps1` from a bash shell hits the PowerShell execution policy**
  (`UnauthorizedAccess`, "running scripts is disabled"). Invoke it from a PowerShell context, or
  pass `-ExecutionPolicy Bypass`. This fails *loudly* for the driver but can fail *silently* in a
  polling loop that only greps stdout for an expected string — a wasted 2-minute poll that reported
  a false negative during #26.

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
