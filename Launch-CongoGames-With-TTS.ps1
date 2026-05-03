param(
    [string]$ExePath = ""
)

$ErrorActionPreference = "Stop"

function Resolve-CongoRoot {
    $candidates = @(
        $PSScriptRoot,
        (Join-Path $PSScriptRoot ".."),
        (Join-Path $PSScriptRoot "..\..")
    )

    foreach ($candidate in $candidates) {
        $full = [System.IO.Path]::GetFullPath($candidate)
        if (Test-Path -LiteralPath (Join-Path $full "Backend")) {
            return $full
        }
    }

    throw "Racine CongoGames introuvable depuis: $PSScriptRoot"
}

$root = Resolve-CongoRoot
$backend = Join-Path $root "Backend"

if (-not (Test-Path -LiteralPath $backend)) {
    throw "Backend introuvable: $backend"
}

if ([string]::IsNullOrWhiteSpace($ExePath)) {
    $exeCandidates = @(
        (Join-Path $PSScriptRoot "CongoGames.exe"),
        (Join-Path $root "Builds\Windows\CongoGames.exe")
    )

    foreach ($candidate in $exeCandidates) {
        if (Test-Path -LiteralPath $candidate) {
            $ExePath = $candidate
            break
        }
    }
}

function Test-TtsReady {
    try {
        $r = Invoke-RestMethod -Uri "http://127.0.0.1:3000/tts/status" -TimeoutSec 2
        return [bool]$r.enabled
    }
    catch {
        return $false
    }
}

if (-not (Test-TtsReady)) {
    Write-Host "Demarrage du backend TTS local..."
    Start-Process -FilePath "cmd.exe" `
        -ArgumentList @("/c", "set TIKTOK_BRIDGE_ENABLED=false&& npm --prefix Backend run start") `
        -WorkingDirectory $root `
        -WindowStyle Minimized

    $deadline = (Get-Date).AddSeconds(45)
    while ((Get-Date) -lt $deadline) {
        if (Test-TtsReady) {
            break
        }

        Start-Sleep -Milliseconds 700
    }
}

if (-not (Test-Path -LiteralPath $ExePath)) {
    throw "EXE introuvable: $ExePath"
}

Write-Host "Lancement de CongoGames..."
Start-Process -FilePath $ExePath -WorkingDirectory (Split-Path -Parent $ExePath)
