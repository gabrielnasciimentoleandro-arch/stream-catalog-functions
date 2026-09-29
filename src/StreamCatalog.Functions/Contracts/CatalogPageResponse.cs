namespace StreamCatalog.Functions.Contracts;

public sealed record CatalogPageResponse(
    IReadOnlyList<CatalogItemResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
