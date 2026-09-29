using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Http;

namespace StreamCatalog.Functions.Functions;

public sealed class GetCoverFunction(ICoverService service)
{
    [Function(nameof(GetCover))]
    public async Task<IActionResult> GetCover(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/covers/{fileName}")] HttpRequest request,
        string fileName,
        CancellationToken cancellationToken)
    {
        ApiResponseFactory.ApplySecurityHeaders(request.HttpContext.Response);

        var result = await service.DownloadAsync(fileName, cancellationToken);
        if (!result.Succeeded)
        {
            return ApiResponseFactory.FromError(request, result.Error!);
        }

        request.HttpContext.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        var cover = result.Value!;
        return new FileContentResult(cover.Content, cover.ContentType)
        {
            FileDownloadName = null,
            EnableRangeProcessing = false,
        };
    }
}
