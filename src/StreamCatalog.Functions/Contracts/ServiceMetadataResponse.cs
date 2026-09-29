namespace StreamCatalog.Functions.Contracts;

public sealed record ServiceMetadataResponse(
    string Name,
    string Version,
    string Documentation,
    string Health);
