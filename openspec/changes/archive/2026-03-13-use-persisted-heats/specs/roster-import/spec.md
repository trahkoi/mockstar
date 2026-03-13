## MODIFIED Requirements

### Requirement: Import activation persists heats to database
The system SHALL save the parsed and role-assigned EventRecord to the database when the user activates an import, instead of saving to browser localStorage.

#### Scenario: Activate import saves to database
- **WHEN** a user clicks "Activate" after reviewing and assigning roles
- **THEN** the system calls POST /api/heats/{eventId} to persist the EventRecord to SQLite

#### Scenario: Activate import redirects to heats list
- **WHEN** the import is successfully saved to database
- **THEN** the user is redirected to the Heats page showing the newly imported event

#### Scenario: Activate import handles save failure
- **WHEN** the database save fails
- **THEN** the system displays an error message and does not redirect

## REMOVED Requirements

### Requirement: Import activation saves to browser localStorage
**Reason**: Replaced by server-side persistence
**Migration**: Data saved to localStorage will not be migrated; users must re-import
