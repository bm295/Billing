# Billing

SaaS Billing Engine API built with ASP.NET Core, EF Core, and .NET 10.

## Project layout

- `Billing.sln`
- `src/Billing.Api/Billing.Api.csproj`
- `tests/Billing.Api.Tests/Billing.Api.Tests.csproj`
- `db/sqlserver/001_initial_schema.sql`
- `docs/requirement.md`

## Run

```powershell
dotnet run --project src\Billing.Api\Billing.Api.csproj
```

## Test

```powershell
dotnet test Billing.sln
```
