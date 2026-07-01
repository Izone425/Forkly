# Database setup

The API uses **PostgreSQL** via EF Core (Npgsql). The connection string lives in
`ConnectionStrings:DefaultConnection` (`appsettings.json`), overridden per
environment — e.g. the shared SIT database in `appsettings.Development.json`.

## Running PostgreSQL locally (no admin, no service)

This machine's installed PostgreSQL 18 has no registered Windows service, so the
API uses a **self-contained cluster** created under `.pgdata` with `initdb` and run
as the current user. It is **not** a service and does **not** survive a reboot.

Start it (creates the cluster + database on first run):

```powershell
pwsh -File scripts/start-postgres.ps1
```

Stop it:

```powershell
& "C:\Program Files\PostgreSQL\18\bin\pg_ctl.exe" -D ".pgdata" stop
```

Connection: `Host=localhost;Port=5432;Database=forkly;Username=forkly` (trust auth
on localhost, so the password is ignored).

### Using your system PostgreSQL service instead

If you later register/start the official service (elevated shell):

```powershell
& "C:\Program Files\PostgreSQL\18\bin\pg_ctl.exe" register -N "postgresql-x64-18" -D "C:\Program Files\PostgreSQL\18\data" -S auto
Start-Service postgresql-x64-18
```

…then create a `forkly` role + database and point `DefaultConnection` at it.

## Docker alternative

If you have Docker, `docker compose up -d` (see `docker-compose.yml`) runs
`postgres:16`; set `DefaultConnection`'s password to `forkly` to match.

## Secrets

`Jwt:Key` and the seed admin credentials are stored in **user-secrets** (loaded only
in the Development environment), not in `appsettings.json`. Run the API with
`ASPNETCORE_ENVIRONMENT=Development` (the default `dotnet run` launch profile does this).
