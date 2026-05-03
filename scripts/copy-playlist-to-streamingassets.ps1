# Copie Congogame/playlist/*.mp3 vers UnityProject/.../Theme/playlist/ en conservant
# les noms d'origine (source de vérité pour les questions blind test).
# Génère aussi des .ogg miroir pour une lecture Unity/FMOD plus robuste.
#
# Unity ouvert : les fichiers peuvent être verrouillés ; les Move-Item sont réessayés.
# Pour éviter les erreurs d'import (octets partiels), fermer Unity reste le plus sûr.

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$src = Join-Path $root "playlist"
$dst = Join-Path $root "UnityProject\Assets\StreamingAssets\Theme\playlist"
if (-not (Test-Path $src)) {
    Write-Error "Dossier introuvable: $src"
}

function Move-ReplaceWithRetry {
    param(
        [Parameter(Mandatory = $true)][string] $SourceLiteralPath,
        [Parameter(Mandatory = $true)][string] $DestinationLiteralPath,
        [int] $MaxAttempts = 40,
        [int] $DelayMilliseconds = 400
    )
    for ($a = 1; $a -le $MaxAttempts; $a++) {
        try {
            Move-Item -LiteralPath $SourceLiteralPath -Destination $DestinationLiteralPath -Force -ErrorAction Stop
            return
        }
        catch [System.IO.IOException] {
            if ($a -eq $MaxAttempts) {
                throw
            }
            Start-Sleep -Milliseconds $DelayMilliseconds
        }
        catch [System.UnauthorizedAccessException] {
            if ($a -eq $MaxAttempts) {
                throw
            }
            Start-Sleep -Milliseconds $DelayMilliseconds
        }
    }
}

New-Item -ItemType Directory -Force -Path $dst | Out-Null
Get-ChildItem -LiteralPath $dst -File -Filter *.partial -ErrorAction SilentlyContinue |
    Remove-Item -Force -ErrorAction SilentlyContinue

# Pas de suppression globale des .mp3/.ogg : Unity verrouille souvent ces fichiers (Remove-Item échoue).

$files = Get-ChildItem -LiteralPath $src -File -Filter *.mp3 |
    Where-Object { $_.Name -notlike "*.reenc.tmp.mp3" } |
    Sort-Object Name
$i = 1
foreach ($f in $files) {
    $name = $f.Name
    $dest = Join-Path $dst $name
    $destTmp = $dest + ".partial"
    Copy-Item -LiteralPath $f.FullName -Destination $destTmp -Force
    Move-ReplaceWithRetry -SourceLiteralPath $destTmp -DestinationLiteralPath $dest

    $oggName = [System.IO.Path]::GetFileNameWithoutExtension($name) + ".ogg"
    $oggDest = Join-Path $dst $oggName
    $oggTmp = $oggDest + ".partial"
    if (Test-Path -LiteralPath $oggTmp) {
        Remove-Item -LiteralPath $oggTmp -Force -ErrorAction SilentlyContinue
    }

    $ffmpegErr = $null
    $ffmpegOk = $false
    try {
        $null = & ffmpeg -y -hide_banner -loglevel error -i "$dest" -vn -c:a libvorbis -qscale:a 4 "$oggTmp" 2>&1
        if ($LASTEXITCODE -ne 0) {
            $ffmpegErr = "ffmpeg exit code $LASTEXITCODE"
        }
        elseif (-not (Test-Path -LiteralPath $oggTmp)) {
            $ffmpegErr = "sortie .partial introuvable"
        }
        elseif ((Get-Item -LiteralPath $oggTmp).Length -lt 256) {
            $ffmpegErr = "fichier .ogg trop petit"
            Remove-Item -LiteralPath $oggTmp -Force -ErrorAction SilentlyContinue
        }
        else {
            Move-ReplaceWithRetry -SourceLiteralPath $oggTmp -DestinationLiteralPath $oggDest
            $ffmpegOk = $true
        }
    }
    catch {
        $ffmpegErr = $_.Exception.Message
    }

    if ($ffmpegOk) {
        Write-Host "OK $name + $oggName"
    }
    else {
        Write-Warning "Copie OK mais conversion OGG échouée pour $name. Vérifie ffmpeg. Détail: $ffmpegErr"
    }
    $i++
}
Write-Host "Pistes copiees: $($i - 1) -> $dst"
