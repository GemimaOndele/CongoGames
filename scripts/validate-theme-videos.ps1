param(
    [string]$ThemeDir = "c:\Congogame\UnityProject\Assets\StreamingAssets\Theme"
)

$ErrorActionPreference = "Stop"

function Get-FieldValue {
    param(
        [string[]]$Lines,
        [string]$Key
    )
    $prefix = "$Key="
    $line = $Lines | Where-Object { $_ -like "$prefix*" } | Select-Object -First 1
    if (-not $line) { return "" }
    return $line.Substring($prefix.Length).Trim()
}

function Test-VideoMetadata {
    param([string]$Path)

    $probe = & ffprobe -v error `
        -select_streams v:0 `
        -show_entries stream=profile,pix_fmt,color_space,color_transfer,color_primaries,r_frame_rate `
        -of default=noprint_wrappers=1:nokey=0 `
        "$Path"

    if ($LASTEXITCODE -ne 0) {
        return [pscustomobject]@{
            file = [System.IO.Path]::GetFileName($Path)
            ok = $false
            reason = "ffprobe failed"
            profile = ""
            pix_fmt = ""
            color_space = ""
            color_transfer = ""
            color_primaries = ""
            r_frame_rate = ""
        }
    }

    $profile = Get-FieldValue -Lines $probe -Key "profile"
    $pixFmt = Get-FieldValue -Lines $probe -Key "pix_fmt"
    $colorSpace = Get-FieldValue -Lines $probe -Key "color_space"
    $colorTransfer = Get-FieldValue -Lines $probe -Key "color_transfer"
    $colorPrimaries = Get-FieldValue -Lines $probe -Key "color_primaries"
    $fps = Get-FieldValue -Lines $probe -Key "r_frame_rate"

    $problems = New-Object System.Collections.Generic.List[string]

    if ($profile -notlike "*Baseline*") { $problems.Add("profile=$profile (expected baseline)") }
    if ($pixFmt -ne "yuv420p") { $problems.Add("pix_fmt=$pixFmt (expected yuv420p)") }
    if ([string]::IsNullOrWhiteSpace($colorSpace) -or $colorSpace -eq "unknown") { $problems.Add("color_space=$colorSpace") }
    if ([string]::IsNullOrWhiteSpace($colorTransfer) -or $colorTransfer -eq "unknown") { $problems.Add("color_transfer=$colorTransfer") }
    if ([string]::IsNullOrWhiteSpace($colorPrimaries) -or $colorPrimaries -eq "unknown") { $problems.Add("color_primaries=$colorPrimaries") }
    if ($fps -ne "30/1") { $problems.Add("r_frame_rate=$fps (expected 30/1)") }

    return [pscustomobject]@{
        file = [System.IO.Path]::GetFileName($Path)
        ok = ($problems.Count -eq 0)
        reason = ($problems -join "; ")
        profile = $profile
        pix_fmt = $pixFmt
        color_space = $colorSpace
        color_transfer = $colorTransfer
        color_primaries = $colorPrimaries
        r_frame_rate = $fps
    }
}

$targets = @("background.mp4", "loop.mp4", "theatre.mp4", "show.mp4")
$results = @()

foreach ($name in $targets) {
    $path = Join-Path $ThemeDir $name
    if (-not (Test-Path $path)) {
        $results += [pscustomobject]@{
            file = $name
            ok = $false
            reason = "file missing"
            profile = ""
            pix_fmt = ""
            color_space = ""
            color_transfer = ""
            color_primaries = ""
            r_frame_rate = ""
        }
        continue
    }
    $results += Test-VideoMetadata -Path $path
}

$results | Format-Table -AutoSize

$failed = $results | Where-Object { -not $_.ok }
if ($failed.Count -gt 0) {
    Write-Error "Theme video validation failed for $($failed.Count) file(s)."
}

Write-Host "Theme video validation passed."
