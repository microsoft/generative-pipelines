# Move to repo root
Set-Location ..

Push-Location infra\Aspire.AppHost

dotnet build
dotnet run
