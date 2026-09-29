using Microsoft.Azure.Cosmos;
using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Domain;

namespace StreamCatalog.Functions.Repositories;

public sealed class CosmosCatalogRepository(Container container) : ICatalogRepository
{
    public async Task AddAsync(CatalogItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        await container.CreateItemAsync(
            item,
            new PartitionKey(item.Type.ToString().ToLowerInvariant()),
            cancellationToken: cancellationToken);
    }

    public async Task<CatalogItem?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var query = new QueryDefinition("SELECT TOP 1 * FROM c WHERE c.id = @id")
            .WithParameter("@id", id);
        var iterator = container.GetItemQueryIterator<CatalogItem>(
            query,
            requestOptions: new QueryRequestOptions { MaxItemCount = 1 });

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            var item = response.FirstOrDefault();
            if (item is not null)
            {
                return item;
            }
        }

        return null;
    }

    public async Task<CatalogPage> QueryAsync(
        CatalogQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var predicates = new List<string>();
        var parameters = new List<(string Name, object Value)>();

        AddStringPredicate(query.Title, "CONTAINS(LOWER(c.title), @title)", "@title");
        AddStringPredicate(
            query.Genre,
            "EXISTS(SELECT VALUE genre FROM genre IN c.genres WHERE LOWER(genre) = @genre)",
            "@genre");

        if (query.Type is not null)
        {
            predicates.Add("c.type = @type");
            parameters.Add(("@type", query.Type.Value.ToString().ToLowerInvariant()));
        }

        if (query.ReleaseYear is not null)
        {
            predicates.Add("c.releaseYear = @releaseYear");
            parameters.Add(("@releaseYear", query.ReleaseYear.Value));
        }

        var whereClause = predicates.Count == 0
            ? string.Empty
            : $" WHERE {string.Join(" AND ", predicates)}";
        var offset = (query.Page - 1) * query.PageSize;

        var itemQuery = BuildQuery(
            $"SELECT * FROM c{whereClause} ORDER BY c.createdAt DESC OFFSET @offset LIMIT @limit",
            parameters)
            .WithParameter("@offset", offset)
            .WithParameter("@limit", query.PageSize);
        var countQuery = BuildQuery(
            $"SELECT VALUE COUNT(1) FROM c{whereClause}",
            parameters);

        var items = await ReadItemsAsync(itemQuery, query.PageSize, cancellationToken);
        var totalItems = await ReadCountAsync(countQuery, cancellationToken);
        return new CatalogPage(items, query.Page, query.PageSize, totalItems);

        void AddStringPredicate(string? value, string predicate, string parameterName)
        {
            if (value is null)
            {
                return;
            }

            predicates.Add(predicate);
            parameters.Add((parameterName, value.ToLowerInvariant()));
        }
    }

    private static QueryDefinition BuildQuery(
        string text,
        IEnumerable<(string Name, object Value)> parameters)
    {
        var definition = new QueryDefinition(text);
        foreach (var parameter in parameters)
        {
            definition.WithParameter(parameter.Name, parameter.Value);
        }

        return definition;
    }

    private async Task<IReadOnlyList<CatalogItem>> ReadItemsAsync(
        QueryDefinition query,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var items = new List<CatalogItem>(pageSize);
        var iterator = container.GetItemQueryIterator<CatalogItem>(
            query,
            requestOptions: new QueryRequestOptions { MaxItemCount = pageSize });

        while (iterator.HasMoreResults && items.Count < pageSize)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            items.AddRange(response);
        }

        return items;
    }

    private async Task<int> ReadCountAsync(
        QueryDefinition query,
        CancellationToken cancellationToken)
    {
        var iterator = container.GetItemQueryIterator<int>(query);
        var total = 0;
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            total += response.Sum();
        }

        return total;
    }
}
