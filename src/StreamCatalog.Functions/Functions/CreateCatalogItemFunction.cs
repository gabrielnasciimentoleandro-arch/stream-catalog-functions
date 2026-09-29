using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Contracts;
using StreamCatalog.Functions.Http;
using StreamCatalog.Functions.Serialization;

namespace StreamCatalog.Functions.Functions;

public sealed class CreateCatalogItemFunction(ICatalogService service)
{
    [Function(nameof(CreateCatalogItem))]
    public async Task<IActionResult> CreateCatalogItem(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/catalog")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        ApiResponseFactory.ApplySecurityHeaders(request.HttpContext.Response);

        CreateCatalogItemRequest? payload;
        try
        {
            payload = await JsonSerializer.DeserializeAsync<CreateCatalogItemRequest>(
                request.Body,
                ApiJson.Options,
                cancellationToken);
        }
        catch (JsonException)
        {
            return ApiResponseFactory.FromError(
                request,
                ApiErrors.InvalidJson(
                    "The request must contain valid JSON, known properties and string enum values."));
        }

        var result = await service.CreateAsync(payload, cancellationToken);
        if (!result.Succeeded)
        {
            return ApiResponseFactory.FromError(request, result.Error!);
        }

        var response = CatalogItemResponse.From(result.Value!);
        request.HttpContext.Response.Headers.Location = $"/api/v1/catalog/items/{response.Id}";
        return ApiResponseFactory.Json(response, StatusCodes.Status201Created);
    }
}
