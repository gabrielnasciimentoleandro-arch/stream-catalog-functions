using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Contracts;
using StreamCatalog.Functions.Http;

namespace StreamCatalog.Functions.Functions;

public sealed class GetCatalogItemFunction(ICatalogService service)
{
    [Function(nameof(GetCatalogItem))]
    public async Task<IActionResult> GetCatalogItem(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/catalog/items/{id}")] HttpRequest request,
        string id,
        CancellationToken cancellationToken)
    {
        ApiResponseFactory.ApplySecurityHeaders(request.HttpContext.Response);

        var result = await service.GetByIdAsync(id, cancellationToken);
        return result.Succeeded
            ? ApiResponseFactory.Json(
                CatalogItemResponse.From(result.Value!),
                StatusCodes.Status200OK)
            : ApiResponseFactory.FromError(request, result.Error!);
    }
}
