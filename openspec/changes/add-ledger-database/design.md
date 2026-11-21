# Design: Add the ledger database

## Context
ledger-api stores entries in memory behind `ILedgerStore`. See proposal.md for why it needs Postgres.

## Goals / Non-Goals
- Goals: persistent entries, traced queries, migrations that the missing-index scenario can later alter.
- Non-Goals: high availability, backups, multiple databases.

## Decisions
- **EF Core with Npgsql over Dapper.** Migrations make the missing-index defect a natural one-line change in a migration file.
- **Migrations applied at startup in development only**, so the lab is one command to run; production-style migration jobs are out of scope.
- **A named data volume** so restarts keep data, with a documented command to wipe it for clean recordings.

## Under the hood
- **AddPostgres / AddDatabase**: the AppHost starts a Postgres container in Docker and injects a
  connection string into ledger-api as `ConnectionStrings__ledgerdb`.
- **Aspire client integration** (`AddNpgsqlDbContext`): registers the DbContext with health checks,
  retries and OpenTelemetry instrumentation, which is where the database spans come from.
- **EF Core migrations**: C# files describing schema changes; `dotnet ef migrations add` generates them,
  and the indexes in them are what the missing-index scenario will later remove.
- **Data volume**: without a volume, the container's data dies with it; with one, restarts keep it.

## Risks / Trade-offs
- [Startup migrations race if several instances start at once] → only one ledger-api instance in the lab.
- [Stale data volumes skew recordings] → recording scripts start from a wiped volume.
