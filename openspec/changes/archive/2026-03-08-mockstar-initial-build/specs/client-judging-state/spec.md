# client-judging-state

Persist active judging data in browser localStorage so scoring survives page refreshes.

## State Shape

Stored as JSON under key `mockstar-state`:

```json
{
  "eventRecords": [],
  "scoreSheets": [],
  "selectedHeatId": null,
  "lastImportedHeatIds": []
}
```

## Behavior

- All judging state (events, heats, scores) lives in browser localStorage.
- The server is stateless — no database, no session state.
- State is restored on page load so scoring survives refresh.
- When no stored data exists, initialize with empty state: `{eventRecords: [], scoreSheets: [], lastImportedHeatIds: []}`.

## Domain Types

### Enums

| Name | Values |
|------|--------|
| DivisionKind | `jack-and-jill`, `strictly-swing` |
| RoundPhase | `prelim`, `quarterfinal`, `semifinal`, `final` |
| ScoringRole | `leader`, `follower`, `couple` |
| HeatStatus | `draft`, `finalized` |
| ImportSourceKind | `image`, `web` |

### Entry Types

| Type | Fields |
|------|--------|
| BibEntry | id, bib, display |
| CoupleEntry | id, leaderBib, followerBib (optional), display |
| Pairing | leaderBib, followerBib |

### Heat Types (discriminated union by divisionKind + phase)

**JackAndJillPrelimHeat**: J&J prelim/quarterfinal/semifinal heat.
- id, name, phase, divisionKind=`jack-and-jill`
- leaderEntries: BibEntry[]
- followerEntries: BibEntry[]
- importSource

**JackAndJillFinalHeat**: J&J final heat. Scored by leader bib, follower bib joinable at any point.
- id, name, phase=`final`, divisionKind=`jack-and-jill`
- leaderEntries: BibEntry[]
- followerEntries: BibEntry[] (may be empty, populated when known)
- pairings: Pairing[] (populated when known)
- importSource

**StrictlyHeat**: Strictly Swing heat. Scored per couple as a unit.
- id, name, phase, divisionKind=`strictly-swing`
- coupleEntries: CoupleEntry[]
- importSource

### Other Entities

- **EventRecord**: Container for heats from one import. Fields: id (string), name, heats[].
- **ScoreSheet**: Scoring record for one role within a heat. Fields: heatId, role (ScoringRole), status (HeatStatus), scores (entryId → score), finalizedAt (optional).
- **ImportSource**: Import metadata. Fields: kind (ImportSourceKind), timestamp.

## Graceful Degradation

When localStorage is unavailable: show a warning banner and fall back to in-memory state (data lost on refresh).
