# incident-hero

An AI agent that does the first hour of an on-call shift: it reads logs, metrics and traces from a running system, names the root cause with evidence, proposes a fix, waits for a human to approve it, applies it, checks the fault has cleared and drafts the postmortem.

It also learns: after each incident it distils a few lessons into a small, curated memory, and brings back only the lessons that matter for the next incident.

Built in C# with [Microsoft Agent Framework](https://github.com/microsoft/agent-framework), running entirely on local models through [Ollama](https://ollama.com). It costs nothing to run.

I'm building it in public as part of my move from senior software engineer to AI engineer. Each phase ends with a git tag and a write-up with real numbers, failures included.

## How it's scored

The repo includes a test environment with planted faults: a small payments system (a payments API, a ledger API and a settlement worker on a queue). Every planted fault has a known root cause, so every diagnosis the agent makes is either right or wrong. Each scenario is run at least three times, because one run of a non-deterministic system is not a result.

Planned fault scenarios (the first three arrive in phase 0, the rest in phase 2):

| Scenario | What goes wrong |
| --- | --- |
| Provider timeouts | A third-party payment provider slows down and retries pile up |
| Missing index | A query slows down after a schema change |
| Poison message | One bad message keeps failing and fills the dead-letter queue |
| Bad config rollout | A configuration change or feature flag starts causing errors |
| Consumer lag | The settlement worker falls behind the queue |

For each run the eval records whether the root cause was right, the tool calls made, tokens, latency and model.

## Phases

| Phase | What ships | Status |
| --- | --- | --- |
| 0. Infrastructure | .NET Aspire payments services, a settlement worker, Postgres, OpenTelemetry, Grafana; fault injection and five scenarios with runbooks | In progress |
| 1. Diagnosis | Agent with log, metric and trace tools; structured diagnosis from a CLI; golden test set | Planned |
| 2. Evaluation | Eval runner, repeated runs, comparison across local model sizes, error analysis, CI gate | Planned |
| 3. Context | Runbook retrieval (pgvector, hybrid search, reranking), lesson memory with a token budget, prompt-injection tests, tools served over MCP | Planned |
| 4. Orchestration | One generalist agent vs specialist sub-agents for logs, metrics and traces; human approval of fixes; checkpoints | Planned |
| 5. Hardening | Prompt-injection tests on log lines, token and latency budgets per incident, routing between model sizes; postmortems that feed the lesson memory, and a test that the learning actually helps | Planned |

## Stack

- C# / .NET, .NET Aspire
- Microsoft Agent Framework, Microsoft.Extensions.AI
- Ollama (local models)
- OpenTelemetry, Grafana (Loki, Prometheus, Tempo)
- PostgreSQL

## Running it

Prerequisites:

- [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0) (the version is pinned in `global.json`)
- Aspire project templates: `dotnet new install Aspire.ProjectTemplates`

Start the lab (payments-api, ledger-api and the Aspire dashboard):

```bash
dotnet run --project src/IncidentHero.AppHost
```

The first run asks to trust the ASP.NET Core development certificate, because the services and
the dashboard use https. The console prints the dashboard link, including a login token. Open it
to see both services running, and their logs, traces and metrics.

Then send requests from [`requests.http`](requests.http) with Visual Studio, VS Code (REST Client
extension) or Rider. It has one request per scenario, each with the expected result in its
comment. For example, post the "Valid payment" request, then open **Traces** in the dashboard:
one trace covers payments-api and ledger-api. The ledger is in memory, so data is lost when the
AppHost stops.

## Licence

MIT. See [LICENSE](LICENSE).
