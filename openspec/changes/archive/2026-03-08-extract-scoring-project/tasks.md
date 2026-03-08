## 1. Project Setup

- [x] 1.1 Add a new `Mockstar.Scoring` project to the solution and wire project references from `Mockstar.ParserApi` and any direct consumers.
- [x] 1.2 Move the shared heat and scoring domain model files from `Mockstar.ParserApi` into `Mockstar.Scoring`.

## 2. Dependency Migration

- [x] 2.1 Update parser services and namespaces to use the scoring models from `Mockstar.Scoring`.
- [x] 2.2 Keep parser request/response DTOs in `Mockstar.ParserApi` and ensure the web app still references only the parser API contract surface it needs.
- [x] 2.3 Update tests to reference the new scoring project layout without breaking the one-to-one app/test project mapping.

## 3. Verification

- [x] 3.1 Run parser API tests to confirm parser behavior and response contracts remain unchanged after the extraction.
- [x] 3.2 Run web app tests to confirm parser client behavior and activation payload expectations still hold after the extraction.
