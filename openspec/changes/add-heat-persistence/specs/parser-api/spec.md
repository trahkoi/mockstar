## ADDED Requirements

### Requirement: Parser API exposes save endpoint
The system SHALL provide an HTTP endpoint that accepts parsed heats and an event ID, persisting them via the heat repository.

#### Scenario: Save parsed heats
- **WHEN** a client submits heats and event ID to the save endpoint
- **THEN** the heats are persisted and a success response is returned

#### Scenario: Save replaces existing heats for event
- **WHEN** a client saves heats for an event ID that already has stored heats
- **THEN** the existing heats are replaced with the new heats

### Requirement: Parser API exposes load endpoint
The system SHALL provide an HTTP endpoint that retrieves previously persisted heats by event ID.

#### Scenario: Load existing heats
- **WHEN** a client requests heats for a known event ID
- **THEN** the heats and event metadata are returned

#### Scenario: Load returns empty for unknown event
- **WHEN** a client requests heats for an unknown event ID
- **THEN** the endpoint returns an empty result (not an error)

### Requirement: Parser API exposes delete endpoint
The system SHALL provide an HTTP endpoint that removes persisted heats for a given event ID.

#### Scenario: Delete heats for event
- **WHEN** a client requests deletion of heats for an event ID
- **THEN** all heats for that event are removed and a success response is returned

### Requirement: Parser API lists stored events
The system SHALL provide an HTTP endpoint that returns a list of all event IDs with stored heats.

#### Scenario: List stored events
- **WHEN** a client requests the list of stored events
- **THEN** the endpoint returns all event IDs that have persisted heats
