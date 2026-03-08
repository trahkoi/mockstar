## Context

Mockstar is a greenfield shadow judging app for WSDC dance events, built on ASP.NET Core + HTMX + _hyperscript. The server is stateless — no database, all judging state persists client-side in localStorage. The scoring interaction (adjusting sliders, seeing ranks update, detecting ties) must be instant with no server round-trips. Everything else (page navigation, import flow, heat selection) flows through HTMX.

## Goals / Non-Goals

**Goals:**
- Build a mobile-first shadow judging web app on ASP.NET Core + HTMX + _hyperscript.
- Support roster import from images (Tesseract.js OCR, client-side) and event web pages (AngleSharp, server-side).
- Support role-aware scoring: leaders and followers separately in J&J prelims/semis, couples as a unit in J&J finals and strictly.
- Support pairing leader and follower bibs in J&J finals at any point.
- Persist judging state in browser localStorage.
- Deploy to Azure App Service.

**Non-Goals:**
- Server-side persistence or database integration (deferred).
- Result processing or analysis features (deferred).
- Multi-user collaboration or real-time syncing (deferred).
- Official WSDC callback, alternate, or tab-room logic.
- Manual roster entry.

## Decisions

### ASP.NET Core Razor Pages organized by feature

```
Pages/
├── Index.cshtml                ← landing / app shell
├── Scoring/
│   ├── Index.cshtml            ← heat scoring page
│   └── _RankingPartial.cshtml  ← HTMX fragment (if needed)
├── Import/
│   ├── Index.cshtml            ← import UI (image + web tabs)
│   └── _ResultPartial.cshtml   ← parsed roster fragment
└── Heats/
    ├── Index.cshtml            ← heat list
    └── _HeatDetailPartial.cshtml
```

Rationale:
- Feature folders keep related pages, partials, and handlers together.
- Razor Pages maps naturally to HTMX's page/fragment model.
- Avoids the ceremony of full MVC controllers for a page-oriented app.

### HTMX for page structure, _hyperscript for inline behavior

```
HTMX owns:
  - Heat list loading and selection (hx-get, hx-swap)
  - Import form submission (hx-post, hx-target)
  - Navigation between features
  - Partial HTML fragment swaps

_hyperscript owns:
  - Slider input event wiring
  - UI toggles (import mode tabs, expand/collapse)
  - Visual state changes (tie highlighting CSS classes)
  - Triggering client-side rank recalculation

Vanilla JS (~15 lines) owns:
  - Rank assignment from scores
  - Tie detection (duplicate score identification)
```

Rationale:
- Clean separation: server controls structure, _hyperscript controls behavior, JS handles computation.
- _hyperscript is the natural HTMX companion for inline DOM manipulation.
- The computation needed (rank + tie-detect) is too algorithmic for _hyperscript but too small for a framework.

### Tesseract.js client-side, AngleSharp server-side

Image OCR runs in the browser via Tesseract.js (bundled as a static asset). The browser performs OCR, extracts text, then POSTs the parsed text to the server for roster parsing and normalization.

Web import (URL-based scraping) goes through the server because of CORS. The server fetches the page, scrapes with AngleSharp, parses, and returns an HTML fragment.

```
Image import:  Browser → Tesseract.js → POST text → Server parses → HTML fragment
Web import:    Browser → POST URL → Server fetches + AngleSharp + parses → HTML fragment
```

Rationale:
- Avoids a server-side OCR dependency.
- Asymmetry between image/web paths is pragmatic and each path is simple.

### Stateless server with client-side localStorage

The server has no database and no session state. It receives requests, processes them, and returns HTML. All judging state (events, heats, scores) lives in browser localStorage under the key `mockstar-state`.

This is an explicit simplification for v1. Server-side persistence will be added in a later change.

### Three heat types as a discriminated union

Heats are modeled as a discriminated union based on division kind and phase, because the scoring semantics are fundamentally different:

```
JackAndJillPrelimHeat    → leaders and followers scored independently
JackAndJillFinalHeat     → couples scored as a unit, identified by leader bib
StrictlyHeat             → couples scored as a unit
```

Rationale:
- J&J prelims/semis, J&J finals, and strictly have different entry structures and scoring models.
- A single Heat type with optional fields would obscure these invariants.
- The type system enforces correctness — you can't accidentally score a prelim heat as a couple.

### Azure App Service deployment

The ASP.NET Core app deploys to Azure App Service as a single unit — Razor Pages and API endpoints in the same process.

## Domain Model

### Enums

| Name | Values |
|------|--------|
| DivisionKind | `jack-and-jill`, `strictly-swing` |
| RoundPhase | `prelim`, `quarterfinal`, `semifinal`, `final` |
| ScoringRole | `leader`, `follower`, `couple` |
| HeatStatus | `draft`, `finalized` |
| ImportSourceKind | `image`, `web` |

### Entry Types

| Type | Fields | ID Format | Display |
|------|--------|-----------|---------|
| BibEntry | id, bib | `bib-{bib}` | `"{bib}"` |
| CoupleEntry | id, leaderBib, followerBib? | `couple-{leader}-{follower\|'solo'}` | `"{leader}/{follower}"` or `"{leader}"` |
| Pairing | leaderBib, followerBib | — | Links leader to follower in J&J finals |

### Heat Types (discriminated union)

**JackAndJillPrelimHeat**: J&J prelim/quarterfinal/semifinal.
- id, name, phase, divisionKind=`jack-and-jill`
- leaderEntries: BibEntry[], followerEntries: BibEntry[]
- importSource
- Either list can be empty (import may contain one role only).
- Scoring: up to 2 ScoreSheets (role=leader, role=follower).

**JackAndJillFinalHeat**: J&J final. Couples scored as a unit, identified by leader bib.
- id, name, phase=`final`, divisionKind=`jack-and-jill`
- leaderEntries: BibEntry[], followerEntries: BibEntry[] (may arrive later)
- pairings: Pairing[] (joinable at any point)
- importSource
- Scoring: 1 ScoreSheet (role=couple), scored against leaderEntries.

**StrictlyHeat**: Strictly Swing, all rounds.
- id, name, phase, divisionKind=`strictly-swing`
- coupleEntries: CoupleEntry[]
- importSource
- Scoring: 1 ScoreSheet (role=couple).

### Other Entity Types

**EventRecord**: Container for a set of heats from one import.
- id (string), name, heats[]

**ScoreSheet**: Scoring record for one role within a heat.
- heatId, role (ScoringRole), status (HeatStatus), scores (entryId → score), finalizedAt (optional)

**ImportSource**: Metadata about the import origin.
- kind (ImportSourceKind), timestamp

### State Shape (localStorage)

Stored as JSON under key `mockstar-state`:
```json
{
  "eventRecords": [],
  "scoreSheets": [],
  "selectedHeatId": null,
  "lastImportedHeatIds": []
}
```

## Roster Parsing Logic

### Regex Patterns

| Pattern | Regex | Purpose |
|---------|-------|---------|
| HEAT_SPLIT | `/(?:^|\n)\s*(Heat\s+\d+[:\s-]*)/gi` | Split text on "Heat N" headers |
| COUPLE_PATTERN | `/(\d{1,4})\s*(?:[+/&-]|and)\s*(\d{1,4})/gi` | Match couple pairs |
| BIB_PATTERN | `/\b\d{1,4}\b/g` | Match individual bib numbers (1-4 digits) |

### Detection Rules

**Division kind**: If text contains "strict" (case-insensitive) → `strictly-swing`, else → `jack-and-jill`.

**Phase**: Test in order: "quarter" → `quarterfinal`, "semi" → `semifinal`, "final" → `final`, else → `prelim`.

**Role sections**: Detect "leader" and "follower"/"follow" headers to split bibs into leaderEntries and followerEntries.

**Division name**: Match `/(novice|newcomer|intermediate|advanced|all-star|champion|masters)[^\n]*/i`. Fallback: `"Imported Division"`.

**Event name**: First non-empty line of the input text.

### Heat Type Inference

| Division Kind | Phase | Heat Type |
|--------------|-------|-----------|
| `jack-and-jill` | prelim, quarterfinal, semifinal | JackAndJillPrelimHeat |
| `jack-and-jill` | final | JackAndJillFinalHeat |
| `strictly-swing` | any | StrictlyHeat |

### Parsing Flow

1. Normalize line endings (`\r` → `\n`), trim whitespace.
2. Detect divisionKind, phase, divisionName, eventName from full text.
3. Determine heat type from divisionKind + phase.
4. Split text using HEAT_SPLIT regex, iterate in pairs (heatLabel, heatBody).
5. For each heat:
   - **StrictlyHeat**: Extract couples with COUPLE_PATTERN → coupleEntries.
   - **JackAndJillPrelimHeat / JackAndJillFinalHeat**: Detect role sections. Extract bibs with BIB_PATTERN into leaderEntries and/or followerEntries. Deduplicate with Set.
6. If no heats found from splitting, try treating entire text as a single "Heat 1".
7. Throw error if still no entries found.

### ID Generation

- Event base ID: `{slugify(eventName)}-{timestamp}` where slugify = lowercase, replace non-alphanumeric with `-`, trim leading/trailing dashes.
- Heat IDs: `{baseId}-heat-{index+1}` (1-based).

## Scoring Logic

### Constants

- `DEFAULT_SCORE = 500` (midpoint of 0-1000 range)

### Score Sheet Creation by Heat Type

| Heat Type | Score Sheets Created |
|-----------|---------------------|
| JackAndJillPrelimHeat | Up to 2: ScoreSheet(role=leader) for leaderEntries, ScoreSheet(role=follower) for followerEntries. Only created for non-empty entry lists. |
| JackAndJillFinalHeat | 1: ScoreSheet(role=couple), scored against leaderEntries. Display enriched with follower bib when paired. |
| StrictlyHeat | 1: ScoreSheet(role=couple), scored against coupleEntries. |

### Functions

- **Create draft score sheet**: Initialize all entries at DEFAULT_SCORE (500), status = `draft`. Role determines which entry list is used.
- **Update score**: Immutable update — return new sheet with specified entry's score changed.
- **Get tied entry IDs**: Group scores by value, return all entry IDs where more than one entry shares the same score.
- **Has ties**: True if any tied entry IDs exist.
- **Finalize score sheet**: Reject if ties exist (throw error). Otherwise set status = `finalized`, set `finalizedAt` to current ISO timestamp.
- **Rank entries**: Sort entries descending by score. Ties broken by display value using locale-aware numeric comparison (`localeCompare` with `{numeric: true}`).
- **Get score for entry**: Lookup by entry ID, fallback to DEFAULT_SCORE.

### Finalization Invariant

A score sheet **cannot** be finalized while any two entries share the same score. The UI must highlight tied entries and disable the finalize button until all ties are resolved.

## Risks / Trade-offs

- [_hyperscript is 0.x with a small community] -> Usage is simple (event wiring, CSS toggling). Replacing with Alpine.js or vanilla JS later is low-cost.
- [Scoring logic exists in client JS and may later also exist in server C#] -> Acceptable for v1. When server-side scoring is needed, C# becomes authoritative and client JS becomes a preview optimization.
- [localStorage limits] -> Sufficient for single-user, single-device model. Revisit when persistence moves server-side.
- [OCR quality varies by photo quality] -> Mitigated by import review step before scoring.
- [Web scraping may break on markup changes] -> Isolated in WebScraper service, easy to update per source.
- [Role detection from OCR text may be unreliable] -> Mitigated by prompting user during import review when role sections are ambiguous.
