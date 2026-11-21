# Proposal

## Why

Every later phase needs a running system that can fail in interesting ways and leaves telemetry
behind when it does. This change builds the smallest version of that: two services, one call
between them, and one trace covering both. Nothing else in the lab can be built until it exists.

Headline claim: this lays the foundation for "evidence from telemetry". Every later diagnosis
reads traces with the service names and span shape set here.

## What Changes

- A .NET solution with `global.json` pinned to the .NET 11 SDK, and the repo initialised with git
  and a `.gitignore`.
- An Aspire AppHost that starts the lab, and a ServiceDefaults project that gives every service
  OpenTelemetry, health checks, service discovery and HTTP resilience.
- `ledger-api`: `POST /entries` records an entry, `GET /accounts/{id}/balance` returns the balance.
  Entries are stored in memory behind a store interface, at most one per required idempotency key.
- `payments-api`: `POST /payments` validates the payment, records it in ledger-api, then responds.
  The client's required `Idempotency-Key` header is passed to the ledger, so a resubmitted payment
  is applied once. A ledger failure becomes an error response, never a success.
- `requests.http` with ready-made requests for every endpoint, as the repeatable manual check.
- README "Running it" section: prerequisites and the one command that starts the lab.
- Manual check: run the AppHost, post a payment, and see one trace covering both services in the
  Aspire dashboard. Then read the account balance and confirm it includes the payment.

Out of scope: a database, messaging, the settlement worker, Grafana or any external telemetry
backend, quality gates and CI, load generation, the third-party payment provider, fault scenarios.

## Capabilities

### New Capabilities

- `payments`: accepting payment requests, validating them, and recording them in the ledger
  before acknowledging.
- `ledger`: recording ledger entries and reporting account balances.
- `telemetry`: trace continuity across lab services. Later changes add queryable backends
  (Grafana) and spans across the database and the message broker to this capability.

### Modified Capabilities

None. There are no existing specs.

## Impact

- New code: solution file, `global.json`, `Directory.Packages.props`, `IncidentHero.AppHost`, `IncidentHero.ServiceDefaults`,
  `PaymentsApi`, `LedgerApi` under `src/`.
- New files: `requests.http`. Changed: `README.md` (Running it).
- New dependencies: .NET 11 SDK, Aspire project templates, Aspire hosting and service defaults
  packages. Docker is not needed yet.
- Repository: the folder becomes a git repository with a public GitHub remote.
- APIs: `POST /payments` (payments-api), `POST /entries` and `GET /accounts/{id}/balance`
  (ledger-api). The Aspire resource names `payments-api` and `ledger-api` become the `service.name`
  in all telemetry from now on.
