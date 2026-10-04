param (
    [string]$m,
    [string]$c = "EFAppContext"
)

if (-not $m) {
    Write-Error "Du skal angive -m (migration name)"
    exit
}

$projectPath = ".\src\DKP.Infrastructure\DKP.Infrastructure.csproj"
$startupPath = ".\src\DKP.Blazor\DKP.Blazor.csproj"

Write-Host "Runs command:"
Write-Host "dotnet ef migrations add $m --context $c --project $projectPath --startup-project $startupPath"

dotnet ef migrations add $m `
    --context $c `
    --project $projectPath `
    --startup-project $startupPath