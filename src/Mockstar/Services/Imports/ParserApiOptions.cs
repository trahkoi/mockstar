namespace Mockstar.Services.Imports;

public sealed class ParserApiOptions
{
    public const string SectionName = "ParserApi";

    public string BaseUrl { get; init; } = "http://localhost:5100/";

    public int TimeoutSeconds { get; init; } = 10;
}
