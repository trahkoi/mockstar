## Why

The scoring page currently sends full JSON state to the server on every interaction just to get HTML back. This is unnecessary complexity - the server should render scoring UI directly from SQLite, with minimal JS handling only slider ranking and tie detection.

## What Changes

- Scoring page loads heat data from SQLite via `/Scoring/{heatId}?role=leader`
- Server renders complete scoring HTML (entries, sliders, tabs)
- Role switching uses HTMX partial swap (no full page reload)
- JS reduced to ~50 lines: rank calculation, tie detection, score persistence across role switches
- Remove localStorage dependency for scoring
- Remove JSON round-trips for slider interactions

## Capabilities

### New Capabilities
- `server-rendered-scoring`: Scoring page rendered from SQLite, minimal JS for ranking/ties only

### Modified Capabilities
- `live-heat-scoring`: Rendering moves server-side; client JS only handles rank numbers and tie highlighting

## Impact

- `Mockstar.Web/Pages/Scoring/` - rewrite to load from repository
- `Mockstar.Web/wwwroot/js/scoring.js` - replace with minimal ~50 line version
- `Mockstar.Web/wwwroot/js/mockstar-state.js` - scoring no longer uses this
- Depends on `IHeatRepository` from `add-heat-persistence` change
- Out of scope: saving scores to server, import/heats page refactors
