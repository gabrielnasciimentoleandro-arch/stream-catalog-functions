using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Contracts;
using StreamCatalog.Functions.Http;

namespace StreamCatalog.Functions.Functions;

public sealed class UploadCoverFunction(ICoverService service)
{
    [Function(nameof(UploadCover))]
    public async Task<IActionResult> UploadCover(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "v1/covers")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        ApiResponseFactory.ApplySecurityHeaders(request.HttpContext.Response);

        var fileName = request.Query["fileName"].ToString();
        var result = await service.UploadAsync(
            fileName,
            request.ContentType,
            request.Body,
            cancellationToken);
        if (!result.Succeeded)
        {
            return ApiResponseFactory.FromError(request, result.Error!);
        }

        var cover = result.Value!;
        var downloadUrl = $"/api/v1/covers/{Uri.EscapeDataString(cover.FileName)}";
        request.HttpContext.Response.Headers.Location = downloadUrl;
        return ApiResponseFactory.Json(
            new CoverUploadResponse(
                cover.FileName,
                cover.OriginalFileName,
                cover.ContentType,
                cover.Content.LongLength,
                downloadUrl),
            StatusCodes.Status201Created);
    }
}
