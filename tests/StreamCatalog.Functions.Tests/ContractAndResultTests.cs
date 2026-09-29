using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Contracts;
using StreamCatalog.Functions.Domain;
using StreamCatalog.Functions.Http;

namespace StreamCatalog.Functions.Tests;

public sealed class ContractAndResultTests
{
    [Fact]
    public void CatalogItemResponse_BuildsCoverUrlWhenPresent()
    {
        var item = new CatalogItem
        {
            Id = new string('a', 32),
            Title = "Arrival",
            Synopsis = "A sufficiently detailed synopsis.",
            Type = CatalogItemType.Movie,
            Genres = ["Drama"],
            ReleaseYear = 2016,
            AgeRating = "12",
            CoverFileName = $"{new string('b', 32)}.jpg",
            CreatedAt = TestSupport.Now,
        };

        var response = CatalogItemResponse.From(item);

        Assert.Equal($"/api/v1/covers/{item.CoverFileName}", response.CoverUrl);
        Assert.Equal(item.Id, response.Id);
        Assert.Equal(item.Title, response.Title);
        Assert.Equal(item.Synopsis, response.Synopsis);
        Assert.Equal(item.Type, response.Type);
        Assert.Equal(item.Genres, response.Genres);
        Assert.Equal(item.ReleaseYear, response.ReleaseYear);
        Assert.Equal(item.AgeRating, response.AgeRating);
        Assert.Equal(item.Seasons, response.Seasons);
        Assert.Equal(item.CoverFileName, response.CoverFileName);
        Assert.Equal(item.CreatedAt, response.CreatedAt);
    }

    [Fact]
    public void ResponseContracts_ExposeEveryValue()
    {
        var page = new CatalogPageResponse([], 2, 10, 15, 2);
        var upload = new CoverUploadResponse("cover.jpg", "poster.jpg", "image/jpeg", 123, "/cover");
        var metadata = new ServiceMetadataResponse("name", "v1", "/docs", "/health");

        Assert.Empty(page.Items);
        Assert.Equal(2, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(15, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal("cover.jpg", upload.FileName);
        Assert.Equal("poster.jpg", upload.OriginalFileName);
        Assert.Equal("image/jpeg", upload.ContentType);
        Assert.Equal(123, upload.Length);
        Assert.Equal("/cover", upload.DownloadUrl);
        Assert.Equal("name", metadata.Name);
        Assert.Equal("v1", metadata.Version);
        Assert.Equal("/docs", metadata.Documentation);
        Assert.Equal("/health", metadata.Health);
    }

    [Fact]
    public void CatalogPage_CalculatesZeroAndRoundedPageCounts()
    {
        var empty = new CatalogPage([], 1, 20, 0);
        var populated = new CatalogPage([], 1, 20, 21);

        Assert.Equal(0, empty.TotalPages);
        Assert.Equal(2, populated.TotalPages);
    }

    [Fact]
    public void OperationResult_CreatesSuccessAndFailure()
    {
        var success = OperationResult.Success("value");
        var failure = OperationResult.Failure<string>(ApiErrors.NotFound("item"));

        Assert.True(success.Succeeded);
        Assert.Equal("value", success.Value);
        Assert.Null(success.Error);
        Assert.False(failure.Succeeded);
        Assert.Null(failure.Value);
        Assert.Equal("not_found", failure.Error!.Code);
    }

    [Theory]
    [InlineData("validation_failed", 400)]
    [InlineData("not_found", 404)]
    [InlineData("payload_too_large", 413)]
    [InlineData("unsupported_media_type", 415)]
    public void ApiResponseFactory_MapsErrorCodeToStatus(string code, int status)
    {
        var context = TestSupport.CreateHttpContext(path: "/resource");
        var error = new ApiError(code, "Title", "Detail");

        var result = ApiResponseFactory.FromError(context.Request, error);

        var problem = TestSupport.AssertProblem(result, status, code);
        Assert.Equal("/resource", problem.Instance);
    }

    [Fact]
    public void ApiResponseFactory_RejectsNullJsonValue()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ApiResponseFactory.Json(null!, StatusCodes.Status200OK));
    }
}
