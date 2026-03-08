## Why

Mockstar currently mixes server-rendered Razor Pages with custom JavaScript that builds significant portions of the interface in the browser. That makes the UI harder to reason about and pushes rendering structure into script files when the intended direction is Razor-first rendering with HTMX and only minimal custom JavaScript.

## What Changes

- Establish a frontend rendering contract that makes Razor Pages and Razor partials the owners of HTML structure.
- Allow HTMX for partial loading and server-rendered fragment swaps.
- Restrict custom JavaScript to state management, event handling, and mutation of existing server-rendered DOM.
- Prohibit custom JavaScript from containing HTML templates or acting as the primary UI rendering layer.
- Define practical decision rules for when behavior belongs in Razor, HTMX, or minimal JavaScript.
- Identify current areas, especially heat selection and scoring, that must be aligned to the new contract during future implementation work.

## Capabilities

### New Capabilities
- `server-first-rendering-contract`: Defines the architectural boundary for frontend rendering, including Razor ownership of markup, HTMX usage, and limits on custom JavaScript DOM rendering.

### Modified Capabilities

## Impact

- Affects `src/Mockstar.Web/Pages/` page and partial ownership patterns.
- Affects `src/Mockstar.Web/wwwroot/js/` by constraining how custom scripts interact with the DOM.
- Influences future changes to heat selection, scoring, and import flows without requiring those implementations in this proposal.
- Reinforces HTMX as the preferred mechanism for introducing new markup from the server.
