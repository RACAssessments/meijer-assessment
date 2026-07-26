# Decision Log

Running log of notable technical decisions made while building the Meijer take-home assessment
(`docs/STME Take Home Assessment.md`). Newest entries at the top. Each entry: context, decision,
and why — so the reasoning survives even if the code around it changes.

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
