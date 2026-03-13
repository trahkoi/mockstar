## 1. API Client Setup

- [x] 1.1 Add HTTP client service in Mockstar.Web to call ParserApi heat endpoints
- [x] 1.2 Create HeatApiClient with GetHeatAsync(eventId, heatId) method
- [x] 1.3 Register HeatApiClient in DI

## 2. Scoring Page Backend

- [x] 2.1 Create new Scoring page at /Scoring/{heatId}
- [x] 2.2 Add PageModel that loads heat from HeatApiClient
- [x] 2.3 Add role parameter handling (default to first available role)
- [x] 2.4 Create ScoringViewModel with entries for current role
- [x] 2.5 Add Panel handler for HTMX partial swap

## 3. Scoring Page Frontend

- [x] 3.1 Create Razor view with role tabs and #scoring-panel container
- [x] 3.2 Create _ScoringPanel partial with entry rows (rank, bib, slider)
- [x] 3.3 Wire HTMX on role tabs: hx-get, hx-target="#scoring-panel"

## 4. Minimal JavaScript

- [x] 4.1 Create scoring.js (~50 lines) with rank/tie logic
- [x] 4.2 Implement score state object for cross-role persistence
- [x] 4.3 Wire slider input event to update ranks
- [x] 4.4 Wire htmx:afterSwap to restore scores and recalculate

## 5. Cleanup

- [x] 5.1 Remove old scoring page dependencies on mockstar-state.js
- [x] 5.2 Update navigation links to new /Scoring/{heatId} URL pattern
