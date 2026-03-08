## Why

The current web app hosts UI, scraping, and roster parsing in one ASP.NET project, which makes the parser hard to reuse independently and couples import behavior to the Razor Pages host. Separating the parser into a minimal API project creates a smaller deployable boundary for ingestion while keeping the main app focused on review and scoring workflows.

## What Changes

- Add a dedicated minimal API project that exposes roster parsing as an HTTP endpoint.
- Move parser-related logic and its web-page scraping dependency into that API project.
- Update the main Mockstar app to call the parser API for web and text imports instead of invoking parser services in-process.
- Keep the cross-project boundary small by exposing only parser API request/response DTOs from the API project to the web app.

## Capabilities

### New Capabilities
- `parser-api`: Provide a minimal HTTP API for scraping and parsing roster input into import-ready data.

### Modified Capabilities
- `roster-import`: Change roster import to use the parser API as its parsing backend while preserving the review and activation experience.

## Impact

- Affected code: `src/Mockstar/Pages/Import`, parser services moved into `src/Mockstar.ParserApi`, solution/project layout, and related tests.
- New API surface: parser endpoint(s) and DTO contracts between Mockstar and the parser service.
- Dependencies: AngleSharp and parsing dependencies live in the new API host; the main app adds an HTTP client for parser calls.
- Operational impact: parser logic can be deployed, versioned, and tested separately from the main UI application.
