# Design: Add quality gates

## Context
The skeleton exists without gates. See proposal.md for why gates come before any agent code.

## Goals / Non-Goals
- Goals: the same checks locally and in CI; a green baseline before scenarios exist.
- Non-Goals: eval runs in CI (phase 2), code coverage targets.

## Decisions
- **The SDK's default analysers (`AnalysisLevel` `latest`)**, nullable reference types on, warnings as errors: the rules every .NET project gets, so the gate reflects normal .NET practice rather than personal taste. Alternative: `latest-recommended`. Rejected because it adds many rules (format providers, logging source generators and similar) that make small sample code noisier to read; single rules can be turned on in `.editorconfig` when they earn their place.
- **GitHub Actions on `ubuntu-latest`.** It's faster than Windows runners and has Docker, which Aspire integration tests need. Alternative: `windows-latest` to match the dev machine. Rejected because its runners have no Linux containers.
- **One integration test through `DistributedApplicationTestingBuilder`**, not unit tests first: scenario regression tests will use the same path, so prove it works now. Alternative: `WebApplicationFactory` per service. Rejected because it can't test the call between the two services.

## Under the hood
- **Directory.Build.props**: MSBuild imports it automatically into every project below its folder,
  so settings live in one place.
- **Analysers**: Roslyn analysers run inside the compiler; `AnalysisLevel` picks which rule set is on (`latest` is the SDK default),
  and `TreatWarningsAsErrors` turns every finding into a build failure.
- **dotnet format --verify-no-changes**: formats in memory and exits non-zero if anything would change,
  using `.editorconfig` as the rule book.
- **Aspire testing** (`DistributedApplicationTestingBuilder`): starts the real AppHost inside the test
  run, so integration tests hit the real services - the same mechanism the scenario regression tests use.

## Risks / Trade-offs
- [Strict analysers flag a lot in generated or template code] → fix it now while the code is small; suppress per file only with a written reason.
- [The ASP.NET Core development certificate is not trusted on the Linux runner, so the https calls
  in the integration test (test to payments-api, payments-api to ledger-api) fail in CI while they
  pass locally] → the workflow runs `dotnet dev-certs https --trust` before the tests. If that is not
  enough on Linux, the test uses the services' http endpoints instead, with the reason in a comment.
- [Aspire tests are slower than unit tests] → acceptable at this size; revisit if CI passes 5 minutes.
