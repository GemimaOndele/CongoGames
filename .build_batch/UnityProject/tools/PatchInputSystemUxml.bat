@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0PatchInputSystemUxml.ps1" -ProjectRoot "%~dp0.."
