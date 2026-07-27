# Meijer Products

Take-home coding assessment for a Meijer interview — a .NET MAUI app showing a product list and
detail screen, with a location-aware "Add to list" share action, backed by an ASP.NET Core Web API
over EF Core/SQLite. Full requirements: `docs/STME Take Home Assessment.md`.

**AI tool usage for this assessment is disclosed in [`docs/AI-USAGE.md`](docs/AI-USAGE.md).**

Work is tracked as GitHub issues in
[RACAssessments/meijer-assessment](https://github.com/RACAssessments/meijer-assessment) and the
[project board](https://github.com/users/blanthor/projects/7).

## Repository layout

```
Assessment/
  Assessment.slnx            # solution file — build/restore from here
  MeijerProducts/            # .NET MAUI app (Android, iOS, Windows)
  MeijerProducts.Api/        # ASP.NET Core Web API (products list/detail, EF Core + SQLite)
  MeijerProducts.Tests/      # xUnit tests — MAUI ViewModels, services, converters
  MeijerProducts.Api.Tests/  # xUnit tests — API endpoints, mapping, seeding
  docker-compose.yml         # containerized API
docs/                        # assessment spec, sample API JSON, architecture reference,
                             # decision log, AI usage disclosure
```

## Prerequisites

- **.NET SDK 10** (developed against 10.0.302)
- **MAUI Windows workload** — `dotnet workload install maui-windows`
- **Docker Desktop** (optional — only for running the API in a container)

Android builds additionally require the Android SDK/emulator; iOS requires a Mac. Windows is the
primary local target and the only one these instructions assume.

## Quick start (end to end)

Two terminals. **Terminal 1 — the API:**

```powershell
cd Assessment/MeijerProducts.Api
dotnet run --launch-profile http     # serves http://localhost:5217
```

The database is created, migrated, and seeded with 30 products on first run. Sanity check it with
`Invoke-RestMethod http://localhost:5217/products`, or browse http://localhost:5217/swagger.

**Terminal 2 — the MAUI app (Windows head):**

```powershell
cd Assessment
dotnet build MeijerProducts/MeijerProducts.csproj -t:Run -f net10.0-windows10.0.19041.0
```

You should get a scrollable list of 30 products. Tap one to open its detail screen, then press
**Add to list** — the app resolves your city from the device location and opens the Windows share
sheet with text of the form `"{title} - {price} from {city} added to list"`. If location is
unavailable or denied, it falls back to `"your area"` and says so on screen rather than failing.

Product images will appear blank — see [Known issues](#known-issues).

## Building

From `Assessment/`:

```powershell
dotnet restore
dotnet build MeijerProducts.Api/MeijerProducts.Api.csproj
dotnet build MeijerProducts/MeijerProducts.csproj -f net10.0-windows10.0.19041.0
```

A bare `dotnet build` at the solution level builds **every** MAUI target framework
(`net10.0-android`, `net10.0-ios`, `net10.0-windows10.0.19041.0`), which is slow and fails outright
without the Android workload installed. Prefer the per-project, single-TFM commands above.

Add `-t:Run` to the MAUI command to build and launch in one step.

## Running the tests

From `Assessment/`:

```powershell
dotnet test MeijerProducts.Tests/MeijerProducts.Tests.csproj      # 42 tests
dotnet test MeijerProducts.Api.Tests/MeijerProducts.Api.Tests.csproj  # 12 tests
```

Run them per-project rather than solution-wide, for the same all-TFM reason as above.

**What's covered.** MAUI side: both ViewModels (load/error/busy lifecycles, the pull-to-refresh
re-entrancy guard, `AddToListCommand`'s share-string construction and city fallback, command
`CanExecute` wiring, Shell query-parameter parsing), `ProductService` against a fake
`HttpMessageHandler` (camelCase deserialization, URI composition, 404 → `null`, 500 → throw), and
both value converters. API side: both endpoints end-to-end through `WebApplicationFactory` against
a real seeded SQLite database (status codes, exact DTO field sets, 404 paths, route constraints),
the DTO mapping, and the seeder's idempotency.

**What isn't, and why.** `LocationService` and `ShareService` are untested by design — they hold no
logic beyond delegating to MAUI Essentials statics (`Geolocation`, `Geocoding`, `Permissions`,
`Share`), so testing them would amount to testing a mock. They're verified manually in the
end-to-end pass instead. XAML/pages aren't unit tested either; running `ContentPage` code outside a
platform head isn't practical.

Two things worth knowing about the setup:

- `MeijerProducts.Tests` targets `net10.0-windows10.0.19041.0` (an exact match against one of the
  MAUI head's TFMs), so **it runs on Windows only**.
- `MeijerProducts.Api.Tests` self-hosts the API in-process — no server needs to be running, and it
  uses a throwaway SQLite file per run rather than touching your local `products.db`.

## Running the API in Docker

From `Assessment/`:

```powershell
docker compose up --build
```

- Products: http://localhost:5217/products
- Swagger: http://localhost:5217/swagger

Compose publishes the container's port 8080 on **5217** deliberately: that's the address **Debug**
builds of the MAUI app hardcode (and the API's `dotnet run` port), so containerized and locally-run
APIs are interchangeable without rebuilding the app. If you change one, change the other —
`Assessment/MeijerProducts/MauiProgram.cs` is the client side.

**Release** builds instead point at the API deployed to Azure Container Instances (issue #34) —
see `docs/azure-deploy.md` for how that's deployed/redeployed, and the 2026-07-27 "Point Release
builds at the ACI production API" entry in `docs/DECISIONS.md` for why the split is a compiler
symbol rather than a settings file.

Seeded SQLite data lives on a named volume, so it survives `docker compose restart`. Stop with
`docker compose down` (add `-v` to also wipe the volume and reset seeded data).

## Running the app (agents / automation)

For anything that needs to launch the app programmatically and interact with it rather than just
build it, use the driver script in
[`Assessment/MeijerProducts/.claude/skills/run-meijerproducts/`](Assessment/MeijerProducts/.claude/skills/run-meijerproducts/SKILL.md):

```powershell
$driver = "Assessment\MeijerProducts\.claude\skills\run-meijerproducts\driver.ps1"

powershell -File $driver launch                            # build + start, waits for the window
powershell -File $driver screenshot -Out C:\tmp\shot.png   # full-screen PNG
powershell -File $driver click -Name "Add to list"         # invoke a button by its text
powershell -File $driver get-text -Name "Add to list"      # read a button's text
powershell -File $driver stop                              # kill it, clear tracked state
```

**Known limitation:** the driver finds buttons via UI Automation's `ControlType.Button`, and the
product list's rows are a `TapGestureRecognizer` on a `Grid`, not buttons — they expose no
`InvokePattern`. So `click` **cannot navigate from the list to the detail screen**; do that step by
hand. `"Add to list"` on the detail screen is a real `Button` and drives fine.

See that skill's `SKILL.md` for the full command reference, prerequisites, and known gotchas.

## Architecture at a glance

- **MVVM throughout** — Views bind to ViewModels built on CommunityToolkit.Mvvm source generators
  (`[ObservableProperty]`, `[RelayCommand]`); no logic in code-behind.
- **Constructor DI** — services, ViewModels, and pages are registered in `MauiProgram.cs` and
  resolved by constructor injection.
- **Everything platform-specific sits behind an interface** — `IProductService` (HTTP),
  `ILocationService` (geolocation + reverse geocoding), `IShareService` (share sheet), and
  `INavigationService` (Shell navigation). That's what makes the ViewModels testable without a
  device.
- **Shell navigation** — list → detail passes the product id as a route query parameter
  (`productdetail?id=7`), not the whole object.
- **Minimal-API backend** — endpoints grouped in `ProductEndpoints.cs`, EF Core over SQLite with a
  checked-in migration and an idempotent seeder that runs at startup.
- **Separate list and detail DTOs** — the list endpoint deliberately omits `description`/`price`,
  matching the assessment's stated contract.

The reasoning behind these choices (and the alternatives ruled out) is logged in
[`docs/DECISIONS.md`](docs/DECISIONS.md). The architectural reference followed throughout is
`docs/Enterprise-Application-Patterns-Using-.NET-MAUI.md`.

## Known issues

- **Product images don't render**
  ([#16](https://github.com/RACAssessments/meijer-assessment/issues/16)) — the seeded image URLs
  point at Meijer's CDN, which returns HTTP 403 to non-browser clients. The data, layout, and image
  bindings are correct; the remote fetch is what fails.

- **"Add to list" may say "your area" instead of a city.** The share text falls back to
  `"... from your area added to list"`, along with an on-screen note, whenever a city can't be
  resolved. On a desktop with no GPS this is common even with location permission granted and the
  Windows location service running: the OS simply never acquires a position fix
  (`Geolocator.LocationStatus` stays `NotInitialized`), so reverse geocoding has nothing to work
  from. This is the designed fallback behaving correctly, not a failure — but it does mean the
  assessment's example output (`"Bananas - $0.59/lb from Chicago added to list"`) is best
  reproduced on a device with real location hardware.

## Project management

Issues and the Kanban board are managed via `gh`/GitHub's GraphQL API, using tokens in a local
(gitignored) `.env` — see `.claude/agents/kanban-manager.md` for the token setup and ready-to-run
recipes (creating issues, linking sub-issues, moving cards between Status columns).

## Decisions

Notable technical decisions (and why) are logged in [`docs/DECISIONS.md`](docs/DECISIONS.md).
