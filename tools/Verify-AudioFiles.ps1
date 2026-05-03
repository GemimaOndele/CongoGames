#!/usr/bin/env pwsh
# CongoGames — Vérifie doublons BGM Resources + vidéos Theme (SHA256).
# Usage depuis la racine du dépôt :
#   pwsh -File tools/Verify-AudioFiles.ps1
#   pwsh -File tools/Verify-AudioFiles.ps1 -ProjectRoot .\UnityProject

param(
    [string]$ProjectRoot = ""
)

$ErrorActionPreference = "Stop"

function Resolve-UnityProjectRoot {
    param([string]$Hint)
    if (-not [string]::IsNullOrWhiteSpace($Hint) -and (Test-Path $Hint)) {
        $assets = Join-Path $Hint "Assets"
        if (Test-Path $assets) { return (Resolve-Path $Hint).Path }
    }
    $here = $PSScriptRoot
    if ($here) {
        $try = Join-Path $here "..\UnityProject"
        if (Test-Path (Join-Path $try "Assets")) { return (Resolve-Path $try).Path }
    }
    $try2 = Join-Path (Get-Location) "UnityProject"
    if (Test-Path (Join-Path $try2 "Assets")) { return (Resolve-Path $try2).Path }
    return $null
}

$root = Resolve-UnityProjectRoot $ProjectRoot
if (-not $root) {
    Write-Host "Impossible de trouver UnityProject (Assets/). Utilise : -ProjectRoot chemin\vers\UnityProject" -ForegroundColor Red
    exit 1
}

$bgmFolder = Join-Path $root "Assets\Resources\Audio\BGM"
$videoFolder = Join-Path $root "Assets\StreamingAssets\Theme"

Write-Host "UnityProject : $root" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

Write-Host "`nBGM : $bgmFolder`n" -ForegroundColor Yellow

if (-not (Test-Path $bgmFolder)) {
    Write-Host "Dossier BGM absent. Crée : Assets/Resources/Audio/BGM" -ForegroundColor Red
    exit 1
}

$audioExtensions = @(".wav", ".ogg", ".mp3", ".m4a")
$bgmFiles = Get-ChildItem $bgmFolder -File | Where-Object { $audioExtensions -contains $_.Extension.ToLowerInvariant() }
$hashToNames = @{}
$dupAudio = $false

foreach ($f in $bgmFiles) {
    $h = (Get-FileHash $f.FullName -Algorithm SHA256).Hash
    if ($hashToNames.ContainsKey($h)) {
        Write-Host "  DOUBLON : $($f.Name) == $($hashToNames[$h])  ($($f.Length) bytes)" -ForegroundColor Red
        $dupAudio = $true
    }
    else {
        Write-Host "  OK $($f.Name) ($($f.Length) bytes)" -ForegroundColor Green
        $hashToNames[$h] = $f.Name
    }
}

if (-not $dupAudio -and $bgmFiles.Count -gt 0) {
    Write-Host "`nAucun doublon détecté parmi les $($bgmFiles.Count) fichier(s) audio BGM." -ForegroundColor Green
}

Write-Host "`nVidéos Theme : $videoFolder`n" -ForegroundColor Yellow

if (-not (Test-Path $videoFolder)) {
    Write-Host "Dossier Theme absent." -ForegroundColor Red
    exit 1
}

$videoDirs = @(
    "quiz", "semantic", "word-scramble", "crossword-lite", "blind-test",
    "mystery-word", "memory", "speed-chrono", "image-guess"
)

$videoHashToPath = @{}
$dupVideo = $false

foreach ($dirName in $videoDirs) {
    $dirPath = Join-Path $videoFolder $dirName
    if (-not (Test-Path $dirPath)) {
        Write-Host "  MANQUANT dossier : Theme/$dirName/" -ForegroundColor Yellow
        continue
    }
    $clips = Get-ChildItem $dirPath -File | Where-Object {
        $e = $_.Extension.ToLowerInvariant()
        $e -eq ".mp4" -or $e -eq ".webm" -or $e -eq ".mov"
    }
    $bg = $clips | Where-Object { $_.Name -match '^(background|loop|show|theatre)\.' }
    if (-not $bg) {
        Write-Host "  Pas de background/loop/show/theatre .mp4|.webm dans : $dirName/" -ForegroundColor Yellow
        continue
    }
    foreach ($v in $bg) {
        $h = (Get-FileHash $v.FullName -Algorithm SHA256).Hash
        $rel = "$dirName/$($v.Name)"
        if ($videoHashToPath.ContainsKey($h)) {
            Write-Host "  DOUBLON vidéo : $rel == $($videoHashToPath[$h])" -ForegroundColor Red
            $dupVideo = $true
        }
        else {
            Write-Host "  OK $rel ($($v.Length) bytes)" -ForegroundColor Green
            $videoHashToPath[$h] = $rel
        }
    }
}

$showMp4 = Join-Path $videoFolder "show.mp4"
if (Test-Path $showMp4) {
    $h = (Get-FileHash $showMp4 -Algorithm SHA256).Hash
    if ($videoHashToPath.ContainsKey($h)) {
        Write-Host "  DOUBLON : show.mp4 == $($videoHashToPath[$h])" -ForegroundColor Red
        $dupVideo = $true
    }
    else {
        Write-Host "  OK show.mp4 (racine Theme)" -ForegroundColor Green
        $videoHashToPath[$h] = "show.mp4"
    }
}

Write-Host "`nGuide : docs/AUDIO_VIDEO_DOWNLOAD_GUIDE.md" -ForegroundColor Cyan
if ($dupAudio -or $dupVideo) {
    Write-Host "`nAction : remplacer les fichiers identiques par des médias différents." -ForegroundColor Yellow
    exit 2
}
exit 0
