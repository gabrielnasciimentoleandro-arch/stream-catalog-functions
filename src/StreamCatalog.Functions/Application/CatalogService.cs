using System.Text.RegularExpressions;
using StreamCatalog.Functions.Contracts;
using StreamCatalog.Functions.Domain;
using StreamCatalog.Functions.Repositories;

namespace StreamCatalog.Functions.Application;

public sealed partial class CatalogService(
    ICatalogRepository repository,
    TimeProvider timeProvider) : ICatalogService
{
    private static readonly HashSet<string> AllowedAgeRatings =
        new(StringComparer.OrdinalIgnoreCase) { "L", "10", "12", "14", "16", "18" };

    public async Task<OperationResult<CatalogItem>> CreateAsync(
        CreateCatalogItemRequest? request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCreateRequest(request);
        if (validation.Count > 0)
        {
            return OperationResult.Failure<CatalogItem>(ApiErrors.Validation(validation));
        }

        var genres = request!.Genres!
            .Select(static genre => genre.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var item = new CatalogItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = request.Title!.Trim(),
            Synopsis = request.Synopsis!.Trim(),
            Type = request.Type!.Value,
            Genres = genres,
            ReleaseYear = request.ReleaseYear!.Value,
            AgeRating = request.AgeRating!.Trim().ToUpperInvariant(),
            Seasons = request.Seasons,
            CoverFileName = string.IsNullOrWhiteSpace(request.CoverFileName)
                ? null
                : request.CoverFileName.Trim().ToLowerInvariant(),
            CreatedAt = timeProvider.GetUtcNow(),
        };

        await repository.AddAsync(item, cancellationToken);
        return OperationResult.Success<CatalogItem>(item);
    }

    public async Task<OperationResult<CatalogItem>> GetByIdAsync(
        string? id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id) || !IdPattern().IsMatch(id))
        {
            var errors = new Dictionary<string, string[]>
            {
                ["id"] = ["The id must contain exactly 32 hexadecimal characters."],
            };

            return OperationResult.Failure<CatalogItem>(ApiErrors.Validation(errors));
        }

        var item = await repository.GetByIdAsync(id.ToLowerInvariant(), cancellationToken);
        return item is null
            ? OperationResult.Failure<CatalogItem>(ApiErrors.NotFound("catalog item"))
            : OperationResult.Success<CatalogItem>(item);
    }

    public Task<OperationResult<CatalogPage>> ListAsync(
        CatalogQuery query,
        CancellationToken cancellationToken) =>
        QueryAsync(query with { Title = null, Genre = null, Type = null, ReleaseYear = null }, false, cancellationToken);

    public Task<OperationResult<CatalogPage>> SearchAsync(
        CatalogQuery query,
        CancellationToken cancellationToken) =>
        QueryAsync(query, true, cancellationToken);

    private async Task<OperationResult<CatalogPage>> QueryAsync(
        CatalogQuery query,
        bool requireFilter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var errors = ValidateQuery(query, requireFilter);
        if (errors.Count > 0)
        {
            return OperationResult.Failure<CatalogPage>(ApiErrors.Validation(errors));
        }

        var normalized = query with
        {
            Title = string.IsNullOrWhiteSpace(query.Title) ? null : query.Title.Trim(),
            Genre = string.IsNullOrWhiteSpace(query.Genre) ? null : query.Genre.Trim(),
        };

        var page = await repository.QueryAsync(normalized, cancellationToken);
        return OperationResult.Success<CatalogPage>(page);
    }

    private Dictionary<string, string[]> ValidateCreateRequest(CreateCatalogItemRequest? request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (request is null)
        {
            errors["body"] = ["A JSON request body is required."];
            return errors;
        }

        ValidateText(errors, "title", request.Title, 2, 120);
        ValidateText(errors, "synopsis", request.Synopsis, 10, 1_000);

        if (request.Type is null || !Enum.IsDefined(request.Type.Value))
        {
            errors["type"] = ["Type must be Movie or Series."];
        }

        ValidateGenres(errors, request.Genres);

        var maximumYear = timeProvider.GetUtcNow().Year + 5;
        if (request.ReleaseYear is null || request.ReleaseYear < 1888 || request.ReleaseYear > maximumYear)
        {
            errors["releaseYear"] = [$"Release year must be between 1888 and {maximumYear}."];
        }

        if (string.IsNullOrWhiteSpace(request.AgeRating) ||
            !AllowedAgeRatings.Contains(request.AgeRating.Trim()))
        {
            errors["ageRating"] = ["Age rating must be L, 10, 12, 14, 16 or 18."];
        }

        if (request.Type == CatalogItemType.Series &&
            (request.Seasons is null || request.Seasons is < 1 or > 100))
        {
            errors["seasons"] = ["Series must have between 1 and 100 seasons."];
        }
        else if (request.Type == CatalogItemType.Movie && request.Seasons is not null)
        {
            errors["seasons"] = ["Movies cannot define seasons."];
        }

        if (!string.IsNullOrWhiteSpace(request.CoverFileName) &&
            !CoverFilePattern().IsMatch(request.CoverFileName.Trim()))
        {
            errors["coverFileName"] = ["Cover file name must be a generated JPG, PNG or WEBP identifier."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateQuery(CatalogQuery query, bool requireFilter)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (query.Page is < 1 or > 1_000)
        {
            errors["page"] = ["Page must be between 1 and 1000."];
        }

        if (query.PageSize is < 1 or > 100)
        {
            errors["pageSize"] = ["Page size must be between 1 and 100."];
        }

        if (query.Title?.Trim().Length > 120)
        {
            errors["title"] = ["Title cannot exceed 120 characters."];
        }

        if (query.Genre?.Trim().Length > 40)
        {
            errors["genre"] = ["Genre cannot exceed 40 characters."];
        }

        if (query.ReleaseYear is not null && query.ReleaseYear is < 1888 or > 9999)
        {
            errors["releaseYear"] = ["Release year must be between 1888 and 9999."];
        }

        if (requireFilter && !query.HasFilter)
        {
            errors["filter"] = ["Provide at least one of: title, genre, type or releaseYear."];
        }

        return errors;
    }

    private static void ValidateText(
        IDictionary<string, string[]> errors,
        string field,
        string? value,
        int minimumLength,
        int maximumLength)
    {
        var length = value?.Trim().Length ?? 0;
        if (length < minimumLength || length > maximumLength)
        {
            errors[field] = [$"The field must contain between {minimumLength} and {maximumLength} characters."];
        }
    }

    private static void ValidateGenres(
        IDictionary<string, string[]> errors,
        IReadOnlyList<string>? genres)
    {
        if (genres is null || genres.Count is < 1 or > 5)
        {
            errors["genres"] = ["Provide between 1 and 5 genres."];
            return;
        }

        if (genres.Any(static genre => string.IsNullOrWhiteSpace(genre) || genre.Trim().Length > 40))
        {
            errors["genres"] = ["Each genre must contain between 1 and 40 characters."];
        }
    }

    [GeneratedRegex("^[0-9a-fA-F]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdPattern();

    [GeneratedRegex(
        "^[0-9a-f]{32}\\.(?:jpg|png|webp)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CoverFilePattern();
}
