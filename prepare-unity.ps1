# Prépare le projet Unity (à lancer avec Unity fermé).
# - Patch menus URP (Unity 6.4 vs ancien menu Scripting/C# Script)
# Usage : .\prepare-unity.ps1
# (Si vous réajoutez le package Input System : npm run unity:patch-input-uxml après la 1re résolution des paquets.)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$unityProject = Join-Path $root "UnityProject"

if (-not (Test-Path -LiteralPath $unityProject)) {
    Write-Error "Dossier UnityProject introuvable : $unityProject"
    exit 1
}

Write-Host "=== CongoGames — préparation Unity ===" -ForegroundColor Cyan
& (Join-Path $unityProject "tools\PatchUrpScriptingMenu.ps1") -ProjectRoot $unityProject

Write-Host ""
Write-Host "Note : Unity peut avertir que des fichiers URP (« immutable ») ont été modifiés." -ForegroundColor Yellow
Write-Host "  C’est attendu. Après mise à jour d’URP ou suppression de Library, relancez ce script." -ForegroundColor Yellow
Write-Host ""
Write-Host "Étapes suivantes :" -ForegroundColor Green
Write-Host "  1. Ouvrir le dossier UnityProject dans Unity 6000.4.x"
Write-Host "  2. Si besoin URP : menu CongoGames > Rendering > Créer et assigner URP"
Write-Host "  3. Après toute suppression de Library : relancer .\prepare-unity.ps1 avant Unity"
Write-Host ""
