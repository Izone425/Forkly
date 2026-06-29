# Starts the local PostgreSQL 18 cluster used by the Forkly API in development.
#
# This is a self-contained cluster created with initdb under .pgdata — it is NOT
# a Windows service, so it does not survive a reboot. Re-run this script to start
# it again after restarting your machine. No admin rights are required.
#
#   pwsh -File scripts/start-postgres.ps1
#
# To stop it:   & "$PgBin\pg_ctl.exe" -D "<repo>\.pgdata" stop

$ErrorActionPreference = "Stop"

$PgBin = "C:\Program Files\PostgreSQL\18\bin"
$Data  = Join-Path $PSScriptRoot "..\.pgdata" | Resolve-Path -ErrorAction SilentlyContinue
if (-not $Data) { $Data = Join-Path $PSScriptRoot "..\.pgdata" }

# Create the cluster the first time (superuser 'forkly', trust auth on localhost).
if (-not (Test-Path (Join-Path $Data "PG_VERSION"))) {
    Write-Host "Initializing new PostgreSQL cluster at $Data ..."
    & "$PgBin\initdb.exe" -D "$Data" -U forkly -A trust -E UTF8 --locale=C | Out-Null
}

# Already running?
if (Get-NetTCPConnection -LocalPort 5432 -State Listen -ErrorAction SilentlyContinue) {
    Write-Host "PostgreSQL already listening on 5432."
} else {
    Write-Host "Starting PostgreSQL on port 5432 ..."
    & "$PgBin\pg_ctl.exe" -D "$Data" -l (Join-Path $Data "server.log") -o "-p 5432" start
    Start-Sleep -Seconds 2
}

# Ensure the application database exists.
& "$PgBin\createdb.exe" -h localhost -p 5432 -U forkly forkly 2>$null
Write-Host "Ready: postgresql://forkly@localhost:5432/forkly"
