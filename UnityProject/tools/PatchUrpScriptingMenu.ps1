# Unity 6.4 : le menu "Assets/Create/Scripting/C# Script" n'existe plus → ValidateMenuItem échoue
# pour les entrées URP sous "Assets/Create/Scripting/...". On les déplace sous "Assets/Create/Rendering/".
# Exécuter avec Unity FERMÉ. Voir prepare-unity.ps1

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
    Write-Host "[CongoGames] Pas de Library\PackageCache — rien à patcher (exit 0)."
    exit 0
}

$replacements = @(
    @{
        Relative = "Editor\ScriptTemplates\ScriptTemplates.cs"
        Old = '[MenuItem("Assets/Create/Scripting/URP Renderer Feature Script",'
        New = '[MenuItem("Assets/Create/Rendering/URP Renderer Feature Script",'
    },
    @{
        Relative = "Editor\RendererFeatures\NewPostProcessTemplateDropdownItems.cs"
        Old = '[MenuItem("Assets/Create/Scripting/URP Post-process Volume Scripts",'
        New = '[MenuItem("Assets/Create/Rendering/URP Post-process Volume Scripts",'
    }
)

$count = 0
Get-ChildItem -LiteralPath $cacheRoot -Directory -Filter "com.unity.render-pipelines.universal@*" -ErrorAction SilentlyContinue | ForEach-Object {
    $pkg = $_.FullName
    foreach ($r in $replacements) {
        $path = Join-Path $pkg $r.Relative
        if (-not (Test-Path -LiteralPath $path)) { continue }
        $raw = [System.IO.File]::ReadAllText($path)
        if ($raw.Contains($r.Old)) {
            [System.IO.File]::WriteAllText($path, $raw.Replace($r.Old, $r.New))
            Write-Host "URP patch : $path"
            $count++
        }
    }
}

if ($count -eq 0) {
    Write-Host "[CongoGames] Aucun fichier URP à patcher (déjà fait ou version de package différente)."
} else {
    Write-Host "[CongoGames] $count fichier(s) URP patché(s). Ouvrez Unity."
}

exit 0
