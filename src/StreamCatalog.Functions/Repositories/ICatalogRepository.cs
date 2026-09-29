using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Domain;

namespace StreamCatalog.Functions.Repositories;

public interface ICatalogRepository
{
    Task AddAsync(CatalogItem item, CancellationToken cancellationToken);

    Task<CatalogItem?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<CatalogPage> QueryAsync(CatalogQuery query, CancellationToken cancellationToken);
}
