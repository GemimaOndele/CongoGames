# Copie Congogame/Jeux_musique_playlist/* vers Unity StreamingAssets (embarqué dans le build Windows).
# Fermez Unity pendant la copie pour éviter des incohérences d’import (même principe que copy-playlist).
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$src = Join-Path $root "Jeux_musique_playlist"
$dst = Join-Path $root "UnityProject\Assets\StreamingAssets\Theme\Jeux_musique_playlist"
if (-not (Test-Path $src)) {
    Write-Error "Dossier introuvable: $src"
}
New-Item -ItemType Directory -Force -Path $dst | Out-Null
$n = 0
foreach ($ext in @("mp3", "ogg", "wav")) {
    Get-ChildItem -LiteralPath $src -File -Filter "*.$ext" -ErrorAction SilentlyContinue | ForEach-Object {
        $final = Join-Path $dst $_.Name
        $tmp = $final + ".partial"
        Copy-Item -LiteralPath $_.FullName -Destination $tmp -Force
        Move-Item -LiteralPath $tmp -Destination $final -Force
        $n++
    }
}
Write-Host "Jeux_musique_playlist: $n fichier(s) -> $dst"
