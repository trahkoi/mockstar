# roster-import Specification

## Purpose
Import heat rosters from image OCR or roster web pages, normalize them into heat-by-heat judging inputs, and present the parsed result for review before activation.

## Requirements

### Requirement: Roster import supports OCR text imports
The system SHALL allow users to import roster data from OCR-extracted text and review the parsed heats before activation.

#### Scenario: OCR text import produces review data
- **WHEN** a user submits OCR-extracted roster text on the import page
- **THEN** the system parses and normalizes the text into reviewable heat data before activation

#### Scenario: OCR text import rejects empty text
- **WHEN** OCR submission produces empty or whitespace-only text
- **THEN** the system returns an import error indicating that OCR did not produce roster text

### Requirement: Roster import supports web roster imports
The system SHALL allow users to import roster data from a roster URL and review the parsed heats before activation.

#### Scenario: Web roster import produces review data
- **WHEN** a user submits a valid roster URL
- **THEN** the system fetches the source text server-side, parses it, and returns reviewable heat data before activation

#### Scenario: Web roster import rejects missing URL input
- **WHEN** a user submits an empty roster URL
- **THEN** the system returns an import error indicating that a roster URL is required

### Requirement: Roster parsing infers heat structure and roles
The system SHALL detect division kind, phase, division name, event name, and heat structure from the imported text and normalize the result into the supported heat types.

#### Scenario: Strictly Swing text produces strictly heats
- **WHEN** imported text identifies a strictly division and contains couple pairs
- **THEN** the parser creates strictly heats with couple entries

#### Scenario: Jack and Jill text produces leader and follower entries
- **WHEN** imported text identifies leader and follower sections for a Jack and Jill heat
- **THEN** the parser creates Jack and Jill heats with the corresponding leader and follower entry lists

### Requirement: Ambiguous role assignments require review
The system SHALL identify imported bibs with ambiguous roles and require the user to review them before activation.

#### Scenario: No role headers are detected
- **WHEN** imported Jack and Jill text contains bibs without recognizable leader or follower sections
- **THEN** the system marks those bibs as ambiguous and displays a role-assignment prompt in the review UI

### Requirement: Finals preserve pairing-ready scoring data
The system SHALL preserve leader-based scoring data for Jack and Jill finals while allowing follower entries and pairings to be absent or added later.

#### Scenario: Final heat imports with leader bibs only
- **WHEN** a Jack and Jill final import contains leader bibs without follower pairings
- **THEN** the system creates a final heat with leader entries present and follower entries and pairings empty

### Requirement: Import review uses the parser API backend
The Mockstar import workflow SHALL obtain parsed and normalized roster data by calling the parser API for both text imports and URL imports instead of invoking parser services in-process.

#### Scenario: OCR text import succeeds through parser API
- **WHEN** a user submits OCR-extracted roster text on the import page
- **THEN** Mockstar sends that text to the parser API and renders the returned review data using the existing import review experience

#### Scenario: Web import succeeds through parser API
- **WHEN** a user submits a roster URL on the import page
- **THEN** Mockstar sends that URL to the parser API and renders the returned review data using the existing import review experience

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

### ~~Requirement: Import activation saves to browser localStorage~~ (REMOVED)
> **Removed by:** use-persisted-heats
> **Reason:** Replaced by server-side persistence. Data saved to localStorage will not be migrated; users must re-import.

### Requirement: Import review preserves parser-driven validation feedback
The Mockstar import workflow SHALL surface parser API validation and parsing failures to the user without replacing them with generic transport errors when the parser service returns a handled failure.

#### Scenario: Parser API returns a validation error
- **WHEN** the parser API rejects a text or URL import because the input is invalid
- **THEN** Mockstar renders the import review partial with the parser-provided error message

#### Scenario: Parser API is unavailable
- **WHEN** Mockstar cannot reach the parser API or the parser API returns an unexpected server failure
- **THEN** Mockstar renders the import review partial with a service-level error indicating that parsing is temporarily unavailable
