# Copie Congogame/playlist/*.mp3 vers UnityProject/.../Theme/playlist/track01.mp3, track02.mp3, ...
# (nécessaire pour le build WebGL ; le dossier racine /playlist sert en build Desktop).
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$src = Join-Path $root "playlist"
$dst = Join-Path $root "UnityProject\Assets\StreamingAssets\Theme\playlist"
if (-not (Test-Path $src)) {
    Write-Error "Dossier introuvable: $src"
}
New-Item -ItemType Directory -Force -Path $dst | Out-Null
$files = Get-ChildItem -LiteralPath $src -File -Filter *.mp3 | Sort-Object Name
$i = 1
foreach ($f in $files) {
    $name = "track{0:D2}.mp3" -f $i
    $dest = Join-Path $dst $name
    Copy-Item -LiteralPath $f.FullName -Destination $dest -Force
    Write-Host "OK $name <= $($f.Name)"
    $i++
}
Write-Host "Pistes copiees: $($i - 1) -> $dst"
