# parser-api Specification

## Purpose
TBD - created by archiving change separate-parser-minimal-api. Update Purpose after archive.
## Requirements
### Requirement: Parser API accepts direct roster text
The system SHALL provide an HTTP endpoint that accepts roster source text and returns parsed and normalized roster data suitable for Mockstar's import review flow.

#### Scenario: Parse supplied roster text
- **WHEN** a client submits roster text to the parser API
- **THEN** the API returns the detected event metadata, parsed heats, normalized review data, and any role-assignment prompts derived from that text

#### Scenario: Reject empty roster text
- **WHEN** a client submits an empty or whitespace-only text payload
- **THEN** the API rejects the request with a validation error describing that roster text is required

### Requirement: Parser API supports URL-based roster ingestion
The system SHALL provide an HTTP endpoint that accepts an absolute roster URL, fetches the source document server-side, extracts roster text, and returns the same parsed and normalized response shape used for direct text parsing.

#### Scenario: Parse roster from URL
- **WHEN** a client submits a valid absolute URL to the parser API
- **THEN** the API fetches the page, extracts roster text, parses it, and returns import-ready data in the standard parser response contract

#### Scenario: Reject invalid URL input
- **WHEN** a client submits a missing, malformed, or non-absolute URL
- **THEN** the API rejects the request with a validation error describing that a valid absolute URL is required

### Requirement: Parser API preserves parser failure details
The system SHALL return parser and scraping failures in a machine-readable error response that allows the Mockstar UI to show import errors without inferring the cause client-side.

#### Scenario: No roster entries found
- **WHEN** parsing completes without finding any roster entries
- **THEN** the API returns an error response that identifies the import as invalid and includes the parser failure message

#### Scenario: Upstream page fetch fails
- **WHEN** URL ingestion cannot fetch the source page
- **THEN** the API returns an error response that identifies the fetch failure without returning a partial parse result

