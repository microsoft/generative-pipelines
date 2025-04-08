# Move to repo root
Set-Location ..

$toolsDir = "tools"

# Get all directories in tools/ excluding those starting with "_"
$projectDirs = Get-ChildItem -Path $toolsDir -Directory | Where-Object { $_.Name -notmatch "^_" }

foreach ($dir in $projectDirs) {
    $csproj = Get-ChildItem -Path $dir.FullName -Filter "*.csproj" -File
    $packageJson = Get-ChildItem -Path $dir.FullName -Filter "package.json" -File
    $pyProjectToml = Get-ChildItem -Path $dir.FullName -Filter "pyproject.toml" -File

    if ($csproj) {
        Write-Host "----------------------------------------------------"
        Write-Host "Building .NET project in $($dir.Name)..."
        Push-Location $dir.FullName
        dotnet build
        Pop-Location
    }

    if ($packageJson)
    {
        Write-Host "----------------------------------------------------"
        Write-Host "Building Node.js project in $( $dir.Name )..."
        Push-Location $dir.FullName
        pnpm install
        pnpm build
        Pop-Location
    }

    if ($pyProjectToml)
    {
        Write-Host "----------------------------------------------------"
        Write-Host "Building Python project in $( $dir.Name )..."
        Push-Location $dir.FullName
        poetry install
        Pop-Location
    }
}

# Build Orchestrator
Write-Host "----------------------------------------------------"
Write-Host "Building Orchestrator..."
Push-Location service\Orchestrator
dotnet build
Pop-Location

# Build Aspire AppHost
Write-Host "----------------------------------------------------"
Write-Host "Building .NET Aspire Host..."
Push-Location infra\dev-with-aspire
dotnet build
Pop-Location
