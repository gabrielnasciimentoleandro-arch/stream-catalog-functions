using StreamCatalog.Functions.Domain;

namespace StreamCatalog.Functions.Application;

public sealed record CatalogQuery(
    string? Title,
    string? Genre,
    CatalogItemType? Type,
    int? ReleaseYear,
    int Page,
    int PageSize)
{
    public static CatalogQuery List(int page, int pageSize) =>
        new(null, null, null, null, page, pageSize);

    public bool HasFilter =>
        Title is not null || Genre is not null || Type is not null || ReleaseYear is not null;
}
