# roster-import

Import heat rosters from images (client-side Tesseract.js OCR) or event web pages (server-side AngleSharp scraping) and normalize them into heat-by-heat judging inputs.

## Import Paths

```
Image import:  Browser → Tesseract.js → POST text → Server parses → HTML fragment
Web import:    Browser → POST URL → Server fetches + AngleSharp + parses → HTML fragment
```

- **Image import**: Client-side OCR via Tesseract.js (bundled static asset). Browser extracts text, POSTs it to the server for parsing.
- **Web import**: Server-side scraping via AngleSharp (CORS requires server fetch). Server fetches page, scrapes, parses, returns HTML fragment.

## Heat Types

The parser produces one of three heat types based on division kind and phase:

```
Heat (base)
  ├── id, name, phase, divisionKind

JackAndJillPrelimHeat : Heat
  ├── leaderEntries: BibEntry[]
  └── followerEntries: BibEntry[]

JackAndJillFinalHeat : Heat
  ├── leaderEntries: BibEntry[]
  ├── followerEntries: BibEntry[]      ← may be empty, joinable later
  └── pairings: Pairing[]             ← populated when known

StrictlyHeat : Heat
  └── coupleEntries: CoupleEntry[]
```

### Entry and Pairing Types

| Type | Fields | Display |
|------|--------|---------|
| BibEntry | id, bib | `"{bib}"` |
| CoupleEntry | id, leaderBib, followerBib? | `"{leader}/{follower}"` or `"{leader}"` |
| Pairing | leaderBib, followerBib | Links a leader to a follower in J&J finals |

### ID Formats

| Type | ID format |
|------|-----------|
| BibEntry | `bib-{bib}` |
| CoupleEntry | `couple-{leader}-{follower\|'solo'}` |

### Heat Type Inference

| Division Kind | Phase | Heat Type |
|--------------|-------|-----------|
| `jack-and-jill` | prelim, quarterfinal, semifinal | JackAndJillPrelimHeat |
| `jack-and-jill` | final | JackAndJillFinalHeat |
| `strictly-swing` | any | StrictlyHeat |

## Role Detection

The source text (OCR or scraped) can contain entries for both roles, or just one. The parser detects role sections:

- Text containing "leader" headers → entries go to `leaderEntries`
- Text containing "follower" / "follow" headers → entries go to `followerEntries`
- Both present → both lists populated
- Neither detected → ambiguous, prompt user during import review

For strictly-swing, role detection does not apply — entries are couples.

## Roster Parsing

### Regex Patterns

| Pattern | Regex | Purpose |
|---------|-------|---------|
| HEAT_SPLIT | `/(?:^|\n)\s*(Heat\s+\d+[:\s-]*)/gi` | Split text on "Heat N" headers |
| COUPLE_PATTERN | `/(\d{1,4})\s*(?:[+/&-]|and)\s*(\d{1,4})/gi` | Match couple pairs: "123/456", "123+456", "123&456", "123-456", "123 and 456" |
| BIB_PATTERN | `/\b\d{1,4}\b/g` | Match individual bib numbers (1-4 digits) |

### Detection Rules

- **Division kind**: Text contains "strict" (case-insensitive) → `strictly-swing`, else → `jack-and-jill`.
- **Phase**: Test in order: "quarter" → `quarterfinal`, "semi" → `semifinal`, "final" → `final`, else → `prelim`.
- **Division name**: Match `/(novice|newcomer|intermediate|advanced|all-star|champion|masters)[^\n]*/i`. Fallback: `"Imported Division"`.
- **Event name**: First non-empty line of the input text.

### Parsing Flow

1. Normalize line endings (`\r` → `\n`), trim whitespace.
2. Detect divisionKind, phase, divisionName, eventName from full text.
3. Determine heat type from divisionKind + phase.
4. Split text using HEAT_SPLIT regex, iterate in pairs (heatLabel, heatBody).
5. For each heat:
   - **StrictlyHeat**: Extract couples with COUPLE_PATTERN → `coupleEntries`.
   - **JackAndJillPrelimHeat / JackAndJillFinalHeat**: Detect role sections. Extract bibs with BIB_PATTERN into `leaderEntries` and/or `followerEntries`. Deduplicate with Set.
6. If no heats found from splitting, try treating entire text as a single "Heat 1".
7. Throw error if still no entries found.

### ID Generation

- Event base ID: `{slugify(eventName)}-{timestamp}` where slugify = lowercase, replace non-alphanumeric with `-`, trim leading/trailing dashes.
- Heat IDs: `{baseId}-heat-{index+1}` (1-based).

## Pairing (J&J Finals)

In J&J finals, leader and follower bibs can be joined at any point — before, during, or after scoring:

- `leaderEntries` is always present (used for live scoring identification).
- `followerEntries` may arrive later via a separate import or manual entry.
- `pairings` links a leader bib to a follower bib. Can be populated incrementally.

## Import Review

Parsed heats and entries are shown in a review/confirmation UI before becoming active judging inputs. For J&J heats where role detection is ambiguous, the user is prompted to assign roles.
