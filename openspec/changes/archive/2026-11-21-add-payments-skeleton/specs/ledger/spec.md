# Spec Delta

## Purpose

Keeps the record of money moving into accounts and reports each account's balance, so other lab
services have one source of truth for balances.

## ADDED Requirements

### Requirement: Ledger records entries
ledger-api SHALL record an entry for an account ID, an amount in integer minor units and a currency
code, and return the new entry's ID.

#### Scenario: Entry recorded
- **WHEN** a client posts an entry for account `acc-1` of 1250 GBP minor units
- **THEN** ledger-api responds 201 with an entry ID

#### Scenario: Invalid entry
- **WHEN** a client posts an entry with a non-positive amount or no account ID
- **THEN** ledger-api responds 400 and records nothing

### Requirement: Repeated entries for one payment are recorded once
ledger-api SHALL require an idempotency key on every entry and record at most one entry per
idempotency key, so a retried request cannot apply a payment twice.

#### Scenario: Retried entry
- **WHEN** a client posts the same entry with idempotency key `pay-1` twice
- **THEN** both responses return the same entry ID
- **AND** the account balance includes the amount once

#### Scenario: Reused key with a different entry
- **WHEN** a client posts an entry with idempotency key `pay-1`, then posts a different account, amount or currency with the same key
- **THEN** ledger-api responds 409 to the second request
- **AND** the account balance includes only the first entry

#### Scenario: Missing idempotency key
- **WHEN** a client posts an entry without an idempotency key
- **THEN** ledger-api responds 400 and records nothing

### Requirement: Balance reflects recorded entries
ledger-api SHALL return an account's balance as the sum of its recorded entries, per currency.

#### Scenario: Balance after entries
- **WHEN** entries of 1250 and 750 GBP minor units are recorded for account `acc-1` and a client requests its balance
- **THEN** ledger-api returns a balance of 2000 GBP minor units for `acc-1`

#### Scenario: Unknown account
- **WHEN** a client requests the balance of an account with no entries
- **THEN** ledger-api responds 200 with a balance of zero and no currencies
