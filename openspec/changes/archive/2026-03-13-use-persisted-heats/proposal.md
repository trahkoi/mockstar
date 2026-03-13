## Why

The app has persistence (SQLite) but doesn't use it. Import saves to localStorage, Heats reads from localStorage, and the new scoring page expects data in the DB. We need to wire Import → DB → Heats → Scoring as a complete flow.

## What Changes

- Import page saves parsed heats to SQLite after user activates import
- Heats page loads events/heats from SQLite instead of localStorage
- Heats page links to new `/Scoring/{heatId}` URL pattern
- **BREAKING**: localStorage is no longer the source of truth; existing browser data won't migrate
- Remove old scoring page and mockstar-state.js dependency from Heats

## Capabilities

### New Capabilities
None - using existing persistence from `add-heat-persistence`

### Modified Capabilities
- `roster-import`: Import activation saves to DB instead of localStorage
- `client-judging-state`: **REMOVED** - replaced by server-side persistence

## Impact

- `Mockstar.Web/Pages/Import/` - call HeatApiClient to save after activation
- `Mockstar.Web/Pages/Heats/` - rewrite to load from HeatApiClient
- `Mockstar.Web/Pages/Scoring/Index.cshtml` - delete old page
- `Mockstar.Web/wwwroot/js/mockstar-state.js` - can be removed
- `Mockstar.Web/wwwroot/js/heats.js` - replace or simplify
- `Mockstar.Web/wwwroot/js/scoring.js` - delete old JS
