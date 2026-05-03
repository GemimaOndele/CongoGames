@echo off
REM CongoGames - Copy downloads into Unity (manual download first)
REM See QUICK_START_DOWNLOAD.md

setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
if not defined DOWNLOADS set "DOWNLOADS=C:\Downloads\CongoGames"
for %%I in ("%SCRIPT_DIR%..") do set "PROJECT=%%~fI\UnityProject"
if defined OVERRIDE_PROJECT set "PROJECT=%OVERRIDE_PROJECT%"

set "BGM_DEST=%PROJECT%\Assets\Resources\Audio\BGM"
set "VIDEO_ROOT=%PROJECT%\Assets\StreamingAssets\Theme"

cls
echo.
echo ============================================================
echo   CongoGames - audio / video copy helper
echo ============================================================
echo   DOWNLOADS = %DOWNLOADS%
echo   PROJECT   = %PROJECT%
echo ============================================================
echo.

if not exist "%DOWNLOADS%" (
    echo ERROR: download folder missing:
    echo    %DOWNLOADS%
    echo Create it, add renamed files, then run again.
    pause
    exit /b 1
)

if not exist "%BGM_DEST%" (
    echo ERROR: BGM folder missing:
    echo    %BGM_DEST%
    pause
    exit /b 1
)

if not exist "%VIDEO_ROOT%" (
    echo ERROR: Theme folder missing:
    echo    %VIDEO_ROOT%
    pause
    exit /b 1
)

echo Paths OK.
echo    BGM   -^> %BGM_DEST%
echo    Theme -^> %VIDEO_ROOT%
echo.

echo [1/2] Copy music to Resources/Audio/BGM ...
echo.

set "STEMS=quiz_theme battle_theme speed_chrono_theme memory_theme word_scramble_theme crossword_theme mystery_word_theme semantic_theme image_to_word_theme lobby_theme blind_test_theme"

for %%S in (%STEMS%) do (
    set "copied="
    for %%E in (mp3 wav ogg m4a) do (
        if "!copied!"=="" (
            if exist "%DOWNLOADS%\%%S.%%E" (
                echo   OK  %%S.%%E
                copy /Y "%DOWNLOADS%\%%S.%%E" "%BGM_DEST%\%%S.%%E" >nul
                set "copied=1"
            )
        )
    )
    if "!copied!"=="" echo   MISSING %%S - use .mp3 .wav .ogg or .m4a
)

echo.

echo [2/2] Copy videos to StreamingAssets/Theme ...
echo.

set "MODES=quiz semantic word-scramble crossword-lite blind-test mystery-word memory speed-chrono image-guess"

for %%D in (%MODES%) do (
    set "dir_path=%VIDEO_ROOT%\%%D"
    set "src_video=%DOWNLOADS%\background_%%D.mp4"
    set "dst_video=!dir_path!\background.mp4"

    if not exist "!dir_path!" (
        echo   mkdir %%D
        mkdir "!dir_path!" 2>nul
    )

    if exist "!src_video!" (
        echo   OK  background_%%D.mp4 -^> %%D\background.mp4
        copy /Y "!src_video!" "!dst_video!" >nul
    ) else (
        echo   MISSING background_%%D.mp4
    )
)

set "src_show=%DOWNLOADS%\show.mp4"
set "dst_show=%VIDEO_ROOT%\show.mp4"

if exist "%src_show%" (
    echo   OK  show.mp4 (Theme root^)
    copy /Y "%src_show%" "%dst_show%" >nul
) else (
    echo   optional missing: show.mp4
)

echo.

echo ------------------------------------------------------------
echo Done. Next:
echo   1. Unity: Ctrl+R refresh
echo   2. Repo root: pwsh -File tools\Verify-AudioFiles.ps1
echo ------------------------------------------------------------
echo.

pause
exit /b 0
