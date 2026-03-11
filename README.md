# Mockstar

Mockstar is a .NET 10 workspace for importing and working with dance competition roster data. The repo contains a Razor Pages web app, a parser API for turning roster text or URLs into structured data, and a shared scoring domain library.

## Projects

- `src/Mockstar.Web` - local UI for importing rosters
- `src/Mockstar.ParserApi` - HTTP API for roster parsing
- `src/Mockstar.Scoring` - shared competition and scoring models

## Run locally

Prerequisite: .NET 10 SDK installed.

1. Restore dependencies:

```bash
dotnet restore Mockstar.slnx
```

2. Start the parser API:

```bash
dotnet run --project src/Mockstar.ParserApi
```

3. In a second terminal, start the web app:

```bash
dotnet run --project src/Mockstar.Web
```

4. Open `http://localhost:5000`.

The web app is configured to call the parser API at `http://localhost:5100`, which matches the default development launch settings in this repo.
