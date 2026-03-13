## Context

Current scoring page sends entire `ClientJudgingState` JSON to server on every interaction, server deserializes and renders HTML, then client JS re-calculates ranks anyway. This is wasteful - server should render directly from SQLite, JS should only handle the interactive ranking.

With `add-heat-persistence` complete, heats are now in SQLite. Mockstar.Web can call ParserApi's `/api/heats/{eventId}` to load heat data.

## Goals / Non-Goals

**Goals:**
- Server renders scoring UI from SQLite (via ParserApi)
- Minimal JS (~50 lines): rank numbers, tie detection, score memory across role switches
- HTMX for role switching (partial swap, no full reload)
- Remove localStorage dependency for scoring page

**Non-Goals:**
- Saving scores to server (future work)
- Refactoring Import or Heats pages (separate changes)
- Persisting scores across page refreshes (in-memory only for now)

## Decisions

### Decision 1: URL structure
**Choice**: `/Scoring/{heatId}?role=leader`
**Rationale**: Heat ID in path (resource), role as query param (view variant). Clean REST-ish URLs.

### Decision 2: Role switching via HTMX partial swap
**Choice**: HTMX `hx-get` with `hx-target="#scoring-panel"` for role tabs
**Rationale**: Avoids full page reload, keeps it simple. Server returns just the panel HTML. Alternatives:
- Full page reload: slower, loses any transient UI state
- Client-side tab switching: would require more JS, defeats purpose

### Decision 3: In-memory score state in JS
**Choice**: Minimal JS object `{ leader: { entryId: score }, follower: { ... } }` persists across role switches
**Rationale**: Scores need to survive HTMX swaps within same session. Not persisted to localStorage - acceptable since save is out of scope. Alternative:
- Hidden form fields: messier, harder to restore on swap

### Decision 4: Mockstar.Web calls ParserApi for heat data
**Choice**: Add HTTP client in Mockstar.Web to call `GET /api/heats/{eventId}`
**Rationale**: Keeps persistence in ParserApi, Web stays a frontend. Could share DB later but HTTP is simpler now.

### Decision 5: Rank updates stay client-side
**Choice**: JS updates `.rank` text and `.is-tied` class on slider input, no server call
**Rationale**: Instant feedback, network-independent. This is the "island" of interactivity.

## Risks / Trade-offs

- [Scores lost on page refresh] → Acceptable for now; save feature is future work
- [ParserApi must be running] → Already required for import; no new dependency
- [Two role scores in memory] → Small; max ~30 entries × 2 roles × 4 bytes = trivial
