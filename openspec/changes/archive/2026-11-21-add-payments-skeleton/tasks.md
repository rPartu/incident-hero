# Tasks

## 1. Prerequisites (Fri 20 Nov, about 20 min)

- [x] 1.1 Install the .NET 11 SDK and verify `dotnet --list-sdks` lists an 11.x SDK next to 10.0.301
- [x] 1.2 Install the Aspire project templates (`dotnet new install Aspire.ProjectTemplates`) and verify `dotnet new list aspire` shows the AppHost and ServiceDefaults templates

## 2. Solution scaffolding (about 30 min)

- [x] 2.1 `git init`, `dotnet new gitignore`, `global.json` pinned to the installed .NET 11 SDK, and verify `dotnet --version` in the repo root prints that SDK
- [x] 2.2 Create `IncidentHero.slnx` with `IncidentHero.AppHost` and `IncidentHero.ServiceDefaults` under `src/`, plus `Directory.Packages.props` with central package management on, and verify `dotnet build` succeeds
- [x] 2.3 Read `IncidentHero.ServiceDefaults/Extensions.cs` end to end, and verify by writing a short comment at the top of the file that lists what `AddServiceDefaults` and `MapDefaultEndpoints` register

## 3. ledger-api (about 30 min)

- [x] 3.1 Add the `LedgerApi` project with ServiceDefaults, registered in the AppHost as `ledger-api`, and verify it appears as running in the Aspire dashboard
- [x] 3.2 Add `ILedgerStore` with an in-memory implementation, `POST /entries` (validation including a required idempotency key, at most one entry per key) and `GET /accounts/{id}/balance`, and verify with requests in `requests.http` covering the ledger spec scenarios: entry recorded, invalid entry, retried entry, reused key with a different entry, missing idempotency key, balance after entries, unknown account

## 4. payments-api (about 30 min)

- [x] 4.1 Add the `PaymentsApi` project with ServiceDefaults, registered in the AppHost as `payments-api` with `.WithReference(ledger)`, and verify it appears as running in the Aspire dashboard
- [x] 4.2 Add the typed `LedgerClient` on `https+http://ledger-api` and `POST /payments` with validation (including a required `Idempotency-Key` header), the header passed to the ledger as its idempotency key, the ledger entry ID returned as the payment ID, ledger 409 returned as a 409 problem, and 502 problem responses on other ledger failures, and verify with requests in `requests.http` for a valid payment, a retried payment, a reused key with a different payment, a missing idempotency key and a zero amount
- [x] 4.3 Stop ledger-api from the dashboard, post a payment, and verify payments-api returns 502 with no payment ID and the trace shows the retried ledger calls

## 5. Documentation (about 15 min)

- [x] 5.1 Replace the README "Running it" placeholder with the prerequisites (.NET 11 SDK, Aspire templates), `dotnet run --project src/IncidentHero.AppHost`, and how to use `requests.http`, and verify by following it from a fresh clone into a temporary folder

## 6. End-to-end check

- [x] 6.1 With both services running, post a payment from `requests.http` and verify one trace in the dashboard has spans from `payments-api` and `ledger-api` under one trace ID, a payments-api log record carries that trace ID, and the account balance includes the payment

## Workflow follow-up

- Create the public `incident-hero` repository on GitHub, add it as `origin`, commit and push.
- Archive the change, then move `add-quality-gates` from `docs/planned-changes/` into `openspec/changes/` and add its `.openspec.yaml`.
