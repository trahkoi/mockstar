## 1. Database Setup

- [x] 1.1 Add Microsoft.EntityFrameworkCore.Sqlite NuGet package to Mockstar.ParserApi
- [x] 1.2 Create Persistence/ folder structure in Mockstar.ParserApi
- [x] 1.3 Create HeatDbContext in Persistence/
- [x] 1.4 Create entity classes in Persistence/Entities/ (HeatEntity, EventEntity)
- [x] 1.5 Configure EF Core relationships (Event → Heats)
- [x] 1.6 Create initial migration

## 2. Repository Layer

- [x] 2.1 Create IHeatRepository interface in Persistence/
- [x] 2.2 Implement HeatRepository in Persistence/
- [x] 2.3 Create mappers in Persistence/Mapping/ (domain ↔ entity)
- [x] 2.4 Register repository in DI container

## 3. API Endpoints

- [x] 3.1 Add POST /api/heats/{eventId} endpoint (save heats)
- [x] 3.2 Add GET /api/heats/{eventId} endpoint (load heats)
- [x] 3.3 Add DELETE /api/heats/{eventId} endpoint (delete heats)
- [x] 3.4 Add GET /api/heats endpoint (list stored event IDs)

## 4. Integration

- [x] 4.1 Configure SQLite database path in appsettings
- [x] 4.2 Apply migrations on startup
- [x] 4.3 Add integration tests for repository
- [x] 4.4 Add API endpoint tests
