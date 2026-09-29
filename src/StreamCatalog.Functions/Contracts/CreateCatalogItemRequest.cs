using StreamCatalog.Functions.Domain;

namespace StreamCatalog.Functions.Contracts;

public sealed record CreateCatalogItemRequest(
    string? Title,
    string? Synopsis,
    CatalogItemType? Type,
    IReadOnlyList<string>? Genres,
    int? ReleaseYear,
    string? AgeRating,
    int? Seasons,
    string? CoverFileName);
