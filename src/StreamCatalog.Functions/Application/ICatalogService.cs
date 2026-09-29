using StreamCatalog.Functions.Contracts;
using StreamCatalog.Functions.Domain;

namespace StreamCatalog.Functions.Application;

public interface ICatalogService
{
    Task<OperationResult<CatalogItem>> CreateAsync(
        CreateCatalogItemRequest? request,
        CancellationToken cancellationToken);

    Task<OperationResult<CatalogItem>> GetByIdAsync(
        string? id,
        CancellationToken cancellationToken);

    Task<OperationResult<CatalogPage>> ListAsync(
        CatalogQuery query,
        CancellationToken cancellationToken);

    Task<OperationResult<CatalogPage>> SearchAsync(
        CatalogQuery query,
        CancellationToken cancellationToken);
}
