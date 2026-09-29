using Microsoft.AspNetCore.Http;
using StreamCatalog.Functions.Domain;
using StreamCatalog.Functions.Http;

namespace StreamCatalog.Functions.Tests;

public sealed class CatalogQueryParserTests
{
    [Fact]
    public void ParseList_UsesDefaults()
    {
        var context = TestSupport.CreateHttpContext();

        var result = CatalogQueryParser.ParseList(context.Request.Query);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Value!.Page);
        Assert.Equal(20, result.Value.PageSize);
        Assert.False(result.Value.HasFilter);
    }

    [Fact]
    public void ParseSearch_ParsesAllParameters()
    {
        var context = TestSupport.CreateHttpContext(
            query: "title=Arrival&genre=Drama&type=movie&releaseYear=2016&page=2&pageSize=5");

        var result = CatalogQueryParser.ParseSearch(context.Request.Query);

        Assert.True(result.Succeeded);
        Assert.Equal("Arrival", result.Value!.Title);
        Assert.Equal("Drama", result.Value.Genre);
        Assert.Equal(CatalogItemType.Movie, result.Value.Type);
        Assert.Equal(2016, result.Value.ReleaseYear);
        Assert.Equal(2, result.Value.Page);
        Assert.Equal(5, result.Value.PageSize);
    }

    [Fact]
    public void ParseSearch_RejectsUnknownDuplicateAndMalformedParameters()
    {
        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["unknown"] = "value",
            ["page"] = "abc",
            ["pageSize"] = new Microsoft.Extensions.Primitives.StringValues(["10", "20"]),
            ["releaseYear"] = "year",
            ["type"] = "0",
        });

        var result = CatalogQueryParser.ParseSearch(query);

        Assert.False(result.Succeeded);
        Assert.Equal(
            ["page", "pageSize", "query", "releaseYear", "type"],
            result.Error!.Errors!.Keys.Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void ParseList_RejectsFilterParameters()
    {
        var context = TestSupport.CreateHttpContext(query: "title=Arrival");

        var result = CatalogQueryParser.ParseList(context.Request.Query);

        Assert.False(result.Succeeded);
        Assert.Contains("query", result.Error!.Errors!);
    }
}
