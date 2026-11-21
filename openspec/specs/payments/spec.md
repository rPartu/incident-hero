# payments Specification

## Purpose

Accepts payment requests from clients and makes sure each accepted payment is recorded in the
ledger before the client is told it succeeded.

## Requirements

### Requirement: Payment accepted only after the ledger records it
payments-api SHALL respond with success to a valid payment only after ledger-api has recorded a
ledger entry for it. Amounts are integer minor units with a currency code.

#### Scenario: Valid payment
- **WHEN** a client posts a payment with an `Idempotency-Key` header, an account ID, a positive amount in minor units and a three-letter currency code
- **THEN** payments-api records an entry for that account and amount in ledger-api, using the client's key as the entry's idempotency key
- **AND** responds 201 with the payment ID, which is the ledger entry ID

### Requirement: A retried payment is applied once
payments-api SHALL require an `Idempotency-Key` header on every payment and pass it to ledger-api
unchanged, so a client that resubmits a payment cannot have it applied twice.

#### Scenario: Retried payment
- **WHEN** a client posts the same payment with `Idempotency-Key: key-1` twice
- **THEN** both responses are 201 with the same payment ID
- **AND** the account balance includes the amount once

#### Scenario: Reused key with a different payment
- **WHEN** a client posts a payment with `Idempotency-Key: key-1`, then posts a different account, amount or currency with the same key
- **THEN** payments-api responds 409 with a problem description to the second request
- **AND** the account balance includes only the first payment

### Requirement: Invalid payments are rejected
payments-api SHALL reject a payment with a missing `Idempotency-Key` header, a missing account ID,
a non-positive amount or a currency code that is not three letters, without calling ledger-api.

#### Scenario: Non-positive amount
- **WHEN** a client posts a payment with an amount of 0
- **THEN** payments-api responds 400 with a problem description naming the invalid field
- **AND** no ledger entry is created

#### Scenario: Missing idempotency key
- **WHEN** a client posts an otherwise valid payment without an `Idempotency-Key` header
- **THEN** payments-api responds 400 with a problem description naming the missing header
- **AND** no ledger entry is created

### Requirement: Ledger failure is not reported as success
payments-api SHALL respond with a server error when ledger-api cannot record the entry, so a client
never sees success for a payment the ledger does not have.

#### Scenario: Ledger unavailable
- **WHEN** a client posts a valid payment and ledger-api does not respond successfully, after any retries
- **THEN** payments-api responds with a 5xx status
- **AND** the response contains no payment ID
