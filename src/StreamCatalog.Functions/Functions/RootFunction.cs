using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using StreamCatalog.Functions.Contracts;
using StreamCatalog.Functions.Http;

namespace StreamCatalog.Functions.Functions;

public sealed class RootFunction
{
    [Function(nameof(GetServiceMetadata))]
    public IActionResult GetServiceMetadata(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1")] HttpRequest request)
    {
        ApiResponseFactory.ApplySecurityHeaders(request.HttpContext.Response);
        return ApiResponseFactory.Json(
            new ServiceMetadataResponse(
                "Stream Catalog Functions",
                "v1",
                "/api/v1/openapi",
                "/api/v1/health"),
            StatusCodes.Status200OK);
    }
}
