using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using StreamCatalog.Functions.Contracts;
using StreamCatalog.Functions.Http;
using StreamCatalog.Functions.Infrastructure;

namespace StreamCatalog.Functions.Functions;

public sealed class HealthFunction(
    ProviderSettings providers,
    TimeProvider timeProvider)
{
    [Function(nameof(GetHealth))]
    public IActionResult GetHealth(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/health")] HttpRequest request)
    {
        ApiResponseFactory.ApplySecurityHeaders(request.HttpContext.Response);
        return ApiResponseFactory.Json(
            new HealthResponse(
                "Healthy",
                timeProvider.GetUtcNow(),
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["catalog"] = providers.Catalog,
                    ["coverStorage"] = providers.CoverStorage,
                }),
            StatusCodes.Status200OK);
    }
}
