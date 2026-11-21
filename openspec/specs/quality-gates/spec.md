# quality-gates Specification

## Purpose

Defines the build, style and test checks every change to the repo must pass, locally and in CI.
The agent's pull requests are later judged by the same gates.

## Requirements

### Requirement: Warnings fail the build
The build SHALL fail on any compiler or analyser warning in any project.

#### Scenario: New warning
- **WHEN** a change introduces a warning
- **THEN** `dotnet build` fails

### Requirement: Formatting is enforced
The repo SHALL fail verification when code does not match `.editorconfig`.

#### Scenario: Style drift
- **WHEN** a change breaks a formatting rule
- **THEN** `dotnet format --verify-no-changes` exits with a non-zero code

### Requirement: CI on every pull request
CI SHALL run build, format verification and all tests on every pull request.

#### Scenario: Failing gate blocks the PR
- **WHEN** any gate fails on a pull request
- **THEN** the CI check is reported as failed
