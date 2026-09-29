using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Contracts;
using StreamCatalog.Functions.Http;

namespace StreamCatalog.Functions.Functions;

public sealed class ListCatalogItemsFunction(ICatalogService service)
{
    [Function(nameof(ListCatalogItems))]
    public async Task<IActionResult> ListCatalogItems(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/catalog")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        ApiResponseFactory.ApplySecurityHeaders(request.HttpContext.Response);

        var query = CatalogQueryParser.ParseList(request.Query);
        if (!query.Succeeded)
        {
            return ApiResponseFactory.FromError(request, query.Error!);
        }

        var result = await service.ListAsync(query.Value!, cancellationToken);
        return result.Succeeded
            ? ApiResponseFactory.Json(ToResponse(result.Value!), StatusCodes.Status200OK)
            : ApiResponseFactory.FromError(request, result.Error!);
    }

    internal static CatalogPageResponse ToResponse(CatalogPage page) =>
        new(
            page.Items.Select(CatalogItemResponse.From).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalItems,
            page.TotalPages);
}
