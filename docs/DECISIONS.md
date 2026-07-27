# Decision Log

Running log of notable technical decisions made while building the Meijer take-home assessment
(`docs/STME Take Home Assessment.md`). Newest entries at the top. Each entry: context, decision,
and why — so the reasoning survives even if the code around it changes.

---

## 2026-07-27 — Point Release builds at the ACI production API

**Context:** Issue #35 picks up the configurable-base-address work the 2026-07-26 "Publish the
Compose API on 5217..." entry deliberately deferred as out of proportion at the time — naming
`a MauiAsset settings JSON or an MSBuild constant` as the two mechanisms for a real fix. Issue #34
has since deployed the API to Azure Container Instances (`http://meijerproducts-api.southcentralus
.azurecontainer.io:8080/`), so the app now has two real environments to point at instead of one.

**Decision:** `MauiProgram.cs`'s single `HttpClient` base-address selection wraps its existing
Android-emulator-vs-loopback ternary in `#if DEBUG`/`#else`, adding a `const` production URL for
the `#else` branch — reusing the `DEBUG` symbol the MAUI template already defines and already uses
one line above (for `AddDebug()` logging) rather than introducing a new MSBuild property or a
settings-file layer. Both Android's `network_security_config.xml` and iOS's `Info.plist` gained a
second cleartext/ATS exception scoped to the production FQDN specifically, alongside the existing
`10.0.2.2` emulator exception — the ACI endpoint has no TLS termination, so it hits the same
HTTPS-only default block the emulator loopback did before the earlier entry scoped an exception
for it.

**Why:** Of the two mechanisms the deferred entry named, the compiler-symbol split is strictly
smaller — no new csproj properties, no new file, and it reuses a distinction (`DEBUG`) the project
already draws for exactly this "local dev vs. everything else" purpose. A settings-JSON/`MauiAsset`
approach would only pay for itself if there were more than two environments or values, neither of
which is true here. Scoping the network exceptions per-host (rather than a blanket
`cleartextTrafficPermitted`/`NSAllowsArbitraryLoads`) keeps the same least-privilege posture the
2026-07-26 "Scope Android cleartext HTTP exception to the emulator host alias only" entry
established, just extended to the one additional real host that now exists. The iOS change is
unverified — iOS builds aren't runnable from this Windows dev environment — but is included for
parity since `net10.0-ios` is still a supported target.

---

## 2026-07-27 — Deploy the API to Azure Container Instances with ephemeral SQLite storage

**Context:** Issue #34 asks for the already-containerized API (previous entry) to run in the
user's own Azure subscription via Azure Container Instances (ACI), pulling from Azure Container
Registry (ACR, `acrmeijer.azurecr.io` in resource group `rg-meijer-assessment`). Unlike the local
Compose setup, ACI has no equivalent of a named volume without provisioning an Azure Files share
and share/mount config.

**Decision:** Push the existing image to ACR unchanged and create the container group with
`ConnectionStrings__Default=Data Source=/app/products.db` (the app's own `WORKDIR`, not the
Compose-only `/app/data` subdirectory, which doesn't exist inside the image without a mounted
volume — pointing SQLite there caused the container to crash-loop with `SQLite Error 14: unable to
open database file`). No Azure Files share is provisioned; the container's writable layer holds the
SQLite file for the life of the container instance, and the app's existing idempotent
migrate-and-seed-on-startup logic simply re-populates the 30-product dataset from scratch on any
restart or redeploy. Same `ASPNETCORE_ENVIRONMENT=Development` as Compose, to keep Swagger
reachable. Registry auth uses the ACR admin username/password (fetched via `az acr credential
show` after enabling the admin user, which was off by default) rather than a managed identity —
simpler for a single-container demo deployment, at the cost of a static reusable credential stored
in `.env` (already gitignored alongside the GitHub tokens).

**Why:** For a take-home assessment deployment, losing seeded data on restart is a non-issue since
re-seeding is instant and deterministic — provisioning a storage account + file share purely to
preserve data nobody depends on between restarts would be effort spent on the wrong thing. Using
the app's real `WORKDIR` instead of inventing a new path keeps the image itself unchanged between
Compose and ACI; only the Compose-specific volume convention doesn't carry over, which is exactly
the kind of environment-specific detail the previous entry's Dockerfile/Compose split was designed
to keep out of the image.

---

## 2026-07-26 — AI usage disclosure as a standalone `docs/AI-USAGE.md`

**Context:** The assessment requires candidates to share "any AI tools or help that were used to
complete the assessments (Prompts, Instructions, Skills, Context files)". This project used Claude
Code heavily and accumulated several instruction artifacts — `CLAUDE.md`, a `kanban-manager`
subagent, a project-scoped `run-meijerproducts` skill with a PowerShell driver, and local settings
— so there was real substance to disclose rather than a one-line acknowledgement. Issue #25.

**Decision:** Wrote `docs/AI-USAGE.md` as a standalone document, linked from the README intro,
covering: tools and models used (sourced from `Co-Authored-By` commit trailers rather than
recollection), an inventory of every committed instruction artifact with line counts and what each
one does, the prompting workflow (issues-before-code, one branch per issue, the mandatory
ask-before-coding gate, the decision-log policy), an explicit AI-produced vs. human-directed split,
how output was verified, and known limitations. Rejected the "friendly summary" register in favor
of pointing every claim at a file path, commit, or issue number a reviewer can open.

**Why:** A standalone doc rather than a README section because the disclosure is an assessment
deliverable in its own right, and burying it in a build guide undersells it; `docs/` rather than
the repo root because it belongs with the spec and the decision log, with the README carrying a
prominent link. Two judgment calls worth recording: the empty 0-byte
`.claude/skills/skill-builder/skill.md` is disclosed *as* an empty placeholder rather than
described as a working tool or quietly omitted, and the "Known limitations" section names the
agent's own failure during this work (a board-automation run that reported success while leaving
three duplicate issues, #19–#21). Both cost nothing and a disclosure that only lists successes
isn't credible.

---

## 2026-07-26 — Publish the Compose API on 5217 to match the MAUI app's base address

**Context:** Issue #24 (sub-issue of #5) surfaced a mismatch that made the containerized API
unusable from the app: `MeijerProducts/MauiProgram.cs` hardcodes `http://localhost:5217/`
(`http://10.0.2.2:5217/` on Android), which is the API's `dotnet run` port from `launchSettings.json`,
while `docker-compose.yml` published the container on 8080. Running `docker compose up` and then
launching the app produced the list screen's connection-error state with nothing to indicate why —
a plausible first experience for a reviewer following the README's Docker section.

**Decision:** Changed the Compose port mapping from `"8080:8080"` to `"5217:8080"`. The container
still listens on 8080 internally — the Dockerfile's `ASPNETCORE_URLS`/`EXPOSE` are untouched — so
only the host-side published port moves. `docker compose up` and `dotnet run` now serve the same
address, and the app works against either without a rebuild. README's Docker URLs updated to 5217,
with a note that the port and `MauiProgram.cs` are two ends of one coupling.

**Why:** The alternatives were worse for a take-home. Pointing the app at 8080 instead would have
broken the plain `dotnet run` workflow, which is the faster inner loop and the one the Quick start
section leads with. Making the base address configurable (a `MauiAsset` settings JSON or an MSBuild
constant) is the right answer for a real app but is an architectural change well out of proportion
to a one-line YAML fix here, and it would add a configuration layer the assessment never asked for.
Documenting the mismatch without fixing it would leave a working-by-accident-only setup in the repo.
Keeping the container's internal port at 8080 preserves the non-privileged-port rationale recorded
in the 2026-07-26 containerization entry; only the host mapping is environment-specific, and
Compose is the right place for that.

---

## 2026-07-26 — New `MeijerProducts.Api.Tests` project, `Mvc.Testing` against a per-run temp SQLite file

**Context:** Issue #23 (sub-issue of #5) asks for endpoint/integration coverage of
`MeijerProducts.Api` — the two product endpoints, the DTO mapping, `DbInitializer.Seed`, and
Swagger — with no existing test project for the API side. `Program.cs` uses top-level statements,
which `WebApplicationFactory<T>` can't target directly without an accessible partial `Program`
type. The app's startup path also runs `db.Database.Migrate()` + `DbInitializer.Seed(db)`
unconditionally against whatever connection string it resolves, which is an opportunity to get a
deterministic seeded dataset for free rather than hand-rolling test fixtures.

**Decision:** Added `Assessment/MeijerProducts.Api.Tests` (net10.0, xUnit +
`Microsoft.AspNetCore.Mvc.Testing` 10.0.10, `ProjectReference` to `MeijerProducts.Api.csproj`),
added to `Assessment.slnx`. Appended `public partial class Program;` to the bottom of
`Program.cs` so the top-level-statement `Program` class is reachable as a generic argument.
Added `ApiFactory : WebApplicationFactory<Program>`, shared across all test classes via an xUnit
`ICollectionFixture`, which overrides `ConnectionStrings:Default` to a per-run temp file path
(`Path.GetTempPath()/meijer-products-tests-{guid}.db`) — the existing startup `Migrate()` + `Seed()`
then populates the same deterministic 30-product dataset once per test run, with no test-side
fixture/seeding code. `ApiFactory.Dispose` calls `SqliteConnection.ClearAllPools()` before deleting
the temp file, since SQLite's connection pool otherwise keeps a native handle open past the point
`WebApplicationFactory` disposes the host, which fails the file delete on Windows.
`DbInitializerTests` (which exercises `Seed` directly, not through the host) uses its own
open in-memory `SqliteConnection` per test instead of the shared `ApiFactory`, since it's testing
the seeding function's own idempotency/shape, not the HTTP surface.

**Why:** Rejected `Microsoft.EntityFrameworkCore.InMemory` for the endpoint tests — it doesn't
enforce the same constraints as the real SQLite provider (e.g. `ValueGeneratedNever()` and
required-column behavior differ), so a pass there wouldn't prove the real provider's `Migrate()`
path works, and Sqlite is already a project dependency with no new package family to reason about.
A real per-run SQLite *file* (vs. a single shared in-memory connection) also matches how the app
actually boots — through `Migrate()` against a `Data Source=` connection string — rather than
short-circuiting it. Sharing one `ApiFactory` per test run (collection fixture) instead of one per
test class avoids re-running migrations for every class while still keeping the dataset
consistent across the list/detail/swagger test files that all read the same seeded rows.

---

## 2026-07-26 — `INavigationService` abstraction over `Shell.Current` navigation

**Context:** Issue #22 (sub-issue of #5) set out to cover `ProductListViewModel` with unit tests.
Every command was reachable except `GoToDetailsCommand`, which called
`Shell.Current.GoToAsync(...)` directly. `Shell.Current` is null outside a running MAUI app, so any
test touching that command threw `NullReferenceException` — the one remaining hidden static
dependency in a ViewModel layer that had otherwise already been abstracted behind
`IProductService`/`ILocationService`/`IShareService`.

**Decision:** Added `Services/INavigationService.cs` (`Task GoToAsync(string route)`) and
`Services/ShellNavigationService.cs` (a one-line delegation to `Shell.Current.GoToAsync`),
registered as a singleton in `MauiProgram.cs` and injected into `ProductListViewModel` alongside
`IProductService`. The route string itself — built from `Routes.ProductDetail` plus the id query
parameter — stays in the ViewModel; only the act of navigating moves behind the interface. Also
promoted `ProductDetailViewModel.LoadProductAsync` from a private method to a `[RelayCommand]` in
the same pass, so `ApplyQueryAttributes`' fire-and-forget load exposes an awaitable
`ExecutionTask` instead of racing the assertions.

**Why:** `docs/Enterprise-Application-Patterns-Using-.NET-MAUI.md` — the architectural reference
this repo adopted on 2026-07-22 — prescribes exactly this abstraction, so the pre-change code was
a deviation from the repo's own stated guide rather than a considered exception. Keeping the route
*string* in the ViewModel means the test
(`GoToDetailsAsync_WhenProductIsSelected_NavigatesToDetailRouteWithId`) pins the exact
`"productdetail?id=7"` shape that `ProductDetailViewModel.ApplyQueryAttributes` parses, closing the
contract between the two ViewModels — a fatter `NavigateToProductDetail(int id)` method would have
hidden that. A full navigation service (back-stack, modal, parameter dictionaries) was ruled out as
speculative for a two-screen app; the interface can grow if a third screen needs it.

---

## 2026-07-26 — New xUnit + Moq test project, referencing the MAUI head project directly

**Context:** Issue #4 ("Add to list" share feature) needed the share-string/fallback logic in
`ProductDetailViewModel` to be unit testable without a device, and no test project existed yet in
the repo (`CLAUDE.md` calls out a standard `dotnet test`-compatible project as the intended path
once tests are added). The obvious approach — a plain `net10.0` test project with a
`ProjectReference` to `MeijerProducts.csproj` — risked friction because the head project is a MAUI
`SingleProject` (`UseMaui=true`) multi-targeting `net10.0-android`/`net10.0-ios`/
`net10.0-windows10.0.19041.0`, none of which is a TFM a plain xUnit project can target directly.

**Decision:** Added `Assessment/MeijerProducts.Tests` (xUnit + Moq), targeting
`net10.0-windows10.0.19041.0` specifically — an exact match against one of the head project's
existing `TargetFrameworks` — with a plain `<ProjectReference Include="..\MeijerProducts\MeijerProducts.csproj">`.
This alone pulled in the MAUI Resizetizer's build targets transitively and failed
(`MSB3231`/duplicate `AppIcon` output), so the reference also sets
`<ExcludeAssets>build;buildMultitargeting;buildTransitive</ExcludeAssets>` — this keeps the
compile-time assembly reference (ViewModels, Models, service interfaces) while stripping the
build-time MSBuild targets/props (icon/splash resizing, XAML source-gen hooks) that only make
sense for the app head, not a test project. Verified with a placeholder fact before writing real
tests, per the exploratory plan for this issue. Added to `Assessment.slnx`.

**Why:** This keeps every existing file in place (no extraction of `Models`/ViewModels into a new
shared class library, which was the documented fallback if this approach failed) and reuses the
already-working Windows toolchain instead of standing up a second build target. The
`ExcludeAssets` fix is narrow and only affects the test project's own build, not the app head's —
lower risk than, say, disabling Resizetizer globally. Windows was chosen as the exact-match TFM
(rather than Android) since it's the primary local dev/build target on this Windows environment
per `CLAUDE.md`.

---

## 2026-07-26 — `ILocationService`/`IShareService` abstractions over Maui Essentials Geolocation/Share

**Context:** Issue #4 needed the detail screen's "Add to list" action to resolve a city from the
device's location and invoke the native share sheet. `Geolocation`, `Geocoding`, `Permissions`,
and `Share` are static Maui Essentials entry points — calling them directly from
`ProductDetailViewModel` would work, but isn't unit-testable without a device and breaks from the
`IProductService` precedent already established for wrapping platform/network calls behind an
interface (see the "MAUI app architecture: MVVM + DI" entry below).

**Decision:** Added `Services/ILocationService` (`Task<string?> GetCurrentCityAsync(...)`, with a
"never throws — returns null on any failure" contract) and `Services/IShareService`
(`Task ShareTextAsync(string text, string? title = null)`), each with a thin implementation
wrapping the static Maui Essentials APIs, registered as singletons in `MauiProgram.cs` alongside
`IProductService`. `ProductDetailViewModel` takes both via constructor injection.

**Why:** Matches the existing `IProductService` abstraction pattern exactly (same DI lifetime,
same interface-first structure), keeps `ProductDetailViewModel` unit-testable with mocked
services (see the "New xUnit + Moq test project" entry above), and centralizes the
permission-request/exception-handling logic for location lookups in one place rather than
scattering `try/catch` around Maui Essentials calls inside the ViewModel.

---

## 2026-07-26 — Location permission across Android, iOS, and Windows for the "Add to list" feature

**Context:** Issue #4's share string requires a city derived from the device's current location,
so the app needs location permission declared on every platform it targets
(`net10.0-android`, `net10.0-ios`, `net10.0-windows10.0.19041.0`). None of the three platform
manifests had any location permission/capability before this.

**Decision:** Added `android.permission.ACCESS_COARSE_LOCATION` to
`Platforms/Android/AndroidManifest.xml` (not `ACCESS_FINE_LOCATION` — only city-level
`Placemark.Locality` is ever used, so coarse is the least-privilege choice, paired with
`GeolocationAccuracy.Medium` in `LocationService`); `NSLocationWhenInUseUsageDescription` to
`Platforms/iOS/Info.plist`; and `<DeviceCapability Name="location" />` to
`Platforms/Windows/Package.appxmanifest`. All three request "when in use" access only, via
`Permissions.RequestAsync<Permissions.LocationWhenInUse>()` at runtime.

**Why:** Least-privilege across all three platforms — the feature never needs fine-grained
coordinates, only a city name. The Windows manifest entry is added for correctness even though its
runtime effect on this project's *unpackaged* build (`WindowsPackageType=None`) is uncertain —
Windows location consent for an unpackaged desktop app is really governed by the OS-level
Settings → Privacy → Location toggle rather than the AppX capability. If location access fails for
any reason on any platform (permission denied, GPS unavailable, geocoding failure), the feature
degrades gracefully rather than blocking: the share sheet still opens with a placeholder ("your
area") substituted for the city — verified locally on Windows dev, where no location fix is
available.

---

## 2026-07-26 — Scope Android cleartext HTTP exception to the emulator host alias only

**Context:** Deploying the MAUI app to the Android emulator to test against the backend API
(reached via `http://10.0.2.2:<port>/`, the emulator's alias for the host machine's loopback)
failed to connect. Android has blocked plaintext HTTP traffic by default since API 28, and the API
only serves plain `http://` (no TLS cert set up for local dev) —
`Platforms/Android/AndroidManifest.xml` had no cleartext exception configured, so the `HttpClient`
call was being rejected before it reached the network.

**Decision:** Add `Platforms/Android/Resources/xml/network_security_config.xml`, permitting
cleartext traffic only for the `10.0.2.2` domain, and reference it from `AndroidManifest.xml` via
`android:networkSecurityConfig="@xml/network_security_config"` — rather than a blanket
`android:usesCleartextTraffic="true"` on the `<application>` element.

**Why:** A scoped `network_security_config.xml` fixes the emulator-to-dev-API case while leaving
the rest of the app's network security policy (HTTPS-only, cleartext blocked) intact for every other
host — closer to what a real deployment would want, versus a blanket flag that would silently permit
plaintext HTTP to *any* domain from the whole app.

---

## 2026-07-26 — Containerize the API with a multi-stage Dockerfile + Compose

**Context:** Issue #12 asks for the API to be runnable as a container, on the current .NET
container conventions (non-privileged port, no reliance on a locally-installed SDK), with its
SQLite data surviving container recreates, and Swagger still reachable for manual verification.

**Decision:** Add `Assessment/MeijerProducts.Api/Dockerfile` — a multi-stage build (`sdk:10.0` to
restore/publish, `aspnet:10.0` to run), binding Kestrel to `0.0.0.0:8080` via `ASPNETCORE_URLS`
and `EXPOSE 8080` (baked into the image, since that's invariant across environments). Add
`Assessment/MeijerProducts.Api/.dockerignore` to keep `bin/`, `obj/`, and local `.db*` files out of
the build context. Add `Assessment/docker-compose.yml` with a single `api` service that builds the
image, maps port 8080, forces `ASPNETCORE_ENVIRONMENT=Development` (so Swagger stays enabled
despite the base image's `Production` default), and points `ConnectionStrings__Default` at
`/app/data/products.db` on a named volume — deployment-specific settings live in Compose rather
than the Dockerfile, so the image itself stays environment-agnostic.

**Why:** Port 8080 avoids requiring root/privileged-port binding inside the container and matches
the current .NET container image convention. Forcing `Development` (rather than leaving the base
image's `Production` default) keeps Swagger reachable for grading/manual verification without
adding conditional logic to `Program.cs`. A named volume at `/app/data`, separate from the app's
own files, means `docker compose restart`/recreate doesn't wipe the seeded SQLite database — the
app's existing `db.Database.Migrate()` + seed-on-startup logic (from the persistence sub-issue)
already handles first-run initialization with no extra Docker-side migration step needed.

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

## 2026-07-24 — Accept SQLitePCLRaw.lib.e_sqlite3 2.1.11 high-severity advisory (GHSA-2m69-gcr7-jv3q)

**Context:** Adding `Microsoft.EntityFrameworkCore.Sqlite` 10.0.10 (for issue #7's persistence
layer) transitively pulls `SQLitePCLRaw.lib.e_sqlite3` 2.1.11, which NuGet flags with a high-severity
advisory — a SQLite memory-corruption bug (aggregate term count vs. column count), fixed upstream in
SQLite 3.50.2+. `SQLitePCLRaw` 2.1.12 is available on NuGet and no longer carries the deprecation
warning, so pinning it explicitly (overriding the transitive version) was a live option.

**Decision:** Leave the transitive `SQLitePCLRaw` version as EF Core 10.0.10 resolves it by default
(currently 2.1.11) rather than adding an explicit override `PackageReference`. Documented here as an
accepted risk instead.

**Why:** This is a local take-home assessment API — SQLite runs entirely server-side behind the two
read-only endpoints, there's no untrusted/attacker-controlled SQL reaching aggregate queries, and the
exploit path described in the advisory isn't reachable through this app's query shape. Forcing a
transitive-version override adds a maintenance seam (has to be re-verified against every future EF
Core upgrade) for a risk that isn't actually exercised here. Revisit if EF Core ships a release that
bundles the fixed SQLitePCLRaw by default, or if this API ever accepts untrusted query input.

---

## Template for new entries

```markdown
## YYYY-MM-DD — Short imperative title

**Context:** What prompted the decision — constraint, requirement, or problem encountered.

**Decision:** What was actually done (file/project level, not a line-by-line diff).

**Why:** The reasoning, trade-offs considered, and what was ruled out and why.
```
