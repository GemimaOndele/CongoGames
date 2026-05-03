#!/usr/bin/env pwsh
# Délègue vers l'outil canonique dans tools/
$script = Join-Path $PSScriptRoot "..\tools\Verify-AudioFiles.ps1"
if (-not (Test-Path $script)) {
    Write-Error "Introuvable: $script"
    exit 1
}
& $script @args
exit $LASTEXITCODE
