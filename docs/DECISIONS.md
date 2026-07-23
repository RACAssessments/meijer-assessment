# Decision Log

Running log of notable technical decisions made while building the Meijer take-home assessment
(`docs/STME Take Home Assessment.md`). Newest entries at the top. Each entry: context, decision,
and why — so the reasoning survives even if the code around it changes.

---

## 2026-07-22 — Drop MacCatalyst as a build target

**Context:** The unmodified `dotnet new maui` template multi-targets `net10.0-android`,
`net10.0-ios`, `net10.0-maccatalyst` (non-Linux hosts), and `net10.0-windows10.0.19041.0`
(Windows hosts). Development is happening entirely on Windows; MacCatalyst builds require a Mac
and can't be built, run, or verified from this environment.

**Decision:** Removed `net10.0-maccatalyst` from `TargetFrameworks` and its associated
`SupportedOSPlatformVersion` entry in `Assessment/MeijerProducts/MeijerProducts.csproj`. Remaining
targets: `net10.0-android`, `net10.0-ios`, `net10.0-windows10.0.19041.0`.

**Why:** A target that can't be built or tested locally is dead weight — it adds restore/build
time and a false sense of coverage without any way to catch regressions in it. iOS is kept because
it's part of the assignment's implied platform surface even though it can't be run here; MacCatalyst
was pure incremental cost with zero verification possible.

---

## 2026-07-22 — Backend: sibling ASP.NET Core Web API project

**Context:** No backend exists yet. The assignment needs a list endpoint and a detail endpoint
backed by *some* persistence layer, built with API best practices.

**Decision:** Add a new ASP.NET Core Web API project as a sibling to `MeijerProducts/` under
`Assessment/` (e.g. `Assessment/MeijerProducts.Api`), added to `Assessment.slnx`, rather than
folding API logic into the MAUI project or standing up a separate repo/solution.

**Why:** Keeps one solution file as the single entry point for the whole assessment (`dotnet build`
from `Assessment/` builds everything), mirrors how the take-home is graded (clone, build, run), and
matches the "sibling project" structure the assignment doc implies. A separate repo would fragment
the submission and complicate the AI-tool-use disclosure requirement, which is scoped to this repo.

---

## 2026-07-22 — MAUI app architecture: MVVM + DI, per Microsoft's enterprise MAUI patterns guide

**Context:** The MAUI project is currently the bare template (`MainPage` code-behind, counter
button, no ViewModels/Services). `docs/Enterprise-Application-Patterns-Using-.NET-MAUI.md` was
bundled into the repo as the intended architectural reference.

**Decision:** Follow that guide's conventions rather than inventing new ones: MVVM (views bind to
ViewModels, no logic in code-behind, ViewModels hold no view references), constructor-injected
services registered in `MauiProgram.cs` via `Microsoft.Extensions.DependencyInjection`, an
`IProductService` abstraction over the HTTP client rather than direct calls from
views/code-behind, Shell navigation with the product id passed as a route parameter (not the whole
object), and the guide's suggested folder layout (`Models/`, `ViewModels/`, `Views/`, `Services/`,
`Converters/`, `Helpers/` — created as needed, not pre-scaffolded empty).

**Why:** The guide is Microsoft's own prescriptive answer to "how should a non-trivial MAUI app be
structured," it's explicitly provided as reference material for this exercise, and reusing an
established, documented convention is more defensible in a take-home review than a bespoke
structure — it signals familiarity with how enterprise MAUI apps are actually built rather than
one-off scripting for the assignment.

---

## Template for new entries

```markdown
## YYYY-MM-DD — Short imperative title

**Context:** What prompted the decision — constraint, requirement, or problem encountered.

**Decision:** What was actually done (file/project level, not a line-by-line diff).

**Why:** The reasoning, trade-offs considered, and what was ruled out and why.
```
