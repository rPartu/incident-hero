# Spec Delta

## Purpose

Makes sure every lab service emits telemetry that identifies it and joins up across service
calls, so a diagnosis can follow one request through the whole system.

## ADDED Requirements

### Requirement: Services identify themselves in telemetry
Every lab service SHALL emit logs, metrics and traces tagged with a stable service name:
`payments-api` and `ledger-api` for the services in this change.

#### Scenario: Service names on spans
- **WHEN** a payment is processed
- **THEN** its spans carry `service.name` values of `payments-api` and `ledger-api`

### Requirement: One trace per payment
Each payment request SHALL produce a single trace that contains the spans from payments-api and
from the ledger-api call it causes.

#### Scenario: Trace spans both services
- **WHEN** a client posts a valid payment
- **THEN** the collected telemetry contains one trace with a server span in payments-api, a client span for the ledger call, and a server span in ledger-api, all sharing one trace ID

### Requirement: Logs carry the trace ID
Log records written while handling a request SHALL carry that request's trace ID.

#### Scenario: Log linked to trace
- **WHEN** payments-api logs while handling a payment
- **THEN** the log record's trace ID matches the payment's trace
