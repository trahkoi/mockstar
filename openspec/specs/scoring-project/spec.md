# scoring-project Specification

## Purpose
TBD - created by archiving change extract-scoring-project. Update Purpose after archive.
## Requirements
### Requirement: Shared scoring models live in a dedicated scoring project
The system SHALL provide a `Mockstar.Scoring` project that owns the shared heat and scoring domain models used across parsing and scoring features instead of keeping those models inside `Mockstar.ParserApi`.

#### Scenario: Parser API references shared scoring models
- **WHEN** parser normalization or parser-adjacent code requires heat and scoring domain types
- **THEN** those types are resolved from `Mockstar.Scoring` rather than from a parser-owned domain namespace

#### Scenario: Web app can depend on scoring models without parser ownership
- **WHEN** the web application needs access to shared scoring domain models
- **THEN** it references `Mockstar.Scoring` directly or transitively without treating `Mockstar.ParserApi` as the owner of those models

### Requirement: Extracting scoring models does not change parser API behavior
The system SHALL preserve the existing parser API request/response behavior and downstream scoring behavior when the shared scoring models move into `Mockstar.Scoring`.

#### Scenario: Parser API response remains stable after extraction
- **WHEN** a client calls the parser API after the scoring-model extraction
- **THEN** the API returns the same parser response contract and error behavior as before the project move

#### Scenario: Existing parser and scoring tests still pass
- **WHEN** the solution test suites run after the scoring-model extraction
- **THEN** parser API tests and web-app tests both validate the unchanged behavior successfully

