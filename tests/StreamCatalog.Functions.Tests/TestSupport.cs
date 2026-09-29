using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Contracts;
using StreamCatalog.Functions.Domain;
using StreamCatalog.Functions.Repositories;

namespace StreamCatalog.Functions.Tests;

internal static class TestSupport
{
    internal static readonly DateTimeOffset Now =
        new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);

    internal static CatalogService CreateCatalogService(
        InMemoryCatalogRepository? repository = null) =>
        new(repository ?? new InMemoryCatalogRepository(), new FixedTimeProvider(Now));

    internal static CreateCatalogItemRequest ValidMovieRequest(
        string title = "Arrival",
        string? coverFileName = null) =>
        new(
            title,
            "A linguist works with visitors from another world.",
            CatalogItemType.Movie,
            ["Science Fiction", "Drama"],
            2016,
            "12",
            null,
            coverFileName);

    internal static DefaultHttpContext CreateHttpContext(
        string method = "GET",
        string path = "/api/v1/catalog",
        string? query = null,
        string? body = null,
        string? contentType = null)
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "test-trace-id";
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.QueryString = string.IsNullOrEmpty(query)
            ? QueryString.Empty
            : new QueryString(query.StartsWith('?') ? query : $"?{query}");
        context.Request.Body = new MemoryStream(
            body is null ? [] : Encoding.UTF8.GetBytes(body));
        context.Request.ContentType = contentType;
        return context;
    }

    internal static ProblemDetails AssertProblem(
        IActionResult result,
        int expectedStatus,
        string expectedCode)
    {
        var json = Assert.IsType<JsonResult>(result);
        Assert.Equal(expectedStatus, json.StatusCode);
        Assert.Equal("application/problem+json", json.ContentType);
        var problem = Assert.IsType<ProblemDetails>(json.Value);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Equal(expectedCode, problem.Extensions["code"]);
        Assert.Equal("test-trace-id", problem.Extensions["traceId"]);
        return problem;
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => value;
}
