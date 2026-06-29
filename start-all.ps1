# =====================================================================
# Forkly — one-command local dev launcher.
#
# Brings up ALL THREE apps that the integrated landing needs:
#   * Forkly-Api      (.NET backend)  -> http://localhost:5080
#   * Forkly-Landing  (Vue, this dir) -> http://localhost:5173
#   * Forkly-Auth     (Vue, login)    -> http://localhost:5174
#
# The landing's login drawer embeds Forkly-Auth (VITE_LOGIN_URL=:5174), so
# login only works when Forkly-Auth is running. Run this and you never have to
# remember to start each app (or `npm install`) by hand.
#
# Usage (from the repo root):   ./start-all.ps1
# =====================================================================

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# --- Free stale dev servers so each app claims the port it expects ----------
# (Forkly-Auth uses strictPort:5174 — if 5174 is taken it would refuse to start,
#  which is exactly what leaves the login drawer blank.)
function Free-Port($port) {
    try {
        Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction Stop |
            Select-Object -ExpandProperty OwningProcess -Unique |
            ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }
    } catch {
        # Nothing listening on this port — fine.
    }
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

Write-Host "Freeing ports 5080, 5173, 5174 ..." -ForegroundColor Cyan
5080, 5173, 5174 | ForEach-Object { Free-Port $_ }

Write-Host "Checking frontend dependencies ..." -ForegroundColor Cyan
Ensure-Deps $root
Ensure-Deps (Join-Path $root 'Forkly-Auth')

Write-Host "Starting all three Forkly apps ..." -ForegroundColor Cyan
Start-App 'Forkly-API'     (Join-Path $root 'Forkly-Api')  'dotnet run'
Start-App 'Forkly-Landing' $root                           'npm run dev'
Start-App 'Forkly-Auth'    (Join-Path $root 'Forkly-Auth') 'npm run dev'

Write-Host ""
Write-Host "Forkly is starting in three separate windows:" -ForegroundColor Green
Write-Host "  API      -> http://localhost:5080"
Write-Host "  Landing  -> http://localhost:5173   (open this one)"
Write-Host "  Auth     -> http://localhost:5174   (login drawer embeds this)"
Write-Host ""
Write-Host "Give them a few seconds, then open http://localhost:5173 and click Login."
