using System.Collections.Concurrent;
using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Domain;

namespace StreamCatalog.Functions.Repositories;

public sealed class InMemoryCatalogRepository : ICatalogRepository
{
    private readonly ConcurrentDictionary<string, CatalogItem> _items =
        new(StringComparer.OrdinalIgnoreCase);

    public Task AddAsync(CatalogItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_items.TryAdd(item.Id, item))
        {
            throw new InvalidOperationException($"A catalog item with id '{item.Id}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<CatalogItem?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        cancellationToken.ThrowIfCancellationRequested();

        _items.TryGetValue(id, out var item);
        return Task.FromResult(item);
    }

    public Task<CatalogPage> QueryAsync(CatalogQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<CatalogItem> result = _items.Values;
        if (query.Title is not null)
        {
            result = result.Where(item =>
                item.Title.Contains(query.Title, StringComparison.OrdinalIgnoreCase));
        }

        if (query.Genre is not null)
        {
            result = result.Where(item =>
                item.Genres.Any(genre =>
                    string.Equals(genre, query.Genre, StringComparison.OrdinalIgnoreCase)));
        }

        if (query.Type is not null)
        {
            result = result.Where(item => item.Type == query.Type);
        }

        if (query.ReleaseYear is not null)
        {
            result = result.Where(item => item.ReleaseYear == query.ReleaseYear);
        }

        var ordered = result
            .OrderByDescending(static item => item.CreatedAt)
            .ThenBy(static item => item.Id, StringComparer.Ordinal)
            .ToArray();

        var offset = (query.Page - 1) * query.PageSize;
        var pageItems = ordered.Skip(offset).Take(query.PageSize).ToArray();
        return Task.FromResult(new CatalogPage(pageItems, query.Page, query.PageSize, ordered.Length));
    }
}
