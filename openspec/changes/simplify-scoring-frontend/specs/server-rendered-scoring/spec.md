## ADDED Requirements

### Requirement: Scoring page loads heat from persistence
The system SHALL load heat data from SQLite (via ParserApi) when rendering the scoring page at `/Scoring/{heatId}`.

#### Scenario: Load heat for scoring
- **WHEN** a user navigates to `/Scoring/{heatId}`
- **THEN** the server fetches the heat from ParserApi and renders the scoring UI

#### Scenario: Heat not found
- **WHEN** a user navigates to `/Scoring/{heatId}` for a non-existent heat
- **THEN** the server returns a 404 or appropriate error page

### Requirement: Scoring page accepts role parameter
The system SHALL accept an optional `role` query parameter (`leader`, `follower`, or `couple`) to determine which entries to display.

#### Scenario: Default role selection
- **WHEN** a user navigates to `/Scoring/{heatId}` without a role parameter
- **THEN** the server selects the first available role for that heat type

#### Scenario: Explicit role selection
- **WHEN** a user navigates to `/Scoring/{heatId}?role=follower`
- **THEN** the server renders the follower entries for scoring

### Requirement: Role switching via HTMX partial swap
The system SHALL provide role tabs that use HTMX to swap the scoring panel without full page reload.

#### Scenario: Switch from leader to follower
- **WHEN** a user clicks the "Followers" tab while viewing leaders
- **THEN** HTMX fetches the follower panel and swaps it into `#scoring-panel`

### Requirement: Server renders entry rows with sliders
The system SHALL render each entry as a row containing rank display, bib number, and a range slider (0-1000, default 500).

#### Scenario: Initial render
- **WHEN** the scoring panel loads
- **THEN** each entry has a slider at 500 and rank based on initial ordering
