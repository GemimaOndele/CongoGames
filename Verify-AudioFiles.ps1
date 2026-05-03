#!/usr/bin/env powershell
# Verify-AudioFiles.ps1
# Objectif : vérifier que les fichiers audio dans Resources/Audio/BGM/ 
# ne sont PAS des doublons (même fichier renommé plusieurs fois)

param(
    [string]$ProjectRoot = (Get-Location)
)

$bgmFolder = Join-Path $ProjectRoot "Assets\Resources\Audio\BGM"
$videoFolder = Join-Path $ProjectRoot "Assets\StreamingAssets\Theme"

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "CongoGames — Vérification des fichiers audio/vidéo" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

# ═══════════════════════════════════════════════════════════════════════════════
# PARTIE 1 — Vérifier les musiques
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "`n📁 Dossier BGM : $bgmFolder`n" -ForegroundColor Yellow

if (-not (Test-Path $bgmFolder)) {
    Write-Host "❌ Dossier $bgmFolder N'EXISTE PAS" -ForegroundColor Red
    Write-Host "   Créer : Assets > Resources > Audio > BGM" -ForegroundColor Yellow
    exit 1
}

$audioFiles = @(
    "quiz_theme.mp3"
    "speed_chrono_theme.mp3"
    "memory_theme.mp3"
    "word_scramble_theme.mp3"
    "crossword_theme.mp3"
    "mystery_word_theme.mp3"
    "semantic_theme.mp3"
    "image_to_word_theme.mp3"
    "lobby_theme.mp3"
)

$fileHashes = @{}
$missingFiles = @()
$duplicateAlert = $false

Write-Host "Analyse des fichiers musiques :" -ForegroundColor Green

foreach ($fileName in $audioFiles) {
    $filePath = Join-Path $bgmFolder $fileName
    
    if (-not (Test-Path $filePath)) {
        Write-Host "  ❌ MANQUANT : $fileName" -ForegroundColor Red
        $missingFiles += $fileName
        continue
    }
    
    # Calculer le hash SHA256
    $hash = (Get-FileHash -Path $filePath -Algorithm SHA256).Hash
    $fileSize = (Get-Item $filePath).Length
    
    # Vérifier si ce hash a déjà été vu
    if ($fileHashes.ContainsKey($hash)) {
        Write-Host "  ⚠️  DOUBLON : $fileName (identique à $($fileHashes[$hash]))" -ForegroundColor Red
        $duplicateAlert = $true
    } else {
        Write-Host "  ✅ OK : $fileName ($fileSize bytes)" -ForegroundColor Green
        $fileHashes[$hash] = $fileName
    }
}

# Résumé musiques
Write-Host "`n📊 Résumé musiques :" -ForegroundColor Cyan
Write-Host "   Fichiers trouvés : $($audioFiles.Count - $missingFiles.Count) / $($audioFiles.Count)" -ForegroundColor Yellow
Write-Host "   Fichiers manquants : $($missingFiles.Count)" -ForegroundColor Yellow
Write-Host "   Fichiers uniques : $($fileHashes.Count)" -ForegroundColor Yellow

if ($duplicateAlert) {
    Write-Host "`n🚨 ATTENTION : Des doublons ont été trouvés !" -ForegroundColor Red
    Write-Host "   Solution : Télécharger des pistes vraiment DIFFÉRENTES" -ForegroundColor Yellow
    Write-Host "   Liens : https://opengameart.org, https://mixkit.co, https://pixabay.com" -ForegroundColor Cyan
} else {
    Write-Host "`n✨ Pas de doublons détectés — musiques toutes différentes !" -ForegroundColor Green
}

# ═══════════════════════════════════════════════════════════════════════════════
# PARTIE 2 — Vérifier les vidéos
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "`n\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "📁 Dossier vidéos : $videoFolder`n" -ForegroundColor Yellow

if (-not (Test-Path $videoFolder)) {
    Write-Host "❌ Dossier $videoFolder N'EXISTE PAS" -ForegroundColor Red
    Write-Host "   Créer : Assets > StreamingAssets > Theme > (dossiers de modes)" -ForegroundColor Yellow
    exit 1
}

$videoDirs = @(
    "quiz"
    "semantic"
    "word-scramble"
    "crossword-lite"
    "blind-test"
    "mystery-word"
    "memory"
    "speed-chrono"
    "image-guess"
)

$videoHashes = @{}
$videoMissingDirs = @()
$videoDuplicateAlert = $false

Write-Host "Analyse des vidéos (background.mp4 par mode) :" -ForegroundColor Green

foreach ($dirName in $videoDirs) {
    $dirPath = Join-Path $videoFolder $dirName
    $videoPath = Join-Path $dirPath "background.mp4"
    
    if (-not (Test-Path $dirPath)) {
        Write-Host "  ❌ DOSSIER MANQUANT : $dirName/" -ForegroundColor Red
        $videoMissingDirs += $dirName
        continue
    }
    
    if (-not (Test-Path $videoPath)) {
        Write-Host "  ⚠️  VIDÉO MANQUANTE : $dirName/background.mp4" -ForegroundColor Yellow
        continue
    }
    
    # Calculer le hash SHA256
    $hash = (Get-FileHash -Path $videoPath -Algorithm SHA256).Hash
    $fileSize = (Get-Item $videoPath).Length
    
    # Vérifier si ce hash a déjà été vu
    if ($videoHashes.ContainsKey($hash)) {
        Write-Host "  ⚠️  DOUBLON : $dirName/background.mp4 (identique à $($videoHashes[$hash]))" -ForegroundColor Red
        $videoDuplicateAlert = $true
    } else {
        Write-Host "  ✅ OK : $dirName/background.mp4 ($fileSize bytes)" -ForegroundColor Green
        $videoHashes[$hash] = $dirName
    }
}

# Résumé vidéos
Write-Host "`n📊 Résumé vidéos :" -ForegroundColor Cyan
Write-Host "   Dossiers trouvés : $($videoDirs.Count - $videoMissingDirs.Count) / $($videoDirs.Count)" -ForegroundColor Yellow
Write-Host "   Vidéos uniques : $($videoHashes.Count)" -ForegroundColor Yellow

if ($videoDuplicateAlert) {
    Write-Host "`n🚨 ATTENTION : Des vidéos identiques trouvées !" -ForegroundColor Red
    Write-Host "   Solution : Télécharger des vidéos DIFFÉRENTES par mode" -ForegroundColor Yellow
} else {
    Write-Host "`n✨ Pas de doublons vidéo — toutes différentes !" -ForegroundColor Green
}

# ═══════════════════════════════════════════════════════════════════════════════
# CONCLUSION
# ═══════════════════════════════════════════════════════════════════════════════

Write-Host "`n\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

$allGood = (-not $duplicateAlert) -and (-not $videoDuplicateAlert) -and ($missingFiles.Count -eq 0) -and ($videoMissingDirs.Count -eq 0)

if ($allGood) {
    Write-Host "✅ SUCCÈS ! Tous les fichiers sont présents et différents" -ForegroundColor Green
    Write-Host "   Tu peux lancer le jeu — musiques et vidéos doivent défiler !" -ForegroundColor Green
} else {
    Write-Host "⚠️  ACTION REQUISE" -ForegroundColor Yellow
    if ($missingFiles.Count -gt 0) {
        Write-Host "`n   Musiques manquantes ($($missingFiles.Count)):" -ForegroundColor Red
        foreach ($f in $missingFiles) {
            Write-Host "      - $f" -ForegroundColor Red
        }
    }
    if ($videoMissingDirs.Count -gt 0) {
        Write-Host "`n   Dossiers vidéo manquants ($($videoMissingDirs.Count)):" -ForegroundColor Red
        foreach ($d in $videoMissingDirs) {
            Write-Host "      - $d/" -ForegroundColor Red
        }
    }
    if ($duplicateAlert -or $videoDuplicateAlert) {
        Write-Host "`n   Doublons trouvés → télécharger des fichiers vraiment différents" -ForegroundColor Red
    }
}

Write-Host "`nGuide complet : AUDIO_VIDEO_DOWNLOAD_GUIDE.md" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan
