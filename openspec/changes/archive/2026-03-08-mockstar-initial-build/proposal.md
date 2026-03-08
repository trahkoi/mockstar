## Why

WSDC shadow judging at live events currently relies on ad hoc notes and memory. A mobile-first web app is needed to import live heat rosters quickly, score dancers in bib order with minimal interaction cost, and keep judging state on the client. The app should be built on ASP.NET Core with HTMX and _hyperscript to establish a foundation for future server-side capabilities like result processing, analysis, and persistent storage.

## What

- Build an ASP.NET Core (.NET 10) web app with Razor Pages serving server-rendered HTML.
- Use HTMX for partial page updates (heat selection, import flow, navigation).
- Use _hyperscript for inline UI behavior (slider wiring, event handling, UI toggles).
- Use Tesseract.js (bundled) running client-side in the browser for image-based OCR import.
- Use AngleSharp server-side for web page scraping.
- Include a small client-side JS sprinkle (~15 lines) for rank computation and tie detection on the scoring screen.
- Build a C# roster text parser using regex patterns to extract heats and entries from OCR/scraped text.
- Keep the server stateless — no database, no server-side persistence. State lives in browser localStorage.
- Deploy to Azure App Service.

## Capabilities

### New Capabilities

- `live-heat-scoring`: Score live heats with role-aware scoring — leaders and followers scored separately in J&J prelims/semis, couples scored as a unit in J&J finals and strictly. Uses raw-score sliders (0-1000), client-side rank assignment, tie highlighting, and finalization gating.
- `roster-import`: Import heat rosters from images (client-side Tesseract.js OCR) or event web pages (server-side AngleSharp scraping). Detects leader/follower role sections and normalizes into typed heats (JackAndJillPrelimHeat, JackAndJillFinalHeat, StrictlyHeat).
- `client-judging-state`: Persist active judging data in browser localStorage so scoring survives page refreshes.

## Impact

- Greenfield ASP.NET Core application.
- Domain logic for roster parsing and normalization implemented in C#.
- Scoring rank/tie computation implemented as minimal client-side vanilla JS.
- Establishes core domain handling for WSDC Jack and Jill and Strictly division formats.

## Domain Context

The app supports two WSDC competition formats with three distinct heat types:

- **Jack and Jill prelims/semis**: Leaders and followers are scored independently. The import can contain both roles or just one.
- **Jack and Jill finals**: Couples are scored as a unit, identified by leader bib. Follower bibs can be paired at any point — before, during, or after scoring.
- **Strictly Swing**: Couples (leader bib / follower bib) are scored as a unit in all rounds.

Scoring uses a 0-1000 raw score slider per entry, displayed in fixed bib order. Ranks are derived from scores. Ties must be resolved before a heat can be finalized — no automatic tie-breaking.

## Resolved Questions

- **Target framework**: .NET 10.
- **Tesseract.js delivery**: Bundled as a static asset.
- **Client persistence**: localStorage. Fits naturally with the stateless server model.
- **Heat type model**: Discriminated union — JackAndJillPrelimHeat, JackAndJillFinalHeat, StrictlyHeat — rather than a single Heat with optional fields.
- **Leader/follower scoring**: Separate score sheets per role in J&J prelims/semis. Single couple score sheet in finals and strictly.
- **J&J final pairing**: Leader and follower bibs joinable at any point, not required upfront.
