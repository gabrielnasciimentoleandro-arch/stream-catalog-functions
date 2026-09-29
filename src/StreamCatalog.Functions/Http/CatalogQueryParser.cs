using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Domain;

namespace StreamCatalog.Functions.Http;

public static class CatalogQueryParser
{
    private static readonly HashSet<string> PaginationParameters =
        new(StringComparer.OrdinalIgnoreCase) { "page", "pageSize" };

    private static readonly HashSet<string> SearchParameters =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "page", "pageSize", "title", "genre", "type", "releaseYear",
        };

    public static OperationResult<CatalogQuery> ParseList(IQueryCollection query) =>
        Parse(query, allowFilters: false);

    public static OperationResult<CatalogQuery> ParseSearch(IQueryCollection query) =>
        Parse(query, allowFilters: true);

    private static OperationResult<CatalogQuery> Parse(
        IQueryCollection query,
        bool allowFilters)
    {
        ArgumentNullException.ThrowIfNull(query);

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var allowed = allowFilters ? SearchParameters : PaginationParameters;
        var unknown = query.Keys.Where(key => !allowed.Contains(key)).Order(StringComparer.Ordinal).ToArray();
        if (unknown.Length > 0)
        {
            errors["query"] = [$"Unknown query parameters: {string.Join(", ", unknown)}."];
        }

        var page = ParseInteger(query, "page", 1, errors);
        var pageSize = ParseInteger(query, "pageSize", 20, errors);
        var releaseYear = allowFilters
            ? ParseNullableInteger(query, "releaseYear", errors)
            : null;
        CatalogItemType? type = null;

        if (allowFilters && TryGetSingle(query, "type", errors, out var typeText) &&
            !string.IsNullOrWhiteSpace(typeText))
        {
            if (int.TryParse(typeText, out _) ||
                !Enum.TryParse<CatalogItemType>(typeText, ignoreCase: true, out var parsedType) ||
                !Enum.IsDefined(parsedType))
            {
                errors["type"] = ["Type must be Movie or Series."];
            }
            else
            {
                type = parsedType;
            }
        }

        var title = allowFilters ? GetText(query, "title", errors) : null;
        var genre = allowFilters ? GetText(query, "genre", errors) : null;
        if (errors.Count > 0)
        {
            return OperationResult.Failure<CatalogQuery>(ApiErrors.Validation(errors));
        }

        return OperationResult.Success<CatalogQuery>(
            new CatalogQuery(title, genre, type, releaseYear, page, pageSize));
    }

    private static int ParseInteger(
        IQueryCollection query,
        string key,
        int defaultValue,
        IDictionary<string, string[]> errors)
    {
        if (!TryGetSingle(query, key, errors, out var text) || string.IsNullOrWhiteSpace(text))
        {
            return defaultValue;
        }

        if (!int.TryParse(text, out var value))
        {
            errors[key] = ["The value must be an integer."];
            return defaultValue;
        }

        return value;
    }

    private static int? ParseNullableInteger(
        IQueryCollection query,
        string key,
        IDictionary<string, string[]> errors)
    {
        if (!TryGetSingle(query, key, errors, out var text) || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (!int.TryParse(text, out var value))
        {
            errors[key] = ["The value must be an integer."];
            return null;
        }

        return value;
    }

    private static string? GetText(
        IQueryCollection query,
        string key,
        IDictionary<string, string[]> errors)
    {
        return TryGetSingle(query, key, errors, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
    }

    private static bool TryGetSingle(
        IQueryCollection query,
        string key,
        IDictionary<string, string[]> errors,
        out string? value)
    {
        if (!query.TryGetValue(key, out StringValues values) || values.Count == 0)
        {
            value = null;
            return false;
        }

        if (values.Count > 1)
        {
            errors[key] = ["The parameter can only be supplied once."];
            value = null;
            return false;
        }

        value = values[0];
        return true;
    }
}
