## ADDED Requirements

### Requirement: Import review uses the parser API backend
The Mockstar import workflow SHALL obtain parsed and normalized roster data by calling the parser API for both text imports and URL imports instead of invoking parser services in-process.

#### Scenario: OCR text import succeeds through parser API
- **WHEN** a user submits OCR-extracted roster text on the import page
- **THEN** Mockstar sends that text to the parser API and renders the returned review data using the existing import review experience

#### Scenario: Web import succeeds through parser API
- **WHEN** a user submits a roster URL on the import page
- **THEN** Mockstar sends that URL to the parser API and renders the returned review data using the existing import review experience

### Requirement: Import review preserves parser-driven validation feedback
The Mockstar import workflow SHALL surface parser API validation and parsing failures to the user without replacing them with generic transport errors when the parser service returns a handled failure.

#### Scenario: Parser API returns a validation error
- **WHEN** the parser API rejects a text or URL import because the input is invalid
- **THEN** Mockstar renders the import review partial with the parser-provided error message

#### Scenario: Parser API is unavailable
- **WHEN** Mockstar cannot reach the parser API or the parser API returns an unexpected server failure
- **THEN** Mockstar renders the import review partial with a service-level error indicating that parsing is temporarily unavailable
