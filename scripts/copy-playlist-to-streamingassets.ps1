# Copie Congogame/playlist/*.mp3 vers UnityProject/.../Theme/playlist/ en conservant
# les noms d'origine (source de vérité pour les questions blind test).
# Génère aussi des .ogg miroir pour une lecture Unity/FMOD plus robuste.
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$src = Join-Path $root "playlist"
$dst = Join-Path $root "UnityProject\Assets\StreamingAssets\Theme\playlist"
if (-not (Test-Path $src)) {
    Write-Error "Dossier introuvable: $src"
}
New-Item -ItemType Directory -Force -Path $dst | Out-Null
# Nettoie les anciennes pistes copiées (trackXX et anciennes versions)
Get-ChildItem -LiteralPath $dst -File -Filter *.mp3 -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -LiteralPath $dst -File -Filter *.ogg -ErrorAction SilentlyContinue | Remove-Item -Force
$files = Get-ChildItem -LiteralPath $src -File -Filter *.mp3 |
    Where-Object { $_.Name -notlike "*.reenc.tmp.mp3" } |
    Sort-Object Name
$i = 1
foreach ($f in $files) {
    $name = $f.Name
    $dest = Join-Path $dst $name
    Copy-Item -LiteralPath $f.FullName -Destination $dest -Force
    $oggName = [System.IO.Path]::GetFileNameWithoutExtension($name) + ".ogg"
    $oggDest = Join-Path $dst $oggName
    $ffmpegErr = $null
    try {
        $null = & ffmpeg -y -hide_banner -loglevel error -i "$dest" -vn -c:a libvorbis -qscale:a 4 "$oggDest" 2>&1
    }
    catch {
        $ffmpegErr = $_.Exception.Message
    }
    if ([string]::IsNullOrWhiteSpace($ffmpegErr) -and (Test-Path $oggDest)) {
        Write-Host "OK $name + $oggName"
    }
    else {
        Write-Warning "Copie OK mais conversion OGG échouée pour $name. Vérifie ffmpeg. Détail: $ffmpegErr"
    }
    $i++
}
Write-Host "Pistes copiees: $($i - 1) -> $dst"
