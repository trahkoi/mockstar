## Why

`Mockstar.ParserApi` currently owns the heat and scoring domain models even though those types represent scoring state that is broader than parsing alone. Extracting them into a dedicated `Mockstar.Scoring` project makes ownership clearer and gives both the web app and parser API a stable shared scoring model without implying that parsing owns the domain.

## What Changes

- Add a new `Mockstar.Scoring` project for shared scoring and heat domain models.
- Move the current scoring/domain model types out of `Mockstar.ParserApi` into `Mockstar.Scoring`.
- Update `Mockstar.ParserApi`, `Mockstar`, and relevant tests to reference `Mockstar.Scoring` instead of the moved in-project domain namespace.
- Preserve existing parser API behavior, import behavior, and client scoring behavior while changing only project boundaries and dependencies.

## Capabilities

### New Capabilities
- `scoring-project`: Provide a dedicated project that owns shared scoring and heat domain models used by parsing and scoring features.

### Modified Capabilities

## Impact

- Affected code: `src/Mockstar.ParserApi/Domain`, parser services, solution/project references, and tests that currently import parser-owned domain types.
- Dependencies: `Mockstar.ParserApi` and `Mockstar` gain a reference to `Mockstar.Scoring`; domain ownership moves out of the parser API project.
- Systems: parser API and web app continue working as before, but the shared scoring model becomes reusable outside parser concerns.
