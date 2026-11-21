# Tasks

## 1. Build and style gates (Sat 21 Nov, about 30 min)

- [ ] 1.1 Add `Directory.Build.props` (`TreatWarningsAsErrors`, `AnalysisLevel` `latest`, nullable on) and `.editorconfig`, fix everything they flag in the skeleton, and verify `dotnet build` and `dotnet format --verify-no-changes` both pass
- [ ] 1.2 Add an unused variable as a scratch edit, verify `dotnet build` fails, then revert it

## 2. Integration tests (about 30 min)

- [ ] 2.1 Add `tests/IncidentHero.Tests` with the Aspire testing package and one test that starts the AppHost, posts a payment and checks the balance, and verify `dotnet test` passes
- [ ] 2.2 Add a test that posts a zero-amount payment and expects 400, and verify `dotnet test` passes

## 3. CI and docs (about 30 min)

- [ ] 3.1 Add `.github/workflows/ci.yml` on `ubuntu-latest` (setup-dotnet from `global.json`, restore, build, format check, `dotnet dev-certs https --trust`, test) for pull requests and pushes to main, and verify the first run on main is green with the integration tests passing over https
- [ ] 3.2 Add a "Quality gates" section to the README listing the three local commands, and verify they pass when run exactly as written

## 4. End-to-end check

- [ ] 4.1 Open a PR with a deliberate warning and verify CI fails; remove it and verify CI passes

## Workflow follow-up

- Merge the PR and archive the change, then move `add-ledger-database` into `openspec/changes/` and add its `.openspec.yaml`.
