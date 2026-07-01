# =====================================================================
# Forkly — one-command local dev launcher.
#
# Brings up Forkly for local dev:
#   * Forkly-Api         (.NET backend)   -> http://localhost:5080
#   * Forkly.MenuService (.NET backend)   -> http://localhost:5100
#   * Forkly-Landing     (Vue, this dir)  -> http://localhost:5173
#
# Login, register and account are served IN the landing app now (no separate
# auth app/port). Run this and you never have to start each piece (or
# `npm install`) by hand.
#
# Usage (from the repo root):   ./start-all.ps1
# =====================================================================

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# --- Free stale dev servers so each app claims the port it expects ----------
function Free-Port($port) {
    try {
        Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction Stop |
            Select-Object -ExpandProperty OwningProcess -Unique |
            ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }
    } catch {
        # Nothing listening on this port — fine.
    }
}

# --- Reap orphaned Forkly backends ------------------------------------------
# Free-Port only kills the process that currently *listens* on a port. Crashed or
# duplicate instances that never claimed their port linger (and lock their .exe,
# breaking the next build). Kill any Forkly apphost left running, by name.
function Stop-StaleForkly {
    Get-Process -Name 'Forkly.Api', 'Forkly.MenuService', 'Forkly.OrderService' -ErrorAction SilentlyContinue |
        ForEach-Object { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue }
}

# --- Install node deps only when missing ------------------------------------
function Ensure-Deps($dir) {
    if (-not (Test-Path (Join-Path $dir 'node_modules'))) {
        Write-Host "Installing dependencies in $dir ..." -ForegroundColor Yellow
        Push-Location $dir
        try { npm install } finally { Pop-Location }
    }
}

# --- Launch an app in its own titled window ---------------------------------
function Start-App($title, $dir, $cmd) {
    Start-Process powershell -ArgumentList @(
        '-NoExit', '-Command',
        "`$Host.UI.RawUI.WindowTitle = '$title'; Set-Location '$dir'; $cmd"
    )
}

Write-Host "Freeing ports 5080, 5100, 5102, 5173 and reaping stale Forkly backends ..." -ForegroundColor Cyan
5080, 5100, 5102, 5173 | ForEach-Object { Free-Port $_ }
Stop-StaleForkly

Write-Host "Checking frontend dependencies ..." -ForegroundColor Cyan
Ensure-Deps $root

Write-Host "Starting Forkly (API + Menu service + landing) ..." -ForegroundColor Cyan
Start-App 'Forkly-API'     (Join-Path $root 'Forkly-Api')                   'dotnet run'
Start-App 'Forkly-Menu'    (Join-Path $root 'Forkly-MenuApi')               'dotnet run'
Start-App 'Forkly-Order'   (Join-Path $root 'backend/Forkly.OrderService')  'dotnet run'
Start-App 'Forkly-Landing' $root                                            'npm run dev'

Write-Host ""
Write-Host "Forkly is starting in separate windows:" -ForegroundColor Green
Write-Host "  API      -> http://localhost:5080"
Write-Host "  Menu     -> http://localhost:5100   (swagger at /swagger)"
Write-Host "  Landing  -> http://localhost:5173   (open this one)"
Write-Host ""
Write-Host "Give them a few seconds, then open http://localhost:5173."
