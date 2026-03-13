## Why

Parsed heats currently exist only in browser memory. If the browser is closed or refreshed, all parsed heat data is lost. Users need persistence so they can resume scoring sessions and access historical heat data.

## What Changes

- Add SQLite-based persistence for parsed heats and event records
- Create a repository pattern for heat storage and retrieval
- Expose API endpoints to save/load heats
- Wire persistence into the existing parser → normalizer flow

## Capabilities

### New Capabilities
- `heat-persistence`: Storage, retrieval, and management of parsed heats using SQLite with repository pattern

### Modified Capabilities
- `parser-api`: Add save/load endpoints that integrate with persistence layer

## Impact

- New SQLite database file will be created (local storage)
- New NuGet dependencies: Microsoft.EntityFrameworkCore.Sqlite
- `Mockstar.ParserApi` gains persistence endpoints
- Existing parse flow unchanged; persistence is additive
