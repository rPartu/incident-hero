# Spec Delta

## ADDED Requirements

### Requirement: Database calls are traced
Database queries SHALL appear as spans in the trace of the request that caused them.

#### Scenario: Balance query span
- **WHEN** a client requests an account balance
- **THEN** the trace includes a database span for the balance query
