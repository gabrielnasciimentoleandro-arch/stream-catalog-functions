using System.Text.Json.Serialization;

namespace StreamCatalog.Functions.Domain;

public sealed record CatalogItem
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("synopsis")]
    public required string Synopsis { get; init; }

    [JsonPropertyName("type")]
    public required CatalogItemType Type { get; init; }

    [JsonPropertyName("genres")]
    public required IReadOnlyList<string> Genres { get; init; }

    [JsonPropertyName("releaseYear")]
    public required int ReleaseYear { get; init; }

    [JsonPropertyName("ageRating")]
    public required string AgeRating { get; init; }

    [JsonPropertyName("seasons")]
    public int? Seasons { get; init; }

    [JsonPropertyName("coverFileName")]
    public string? CoverFileName { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }
}
