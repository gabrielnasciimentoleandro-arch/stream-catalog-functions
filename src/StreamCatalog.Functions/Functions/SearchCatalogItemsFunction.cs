using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Http;

namespace StreamCatalog.Functions.Functions;

public sealed class SearchCatalogItemsFunction(ICatalogService service)
{
    [Function(nameof(SearchCatalogItems))]
    public async Task<IActionResult> SearchCatalogItems(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/catalog/search")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        ApiResponseFactory.ApplySecurityHeaders(request.HttpContext.Response);

        var query = CatalogQueryParser.ParseSearch(request.Query);
        if (!query.Succeeded)
        {
            return ApiResponseFactory.FromError(request, query.Error!);
        }

        var result = await service.SearchAsync(query.Value!, cancellationToken);
        return result.Succeeded
            ? ApiResponseFactory.Json(
                ListCatalogItemsFunction.ToResponse(result.Value!),
                StatusCodes.Status200OK)
            : ApiResponseFactory.FromError(request, result.Error!);
    }
}
