#!/usr/bin/env pwsh
# Reencode Theme MP4 (H.264 baseline + yuv420p + couleurs explicites) pour Unity / Windows Media Foundation.
# Corrige : "Unexpected timestamp values" (profil H.264 non-baseline) et "Color primaries 0".
#
# Usage (racine du depot) :
#   pwsh -File tools/Reencode-ThemeMp4-Baseline.ps1
#   pwsh -File tools/Reencode-ThemeMp4-Baseline.ps1 -ProjectRoot C:\Congogame\UnityProject

param(
    [string]$ProjectRoot = ""
)

$ErrorActionPreference = "Stop"

$ffmpeg = Get-Command ffmpeg -ErrorAction SilentlyContinue
if (-not $ffmpeg) {
    Write-Host "ffmpeg introuvable (PATH). Installe avec : winget install ffmpeg" -ForegroundColor Red
    exit 1
}

function Resolve-UnityProject([string]$Hint) {
    if ($Hint -and (Test-Path $Hint)) {
        $a = Join-Path $Hint "Assets"
        if (Test-Path $a) { return (Resolve-Path $Hint).Path }
    }
    $try = Join-Path $PSScriptRoot "..\UnityProject"
    if (Test-Path (Join-Path $try "Assets")) { return (Resolve-Path $try).Path }
    $try2 = Join-Path (Get-Location) "UnityProject"
    if (Test-Path (Join-Path $try2 "Assets")) { return (Resolve-Path $try2).Path }
    return $null
}

$root = Resolve-UnityProject $ProjectRoot
if (-not $root) {
    Write-Host "UnityProject introuvable. Utilise -ProjectRoot." -ForegroundColor Red
    exit 1
}

$theme = Join-Path $root "Assets\StreamingAssets\Theme"
if (-not (Test-Path $theme)) {
    Write-Host "Dossier Theme introuvable : $theme" -ForegroundColor Red
    exit 1
}

$mp4s = Get-ChildItem -Path $theme -Filter "*.mp4" -Recurse -File
if (-not $mp4s) {
    Write-Host "Aucun .mp4 sous $theme" -ForegroundColor Yellow
    exit 0
}

Write-Host "Theme : $theme" -ForegroundColor Cyan
Write-Host "Fichiers : $($mp4s.Count)" -ForegroundColor Cyan
Write-Host ""

foreach ($f in $mp4s) {
    $tmp = $f.FullName + ".baseline.tmp.mp4"
    $rel = $f.FullName.Substring($theme.Length).TrimStart([char]'\')
    Write-Host "  -> $rel ... " -NoNewline
    # VUI x264 (bt709) + conteneur : reduit "Color primaries 0" sous Windows Media Foundation.
    & ffmpeg -hide_banner -loglevel error -y -i $f.FullName `
        -c:v libx264 -profile:v baseline -level 3.1 -pix_fmt yuv420p `
        -x264-params "colorprim=bt709:transfer=bt709:colormatrix=bt709" `
        -colorspace bt709 -color_primaries bt709 -color_trc bt709 `
        -c:a aac -b:a 128k -movflags +faststart `
        $tmp
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ECHEC ffmpeg" -ForegroundColor Red
        Remove-Item -Force $tmp -ErrorAction SilentlyContinue
        continue
    }
    Move-Item -Force $tmp $f.FullName
    Write-Host "OK" -ForegroundColor Green
}

Write-Host ""
Write-Host "Termine. Unity : Ctrl+R puis rejoue." -ForegroundColor Cyan
