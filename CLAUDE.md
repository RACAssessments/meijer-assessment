# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository state

This is a take-home coding assessment for a Meijer interview (see `docs/STME Take Home Assessment.md`).
The `Assessment/MeijerProducts` project is currently the **unmodified `dotnet new maui` template** — it has
the default counter-button `MainPage` and no app-specific code yet. There is no backend API project in the
repo yet either. The task is to build both from this starting point.

### Git Repository
https://github.com/RACAssessments/meijer-assessment.git

### Project
https://github.com/users/blanthor/projects/7

## The assignment

Full requirements: `docs/STME Take Home Assessment.md` (sample API JSON also in `docs/product-details.json`
and `docs/products (1).json`). Summary:

1. **Backend**: A .NET API exposing two endpoints — a products list (`id`, `imageUrl`, `summary`, `title`)
   and a product detail (`id`, `imageUrl`, `summary`, `title`, `description`, `price`) — backed by a
   persistence layer of choice, built with API best practices. No backend project exists yet; it needs to
   be created (e.g. as a sibling ASP.NET Core Web API project alongside `MeijerProducts` in `Assessment/`,
   added to `Assessment.slnx`).
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
```

- Supported platforms only: `net10.0-android`, `net10.0-ios` (non-Linux hosts only), and
  `net10.0-windows10.0.19041.0` (Windows hosts only) — see `MeijerProducts.csproj`. MacCatalyst is not a
  supported target.
- iOS builds require a Mac (or a paired Mac) and aren't available from this Windows environment.
- There is no test project yet. If unit tests are added (ViewModels/services), prefer a standard
  `dotnet test`-compatible project (xUnit/NUnit) referencing the MAUI project's non-UI classes, since
  running MAUI `ContentPage`/XAML code outside a platform head isn't practical to test directly.
- No linter/formatter is configured beyond the default .NET SDK analyzers.

## Architecture guidance

There's no established in-repo architecture yet beyond the MAUI template's `App` → `AppShell` → `MainPage`
shell navigation. `docs/Enterprise-Application-Patterns-Using-.NET-MAUI.md` (Microsoft's MAUI enterprise
patterns eBook, bundled for reference) is the intended architectural guide — follow it rather than
reinventing conventions:

- **MVVM**: Views (XAML `ContentPage`s) bind to ViewModels; ViewModels expose bindable properties and
  `ICommand`s and hold no view references; Models are plain data/DTO classes. Avoid logic in code-behind.
- **Dependency injection**: register services/ViewModels/pages in `MauiProgram.cs` via
  `builder.Services`, resolve through constructor injection (standard `Microsoft.Extensions.DependencyInjection`,
  already wired into `MauiProgram`).
- **Services layer**: put HTTP client / API access behind an interface (e.g. `IProductService`) injected
  into ViewModels, not called directly from views or code-behind.
- **Suggested folder layout** inside `MeijerProducts/` (per the guide's "eShop project" structure — add
  folders as needed, don't pre-create empty ones): `Models/`, `ViewModels/`, `Views/`, `Services/`,
  `Converters/`, `Helpers/`.
- **Navigation**: use Shell navigation (`AppShell.xaml` route registration + `Shell.Current.GoToAsync`)
  for list → detail, passing the product id as a route parameter rather than the whole object.
- For the location → city lookup and the share action, use MAUI's cross-platform abstractions
  (`Microsoft.Maui.Devices.Sensors.Geolocation`, `Microsoft.Maui.ApplicationModel.DataTransfer.Share`)
  rather than platform-specific code, and remember to declare the relevant platform permissions
  (location) under `Platforms/*` (e.g. `Platforms/Android/AndroidManifest.xml`, iOS `Info.plist`).

## Decision log

`docs/DECISIONS.md` tracks notable technical decisions (context, decision, why) for this
assessment. Before adding/removing/upgrading a NuGet package, adding or changing a device
capability/permission (location, camera, etc.), or altering an architectural layer (new
project, new service abstraction, changed navigation pattern, etc.), ask the user whether it
warrants a new entry — don't add the entry unprompted, and don't skip asking.
