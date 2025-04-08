# Move to repo root
Set-Location ..

Push-Location infra\dev-with-aspire

dotnet build
dotnet run
