## Context

Currently: Import parses via ParserApi → saves to localStorage. Heats/Scoring read from localStorage. New scoring page at `/Scoring/{heatId}` reads from SQLite but DB is empty because Import doesn't persist.

After `add-heat-persistence`: ParserApi has `/api/heats/*` endpoints for CRUD. `HeatApiClient` in Mockstar.Web can call these.

## Goals / Non-Goals

**Goals:**
- Import activation saves parsed EventRecord to SQLite
- Heats page loads from SQLite, displays list, links to new scoring
- Complete flow: Import → DB → Heats → Scoring
- Remove localStorage as source of truth

**Non-Goals:**
- Migrating existing localStorage data
- Score persistence (future work)
- Role assignment UI rework (keep existing UX, just change storage)

## Decisions

### Decision 1: Save after role assignment
**Choice**: Save to DB after user confirms role assignments and clicks "Activate"
**Rationale**: Role assignments modify the EventRecord; save the final version. Alternative: save immediately after parse, then update - adds complexity.

### Decision 2: Heats page server-rendered
**Choice**: Heats page loads from HeatApiClient, renders server-side, no localStorage
**Rationale**: Consistent with new scoring page approach. HTMX for heat selection/detail.

### Decision 3: Remove old scoring page
**Choice**: Delete `/Scoring/Index` and related JS, keep only `/Scoring/{heatId}`
**Rationale**: Clean break. Old page requires localStorage which we're removing.

### Decision 4: Keep import review UX
**Choice**: Import still shows review UI with role assignment, just saves to DB not localStorage
**Rationale**: Minimal UI change, familiar flow. Only the storage backend changes.

## Risks / Trade-offs

- [Breaking change] → Document clearly; no auto-migration
- [ParserApi must be running] → Already required; no new dependency
- [Loss of offline capability] → Acceptable for judging tool; network expected
