# Tasks

## 1. Postgres in the lab (Sat 28 Nov, first half, about 30 min)

- [ ] 1.1 Add Postgres with a named data volume and a `ledgerdb` database to the AppHost, referenced from ledger-api, and verify the container and database show as healthy in the dashboard

## 2. EF Core store (about 1 h)

- [ ] 2.1 Add the DbContext and an `entries` table, and generate the first migration with indexes on account ID and date and a unique index on payment ID, and verify the migration file contains all three indexes
- [ ] 2.2 Replace the in-memory `ILedgerStore` with the EF Core implementation (registered as scoped, like the DbContext) and apply migrations at startup in development, and verify the existing integration tests still pass
- [ ] 2.3 Add an integration test that posts the same entry twice with one payment ID and expects it once in the balance, and verify it passes against Postgres

## 3. Docs (about 15 min)

- [ ] 3.1 Update the README: Docker Desktop is now a prerequisite, and add the command that wipes the data volume for a clean recording, and verify the wipe command works as written

## 4. End-to-end check

- [ ] 4.1 Post payments, restart the AppHost and verify the balance survives; verify the balance request's trace contains a database span

## Workflow follow-up

- Commit, push and archive the change, then move `add-settlement-messaging` into `openspec/changes/` and add its `.openspec.yaml`.
