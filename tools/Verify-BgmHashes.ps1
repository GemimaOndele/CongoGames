# Détecte les BGM Resources identiques (copie du même fichier sous plusieurs noms).
# Usage : pwsh -File tools/Verify-BgmHashes.ps1
$dir = Join-Path $PSScriptRoot "..\UnityProject\Assets\Resources\Audio\BGM"
if (-not (Test-Path $dir)) { Write-Error "Dossier introuvable: $dir"; exit 1 }
$groups = Get-ChildItem $dir -File -Include *.wav,*.ogg,*.mp3 | ForEach-Object {
    $h = Get-FileHash $_.FullName -Algorithm SHA256
    [PSCustomObject]@{ Name = $_.Name; Hash = $h.Hash; Bytes = $_.Length }
} | Group-Object Hash
foreach ($g in $groups) {
    if ($g.Count -gt 1) {
        Write-Host "DOUBLON (meme contenu SHA256) :" -ForegroundColor Yellow
        $g.Group | ForEach-Object { Write-Host "  $($_.Name) ($($_.Bytes) bytes)" }
    }
}
Write-Host "Termine. Remplacez les fichiers par des pistes distinctes (voir docs/FREE_THEME_MEDIA_SOURCES.md)."
