using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mockstar.ParserApi.Contracts;
using Mockstar.ParserApi.Persistence;
using Mockstar.ParserApi.Persistence.Mapping;
using Mockstar.ParserApi.Services.Rosters;
using Mockstar.ParserApi.Services;

namespace Mockstar.ParserApi;

public sealed class Program
{
    public static void Main(string[] args)
    {
        CreateApp(args).Run();
    }

    internal static WebApplication CreateApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpClient();
        builder.Services.AddScoped<RosterParser>();
        builder.Services.AddScoped<RosterNormalizer>();
        builder.Services.AddScoped<WebScraper>();
        builder.Services.AddScoped<ParserImportService>();

        // Persistence
        var connectionString = builder.Configuration.GetConnectionString("HeatDb")
            ?? "Data Source=mockstar.db";
        builder.Services.AddDbContext<HeatDbContext>(options =>
            options.UseSqlite(connectionString));
        builder.Services.AddScoped<IHeatRepository, HeatRepository>();

        var app = builder.Build();

        // Apply migrations on startup
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<HeatDbContext>();
            dbContext.Database.Migrate();
        }

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        app.MapPost("/api/parser/text", ParseTextAsync);
        app.MapPost("/api/parser/url", ParseUrlAsync);

        // Heat storage endpoints
        app.MapPost("/api/heats/{eventId}", SaveHeatsAsync);
        app.MapGet("/api/heats/{eventId}", LoadHeatsAsync);
        app.MapDelete("/api/heats/{eventId}", DeleteHeatsAsync);
        app.MapGet("/api/heats", ListEventsAsync);

        return app;
    }

    private static async Task<IResult> ParseTextAsync(
        ParseRosterTextRequest request,
        ParserImportService parserImportService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return CreateProblem(StatusCodes.Status400BadRequest, "validation_failed", "Roster text is required.");
        }

        try
        {
            var response = await parserImportService.ParseTextAsync(request.Text, cancellationToken);
            return Results.Ok(response);
        }
        catch (ArgumentException exception)
        {
            return CreateProblem(StatusCodes.Status400BadRequest, "validation_failed", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return CreateProblem(StatusCodes.Status400BadRequest, "parse_failed", exception.Message);
        }
    }

    private static async Task<IResult> ParseUrlAsync(
        ParseRosterUrlRequest request,
        ParserImportService parserImportService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return CreateProblem(StatusCodes.Status400BadRequest, "validation_failed", "A valid absolute URL is required.");
        }

        try
        {
            var response = await parserImportService.ParseUrlAsync(request.Url, cancellationToken);
            return Results.Ok(response);
        }
        catch (ArgumentException exception)
        {
            return CreateProblem(StatusCodes.Status400BadRequest, "validation_failed", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return CreateProblem(StatusCodes.Status400BadRequest, "parse_failed", exception.Message);
        }
        catch (HttpRequestException exception)
        {
            return CreateProblem(StatusCodes.Status502BadGateway, "upstream_fetch_failed", exception.Message);
        }
    }

    private static IResult CreateProblem(int statusCode, string errorCode, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = "Parser request failed",
            Detail = detail
        };
        problem.Extensions["errorCode"] = errorCode;
        return Results.Problem(problem);
    }

    private static async Task<IResult> SaveHeatsAsync(
        string eventId,
        SaveHeatsRequest request,
        IHeatRepository repository,
        CancellationToken cancellationToken)
    {
        var domainEvent = ContractMapper.ToDomain(request.EventRecord);
        // Ensure the event ID from the route matches
        var eventToSave = domainEvent with { Id = eventId };
        await repository.SaveAsync(eventToSave, cancellationToken);
        return Results.Ok();
    }

    private static async Task<IResult> LoadHeatsAsync(
        string eventId,
        IHeatRepository repository,
        CancellationToken cancellationToken)
    {
        var eventRecord = await repository.LoadAsync(eventId, cancellationToken);
        var contract = eventRecord is not null ? ContractMapper.ToContract(eventRecord) : null;
        return Results.Ok(new LoadHeatsResponse(contract));
    }

    private static async Task<IResult> DeleteHeatsAsync(
        string eventId,
        IHeatRepository repository,
        CancellationToken cancellationToken)
    {
        await repository.DeleteAsync(eventId, cancellationToken);
        return Results.Ok();
    }

    private static async Task<IResult> ListEventsAsync(
        IHeatRepository repository,
        CancellationToken cancellationToken)
    {
        var eventIds = await repository.ListEventIdsAsync(cancellationToken);
        return Results.Ok(new ListEventsResponse(eventIds));
    }
}
