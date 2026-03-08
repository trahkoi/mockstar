# live-heat-scoring

Score a single live heat using raw-score sliders (0-1000), assign ranks client-side, highlight ties, and block finalization until ties are resolved.

## Scoring Models

Scoring differs by heat type. Each score sheet covers one set of entries for one role.

```
JackAndJillPrelimHeat
  → ScoreSheet(role=leader)    scores leaderEntries
  → ScoreSheet(role=follower)  scores followerEntries
  (0, 1, or 2 sheets depending on which roles were imported)

JackAndJillFinalHeat
  → ScoreSheet(role=couple)    scored by leader bib, follower bib shown when paired

StrictlyHeat
  → ScoreSheet(role=couple)    scored by couple entry
```

### ScoreSheet

```
ScoreSheet
  ├── heatId
  ├── role: leader | follower | couple
  ├── status: draft | finalized
  ├── scores: { entryId → score }
  └── finalizedAt?
```

## Behavior

- Each entry in the score sheet gets a 0-1000 slider, displayed in fixed bib order.
- Default score is 500 (midpoint).
- Ranks are assigned client-side by sorting entries descending by score.
- Ties (duplicate scores) are broken for display using `localeCompare` with `{numeric: true}` on the entry display value.
- Tied entries are visually highlighted.
- The finalize button is disabled while any two entries share the same score.
- Finalization sets status to `finalized` and records `finalizedAt` as an ISO timestamp.
- A finalized heat cannot have ties — finalization must be rejected if ties exist.

## Entry Presentation

| Heat Type | Role | Entries Shown | Display |
|-----------|------|---------------|---------|
| JackAndJillPrelimHeat | leader | leaderEntries | Bib number |
| JackAndJillPrelimHeat | follower | followerEntries | Bib number |
| JackAndJillFinalHeat | couple | leaderEntries | Leader bib (+ follower bib when paired) |
| StrictlyHeat | couple | coupleEntries | Leader/Follower bib pair |

## J&J Final Pairing

In J&J finals, scoring is done by leader bib. When a pairing exists (leader↔follower), the display enriches to show both bibs. Pairings can be added at any point — the score sheet references leader bib entries regardless of pairing state.

## Scoring Functions

- **Create draft score sheet**: Initialize all entries at 500, status = `draft`. Role determines which entry list is used.
- **Update score**: Immutable update — return new sheet with specified entry's score changed.
- **Get tied entry IDs**: Group scores by value, return all entry IDs where more than one entry shares the same score.
- **Has ties**: True if any tied entry IDs exist.
- **Finalize score sheet**: Reject if ties exist. Otherwise set status = `finalized`, set `finalizedAt`.
- **Rank entries**: Sort descending by score, ties broken by display value with numeric locale compare.
- **Get score for entry**: Lookup by entry ID, fallback to 500.

## Implementation Notes

- Rank computation and tie detection implemented as ~15 lines of vanilla JS client-side.
- _hyperscript handles slider event wiring, CSS class toggling for tie highlights, and finalize button gating.
- No server round-trip for scoring interactions.
