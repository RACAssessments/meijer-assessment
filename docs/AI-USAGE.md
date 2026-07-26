# AI Tool Usage Disclosure

The assessment asks candidates to *"share all your code artifacts along with any AI tools or help
that were used to complete the assessments (Prompts, Instructions, Skills, Context files)."* This
document is that disclosure.

Short version: this assessment was built with **Claude Code** (Anthropic's agentic CLI) used
heavily throughout, under human direction on scope, architecture, and review. Every AI instruction
file used is committed to this repository and listed below — nothing driving the agent is hidden
from the reviewer.

---

## 1. Tools used

| Tool | Role |
|---|---|
| **Claude Code** (Anthropic CLI, run in VS Code) | Wrote and edited most application code, tests, and documentation; ran builds and test suites; launched and drove the MAUI app for visual verification |
| **`gh` CLI + GitHub GraphQL API** | Driven by the agent to create issues, link sub-issues, open pull requests, and move cards on the project board |
| **.NET SDK 10.0.302** with the `maui-windows` workload | Build/test toolchain (not AI) |

Models varied by session. Rather than quote a figure that goes stale, the authoritative record is
in the repository itself — the `Co-Authored-By` trailers on commits name the model that produced
each one:

```powershell
git log --format="%b" | Select-String "Co-Authored-By" | Group-Object -NoElement
```

At the time of writing that showed Claude Sonnet 5, Claude Haiku 4.5, and Claude Opus 5 across 9 of
25 commits; the rest are merges and early setup commits made without trailers.

---

## 2. Instruction and context files in this repository

These are the files that shape how the agent behaves. All are committed and readable.

### [`CLAUDE.md`](../CLAUDE.md) (repo root, ~120 lines)

The main context file, loaded automatically by Claude Code at the start of every session. It
contains the repository state, the assignment summary, build/test/run commands, architecture
conventions, and — most consequentially — two process rules described in §3.

### [`.claude/agents/kanban-manager.md`](../.claude/agents/kanban-manager.md) (~123 lines)

A subagent definition for GitHub issue and project-board operations: which of three tokens to use
for which API surface, and ready-to-run GraphQL recipes for creating issues, linking sub-issues,
adding board items, and setting Status fields.

### [`Assessment/MeijerProducts/.claude/skills/run-meijerproducts/`](../Assessment/MeijerProducts/.claude/skills/run-meijerproducts/SKILL.md)

A project-scoped skill: [`SKILL.md`](../Assessment/MeijerProducts/.claude/skills/run-meijerproducts/SKILL.md)
(~120 lines) plus [`driver.ps1`](../Assessment/MeijerProducts/.claude/skills/run-meijerproducts/driver.ps1)
(~155 lines of PowerShell). It builds and launches the MAUI Windows head and drives it through UI
Automation — launch, screenshot, click a button by name, read button text, stop. This is what let
the app be verified *visually* rather than only compiled.

Its "Gotchas" section is worth a look, because it records real failures hit while building it:
window-chrome buttons shadowing the app's own buttons in UI Automation's document order; DPI
mismatch between `GetWindowRect` and `CopyFromScreen` producing misaligned screenshot crops;
non-ASCII characters breaking the PowerShell parser with errors reported far from the actual cause;
and the limitation that `click` cannot open the product detail screen, because list rows are a
`TapGestureRecognizer` on a `Grid` and expose no `InvokePattern`.

### [`.claude/settings.local.json`](../.claude/settings.local.json)

Local Claude Code configuration — a permission allowlist for a handful of read-only `gh`/`docker`
commands plus a scratch directory. Configuration only; no project logic.

### `.claude/skills/skill-builder/skill.md`

**Empty (0 bytes).** A placeholder directory that was created but never filled in. Listed here only
for completeness — it has no content and had no effect on any output.

### Not committed

The repository root has a `.env` file holding three GitHub personal access tokens, used by the
agent for issue and project-board operations. It is gitignored (`.gitignore` line 20) and is **not**
part of this repository. No credentials are committed.

---

## 3. How the work was prompted

The workflow mattered more to the result than any individual prompt, so it's worth describing.

**Issues before code.** The assessment was decomposed into GitHub issues on a project board before
implementation began. Large issues were split into sub-issues — #2 (backend) became #6–#12, and #5
(quality/testing/docs) became #22–#26. Each was worked on its own branch and merged via pull
request. `git log` and the PR history show this; the branch names encode the issue numbers
(`22-maui-unit-tests`, `23-api-tests`, `24-docs-pass`).

**A hard gate before writing code.** `CLAUDE.md` defines a mandatory sequence for "work on issue
#N": read the issue first, move its board card to In progress, branch off `master`, and then **ask
for explicit human confirmation before writing any code** — restated as applying "every time, not
just the first time." This was a deliberate control on agent autonomy: it forces the plan to be
reviewed against the actual issue scope before any file is touched.

**Architecture was a human choice, not an agent invention.** The decision to follow Microsoft's
*Enterprise Application Patterns Using .NET MAUI* (bundled at
`docs/Enterprise-Application-Patterns-Using-.NET-MAUI.md`) was made up front and recorded in
[`docs/DECISIONS.md`](DECISIONS.md) on 2026-07-22. `CLAUDE.md` instructs the agent to follow that
guide "rather than reinventing conventions." When the agent later found code that deviated from it
— ViewModels calling `Shell.Current` directly — the guide is what justified the fix.

**A standing rule to stop and ask.** `CLAUDE.md` requires the agent to ask the user before adding,
removing, or upgrading a NuGet package, changing a device capability or permission, or altering an
architectural layer — and to ask whether the change warrants a decision-log entry, explicitly
"don't add the entry unprompted, and don't skip asking."

The effect is that [`docs/DECISIONS.md`](DECISIONS.md) is itself part of this disclosure: it is the
human-approved record of every consequential technical choice, including alternatives rejected and
risks knowingly accepted (for example, the 2026-07-24 entry accepting a high-severity SQLitePCLRaw
advisory, and the entry rejecting `EFCore.InMemory` for API tests because it wouldn't exercise the
real provider's `Migrate()` path).

### Representative prompts

These are *representative*, not a verbatim transcript. The first four are quoted exactly from a
recorded session; the rest are typical of the pattern used throughout.

- `begin work on issue #5`
- `23 is done. start 24`
- `push and open a PR for 22`
- `Kanban agent should use GITHUB_TOKEN_CLASSIC`
- *"Build the Windows head and take a screenshot of the detail page."*
- *"Does this warrant a DECISIONS entry?"*

Most sessions were short, imperative, and issue-scoped rather than long specifications — the
standing context in `CLAUDE.md` carried the detail, which is why that file matters more than any
single prompt.

---

## 4. What the AI produced vs. what was human-directed

**Largely AI-produced, then reviewed:** the MAUI ViewModels/services/converters and their XAML, the
minimal-API endpoints, DTOs and mapping, the EF Core model, migration and seed data, the
Dockerfile and Compose setup, both test projects, and the documentation in this repository —
including this file.

**Human-owned:** interpreting the assessment; decomposing it into issues and sequencing them;
choosing the architectural reference; approving every package addition, permission change, and
architectural change; the trade-off calls recorded in the decision log; and review of each pull
request before merge.

The honest characterization is that the AI did the typing and much of the design-within-constraints,
while the constraints, the sequencing, and the accept/reject decisions were human.

---

## 5. How the output was verified

- **Builds** — the MAUI Windows head and the API both build clean.
- **Tests** — 42 tests in `MeijerProducts.Tests` (ViewModels, `ProductService` against a fake
  `HttpMessageHandler`, converters) and 12 in `MeijerProducts.Api.Tests` (both endpoints end-to-end
  through `WebApplicationFactory` against a real seeded SQLite database, plus mapping and seeder
  tests). Counts quoted in the README came from actual runs.
- **The running app** — launched on the Windows head and driven via the `run-meijerproducts` skill,
  with screenshots, rather than assumed working from a successful compile.
- **The API** — exercised through Swagger and direct HTTP calls.
- **Review** — changes landed through pull requests rather than direct pushes to `master`.

---

## 6. Known limitations

Stated plainly, because a disclosure that only claims successes isn't much of a disclosure:

- **Product images don't render** — issue
  [#16](https://github.com/RACAssessments/meijer-assessment/issues/16). The seeded image URLs point
  at Meijer's CDN, which returns HTTP 403 to non-browser clients. Data, layout, and bindings are
  correct; the remote fetch fails.
- **`LocationService` and `ShareService` are not unit tested.** They only delegate to MAUI
  Essentials statics (`Geolocation`, `Geocoding`, `Permissions`, `Share`), so unit tests would test
  a mock. They are verified by running the app instead.
- **XAML and pages are not unit tested**, and only the **Windows** head was run. Android and iOS
  builds are configured but were not exercised on a device or emulator as part of this submission.
- **The agent was not infallible.** One automated issue-creation run reported success while having
  partially failed, leaving three duplicate issues (#19–#21, since closed). Agent output was checked
  against the actual GitHub state rather than taken at its word — which is the general lesson worth
  drawing from this project.
