# Meijer Products

Take-home coding assessment for a Meijer interview — a .NET MAUI app with a product list/detail UI,
backed by a .NET Web API. Full requirements: `docs/STME Take Home Assessment.md`. Work is tracked
as GitHub issues in [RACAssessments/meijer-assessment](https://github.com/RACAssessments/meijer-assessment)
and the [project board](https://github.com/users/blanthor/projects/7).

## Repository layout

```
Assessment/
  Assessment.slnx           # solution file — build/restore from here
  MeijerProducts/            # .NET MAUI app (Android, iOS, Windows)
  MeijerProducts.Api/        # ASP.NET Core Web API (products list/detail, EF Core + SQLite)
  docker-compose.yml         # containerized API
docs/                        # assessment spec, sample API JSON, architecture reference, decision log
```

## Building

From `Assessment/`:

```powershell
dotnet restore
dotnet build
```

To build/run a single platform (Windows is fastest for local dev on Windows):

```powershell
dotnet build MeijerProducts/MeijerProducts.csproj -f net10.0-windows10.0.19041.0
dotnet build MeijerProducts/MeijerProducts.csproj -t:Run -f net10.0-windows10.0.19041.0
```

Android requires the Android SDK/emulator; iOS requires a Mac. See `CLAUDE.md` for the full command
reference and architecture conventions.

## Running the API in Docker

From `Assessment/`:

```powershell
docker compose up --build
```

- Products: http://localhost:8080/products
- Swagger: http://localhost:8080/swagger

Seeded SQLite data lives on a named volume, so it survives `docker compose restart`. Stop with
`docker compose down` (add `-v` to also wipe the volume and reset seeded data).

## Running the app (agents / automation)

For anything that needs to launch the app programmatically and interact with it (click buttons,
take screenshots) rather than just build it, use the driver script in
[`Assessment/MeijerProducts/.claude/skills/run-meijerproducts/`](Assessment/MeijerProducts/.claude/skills/run-meijerproducts/SKILL.md):

```powershell
$driver = "Assessment\MeijerProducts\.claude\skills\run-meijerproducts\driver.ps1"

powershell -File $driver launch                          # build + start, waits for the window
powershell -File $driver screenshot -Out C:\tmp\shot.png  # full-screen PNG
powershell -File $driver click -Name "Click me"           # invoke a button by its text
powershell -File $driver get-text                         # read the app's button text
powershell -File $driver stop                             # kill it, clear tracked state
```

See that skill's `SKILL.md` for the full command reference, prerequisites, and known gotchas.

## Project management

Issues and the Kanban board are managed via `gh`/GitHub's GraphQL API, using tokens in a local
(gitignored) `.env` — see `.claude/agents/kanban-manager.md` for the token setup and ready-to-run
recipes (creating issues, linking sub-issues, moving cards between Status columns).

## Decisions

Notable technical decisions (and why) are logged in `docs/DECISIONS.md`.
