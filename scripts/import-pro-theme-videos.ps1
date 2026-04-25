# Télécharge 4 clips vidéo de démonstration (liens directs MP4) pour les fonds thème.
# Cible: UnityProject/Assets/StreamingAssets/Theme/background|loop|theatre|show.mp4

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$themeDir = Join-Path $root "UnityProject/Assets/StreamingAssets/Theme"
New-Item -ItemType Directory -Force -Path $themeDir | Out-Null

$targets = @(
  @{ Name = "background.mp4"; Url = "https://interactive-examples.mdn.mozilla.net/media/cc0-videos/flower.mp4" },
  @{ Name = "loop.mp4";       Url = "https://www.w3schools.com/html/mov_bbb.mp4" },
  @{ Name = "theatre.mp4";    Url = "https://samplelib.com/lib/preview/mp4/sample-5s.mp4" },
  @{ Name = "show.mp4";       Url = "https://samplelib.com/lib/preview/mp4/sample-10s.mp4" }
)

foreach ($t in $targets) {
  $dest = Join-Path $themeDir $t.Name
  Write-Host "Téléchargement $($t.Url) -> $dest"
  Invoke-WebRequest -Uri $t.Url -OutFile $dest -UseBasicParsing
}

Write-Host "OK: 4 vidéos importées dans $themeDir"
