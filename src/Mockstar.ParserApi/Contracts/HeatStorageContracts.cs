namespace Mockstar.ParserApi.Contracts;

public sealed record SaveHeatsRequest(ParserEventRecord EventRecord);

public sealed record LoadHeatsResponse(ParserEventRecord? EventRecord);

public sealed record ListEventsResponse(IReadOnlyList<string> EventIds);
