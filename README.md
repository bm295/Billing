# Billing

SaaS Billing Engine API built with ASP.NET Core, EF Core, and .NET 10.

## Project layout

- `Billing.sln`
- `src/Billing.Api/Billing.Api.csproj`
- `src/Billing.Api/Features/*` - API endpoints, contracts, and services grouped by feature
- `src/Billing.Api/Domain` - shared domain entities and constants
- `src/Billing.Api/Data` - EF Core context and seed data
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

## Pull request checks

Pull requests automatically restore and build the solution, run the test suite, and
collect line coverage. The coverage percentage is displayed in the GitHub Actions
job summary, and the Cobertura XML report is retained as a workflow artifact for 14
days.
