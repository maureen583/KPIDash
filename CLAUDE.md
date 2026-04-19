# KPIDash

Blazor WebAssembly + ASP.NET Core Web API demo app showing how client data stays
private. Fake data simulates a rubber mixing operation with 4 pieces of equipment.

## Stack

- **KPIDash.UI** — Blazor WASM (UI)
- **KPIDash.API** — ASP.NET Core Web API + Swagger
- **KPIDash.Data** — Dapper + SQLite (server-side only)
- **KPIDash.Services** — Business logic
- **KPIDash.Seeder** — Bogus fake data generator

## Database

SQLite at `/data/kpi.db`. Schema managed via numbered migration scripts in
`KPIDash.Data/Scripts/`. DbConnectionFactory runs pending migrations on startup.

## Commands

- `dotnet build` — build solution
- `dotnet run --project KPIDash.API` — run API + serve WASM
- `dotnet restore` — restore NuGet packages
- `sqlite3 /data/kpi.db` — inspect database

## Testing Rules

- Always kill any running `dotnet run` processes and free the port before finishing a session
- Use `lsof -ti:5250 | xargs kill -9` to clear port 5250 after testing
- Never leave background server processes running

## Architecture Rules

- UI never references Data, Services, or Seeder directly
- UI calls API via HTTP only — all data access is server-side
- New DB changes go in a new numbered migration file, never edit existing ones
- Repositories own all SQL — no raw queries in Services or Controllers

## Domain

Rubber mixing operation with 4 equipment pieces in sequence:

1. Conveyor (infeed)
2. Internal Mixer (Banbury)
3. Mill
4. Cooling Line

Equipment status is derived from sensor readings: Running / Idle / Down.
Idle = healthy but not producing. Down = fault or parameter out of range.
