# Spec Delta

## ADDED Requirements

### Requirement: Ledger entries persist
ledger-api SHALL store entries durably so they survive a restart of the lab.

#### Scenario: Balance after restart
- **WHEN** payments are posted and the AppHost is restarted
- **THEN** the account balance still includes those payments
