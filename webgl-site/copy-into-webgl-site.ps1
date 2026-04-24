# Copie la sortie d'un build WebGL Unity vers ce dossier, sans supprimer / ecraser la config (vercel.json, index.html, etc.).
#
# Usage (depuis la racine du depot) :
#   .\webgl-site\copy-into-webgl-site.ps1
#   .\webgl-site\copy-into-webgl-site.ps1 -SourcePath "C:\Builds\MonDossierWebGL"
#
# Sans -SourcePath : le script cherche, dans l'ordre, un build WebGL valide (index.html) dans :
#   1) ..\BuildsWebGL (racine du depot)   2) C:\Builds\CongoWebGL
# Sinon, passez le chemin Unity (File > Build Settings > WebGL > Build > Enregistrer).
param([string]$SourcePath)
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$repoRoot = Split-Path $root -Parent

if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    $candidates = @(
        (Join-Path $repoRoot "BuildsWebGL"),
        "C:\Builds\CongoWebGL"
    )
    foreach ($c in $candidates) {
        if (-not (Test-Path -LiteralPath $c)) { continue }
        $idx = Join-Path $c "index.html"
        if (Test-Path -LiteralPath $idx) { $SourcePath = (Get-Item -LiteralPath $c).FullName; break }
    }
    if ([string]::IsNullOrWhiteSpace($SourcePath)) {
        Write-Host ""
        Write-Host "ERREUR : aucun build WebGL trouve aux emplacements par defaut :" -ForegroundColor Yellow
        foreach ($c in $candidates) { Write-Host "  - $c" -ForegroundColor Gray }
        Write-Host ""
        Write-Host "Dans Unity : File > Build Settings > WebGL > Build, enregistre le build, puis lancez par exemple :"
        Write-Host "  .\webgl-site\copy-into-webgl-site.ps1 -SourcePath `"C:\ton\chemin\build`""
        Write-Host ""
        exit 1
    }
    Write-Host "Source WebGL (auto) : $SourcePath" -ForegroundColor DarkCyan
}
elseif (-not (Test-Path -LiteralPath $SourcePath)) {
    Write-Host ""
    Write-Host "ERREUR : ce dossier n'existe pas :" -ForegroundColor Yellow
    Write-Host "  $SourcePath" -ForegroundColor Gray
    Write-Host ""
    $suggest1 = Join-Path $repoRoot "BuildsWebGL"
    $suggest2 = "C:\Builds\CongoWebGL"
    Write-Host "Exemples :" -ForegroundColor DarkGray
    Write-Host "  .\webgl-site\copy-into-webgl-site.ps1 -SourcePath `"$suggest1`""
    Write-Host "  .\webgl-site\copy-into-webgl-site.ps1 -SourcePath `"$suggest2`""
    Write-Host ""
    exit 1
}

$src = (Get-Item -LiteralPath $SourcePath).FullName
$index = Join-Path $src "index.html"
if (-not (Test-Path $index)) { throw "index.html manquant : ce n'est pas un dossier de build WebGL Unity complet." }
# index.html : version du depot (Web Audio, titres) — ne pas ecraser avec le template Unity brut.
$protected = @("vercel.json", "package.json", "README.md", "copy-into-webgl-site.ps1", ".gitignore", "index.html")
Get-ChildItem -Path $root -Force | ForEach-Object {
    if ($_.Name -in $protected) { return }
    Remove-Item -LiteralPath $_.FullName -Recurse -Force
}
Get-ChildItem -Path $src -Force | ForEach-Object {
    if ($_.Name -in $protected) { return }
    $dest = Join-Path $root $_.Name
    Copy-Item -LiteralPath $_.FullName -Destination $dest -Recurse -Force
}
Write-Host "OK : build WebGL copié dans $root"
Write-Host "Deploiement :  npm run webgl:vercel  (a la racine du depot). Pas  npx vercel  seul a la racine (sinon scan de UnityProject -> EBUSY)." -ForegroundColor Green
