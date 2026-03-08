# Frontend Rendering Contract

This document applies the OpenSpec change `define-server-first-rendering-contract` to the current `src/Mockstar.Web` implementation.

## Rendering Boundary

- Razor Pages and Razor partials own application HTML structure.
- HTMX is the default way to load structural markup after the initial page response.
- Custom JavaScript may read browser state, listen to events, update existing text and attributes, toggle classes, and call browser-only APIs such as OCR.
- Custom JavaScript must not build application UI with HTML strings, `innerHTML`, `insertAdjacentHTML`, or equivalent client-side templates.
- Business-critical rules such as score finalization validity remain enforceable outside DOM mutation code.

## Current Responsibilities In `src/Mockstar.Web`

- `Pages/Import/*`: Razor owns the import page shell and server review fragment; browser JavaScript performs OCR and submits extracted text for server-rendered review output.
- `Pages/Heats/*`: Razor owns the heat page shell plus list and detail partials; browser JavaScript reads local judging state and uses HTMX to request those fragments.
- `Pages/Scoring/*`: Razor owns the scoring shell, role controls, pairing controls, and score rows; browser JavaScript updates existing controls from local state and requests a fresh shell when structural markup must change.
- `wwwroot/js/mockstar-state.js`: owns local judging state transitions and scoring calculations, but not structural HTML generation.

## Violations Found During Audit

- `wwwroot/js/heats.js` previously assembled the heat list with template literals and `innerHTML`.
- `wwwroot/js/scoring.js` previously assembled the scoring header, role switcher, pairing panel, and score rows with template literals and `innerHTML`.
- `wwwroot/js/import.js` previously wrote the server response into `#import-result` with `innerHTML` after a manual `fetch`.

## Decision Rules

- If a user action needs new markup that is not already in the DOM, return Razor-rendered HTML and swap it with HTMX.
- If the markup already exists, JavaScript may update values, classes, disabled state, or ordering without asking the server for a new fragment.
- If a feature depends on browser-only capabilities, keep the capability in JavaScript but target server-rendered containers.
