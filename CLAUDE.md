# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository state

This is a take-home coding assessment for a Meijer interview (see `docs/STME Take Home Assessment.md`).
Both halves of the assignment are built out:

- `Assessment/MeijerProducts` — the MAUI app: product list and detail screens (MVVM +
  CommunityToolkit.Mvvm, constructor DI, Shell navigation) with the location-aware "Add to list" share
  action.
- `Assessment/MeijerProducts.Api` — the ASP.NET Core minimal API: `GET /products` and
  `GET /products/{id}` over EF Core/SQLite, with a checked-in migration and an idempotent 30-product
  seeder that runs at startup. Containerized via `Assessment/docker-compose.yml`.
- `Assessment/MeijerProducts.Tests` and `Assessment/MeijerProducts.Api.Tests` — xUnit suites for each side.

Remaining work is tracked under issue #5 (testing, docs, and submission readiness). `README.md` is the
human-facing build/run/test guide; keep it in sync when behavior changes.

### Git Repository
https://github.com/RACAssessments/meijer-assessment.git

### Project
https://github.com/users/blanthor/projects/7

## The assignment

Full requirements: `docs/STME Take Home Assessment.md` (sample API JSON also in `docs/product-details.json`
and `docs/products (1).json`). Summary:

1. **Backend**: A .NET API exposing two endpoints — a products list (`id`, `imageUrl`, `summary`, `title`)
   and a product detail (`id`, `imageUrl`, `summary`, `title`, `description`, `price`) — backed by a
   persistence layer of choice, built with API best practices.
2. **MAUI app** (`Assessment/MeijerProducts`):
   - A product list screen (image thumbnail, title, summary) where tapping an item navigates to a detail
     screen (full image, title, description, price).
   - Detail screen has an "Add to list" action that builds a shareable string of the form
     `"{title} - {price} from {city} added to list"` (city derived from the device's current location) and
     invokes the platform share sheet.
3. Organize the code with solid architecture/best practices; unit tests are a plus.
4. Candidates are asked to disclose any AI tools/prompts/instructions/skills used to complete the
   assessment — keep that in mind when using Claude Code here (document prompts/approach for submission).

## Commands

All commands run from `Assessment/` (where `Assessment.slnx` lives) unless noted.

```powershell
# Restore
dotnet restore

# Build all target frameworks
dotnet build

# Build/run for a single platform (fastest inner loop — Windows, since dev is on Windows)
dotnet build MeijerProducts/MeijerProducts.csproj -f net10.0-windows10.0.19041.0
dotnet build MeijerProducts/MeijerProducts.csproj -t:Run -f net10.0-windows10.0.19041.0

# Android (requires Android SDK/emulator configured)
dotnet build MeijerProducts/MeijerProducts.csproj -f net10.0-android
dotnet build MeijerProducts/MeijerProducts.csproj -t:Run -f net10.0-android

# API
dotnet build MeijerProducts.Api/MeijerProducts.Api.csproj
cd MeijerProducts.Api; dotnet run --launch-profile http   # http://localhost:5217

# Tests — run per-project, never solution-wide (see below)
dotnet test MeijerProducts.Tests/MeijerProducts.Tests.csproj
dotnet test MeijerProducts.Api.Tests/MeijerProducts.Api.Tests.csproj
```

- Supported platforms only: `net10.0-android`, `net10.0-ios` (non-Linux hosts only), and
  `net10.0-windows10.0.19041.0` (Windows hosts only) — see `MeijerProducts.csproj`. MacCatalyst is not a
  supported target.
- iOS builds require a Mac (or a paired Mac) and aren't available from this Windows environment.
- **Never run `dotnet build`/`dotnet test` at the solution level** — it pulls the MAUI head into the build
  graph and builds every TFM, which is slow and fails outright without the Android workload. Always pass a
  specific project (and `-f` for the MAUI head).
- `MeijerProducts.Tests` targets `net10.0-windows10.0.19041.0` (exact match against one of the head's
  TFMs, with `ExcludeAssets` on the project reference to strip Resizetizer's build targets), so it runs on
  Windows only. `MeijerProducts.Api.Tests` is plain `net10.0` and self-hosts the API via
  `WebApplicationFactory` against a throwaway SQLite file — no running server needed.
- `LocationService`/`ShareService` are deliberately untested (they only delegate to MAUI Essentials
  statics); their behavior is verified by launching the app, not by unit tests.
- A build can fail with `MSB3021`/file-lock errors if a previously launched `MeijerProducts.exe` is still
  running — stop it via the `run-meijerproducts` skill's `driver.ps1 stop`.
- No linter/formatter is configured beyond the default .NET SDK analyzers.

## Architecture guidance

The conventions below are established in the codebase — match them rather than reinventing.
`docs/Enterprise-Application-Patterns-Using-.NET-MAUI.md` (Microsoft's MAUI enterprise patterns eBook,
bundled for reference) is the architectural guide they came from; consult it for anything not covered
here:

- **MVVM**: Views (XAML `ContentPage`s) bind to ViewModels; ViewModels expose bindable properties and
  `ICommand`s and hold no view references; Models are plain data/DTO classes. Avoid logic in code-behind.
- **Dependency injection**: register services/ViewModels/pages in `MauiProgram.cs` via
  `builder.Services`, resolve through constructor injection (standard `Microsoft.Extensions.DependencyInjection`,
  already wired into `MauiProgram`).
- **Services layer**: everything platform- or IO-bound sits behind an interface injected into ViewModels,
  never called statically from a ViewModel or code-behind. Existing: `IProductService` (HTTP),
  `ILocationService` (geolocation + reverse geocoding), `IShareService` (share sheet),
  `INavigationService` (Shell navigation). Follow this pattern for anything new — it's what keeps the
  ViewModels testable without a device.
- **Folder layout** inside `MeijerProducts/` (per the guide's "eShop project" structure): `Models/`,
  `ViewModels/`, `Views/`, `Services/`, `Converters/`, `Helpers/`.
- **Navigation**: Shell navigation with routes registered in `AppShell.xaml` and route names in
  `Helpers/Routes.cs`. ViewModels call `INavigationService.GoToAsync(...)`, **not** `Shell.Current`
  directly. List → detail passes the product id as a route query parameter (`productdetail?id=7`),
  received via `IQueryAttributable.ApplyQueryAttributes` — never the whole object.
- For the location → city lookup and the share action, use MAUI's cross-platform abstractions
  (`Microsoft.Maui.Devices.Sensors.Geolocation`, `Microsoft.Maui.ApplicationModel.DataTransfer.Share`)
  rather than platform-specific code — wrapped behind `ILocationService`/`IShareService` as above.
  Platform permissions (location) are already declared under `Platforms/*`
  (`Platforms/Android/AndroidManifest.xml`, iOS `Info.plist`, Windows `Package.appxmanifest`).
- **Backend**: minimal APIs grouped in `Endpoints/ProductEndpoints.cs` (no controllers), separate list
  and detail DTOs in `Contracts/` — the list DTO deliberately omits `description`/`price` per the
  assessment's contract — EF Core over SQLite in `Data/`, with `Migrate()` + an idempotent seeder at
  startup.

## Workflow: "work on issue #N"

When the user asks to work on a specific GitHub issue, follow this sequence and don't skip steps:

1. Look up the issue (`gh issue view <N> --repo RACAssessments/meijer-assessment`) to see the actual
   scope before doing anything else.
2. Move the issue's card on the project board (https://github.com/users/blanthor/projects/7) to
   **"In progress"** — use the `github-tracker` agent (from the `maui-factory` plugin).
3. Create a new branch off `master` for the issue (e.g. `git checkout -b <N>-short-slug`) — do this
   even if there are uncommitted changes sitting on `master`; they carry over onto the new branch.
4. Ask the user for explicit confirmation before writing any code ("Ready to start coding on this?" or
   similar). Wait for a yes before making file changes.

Only after confirmation should implementation begin. This applies every time, not just the first time —
don't assume it's understood from a prior turn in the conversation.

## GitHub access

Board and issue work goes through the **`github-tracker` agent** from the `maui-factory` plugin. It
reads this project's coordinates — repo, board owner/number, and the GraphQL node IDs for the
Status/Priority/Size fields — from `.claude/tracker.json`, and re-derives them if they ever go stale.
Don't hand-roll board mutations, and don't copy node IDs anywhere else.

For direct `gh` calls, use the classic token `GITHUB_TOKEN_CLASSIC` from `.env` — the default `gh`
token is a fine-grained PAT and cannot access the RACAssessments organization. Set `GH_TOKEN` to this
value before running commands that touch the project or org-specific resources. **Never print a token
value or read `.env` into the conversation** — pipe it straight into the environment variable.

`gh project` subcommands do not work against this board (`unknown owner type` / `resource not found`)
— that's a client-side bug in the CLI wrapper, not permissions. Always use `gh api graphql` directly.

## Decision log

`docs/DECISIONS.md` tracks notable technical decisions (context, decision, why) for this
assessment. Before adding/removing/upgrading a NuGet package, adding or changing a device
capability/permission (location, camera, etc.), or altering an architectural layer (new
project, new service abstraction, changed navigation pattern, etc.), ask the user whether it
warrants a new entry — don't add the entry unprompted, and don't skip asking.
