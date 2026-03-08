## 1. Define and document the rendering boundary

- [x] 1.1 Review current Razor, HTMX, and custom JavaScript responsibilities in `src/Mockstar.Web`
- [x] 1.2 Document the rendering contract in code-facing guidance derived from this OpenSpec change
- [x] 1.3 Identify custom JavaScript patterns that violate the contract, including HTML string rendering and `innerHTML`-driven UI assembly

## 2. Align page flows to server-owned markup

- [x] 2.1 Refactor heat selection so Razor and HTMX own list and detail markup structure
- [x] 2.2 Refactor scoring views so Razor owns score row and control markup while JavaScript only mutates existing elements
- [x] 2.3 Refactor import interactions so browser-only JavaScript targets server-rendered containers instead of constructing structural UI

## 3. Enforce and verify the contract

- [x] 3.1 Update custom scripts to remove application HTML templates and equivalent string-based UI rendering
- [x] 3.2 Verify HTMX is used when new structural markup must appear after page load
- [x] 3.3 Run the relevant test suites and manual UI checks to confirm existing behavior still works under the new rendering boundary
