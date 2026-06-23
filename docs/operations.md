# Operations Runbook

## Deploying Database Migrations Before the API

Run Entity Framework Core migrations as a separate deployment step before starting or rolling out a new API version. This keeps schema changes explicit, lets operators inspect SQL before it reaches production, and avoids relying on application startup to mutate the database.

### Preconditions

- Confirm the release has been built and tested from the exact commit that will be deployed.
- Confirm the production `ConnectionStrings:BillingDb` secret is present in the deployment environment.
- Confirm the target SQL Server database is reachable from the runner that will execute migrations.
- Confirm you have database credentials with permission to create or alter the billing schema.
- Take or verify a recent database backup before applying migrations in production.

### Review Pending Migrations

From the repository root, list migrations that have not been applied to the target database:

```bash
dotnet ef migrations list \
  --project src/Billing.Infrastructure/Billing.Infrastructure.csproj \
  --startup-project src/Billing.Api/Billing.Api.csproj \
  --context BillingDbContext
```

The initial production schema migration is `20260623000000_InitialBillingSchema`. If the command shows no pending migrations, do not apply schema changes for this release.

### Generate and Review SQL

Generate an idempotent SQL script and have it reviewed before production execution:

```bash
dotnet ef migrations script --idempotent \
  --project src/Billing.Infrastructure/Billing.Infrastructure.csproj \
  --startup-project src/Billing.Api/Billing.Api.csproj \
  --context BillingDbContext \
  --output artifacts/billing-migrations.sql
```

Review `artifacts/billing-migrations.sql` for destructive operations, long-running table rewrites, lock-heavy index changes, and expected data backfills. Attach the reviewed script to the release record.

### Apply Migrations

Prefer applying the reviewed SQL script with the organization's standard database deployment tool. If `dotnet ef` is the approved deployment path for the environment, apply migrations with:

```bash
dotnet ef database update \
  --project src/Billing.Infrastructure/Billing.Infrastructure.csproj \
  --startup-project src/Billing.Api/Billing.Api.csproj \
  --context BillingDbContext
```

Run this command with the same `ConnectionStrings:BillingDb` value that the production API will use. The API includes startup validation for this setting, but migration execution should verify the secret before the API rollout begins.

### Verify Before API Rollout

After migration execution and before deploying the API:

1. Re-run `dotnet ef migrations list` against the production database and confirm there are no pending migrations.
2. Confirm the `__EFMigrationsHistory` table contains the expected latest migration ID.
3. Run a lightweight database connectivity check from the deployment environment.
4. Start or roll out the API only after the migration status is verified.

### Rollback Guidance

If migration execution fails, stop the API rollout, preserve the migration logs, and restore from the verified backup if the database is left in an unsafe partial state. If the API rollout fails after migrations succeeded, prefer rolling the API back to the previous version only when the applied migration is backward compatible; otherwise follow the release-specific rollback plan.
