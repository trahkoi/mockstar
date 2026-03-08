## 1. Project Scaffold

- [x] 1.1 Create ASP.NET Core (.NET 10) web project with Razor Pages
- [x] 1.2 Add HTMX and _hyperscript as bundled static assets in wwwroot
- [x] 1.3 Bundle Tesseract.js as a static asset in wwwroot for client-side OCR
- [x] 1.4 Create shared layout with mobile-first responsive structure
- [x] 1.5 Set up feature-organized page structure (Scoring/, Import/, Heats/) with placeholder pages

## 2. Domain Model and Parsing

- [x] 2.1 Define C# domain types: enums (DivisionKind, RoundPhase, ScoringRole, HeatStatus, ImportSourceKind), entry types (BibEntry, CoupleEntry, Pairing), heat types as discriminated union (JackAndJillPrelimHeat with leaderEntries/followerEntries, JackAndJillFinalHeat with leaderEntries/followerEntries/pairings, StrictlyHeat with coupleEntries), ScoreSheet with role field, EventRecord, ImportSource. See design.md Domain Model section for complete field definitions.
- [x] 2.2 Implement RosterParser service in C#: regex-based text parsing using the three patterns defined in design.md (HEAT_SPLIT, COUPLE_PATTERN, BIB_PATTERN). Includes division kind detection ("strict" → strictly-swing), phase detection (quarter/semi/final/prelim), role section detection (leader/follower headers), division name extraction, and the full parsing flow (normalize line endings → detect metadata → determine heat type → split on heat headers → extract entries into role-specific lists → fallback to single heat).
- [x] 2.3 Implement RosterNormalizer service in C#: heat type inference (divisionKind + phase → JackAndJillPrelimHeat/JackAndJillFinalHeat/StrictlyHeat), entry construction with ID formats (bib-{n}, couple-{l}-{f}), event/heat ID generation with slugified names and timestamps.
- [x] 2.4 Implement WebScraper service: fetch URL with HttpClient, extract text content with AngleSharp
- [x] 2.5 Add unit tests for roster parsing across formats: strictly-swing couples (123/456 pattern), jack-and-jill leader/follower role detection, J&J finals with leader bibs, multi-heat splitting, single-heat fallback, division/phase detection edge cases, ambiguous role handling

## 3. Import Flow

- [x] 3.1 Build Import Razor Page with image/web tab switching via _hyperscript
- [x] 3.2 Build web import endpoint: POST URL → WebScraper → RosterParser → RosterNormalizer → return HTML fragment via HTMX
- [x] 3.3 Build image import client-side flow: file input → Tesseract.js OCR in browser → POST extracted text → RosterParser → RosterNormalizer → return HTML fragment
- [x] 3.4 Build import review/confirmation UI showing parsed heats and entries before they become active. Prompt user to assign roles when role detection is ambiguous.

## 4. Heat List and Scoring

- [x] 4.1 Build Heats Razor Page showing imported events and heats with HTMX-driven heat selection
- [x] 4.2 Build Scoring Razor Page with bib-ordered slider rows (0-1000, default 500) and _hyperscript for slider event wiring. Show role context (leader/follower/couple) for the active score sheet.
- [x] 4.3 Add client-side vanilla JS (~15 lines) for rank assignment (descending by score, ties broken by display with numeric locale compare) and tie detection (group by score value, flag entries sharing a score)
- [x] 4.4 Add _hyperscript tie highlighting (CSS class toggling on duplicate scores) and finalize button gating (disabled while ties exist)
- [x] 4.5 Support heat-type-specific entry presentation: leader bibs and follower bibs separately (J&J prelim/semi), leader bib with optional paired follower bib (J&J final), couple entries with leader/follower (strictly)
- [x] 4.6 Support role switching within a J&J prelim/semi heat — navigate between leader and follower score sheets for the same heat

## 5. J&J Final Pairing

- [x] 5.1 Build pairing UI for J&J final heats: allow linking a leader bib to a follower bib at any point (before, during, or after scoring)
- [x] 5.2 Update scoring display to show paired follower bib alongside leader bib when a pairing exists

## 6. Client-Side Persistence

- [x] 6.1 Implement localStorage read/write under key `mockstar-state` for the full state shape: `{eventRecords, scoreSheets, selectedHeatId, lastImportedHeatIds}`
- [x] 6.2 Wire state restoration on page load so scoring survives refresh. Initialize empty state as `{eventRecords: [], scoreSheets: [], lastImportedHeatIds: []}` when no stored data exists.
- [x] 6.3 Add graceful degradation when localStorage is unavailable (warning banner, in-memory fallback)

## 7. Deployment

- [x] 7.1 Configure Azure App Service deployment for the ASP.NET Core application
- [x] 7.2 Verify end-to-end feature parity: image import with OCR, web import with scraping, heat listing, role-aware scoring with rank/tie-detect, finalization gating, J&J final pairing, localStorage persistence across refresh
