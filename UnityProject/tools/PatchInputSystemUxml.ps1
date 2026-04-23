# Optionnel : seulement si le projet inclut com.unity.inputsystem (non utilisé par défaut CongoGames).
# Certains UXML du package déclarent InputActionAsset — TypeLoadException à l'import.
# On remplace par UnityEngine.Object. Exécuter avec Unity FERMÉ après 1re résolution des paquets.
# Commande : npm run unity:patch-input-uxml

param(
    [string] $ProjectRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $ProjectRoot = Resolve-Path (Join-Path $scriptDir "..")
}

$cacheRoot = Join-Path $ProjectRoot "Library\PackageCache"
if (-not (Test-Path -LiteralPath $cacheRoot)) {
    Write-Host "[CongoGames] Pas de Library\PackageCache — ouvrez Unity une fois pour télécharger les paquets, fermez l'éditeur, puis relancez ce script (ou prepare-unity.ps1)." -ForegroundColor Yellow
    exit 0
}

$replacements = @(
    @{ Old = "UnityEngine.InputSystem.InputActionAsset, Unity.InputSystem"; New = "UnityEngine.Object, UnityEngine.CoreModule" },
    @{ Old = "UnityEngine.InputSystem.InputActionAsset,Unity.InputSystem"; New = "UnityEngine.Object,UnityEngine.CoreModule" }
)

$count = 0
Get-ChildItem -LiteralPath $cacheRoot -Directory -Filter "com.unity.inputsystem@*" -ErrorAction SilentlyContinue | ForEach-Object {
    $pkg = $_.FullName
    Get-ChildItem -LiteralPath $pkg -Recurse -Filter "*.uxml" -File -ErrorAction SilentlyContinue | ForEach-Object {
        $path = $_.FullName
        $raw = [System.IO.File]::ReadAllText($path)
        $orig = $raw
        foreach ($r in $replacements) {
            if ($raw.Contains($r.Old)) {
                $raw = $raw.Replace($r.Old, $r.New)
            }
        }
        if ($raw -ne $orig) {
            [System.IO.File]::WriteAllText($path, $raw)
            Write-Host "Input System UXML patch : $path"
            $count++
        }
    }
}

if ($count -eq 0) {
    Write-Host "[CongoGames] Aucun UXML Input System à patcher (déjà fait, cache absent, ou libellé de type différent dans cette version du package)."
} else {
    Write-Host "[CongoGames] $count fichier(s) UXML patché(s). Rouvrez Unity." -ForegroundColor Green
}

exit 0
