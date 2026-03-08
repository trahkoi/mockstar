## Context

Mockstar currently uses Razor Pages for the page shell and some server-rendered fragments, but several UI surfaces are assembled in custom JavaScript. This mixes markup ownership across Razor, HTMX, `_hyperscript`, and imperative scripts, making it hard to tell where structure should live and increasing the risk that future UI work drifts further into browser-side rendering.

The intended direction is not to remove JavaScript entirely. HTMX remains part of the stack, browser-only features such as OCR still require JavaScript, and client-side state can remain on the device. The architectural change is narrower: HTML structure should be server-owned, while custom JavaScript should be limited to manipulating existing DOM, maintaining client-side state, and handling behavior that cannot reasonably be expressed through Razor or HTMX alone.

## Goals / Non-Goals

**Goals:**
- Establish a clear rendering boundary that developers can apply consistently across Razor pages, partials, and scripts.
- Preserve room for client-side state and immediate interaction feedback without allowing custom scripts to become the primary rendering layer.
- Make HTMX the default mechanism for introducing new server-rendered markup into an already-loaded page.
- Provide clear decision rules for future work in scoring, heat selection, and import flows.

**Non-Goals:**
- Re-architect the full judging flow in this proposal.
- Remove HTMX or browser-only JavaScript capabilities such as OCR.
- Force all interaction logic onto the server or eliminate client-side state.
- Define detailed per-page implementation plans beyond the rendering contract and its adoption guidance.

## Decisions

### Razor and partials own HTML structure
HTML structure will be defined in Razor Pages and Razor partials. Custom JavaScript may target and mutate server-rendered elements, but it will not create application UI structure via HTML strings or client-side templates.

Alternative considered:
- Allowing small HTML template literals in JavaScript for convenience. Rejected because it reintroduces split ownership and makes the boundary subjective instead of enforceable.

### HTMX is the preferred path for introducing new markup
When the UI needs to reveal or replace structural regions after initial page load, the preferred approach is to request server-rendered HTML via HTMX and swap it into a server-owned container.

Alternatives considered:
- Pre-rendering all possible markup and toggling visibility with JavaScript. Rejected as a default because it bloats pages and obscures the distinction between active and inactive UI.
- Building structural fragments in JavaScript. Rejected because it violates the rendering boundary.

### Custom JavaScript may mutate existing DOM and maintain local state
Custom JavaScript remains acceptable for:
- listening to events
- updating text and attributes on existing elements
- toggling CSS classes and disabled state
- reordering existing nodes where necessary
- maintaining client-side judging state
- browser-only capabilities such as OCR

This preserves fast local feedback while keeping markup definitions in Razor.

Alternative considered:
- Disallowing all client-side state and calculations. Rejected because the product benefits from immediate local interactions, especially around scoring.

### Business-critical protections must not rely only on client-side enforcement
Client-side logic may provide instant feedback, but business-critical guarantees such as persistence and finalization validity must remain enforceable outside the browser rendering layer.

Alternative considered:
- Treating client-side logic as sufficient enforcement because state is kept locally. Rejected because it couples correctness to one execution path and makes future persistence or synchronization harder.

## Risks / Trade-offs

- [Boundary drift through convenience exceptions] → Mitigation: encode hard rules such as no HTML strings in custom JavaScript and review against those rules.
- [Too much markup rendered up front to avoid HTMX] → Mitigation: prefer HTMX partial swaps when new structure is needed instead of pre-rendering hidden UI by default.
- [Client-side state remains complex even without client-side markup rendering] → Mitigation: keep state modules focused on data and behavior, not DOM generation.
- [Different developers interpret “minimal JavaScript” differently] → Mitigation: define explicit allowed and disallowed patterns in the spec and tasks.
- [Some interactions may feel slower if over-shifted to HTMX] → Mitigation: allow small local DOM updates for immediate feedback where the structure already exists.

## Migration Plan

1. Define the rendering contract in OpenSpec so future implementation work has a concrete standard.
2. Audit current custom scripts against the contract and identify violations such as HTML string generation and `innerHTML`-driven rendering.
3. Refactor affected flows incrementally so Razor/partials own structure while scripts become enhancement-only.
4. Preserve browser-only features and fast client-side interactions where they fit the contract.

Rollback is low-risk because this proposal is architectural guidance; individual implementation changes can be reverted per flow if they prove too restrictive.

## Open Questions

- Should `_hyperscript` be treated the same as custom JavaScript for markup ownership, or is it acceptable for limited declarative class/attribute toggling?
- Which current flows should be migrated first: heat selection, scoring, or import?
- How much client-side DOM reordering is acceptable before it starts to function as a rendering layer in practice?
