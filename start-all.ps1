param(
  [ValidateSet("dev", "prod")]
  [string]$Mode = "dev"
)

Write-Host "CongoGames start-all mode: $Mode"
Set-Location "$PSScriptRoot\Backend"

if ($Mode -eq "prod") {
  npm run start
} else {
  npm run dev
}
