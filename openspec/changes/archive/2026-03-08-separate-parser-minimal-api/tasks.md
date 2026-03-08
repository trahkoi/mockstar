## 1. Solution Setup

- [x] 1.1 Add a new `Mockstar.ParserApi` minimal API project to the solution and configure local launch/settings for it.
- [x] 1.2 Define the parser request/response DTOs inside `Mockstar.ParserApi` and reference that API project from Mockstar.
- [x] 1.3 Move parser-specific package references and configuration from the main web app into the parser API where applicable.

## 2. Parser API Implementation

- [x] 2.1 Move or extract roster parsing, normalization, and web scraping logic behind the parser API host.
- [x] 2.2 Implement a text-parse endpoint that validates input and returns parsed and normalized import data.
- [x] 2.3 Implement a URL-parse endpoint that validates absolute URLs, fetches the source page, and returns the same response contract.
- [x] 2.4 Implement machine-readable error responses for validation, parser failures, and upstream fetch failures.

## 3. Mockstar Integration

- [x] 3.1 Add a typed parser API client in Mockstar with configurable base URL, timeout, and error mapping.
- [x] 3.2 Update the import page handlers to use the parser API client for both OCR text and URL imports.
- [x] 3.3 Remove obsolete in-process parser service registrations from the main app after the new integration is wired.

## 4. Verification

- [x] 4.1 Add or update tests covering parser API text parsing, URL parsing, and error responses.
- [x] 4.2 Add or update Mockstar integration tests for import review success and failure paths through the parser API client.
- [x] 4.3 Verify the end-to-end import review flow still produces the same activation payload structure for downstream scoring state.
