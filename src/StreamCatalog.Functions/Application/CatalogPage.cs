using StreamCatalog.Functions.Domain;

namespace StreamCatalog.Functions.Application;

public sealed record CatalogPage(
    IReadOnlyList<CatalogItem> Items,
    int Page,
    int PageSize,
    int TotalItems)
{
    public int TotalPages => TotalItems == 0
        ? 0
        : (int)Math.Ceiling(TotalItems / (double)PageSize);
}
