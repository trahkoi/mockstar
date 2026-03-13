## ADDED Requirements

### Requirement: Heat repository stores heats by event
The system SHALL provide a repository that persists heats associated with an event identifier, allowing heats to be saved, retrieved, and deleted.

#### Scenario: Save heats for an event
- **WHEN** the repository receives heats with an event ID
- **THEN** the heats are persisted to SQLite storage

#### Scenario: Retrieve heats by event ID
- **WHEN** a client requests heats for a specific event ID
- **THEN** the repository returns all heats associated with that event

#### Scenario: Delete heats for an event
- **WHEN** a client requests deletion of heats for an event ID
- **THEN** all heats for that event are removed from storage

### Requirement: Heat repository persists event metadata
The system SHALL store event metadata (EventRecord) alongside heats, preserving the event name, slug, division kind, and phase information.

#### Scenario: Event metadata is stored with heats
- **WHEN** heats are saved to the repository
- **THEN** the associated EventRecord is also persisted

#### Scenario: Event metadata is retrieved with heats
- **WHEN** heats are retrieved from the repository
- **THEN** the associated EventRecord is included in the response

### Requirement: Heat persistence uses SQLite database
The system SHALL use SQLite as the storage backend with Entity Framework Core for data access and migrations.

#### Scenario: Database file is created on startup
- **WHEN** the application starts and no database file exists
- **THEN** the database file is created with the required schema

#### Scenario: Migrations are applied automatically
- **WHEN** the application starts with a new schema version
- **THEN** pending migrations are applied to the database
