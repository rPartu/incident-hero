# Add quality gates

## Why
The agent's PRs will be judged by these gates, so they must exist - and be green - before any agent
code. They also keep the lab itself clean.

## What Changes
- `Directory.Build.props`: `TreatWarningsAsErrors`, the SDK's default analysers and nullable enabled for every project.
- `.editorconfig` with the code style rules.
- A test project with one integration test using Aspire's testing package.
- GitHub Actions workflow: restore, build, `dotnet format --verify-no-changes`, test.

## Capabilities

### New Capabilities

- `quality-gates`: build warnings, formatting and tests enforced locally and on every pull request.

### Modified Capabilities

None.

## Impact

- New files: `Directory.Build.props`, `.editorconfig`, a test project under `tests/`, `.github/workflows/ci.yml`.
- Existing code: anything the analysers flag in the skeleton is fixed.
- New dependencies: Aspire testing package, xUnit.

## Check
Add a deliberate warning on a branch, open a PR, and watch CI fail; remove it and watch CI pass.

## Headline
Protects "without lowering code quality": these gates are that definition.

## Out of scope
Scenario regression tests, eval runs in CI.
