## Context

Mockstar currently runs roster scraping, roster parsing, normalization, and the Razor Pages UI inside a single web project. The import page depends on in-process services (`WebScraper`, `RosterParser`, `RosterNormalizer`) to turn either OCR text or a roster URL into review data.

This makes the parser difficult to reuse independently and forces deployment of the full UI host even when only parsing behavior is needed. The requested change is to split parser responsibilities into a separate minimal API project without changing the user-visible import review flow.

Constraints:

- The import page must continue supporting both text-based and URL-based imports.
- Existing parsing behavior, normalization rules, and error messages should remain stable where possible.
- The API contract must carry enough information for Mockstar to render import review and activate the resulting roster state.
- The final project layout should stay simple: one web project and one parser API project.

## Goals / Non-Goals

**Goals:**

- Introduce a dedicated minimal API host for scraping and parsing roster imports.
- Preserve the current import review experience in the main Mockstar app.
- Define stable request/response DTOs that isolate the UI from parser implementation details.
- Keep parser deployment independent from the Razor Pages host.
- Keep the codebase to two application projects: `Mockstar` and `Mockstar.ParserApi`.

**Non-Goals:**

- Redesign the roster parsing heuristics or import review UX.
- Replace the existing domain model used by the rest of Mockstar scoring flows.
- Add authentication, rate limiting, or cross-service orchestration beyond the minimum needed for local app-to-app communication.

## Decisions

### Create a dedicated `Mockstar.ParserApi` minimal API project

The parser boundary should be explicit and deployable on its own, so the scraping and parsing entry points move into a new ASP.NET Core minimal API project.

Rationale:

- Keeps ingestion concerns out of the Razor Pages host.
- Lets the parser run as a standalone service in development or deployment.
- Fits the requested “separate minimal API project” shape directly.

Alternative considered:

- Extract only a shared class library and keep parsing in-process. Rejected because it improves reuse but does not create the independent API boundary the change calls for.

### Keep parser implementation behind HTTP DTO contracts

The API should expose request/response DTOs for two operations: parse supplied text and fetch-then-parse a URL. The response should include parsed roster details plus normalized output needed for the review UI. Those DTOs live in the `Mockstar.ParserApi` assembly and are referenced directly by `Mockstar`; no extra shared contracts project is introduced.

Rationale:

- Avoids coupling the UI to parser service classes.
- Keeps the main app focused on transport and presentation concerns.
- Allows parser internals to evolve without forcing the UI to understand scraping or parsing mechanics.
- Keeps the project graph aligned with the desired two-project layout.

Alternative considered:

- Create a separate shared contracts or core library. Rejected because it adds extra projects without improving the required deployment boundary.

### Move scraping dependency into the parser API

`AngleSharp` and the current `WebScraper` behavior belong in the parser service because URL imports are part of ingestion, not UI rendering.

Rationale:

- Keeps CORS-driven server fetch behavior in one place.
- Avoids the main app retaining HTML fetching logic after the split.
- Produces one consistent parsing path for both URL and text imports.

Alternative considered:

- Keep URL fetching in Mockstar and only remote the text parser. Rejected because it would preserve the current cross-cutting responsibility split.

### Use an outbound parser client in Mockstar import pages

The import page should depend on a typed HTTP client or thin service wrapper that calls the parser API and maps failures to the same partial-view error flow used today.

Rationale:

- Limits changes to the existing page model.
- Centralizes retry, timeout, and base-URL configuration.
- Keeps the page model free of low-level HTTP concerns.

Alternative considered:

- Call `HttpClient` directly from the page model. Rejected because it spreads integration details into UI handlers and makes testing harder.

## Risks / Trade-offs

- [Extra network hop between UI and parser] -> Mitigate with a local configured base URL, short timeouts, and a narrow payload.
- [Direct reference from web app to API assembly exposes more than the contract DTOs] -> Mitigate by keeping the consumed namespace small and limiting Mockstar usage to API request/response types plus the typed client.
- [Error wording changes after transport is introduced] -> Mitigate by preserving current parser validation messages and passing API problem details through the import client.
- [Deployment complexity increases from one host to two] -> Mitigate with clear local configuration, solution wiring, and health/startup documentation.

## Migration Plan

1. Add the parser API project and shared contracts to the solution.
2. Move scraping and parser-facing application logic into the parser API project behind API endpoints.
3. Update Mockstar import handlers to call the parser client instead of in-process services.
4. Keep the existing review partial and activation payload generation working against the API response.
5. Remove obsolete parser registrations from the main app once the end-to-end flow passes tests.

Rollback:

- Re-enable the existing in-process services and revert the import page to the current service registrations if the parser API integration blocks import flows.

## Open Questions

- Will the parser API be run only as an internal local dependency, or should the contract anticipate future remote deployment concerns such as auth and rate limiting?
