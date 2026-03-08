## Context

The current codebase has two application projects: `Mockstar` and `Mockstar.ParserApi`. `Mockstar.ParserApi` currently contains both parser-specific code and the shared heat/scoring domain models used by parser normalization and parser-adjacent tests. That ownership is misleading because the moved types describe scoring concepts that outlive parsing and are conceptually shared across import, judging state, and scoring behavior.

The requested change is to extract those domain models into a new `Mockstar.Scoring` project while preserving existing parser API behavior, import review behavior, and client scoring behavior.

Constraints:

- Parser request/response DTOs should remain owned by `Mockstar.ParserApi`; only scoring/domain models move.
- Existing behavior and serialized shapes must remain stable unless explicitly changed later.
- The solution should keep a clear one-to-one relationship between application projects and their test projects.

## Goals / Non-Goals

**Goals:**

- Add a dedicated `Mockstar.Scoring` project that owns shared heat and scoring models.
- Remove parser-project ownership of scoring/domain types.
- Update `Mockstar.ParserApi`, `Mockstar`, and tests to reference `Mockstar.Scoring` where appropriate.
- Preserve parser API contracts and user-visible behavior.

**Non-Goals:**

- Redesign parser DTOs or the import review contract.
- Change browser state shape or scoring rules.
- Introduce additional service boundaries beyond the new shared scoring project.

## Decisions

### Move only scoring/domain models into `Mockstar.Scoring`

The extraction should be narrow: move the heat/scoring domain types and enums into a new class library, while leaving parser DTOs and parser services in `Mockstar.ParserApi`.

Rationale:

- Matches the user’s goal of separating scoring models from parser ownership.
- Avoids reintroducing the broader “shared everything” split that was just removed.
- Keeps the parser API boundary small and intentional.

Alternative considered:

- Move both scoring models and parser DTOs into `Mockstar.Scoring`. Rejected because API contracts are transport concerns, not scoring-domain concerns.

### Keep parser API DTOs in `Mockstar.ParserApi`

The request/response DTOs consumed by `Mockstar` should remain defined in the parser API project even after the shared scoring library is introduced.

Rationale:

- Preserves the current web-to-parser contract shape.
- Prevents the new scoring project from becoming a generic dumping ground for transport models.
- Keeps project responsibilities clear: scoring project for domain, parser API project for HTTP surface.

Alternative considered:

- Create separate `Mockstar.Parser.Contracts` and `Mockstar.Scoring` projects. Rejected because it adds an extra project without being required by the current request.

### Update tests to follow project ownership

Parser API tests should reference `Mockstar.ParserApi` plus `Mockstar.Scoring` transitively through project references, while web tests should continue validating the web-facing parser client behavior.

Rationale:

- Keeps the one-to-one project/test mapping intact.
- Ensures tests validate the new dependency direction after extraction.

## Risks / Trade-offs

- [The scoring project grows into a second catch-all shared library] -> Mitigate by limiting it to scoring/heat domain types and keeping parser DTOs in the API project.
- [Serialization regressions after namespace/project moves] -> Mitigate by preserving property names and running parser API plus web-client tests against the moved types.
- [Dependency confusion between client state models and shared scoring models] -> Mitigate by keeping browser-facing client DTOs in `Mockstar` and only moving domain models used for parser normalization/scoring logic.

## Migration Plan

1. Add `Mockstar.Scoring` to the solution.
2. Move scoring/domain model types from `Mockstar.ParserApi` into `Mockstar.Scoring`.
3. Update `Mockstar.ParserApi` services and tests to reference the new scoring project.
4. Update `Mockstar` references if any scoring-domain types are consumed directly.
5. Run the full test suite to confirm parser and web behavior remain unchanged.

Rollback:

- Move the domain files back into `Mockstar.ParserApi` and remove the `Mockstar.Scoring` project if the extraction causes unresolved coupling or serialization issues.

## Open Questions

- Should any current `Mockstar` client-state models eventually converge with `Mockstar.Scoring` types, or should browser-state DTOs remain permanently separate?
