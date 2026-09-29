namespace StreamCatalog.Functions.Application;

public sealed record ApiError(
    string Code,
    string Title,
    string Detail,
    IReadOnlyDictionary<string, string[]>? Errors = null);
