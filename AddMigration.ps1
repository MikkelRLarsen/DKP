param (
    [string]$m,
    [string]$c = "DkpDbContext"
)

if (-not $m) {
    Write-Error "Du skal angive -m (migration name)"
    exit
}

$projectPath = Join-Path $PSScriptRoot "src\DKP.Infrastructure\DKP.Infrastructure.csproj"
# Design-time factory lives in Infrastructure. No web host, OAuth secrets or cold-start migration required.
$startupPath = $projectPath

Write-Host "Runs command:"
Write-Host "dotnet ef migrations add $m --context $c --project $projectPath --startup-project $startupPath"

dotnet ef migrations add $m `
    --context $c `
    --project $projectPath `
    --startup-project $startupPath `
    --output-dir Persistence\Migrations

if ($LASTEXITCODE -ne 0) { throw "Migration generation failed." }
