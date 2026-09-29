using StreamCatalog.Functions.Domain;

namespace StreamCatalog.Functions.Contracts;

public sealed record CatalogItemResponse(
    string Id,
    string Title,
    string Synopsis,
    CatalogItemType Type,
    IReadOnlyList<string> Genres,
    int ReleaseYear,
    string AgeRating,
    int? Seasons,
    string? CoverFileName,
    string? CoverUrl,
    DateTimeOffset CreatedAt)
{
    public static CatalogItemResponse From(CatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var coverUrl = item.CoverFileName is null
            ? null
            : $"/api/v1/covers/{Uri.EscapeDataString(item.CoverFileName)}";

        return new CatalogItemResponse(
            item.Id,
            item.Title,
            item.Synopsis,
            item.Type,
            item.Genres,
            item.ReleaseYear,
            item.AgeRating,
            item.Seasons,
            item.CoverFileName,
            coverUrl,
            item.CreatedAt);
    }
}
