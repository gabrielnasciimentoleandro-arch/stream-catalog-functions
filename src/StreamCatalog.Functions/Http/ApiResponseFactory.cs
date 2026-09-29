using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Serialization;

namespace StreamCatalog.Functions.Http;

public static class ApiResponseFactory
{
    public static IActionResult Json(object value, int statusCode)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new JsonResult(value, ApiJson.Options) { StatusCode = statusCode };
    }

    public static IActionResult FromError(HttpRequest request, ApiError error)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(error);

        var status = error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "payload_too_large" => StatusCodes.Status413PayloadTooLarge,
            "unsupported_media_type" => StatusCodes.Status415UnsupportedMediaType,
            _ => StatusCodes.Status400BadRequest,
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Title,
            Detail = error.Detail,
            Type = $"https://httpstatuses.io/{status}",
            Instance = request.Path,
        };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = request.HttpContext.TraceIdentifier;
        if (error.Errors is not null)
        {
            problem.Extensions["errors"] = error.Errors;
        }

        return new JsonResult(problem, ApiJson.Options)
        {
            StatusCode = status,
            ContentType = "application/problem+json",
        };
    }

    public static void ApplySecurityHeaders(HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers.Append("Referrer-Policy", "no-referrer");
        response.Headers.Append("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'");
    }
}
