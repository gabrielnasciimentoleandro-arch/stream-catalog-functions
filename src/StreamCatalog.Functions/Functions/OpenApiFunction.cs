using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using StreamCatalog.Functions.Http;

namespace StreamCatalog.Functions.Functions;

public sealed class OpenApiFunction
{
    private const string ResourceName = "StreamCatalog.OpenApi.yaml";
    private static readonly Lazy<string> Document = new(ReadEmbeddedDocument);

    [Function(nameof(GetOpenApi))]
    public IActionResult GetOpenApi(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v1/openapi")] HttpRequest request)
    {
        ApiResponseFactory.ApplySecurityHeaders(request.HttpContext.Response);
        return new ContentResult
        {
            Content = Document.Value,
            ContentType = "application/yaml; charset=utf-8",
            StatusCode = StatusCodes.Status200OK,
        };
    }

    private static string ReadEmbeddedDocument() =>
        ReadDocument(Assembly.GetExecutingAssembly(), ResourceName);

    internal static string ReadDocument(Assembly assembly, string resourceName)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The embedded OpenAPI document was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
