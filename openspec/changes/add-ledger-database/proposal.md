# Add the ledger database

## Why
The missing-index scenario needs a real database, and a ledger that forgets everything on restart
is not a believable system.

## What Changes
- Postgres container in the AppHost, referenced by ledger-api.
- ledger-api stores entries with EF Core; first migration creates the `entries` table and its indexes.
- Migrations applied at startup in development.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `ledger`: entries persist across restarts.
- `telemetry`: database queries appear as spans in the request's trace.

## Impact

- ledger-api: `ILedgerStore` gets an EF Core implementation; the in-memory one is removed.
- AppHost: Postgres container with a data volume.
- New dependencies: Aspire Postgres hosting, Npgsql EF Core client integration. Docker Desktop is now required.

## Check
Post payments, restart the AppHost, and the balance is still there. The query for a balance shows up
as a database span inside the payment trace.

## Headline
Enables the missing-index scenario.

## Out of scope
Messaging, Grafana, load.
