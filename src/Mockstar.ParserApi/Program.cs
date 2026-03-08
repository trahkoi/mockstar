using Microsoft.AspNetCore.Mvc;
using Mockstar.ParserApi.Contracts;
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

        var app = builder.Build();

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        app.MapPost("/api/parser/text", ParseTextAsync);
        app.MapPost("/api/parser/url", ParseUrlAsync);

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
}
