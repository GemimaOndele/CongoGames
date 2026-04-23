#Requires -Version 5.1
<#
.SYNOPSIS
  Sauvegarde une copie du projet, garde .env (et optionnellement une image), puis
  aligne le dépôt sur origin (master ou main) comme "git pull" propre.

  À lancer UNIQUEMENT avec Unity (et idéalement le terminal node) fermés.

  Usage (PowerShell, à la racine du dépôt) :
    .\reset-vers-origin.ps1
    .\reset-vers-origin.ps1 -Image "C:\Congogame\mon_image.png"
    .\reset-vers-origin.ps1 -SkipBackup
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
  [string] $Image = "",
  [switch] $SkipBackup
)

$ErrorActionPreference = "Stop"

# Racine = dossier où se trouve ce script
$RepoRoot = $PSScriptRoot
if (-not (Test-Path (Join-Path $RepoRoot ".git"))) {
  Write-Error "Ce script doit être à la racine d'un dépôt Git (dossier .git manquant) : $RepoRoot"
  exit 1
}

$parent = Split-Path -Parent $RepoRoot
$stamp  = Get-Date -Format "yyyyMMdd-HHmmss"
$FullBackup   = Join-Path $parent "Congogame-sauvegarde-$stamp"
$Keep         = Join-Path $FullBackup "_fichiers-a-garder"

function Test-Git {
  if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Error "Git n'est pas installé ou pas dans le PATH."
    exit 1
  }
}

function Get-DefaultBranch {
  $remote = "origin"
  $branches = @("master", "main")
  foreach ($b in $branches) {
    $r = & git -C $RepoRoot rev-parse --verify "refs/remotes/$remote/$b" 2>$null
    if ($LASTEXITCODE -eq 0) { return $b }
  }
  Write-Error "Aucune branche origin/master ni origin/main trouvée. Vérifiez : git remote -v, git fetch origin"
  exit 1
}

Write-Host ""
Write-Host "=== CongoGames — remise en ligne avec le dépôt distant ===" -ForegroundColor Cyan
Write-Host "Dossier du projet : $RepoRoot"
Write-Host ""

if (-not $SkipBackup) {
  if ($PSCmdlet.ShouldProcess($RepoRoot, "Sauvegarde + alignement sur origin")) {
    if (-not $PSCmdlet.WhatIf) {
      Write-Host "Étape 1/4 : copie de sauvegarde (hors gros caches) -> $FullBackup" -ForegroundColor Yellow
      New-Item -ItemType Directory -Force -Path $FullBackup | Out-Null
      # Robocopy : miroir partiel, exclut Library/Temp/ node_modules (copie beaucoup plus légère)
      $ro = & robocopy $RepoRoot $FullBackup /E /NFL /NDL /NJH /NJS /nc /ns /np /XD "UnityProject\Library" "UnityProject\Temp" "UnityProject\obj" "node_modules" "Backend\node_modules" 2>&1
      $rc = $LASTEXITCODE
      if ($rc -ge 8) { Write-Warning "Robocopy a signalé un problème (code $rc). Vérifiez l'espace disque et les droits." }
    }
  }
} else {
  Write-Host "Sauvegarde complète ignorée (-SkipBackup)." -ForegroundColor DarkYellow
}

# Toujours préserver .env + image listée
if (-not $PSCmdlet.WhatIf) {
  New-Item -ItemType Directory -Force -Path $Keep | Out-Null
  $envPath = Join-Path $RepoRoot "Backend\.env"
  if (Test-Path -LiteralPath $envPath) {
    Copy-Item -LiteralPath $envPath -Destination (Join-Path $Keep "Backend.env.copie") -Force
    Write-Host "OK : Backend\.env copié dans le dossier de garde -> $Keep" -ForegroundColor Green
  } else {
    Write-Host "Info : pas de Backend\.env trouvé (normal s'il n'a jamais été créé)." -ForegroundColor DarkGray
  }

  if ($Image -and $Image.Trim() -ne "") {
    if (Test-Path -LiteralPath $Image) {
      $name = [System.IO.Path]::GetFileName($Image)
      Copy-Item -LiteralPath $Image -Destination (Join-Path $Keep $name) -Force
      Set-Content -Path (Join-Path $Keep "image-chemin-cible.txt") -Value (Resolve-Path -LiteralPath $Image).Path -Encoding UTF8
      Write-Host "OK : image copiée -> $Keep\$name" -ForegroundColor Green
    } else {
      Write-Warning "Fichier image introuvable : $Image (étape image ignorée)."
    }
  }
}

# Git
if (-not $PSCmdlet.WhatIf) {
  Test-Git
  $branch = Get-DefaultBranch
  Write-Host ""
  Write-Host "Étape 2/4 : fetch origin" -ForegroundColor Yellow
  & git -C $RepoRoot fetch origin
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

  Write-Host "Étape 3/4 : branche cible = $branch, reset + nettoyage" -ForegroundColor Yellow
  & git -C $RepoRoot checkout $branch
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  & git -C $RepoRoot reset --hard "origin/$branch"
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  & git -C $RepoRoot clean -fd
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

# Restauration
if (-not $PSCmdlet.WhatIf) {
  Write-Host ""
  Write-Host "Étape 4/4 : restauration de .env et de l'image" -ForegroundColor Yellow
  $keptEnv = Join-Path $Keep "Backend.env.copie"
  if (Test-Path -LiteralPath $keptEnv) {
    $dest = Join-Path $RepoRoot "Backend\.env"
    $backend = Join-Path $RepoRoot "Backend"
    if (-not (Test-Path $backend)) { New-Item -ItemType Directory -Path $backend -Force | Out-Null }
    Copy-Item -LiteralPath $keptEnv -Destination $dest -Force
    Write-Host "OK : Backend\.env restauré." -ForegroundColor Green
  }

  $mapPath = Join-Path $Keep "image-chemin-cible.txt"
  if (Test-Path -LiteralPath $mapPath) {
    $destImg = (Get-Content -LiteralPath $mapPath -Raw -Encoding UTF8).Trim()
    $baseName = [System.IO.Path]::GetFileName($destImg)
    $keptImg = Join-Path $Keep $baseName
    if (Test-Path -LiteralPath $keptImg) {
      $dir = Split-Path -Parent $destImg
      if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
      Copy-Item -LiteralPath $keptImg -Destination $destImg -Force
      Write-Host "OK : image restaurée -> $destImg" -ForegroundColor Green
    }
  }
}

Write-Host ""
Write-Host "Terminé. Si une sauvegarde a été faite, elle se trouve ici :" -ForegroundColor Cyan
Write-Host "  $FullBackup" -ForegroundColor Cyan
Write-Host ""
Write-Host "Suite :  cd Backend ; npm install" -ForegroundColor White
Write-Host "        Puis : Unity 6000.4.x, ouvrir UnityProject, Play ; npm run start-all" -ForegroundColor White
Write-Host ""
