param(
  [ValidateSet("dev", "live", "prod")]
  [string]$Mode = "dev"
)

Write-Host "CongoGames start-all mode: $Mode"
Set-Location "$PSScriptRoot\Backend"

if ($Mode -ne "prod") {
  Write-Host "npm install (Backend)..."
  npm install
}

if ($Mode -eq "prod") {
  npm run start
} elseif ($Mode -eq "live") {
  $env:TIKTOK_BRIDGE_ENABLED = "true"
  npm run dev
} else {
  $env:TIKTOK_BRIDGE_ENABLED = "false"
  npm run dev
}
