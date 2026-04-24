# Télécharge une vidéo YouTube vers un .mp4 local (NON versionné) pour tests dans StreamingAssets.
# Prérequis : yt-dlp (https://github.com/yt-dlp/yt-dlp) sur le PATH.
# Usage (PowerShell, depuis la racine du dépôt) :
#   .\tools\fetch-youtube-theme.ps1 "https://youtu.be/HeRXfvCmarc" quiz
# Le fichier est écrit vers UnityProject/Assets/StreamingAssets/Theme/_dev_import/<modeId>/background.mp4
param(
  [Parameter(Mandatory = $true)][string]$Url,
  [Parameter(Mandatory = $true)][string]$ModeId
)
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$outDir = Join-Path $root "UnityProject/Assets/StreamingAssets/Theme/_dev_import/$ModeId"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$outTemplate = Join-Path $outDir "background.%(ext)s"
$bin = Get-Command yt-dlp -ErrorAction SilentlyContinue
if (-not $bin) {
  Write-Error "yt-dlp est introuvable. Installez-le puis relancez: winget install yt-dlp  (ou voir https://github.com/yt-dlp/yt-dlp )"
}
& yt-dlp -f "bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best" --merge-output-format mp4 -o $outTemplate $Url
# Renommer en background.mp4 si yt-dlp a choisi un autre nom
$mp4 = Get-ChildItem -Path $outDir -Filter "background.*" -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($mp4 -and $mp4.Extension -ne ".mp4") {
  $dest = Join-Path $outDir "background.mp4"
  Move-Item -LiteralPath $mp4.FullName -Destination $dest -Force
}
Write-Host "OK — vérifyez: $outDir\background*.mp4 (dossier ignoré par .gitignore). Réglez le mode thème en local (sans URL YouTube dans remote_media)."
