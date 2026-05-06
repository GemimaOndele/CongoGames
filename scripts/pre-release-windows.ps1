param(
    [string]$UnityExe = "",
    [string]$OutputExe = "",
    [switch]$SkipBuild,
    [switch]$RequireTtsHealthy
)

$ErrorActionPreference = "Stop"

function Resolve-CongoRoot {
    $candidates = @(
        (Join-Path $PSScriptRoot ".."),
        $PSScriptRoot
    )

    foreach ($candidate in $candidates) {
        $full = [System.IO.Path]::GetFullPath($candidate)
        if (Test-Path -LiteralPath (Join-Path $full "UnityProject")) {
            return $full
        }
    }

    throw "Racine CongoGames introuvable depuis: $PSScriptRoot"
}

function Resolve-UnityExe([string]$preferred) {
    $paths = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($preferred)) { [void]$paths.Add($preferred) }
    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_EXE)) { [void]$paths.Add($env:UNITY_EXE) }

    $known = "C:\Program Files\Unity\Hub\Editor\6000.4.3f1\Editor\Unity.exe"
    [void]$paths.Add($known)

    $hubRoot = "C:\Program Files\Unity\Hub\Editor"
    if (Test-Path -LiteralPath $hubRoot) {
        Get-ChildItem -LiteralPath $hubRoot -Directory -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending |
            ForEach-Object {
                [void]$paths.Add((Join-Path $_.FullName "Editor\Unity.exe"))
            }
    }

    foreach ($p in $paths | Select-Object -Unique) {
        if (Test-Path -LiteralPath $p) {
            return [System.IO.Path]::GetFullPath($p)
        }
    }

    throw "Unity.exe introuvable. Passe -UnityExe ou définit UNITY_EXE."
}

function Test-CommandAvailable([string]$name) {
    $cmd = Get-Command $name -ErrorAction SilentlyContinue
    return $null -ne $cmd
}

function Test-TtsHealthy {
    try {
        $resp = Invoke-RestMethod -Uri "http://127.0.0.1:3000/tts/status" -TimeoutSec 3
        return [bool]$resp.enabled
    }
    catch {
        return $false
    }
}

function Assert-PlaylistFiles([string]$playlistDir) {
    if (-not (Test-Path -LiteralPath $playlistDir)) {
        throw "Dossier playlist introuvable: $playlistDir"
    }

    $audio = Get-ChildItem -LiteralPath $playlistDir -File -ErrorAction Stop |
        Where-Object { $_.Extension -in @(".ogg", ".mp3", ".wav") }

    if (-not $audio -or $audio.Count -lt 1) {
        throw "Aucune piste audio détectée dans: $playlistDir"
    }

    Write-Host "[pre-release] Playlist audio detectée: $($audio.Count) fichier(s)"
}

$root = Resolve-CongoRoot
$unityProject = Join-Path $root "UnityProject"
$datasetsDir = Join-Path $unityProject "Assets\StreamingAssets\Datasets"
$playlistDir = Join-Path $unityProject "Assets\StreamingAssets\Theme\playlist"
$buildDir = Join-Path $root "Builds"
$buildLog = Join-Path $buildDir "unity-build.log"

if (-not (Test-Path -LiteralPath $unityProject)) {
    throw "Projet Unity introuvable: $unityProject"
}

if (-not (Test-CommandAvailable "ffmpeg")) {
    throw "ffmpeg introuvable dans PATH (requis pour fallback audio local)."
}
Write-Host "[pre-release] ffmpeg OK"

Assert-PlaylistFiles -playlistDir $playlistDir

$blindMeta = Join-Path $datasetsDir "blind_playlist_meta.json"
if (-not (Test-Path -LiteralPath $blindMeta)) {
    throw "blind_playlist_meta.json manquant: $blindMeta"
}
Write-Host "[pre-release] blind_playlist_meta.json OK"

$ttsHealthy = Test-TtsHealthy
if ($RequireTtsHealthy -and -not $ttsHealthy) {
    throw "Backend TTS non disponible sur http://127.0.0.1:3000/tts/status"
}
if ($ttsHealthy) {
    Write-Host "[pre-release] TTS status OK"
}
else {
    Write-Warning "[pre-release] TTS indisponible (non bloquant sans -RequireTtsHealthy)."
}

if ([string]::IsNullOrWhiteSpace($OutputExe)) {
    $OutputExe = Join-Path $root "Builds\Windows\CongoGames.exe"
}
$OutputExe = [System.IO.Path]::GetFullPath($OutputExe)

if (-not $SkipBuild) {
    New-Item -ItemType Directory -Path $buildDir -Force | Out-Null
    $resolvedUnityExe = Resolve-UnityExe -preferred $UnityExe
    Write-Host "[pre-release] Unity: $resolvedUnityExe"
    Write-Host "[pre-release] Build output: $OutputExe"

    $env:CONGOGAMES_BUILD_OUTPUT = $OutputExe
    $proc = Start-Process -FilePath $resolvedUnityExe -ArgumentList @(
        "-batchmode",
        "-quit",
        "-projectPath", $unityProject,
        "-executeMethod", "CongoGames.EditorTools.CongoGamesBuild.BuildWindows64",
        "-logFile", $buildLog
    ) -PassThru -Wait

    $exitCode = $proc.ExitCode
    if ($exitCode -ne 0) {
        throw "Build Unity échoué (exit code $exitCode). Voir: $buildLog"
    }

    if (-not (Test-Path -LiteralPath $buildLog)) {
        throw "Log de build introuvable: $buildLog"
    }

    $logContent = Get-Content -LiteralPath $buildLog -Raw
    if ($logContent -notmatch "\[CongoGames\] Playlist check OK") {
        throw "Check playlist pré-build non détecté dans le log Unity."
    }
    if ($logContent -notmatch "\[CongoGames\] Build Windows OK") {
        throw "Succès build non détecté dans le log Unity."
    }

    if (-not (Test-Path -LiteralPath $OutputExe)) {
        throw "EXE de sortie introuvable après build: $OutputExe"
    }
    Write-Host "[pre-release] Build Windows OK"
}
else {
    Write-Host "[pre-release] Build ignoré (SkipBuild)"
}

Write-Host "[pre-release] CHECK GLOBAL OK"
