## ADDED Requirements

### Requirement: Razor owns application markup structure
The system SHALL define application UI structure in Razor Pages and Razor partials rather than in custom JavaScript files.

#### Scenario: Initial page structure is server-rendered
- **WHEN** a user loads a Mockstar page
- **THEN** the page SHALL return the structural HTML for that view from Razor Pages or Razor partials

#### Scenario: Custom JavaScript enhances existing elements
- **WHEN** custom JavaScript runs on a page
- **THEN** it SHALL target elements that were already rendered by Razor rather than defining equivalent markup in script

### Requirement: Custom JavaScript must not act as a markup templating layer
The system SHALL prohibit custom JavaScript from containing application UI HTML templates or generating major interface regions through `innerHTML`, `insertAdjacentHTML`, or equivalent string-based DOM rendering.

#### Scenario: Script updates visible values without generating markup
- **WHEN** a score slider changes or local state updates
- **THEN** custom JavaScript SHALL update existing text, attributes, classes, or element order without constructing new UI markup strings

#### Scenario: Review detects structural HTML inside custom scripts
- **WHEN** a proposed custom JavaScript change introduces application HTML templates
- **THEN** the change SHALL be treated as a violation of the rendering contract

### Requirement: HTMX introduces new server-rendered markup
The system SHALL use HTMX requests and swaps as the preferred mechanism for loading or replacing markup that is not already present in the DOM.

#### Scenario: New structural region appears after interaction
- **WHEN** a user action requires a new structural UI region that was not rendered initially
- **THEN** the application SHALL obtain that markup from a Razor handler or partial and insert it via HTMX rather than assembling it in custom JavaScript

#### Scenario: Existing structure only needs value or state updates
- **WHEN** the required UI change can be expressed by mutating existing elements
- **THEN** the application MAY use minimal custom JavaScript instead of HTMX

### Requirement: Client-side state may exist without owning markup
The system SHALL allow client-side state and local interaction logic as long as that state does not make custom JavaScript the source of truth for markup structure.

#### Scenario: Local judging state drives existing controls
- **WHEN** the browser stores draft judging state locally
- **THEN** custom JavaScript MAY use that state to update existing controls and indicators without generating score rows, panels, or forms from script templates

#### Scenario: Browser-only capability requires JavaScript
- **WHEN** a feature depends on browser-only capabilities such as OCR
- **THEN** custom JavaScript MAY implement that behavior while still targeting server-rendered containers for UI structure

### Requirement: Business-critical protections do not rely solely on rendering-layer JavaScript
The system SHALL avoid placing business-critical guarantees exclusively in client-side rendering logic.

#### Scenario: Finalization rule matters beyond the current DOM state
- **WHEN** a workflow includes a rule such as preventing invalid finalization
- **THEN** that rule SHALL remain enforceable outside client-side DOM mutation code

#### Scenario: Client-side feedback is immediate but non-authoritative
- **WHEN** custom JavaScript provides instant visual feedback for ranking, ties, or control state
- **THEN** that feedback SHALL be treated as presentation behavior rather than the sole enforcement point for correctness
