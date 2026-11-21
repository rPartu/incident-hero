# Design

## Context

The repo has only a README, a licence and the OpenSpec setup. There is no git repository, solution
or code yet, and no GitHub repository. The development machine has only the .NET 10.0.301 SDK and
no Aspire project templates, so both must be installed before any code. Docker Desktop is
installed but not needed until `add-ledger-database`. The stack is fixed by the project config: C# on .NET 11, Aspire for local
orchestration, Docker Desktop on Windows. Later changes add Postgres behind the ledger
(`add-ledger-database`), RabbitMQ and a settlement worker (`add-settlement-messaging`) and Grafana
(`add-grafana-telemetry`), so the choices here must leave room for those without renames.
See proposal.md for motivation and the specs for the required behaviour.

## Goals / Non-Goals

**Goals:**
- Service names, routes and the money representation that last for the whole project, because
  recorded telemetry snapshots depend on them.
- A ledger storage seam that the database change can fill without touching endpoints.
- A healthy HTTP client between the two services that later fault scenarios can be compared against.

**Non-Goals:**
- Automated tests. The test project and CI arrive in `add-quality-gates`, the next change. A
  `.http` file is the repeatable manual check until then.
- Authentication, persistence, settlement status, currency conversion.

## Decisions

**1. Project layout: `src/` with one folder per project, resource names in kebab-case.**
Projects: `IncidentHero.AppHost`, `IncidentHero.ServiceDefaults`, `PaymentsApi`, `LedgerApi`,
in `IncidentHero.slnx`. The AppHost registers them as `payments-api` and `ledger-api`.
Alternative: names like `Payments` / `Ledger`. Rejected because the resource name becomes
`service.name` in every span, and changing it later would break recorded snapshots and lessons.

**2. Minimal APIs over controllers.**
Three endpoints don't justify controllers, and less code means less for you to read now and for the
agent to search later. Alternative: MVC controllers, which add attribute routing and model binding
conventions with nothing to gain at this size.

**3. Money as `long amountMinor` plus `string currency`.**
For example `{ "accountId": "acc-1", "amountMinor": 1250, "currency": "GBP" }`. This is the usual
payments practice and avoids rounding bugs. Alternative: `decimal amount`. Rejected because
decimals invite rounding and formatting differences between services, which would be a fault
nobody planted.

**4. Ledger storage behind `ILedgerStore`, in memory for now.**
A `ConcurrentDictionary`-backed store registered as a singleton. The database change swaps the
implementation. Alternative: EF Core's in-memory provider now. Rejected because it pulls in EF
before the database change decides how EF is used, and it behaves differently from Postgres anyway.

**5. The client supplies the idempotency key and payments-api passes it to the ledger.**
`POST /payments` requires an `Idempotency-Key` header. payments-api sends it unchanged as the
required `idempotencyKey` on `POST /entries`, and the ledger records at most one entry per key.
A repeat with the same key and same entry returns the original entry ID; a repeat with the same
key but a different account, amount or currency gets `409 Conflict`, so a caller bug is surfaced
instead of hidden. The payment ID returned to the client is the ledger entry ID, so a retried
payment gets the same payment ID back without payments-api storing anything.

One key covers both kinds of retry. A client resubmitting `POST /payments` reuses its key, and the
standard resilience handler (decision 6) retrying `POST /entries` after a timeout resends the same
key. The key is required rather than optional so no caller can skip it and silently lose the
protection.

Alternative: payments-api generates a GUID per request as the key. Rejected because it only covers
the handler's retries: a client resubmission gets a new GUID and the payment is recorded twice.
Alternative: turn off retries for unsafe methods (`DisableForUnsafeHttpMethods`). Rejected because
it does nothing for client resubmissions, the retries are the healthy baseline that the
provider-timeouts scenario is later measured against, and client-supplied idempotency keys are how
real payment APIs handle this.

**6. Keep ServiceDefaults' standard resilience handler on the ledger client.**
The handler adds a timeout, retries with backoff and a circuit breaker. The provider-timeouts
scenario will break a different client (the third-party provider), so this one stays the healthy
example. Alternative: no resilience, so failures show up sooner. Rejected because it would make the
lab less realistic than the systems the agent is meant to diagnose.

**7. Ledger failures map to 502 with a problem description.**
When the ledger call fails after retries (`HttpRequestException`, a timeout or a non-success
status other than 409), payments-api returns `502 Bad Gateway` with `application/problem+json` and
no payment ID. A ledger `409` is the client's own key conflict (decision 5), not a downstream
fault, so payments-api returns it as `409 Conflict` with a problem description. Validation errors,
including a missing `Idempotency-Key` header, return `400` with a validation problem. Alternative: let exceptions bubble up as
500. Rejected because a 502 tells the agent the fault is downstream, which is evidence a diagnosis
should be able to use.

**8. Ledger client typed with service discovery: `https+http://ledger-api`.**
A typed `LedgerClient` registered with `AddHttpClient<LedgerClient>`, its base address set to the
discovery name. Alternative: read ledger-api's URL from configuration by hand. Rejected because
Aspire already injects it, and discovery is how the later services will find each other too.

**9. Central package management (`Directory.Packages.props`) from the first commit.**
Package versions live in one file. Aspire and later Microsoft Agent Framework prerelease versions
must be pinned in one place. Alternative: versions in each `.csproj`, which drift as projects are
added. `Directory.Build.props` and analyser settings are left to `add-quality-gates`.

## Under the hood

- **AppHost**: a normal .NET program whose `Program.cs` describes the system. `AddDotnetProject("payments-api", path)`
  registers a project (Aspire 17 replaced the older `AddProject<T>`), and `.WithReference(ledger)` injects ledger-api's endpoint into payments-api
  as configuration (`services__ledger-api__https__0`). `dotnet run` on the AppHost starts every
  resource and the dashboard.
- **Service discovery**: `https+http://ledger-api` is not a real host name. `AddServiceDefaults`
  registers a resolver that reads the injected configuration and turns the name into an address,
  preferring https and falling back to http.
- **ServiceDefaults** (`AddServiceDefaults`, `MapDefaultEndpoints`): one call that wires up
  OpenTelemetry for logs, metrics and traces, the `/health` and `/alive` endpoints, service
  discovery, and the standard resilience handler on every `HttpClient`. It is plain code in
  `Extensions.cs` in your repo, not a hidden package, so read it end to end.
- **Resilience handler**: a pipeline of Polly strategies around each request: total timeout,
  retry with exponential backoff and jitter, circuit breaker, and per-attempt timeout. Every retry
  is its own HTTP client span, which is why retries show up in traces.
- **Trace propagation**: the HttpClient instrumentation adds a W3C `traceparent` header to the
  outgoing call. ASP.NET Core in ledger-api reads it and parents its server span to it, so both
  services' spans share one trace ID. Logging inside a request picks up the current `Activity`, which
  is how logs get the trace ID.
- **Dashboard and OTLP**: the AppHost runs the Aspire dashboard with an OTLP endpoint and sets
  `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_SERVICE_NAME` on each project. That is why telemetry shows
  up without exporter configuration, and why the resource name becomes `service.name`.
- **Problem details**: `AddProblemDetails()` plus `Results.Problem` / `Results.ValidationProblem`
  produce RFC 9457 `application/problem+json` bodies, the standard shape for HTTP API errors.

## Risks / Trade-offs

- [Aspire or another package does not yet fully support .NET 11] → Pin versions in
  `Directory.Packages.props`. If a project cannot target `net11.0`, fall back to `net10.0` for that
  project only and note it in the README.
- [Retries hide brief ledger failures from early diagnosis] → Accepted. Failed attempts still appear
  as retry spans in the trace.
- [The in-memory ledger loses data on restart] → Accepted until `add-ledger-database`. The manual
  check runs within one AppHost session.
- [No automated tests until the next change] → `add-quality-gates` is scheduled for the next day.
  Its first integration test covers the same post-payment, check-balance path as the `.http` file.
