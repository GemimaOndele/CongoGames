# Réutilise Theme/show.mp4 comme background.mp4 pour chaque mode (liens physiques NTFS = une seule copie sur disque).
# Usage : powershell -ExecutionPolicy Bypass -File tools/theme-link-background-videos.ps1
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$theme = Join-Path $root "UnityProject/Assets/StreamingAssets/Theme"
$src = Join-Path $theme "show.mp4"
if (-not (Test-Path $src)) {
    Write-Error "Manquant : $src — place une vidéo loop à la racine Theme ou ajuste ce script."
}
$dirs = @(
    "quiz", "semantic", "word-scramble", "crossword-lite", "blind-test",
    "mystery-word", "memory", "speed-chrono", "image-guess"
)
foreach ($d in $dirs) {
    $dir = Join-Path $theme $d
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
    foreach ($name in @("background.mp4", "loop.mp4")) {
        $dst = Join-Path $dir $name
        if (Test-Path $dst) { Remove-Item $dst -Force }
        cmd /c mklink /H "`"$dst`"" "`"$src`""
    }
}
$globalDir = Join-Path $theme "_global"
if (-not (Test-Path $globalDir)) { New-Item -ItemType Directory -Path $globalDir | Out-Null }
foreach ($name in @("background.mp4", "loop.mp4")) {
    $dst = Join-Path $globalDir $name
    if (Test-Path $dst) { Remove-Item $dst -Force }
    cmd /c mklink /H "`"$dst`"" "`"$src`""
}
Write-Host "OK — background.mp4 + loop.mp4 liés à show.mp4 (rotation vidéo + _global)."
