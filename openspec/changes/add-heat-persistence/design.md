## Context

Mockstar currently parses heats from roster text via ParserApi, normalizes them, and returns the data to the browser. All state lives client-side. If the browser closes, data is lost. Users need server-side persistence to resume sessions and access historical data.

Current architecture: `RosterParser` → `RosterNormalizer` → `ParserImportResponseFactory` → JSON response. Domain models (`Heat`, `EventRecord`) live in `Mockstar.Scoring`. No database or storage layer exists.

## Goals / Non-Goals

**Goals:**
- Persist parsed heats and event records to SQLite
- Retrieve heats by event ID
- Delete heats when no longer needed
- Keep persistence layer decoupled from parser logic

**Non-Goals:**
- User authentication or multi-tenancy
- Cloud database (SQLite is sufficient for local use)
- Real-time sync between clients
- Migrating existing browser-stored data

## Decisions

### Decision 1: SQLite with EF Core
**Choice**: Use SQLite via Entity Framework Core
**Rationale**: Single-file database, no server setup, EF Core provides migrations and clean repository pattern. Alternatives considered:
- LiteDB: Simpler but less ecosystem support
- PostgreSQL: Overkill for local-first app

### Decision 2: Repository in Mockstar.ParserApi (extraction-ready)
**Choice**: Add `IHeatRepository` interface and `HeatRepository` implementation in ParserApi project, organized in a `Persistence/` folder for easy future extraction.
**Rationale**: Keeps everything in one project for simplicity. Interface + folder structure enables clean extraction to `Mockstar.Persistence` later. Alternatives considered:
- Mockstar.Scoring: Would couple domain models to EF Core dependencies
- New Mockstar.Persistence project: Premature; adds complexity now

### Decision 3: Store normalized domain models
**Choice**: Persist `Heat` and `EventRecord` domain models, not raw parsed data
**Rationale**: Domain models are stable contract; parsed data is intermediate. Users want to resume from normalized state.

### Decision 4: Event-scoped storage
**Choice**: Store heats grouped by `EventRecord.Id` (the slugified event identifier)
**Rationale**: Natural grouping; supports "load all heats for event X" queries.

### Decision 5: Extraction-ready folder structure
**Choice**: Organize persistence code in `Mockstar.ParserApi/Persistence/` with subfolders:
```
Persistence/
├── IHeatRepository.cs      # Interface (move to Scoring later if needed)
├── HeatRepository.cs       # Implementation
├── HeatDbContext.cs        # EF Core context
├── Entities/               # EF entity classes
│   ├── HeatEntity.cs
│   └── EventEntity.cs
└── Mapping/                # Domain ↔ Entity mappers
```
**Rationale**: Single folder = single extraction target. Interface segregation enables swapping implementations.

## Risks / Trade-offs

- [SQLite concurrency] → Single writer; acceptable for local tool. Document limitation.
- [Schema migrations] → EF Core migrations handle this; include in setup docs.
- [Data growth] → Add cleanup endpoint; users can delete old events.
- [Future extraction] → Mitigated by folder structure and interface segregation.
