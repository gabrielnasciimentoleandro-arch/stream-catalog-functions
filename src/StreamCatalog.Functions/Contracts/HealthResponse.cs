namespace StreamCatalog.Functions.Contracts;

public sealed record HealthResponse(
    string Status,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, string> Providers);
