# Copie la sortie d'un build WebGL Unity vers ce dossier, sans supprimer vercel.json / README.
#
# Usage (depuis la racine du depot) :
#   .\webgl-site\copy-into-webgl-site.ps1 -SourcePath "LE_CHEMIN_EXACT_OU_UNITY_A_BUILD"
#
# Le chemin est celui que Unity affiche quand tu fais File > Build Settings > WebGL > Build
# (pas un exemple : C:\Builds\... doit vraiment exister sur ton disque).
param(
    [Parameter(Mandatory = $true)][string]$SourcePath
)
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

if (-not (Test-Path -LiteralPath $SourcePath)) {
    Write-Host ""
    Write-Host "ERREUR : ce dossier n'existe pas :" -ForegroundColor Yellow
    Write-Host "  $SourcePath" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Dans Unity : File > Build Settings > WebGL > Build, enregistre le build dans un dossier que tu connais"
    Write-Host "(Bureau, Documents, ou p.ex. un dossier dans ce depot), puis donne le chemin COMPLET ici."
    Write-Host ""
    $suggest = Join-Path (Split-Path $root -Parent) "BuildsWebGL"
    Write-Host "Exemple si tu crees le dossier manuellement puis y build :" -ForegroundColor DarkGray
    Write-Host "  .\webgl-site\copy-into-webgl-site.ps1 -SourcePath `"$suggest`"" -ForegroundColor DarkGray
    Write-Host ""
    exit 1
}

$src = (Get-Item -LiteralPath $SourcePath).FullName
$index = Join-Path $src "index.html"
if (-not (Test-Path $index)) { throw "index.html manquant : ce n'est pas un dossier de build WebGL Unity complet." }
$protected = @("vercel.json", "README.md", "copy-into-webgl-site.ps1", ".gitignore")
Get-ChildItem -Path $root -Force | ForEach-Object {
    if ($_.Name -in $protected) { return }
    Remove-Item -LiteralPath $_.FullName -Recurse -Force
}
Get-ChildItem -Path $src -Force | ForEach-Object {
    $dest = Join-Path $root $_.Name
    Copy-Item -LiteralPath $_.FullName -Destination $dest -Recurse -Force
}
Write-Host "OK : build WebGL copié dans $root — lancez : npm run webgl:vercel (depuis le depot) ou npx vercel --prod ici"
