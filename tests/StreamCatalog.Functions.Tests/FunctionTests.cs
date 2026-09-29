using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Contracts;
using StreamCatalog.Functions.Functions;
using StreamCatalog.Functions.Infrastructure;
using StreamCatalog.Functions.Repositories;
using StreamCatalog.Functions.Serialization;

namespace StreamCatalog.Functions.Tests;

public sealed class FunctionTests
{
    [Fact]
    public async Task CreateAndGetCatalogItem_ReturnExpectedHttpResults()
    {
        var service = TestSupport.CreateCatalogService();
        var createFunction = new CreateCatalogItemFunction(service);
        var createContext = TestSupport.CreateHttpContext(
            "POST",
            body: JsonSerializer.Serialize(TestSupport.ValidMovieRequest(), ApiJson.Options),
            contentType: "application/json");

        var createdResult = await createFunction.CreateCatalogItem(
            createContext.Request,
            CancellationToken.None);

        var createdJson = Assert.IsType<JsonResult>(createdResult);
        Assert.Equal(StatusCodes.Status201Created, createdJson.StatusCode);
        var created = Assert.IsType<CatalogItemResponse>(createdJson.Value);
        Assert.Equal($"/api/v1/catalog/items/{created.Id}", createContext.Response.Headers.Location);
        Assert.Equal("nosniff", createContext.Response.Headers.XContentTypeOptions);

        var getContext = TestSupport.CreateHttpContext(path: $"/api/v1/catalog/items/{created.Id}");
        var getResult = await new GetCatalogItemFunction(service).GetCatalogItem(
            getContext.Request,
            created.Id,
            CancellationToken.None);
        var getJson = Assert.IsType<JsonResult>(getResult);
        Assert.Equal(StatusCodes.Status200OK, getJson.StatusCode);
        Assert.Equal(created.Id, Assert.IsType<CatalogItemResponse>(getJson.Value).Id);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{\"title\":\"Arrival\",\"unknown\":true}")]
    [InlineData("{\"type\":0}")]
    public async Task CreateCatalogItem_RejectsInvalidJson(string body)
    {
        var context = TestSupport.CreateHttpContext(
            "POST",
            body: body,
            contentType: "application/json");

        var result = await new CreateCatalogItemFunction(TestSupport.CreateCatalogService())
            .CreateCatalogItem(context.Request, CancellationToken.None);

        TestSupport.AssertProblem(result, StatusCodes.Status400BadRequest, "invalid_json");
    }

    [Fact]
    public async Task CreateCatalogItem_RejectsSemanticallyInvalidPayload()
    {
        var context = TestSupport.CreateHttpContext(
            "POST",
            body: "{}",
            contentType: "application/json");

        var result = await new CreateCatalogItemFunction(TestSupport.CreateCatalogService())
            .CreateCatalogItem(context.Request, CancellationToken.None);

        var problem = TestSupport.AssertProblem(
            result,
            StatusCodes.Status400BadRequest,
            "validation_failed");
        Assert.NotNull(problem.Extensions["errors"]);
    }

    [Fact]
    public async Task ListAndSearch_ReturnPagedResponses()
    {
        var service = TestSupport.CreateCatalogService();
        await service.CreateAsync(TestSupport.ValidMovieRequest(), CancellationToken.None);

        var listContext = TestSupport.CreateHttpContext(query: "page=1&pageSize=10");
        var list = await new ListCatalogItemsFunction(service).ListCatalogItems(
            listContext.Request,
            CancellationToken.None);
        var listPage = Assert.IsType<CatalogPageResponse>(Assert.IsType<JsonResult>(list).Value);
        Assert.Single(listPage.Items);

        var searchContext = TestSupport.CreateHttpContext(
            path: "/api/v1/catalog/search",
            query: "title=Arrival");
        var search = await new SearchCatalogItemsFunction(service).SearchCatalogItems(
            searchContext.Request,
            CancellationToken.None);
        var searchPage = Assert.IsType<CatalogPageResponse>(Assert.IsType<JsonResult>(search).Value);
        Assert.Single(searchPage.Items);
    }

    [Fact]
    public async Task ListAndSearch_ReturnProblemsForInvalidQueries()
    {
        var service = TestSupport.CreateCatalogService();
        var listContext = TestSupport.CreateHttpContext(query: "unknown=value");
        var listServiceContext = TestSupport.CreateHttpContext(query: "page=0");
        var searchContext = TestSupport.CreateHttpContext(
            path: "/api/v1/catalog/search");
        var searchParserContext = TestSupport.CreateHttpContext(
            path: "/api/v1/catalog/search",
            query: "type=invalid");

        var function = new ListCatalogItemsFunction(service);
        var list = await function.ListCatalogItems(listContext.Request, CancellationToken.None);
        var listService = await function.ListCatalogItems(
            listServiceContext.Request,
            CancellationToken.None);
        var searchFunction = new SearchCatalogItemsFunction(service);
        var search = await searchFunction.SearchCatalogItems(
            searchContext.Request,
            CancellationToken.None);
        var searchParser = await searchFunction.SearchCatalogItems(
            searchParserContext.Request,
            CancellationToken.None);

        TestSupport.AssertProblem(list, StatusCodes.Status400BadRequest, "validation_failed");
        TestSupport.AssertProblem(listService, StatusCodes.Status400BadRequest, "validation_failed");
        TestSupport.AssertProblem(search, StatusCodes.Status400BadRequest, "validation_failed");
        TestSupport.AssertProblem(searchParser, StatusCodes.Status400BadRequest, "validation_failed");
    }

    [Fact]
    public async Task GetCatalogItem_ReturnsNotFoundProblem()
    {
        var context = TestSupport.CreateHttpContext(path: "/api/v1/catalog/items/missing");
        var id = new string('f', 32);

        var result = await new GetCatalogItemFunction(TestSupport.CreateCatalogService())
            .GetCatalogItem(context.Request, id, CancellationToken.None);

        TestSupport.AssertProblem(result, StatusCodes.Status404NotFound, "not_found");
    }

    [Fact]
    public async Task UploadAndDownloadCover_ReturnExpectedHttpResults()
    {
        var service = new CoverService(new InMemoryCoverStorage());
        var uploadContext = TestSupport.CreateHttpContext(
            "POST",
            "/api/v1/covers",
            "fileName=poster.png",
            contentType: "image/png");
        uploadContext.Request.Body = new MemoryStream([1, 2, 3]);

        var upload = await new UploadCoverFunction(service).UploadCover(
            uploadContext.Request,
            CancellationToken.None);

        var uploadJson = Assert.IsType<JsonResult>(upload);
        Assert.Equal(StatusCodes.Status201Created, uploadJson.StatusCode);
        var response = Assert.IsType<CoverUploadResponse>(uploadJson.Value);
        Assert.Equal(response.DownloadUrl, uploadContext.Response.Headers.Location);

        var downloadContext = TestSupport.CreateHttpContext(path: response.DownloadUrl);
        var download = await new GetCoverFunction(service).GetCover(
            downloadContext.Request,
            response.FileName,
            CancellationToken.None);
        var file = Assert.IsType<FileContentResult>(download);
        Assert.Equal(new byte[] { 1, 2, 3 }, file.FileContents);
        Assert.Equal("image/png", file.ContentType);
        Assert.Contains("immutable", downloadContext.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task UploadCover_MapsUnsupportedMediaTypeAndTooLargeErrors()
    {
        var service = new CoverService(new InMemoryCoverStorage());
        var unsupportedContext = TestSupport.CreateHttpContext(
            "POST",
            "/api/v1/covers",
            "fileName=poster.gif",
            contentType: "image/gif");
        unsupportedContext.Request.Body = new MemoryStream([1]);

        var unsupported = await new UploadCoverFunction(service).UploadCover(
            unsupportedContext.Request,
            CancellationToken.None);
        TestSupport.AssertProblem(
            unsupported,
            StatusCodes.Status415UnsupportedMediaType,
            "unsupported_media_type");

        var largeContext = TestSupport.CreateHttpContext(
            "POST",
            "/api/v1/covers",
            "fileName=poster.jpg",
            contentType: "image/jpeg");
        largeContext.Request.Body = new MemoryStream(new byte[CoverService.MaximumFileSize + 1]);
        var large = await new UploadCoverFunction(service).UploadCover(
            largeContext.Request,
            CancellationToken.None);
        TestSupport.AssertProblem(
            large,
            StatusCodes.Status413PayloadTooLarge,
            "payload_too_large");
    }

    [Fact]
    public async Task GetCover_ReturnsValidationProblem()
    {
        var context = TestSupport.CreateHttpContext(path: "/api/v1/covers/invalid");

        var result = await new GetCoverFunction(new CoverService(new InMemoryCoverStorage()))
            .GetCover(context.Request, "invalid", CancellationToken.None);

        TestSupport.AssertProblem(result, StatusCodes.Status400BadRequest, "validation_failed");
    }

    [Fact]
    public void OpenApiFunction_ValidatesEmbeddedResourceArguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OpenApiFunction.ReadDocument(null!, "resource"));
        Assert.Throws<ArgumentException>(() =>
            OpenApiFunction.ReadDocument(typeof(OpenApiFunction).Assembly, ""));
        Assert.Throws<InvalidOperationException>(() =>
            OpenApiFunction.ReadDocument(typeof(OpenApiFunction).Assembly, "missing.resource"));
    }

    [Fact]
    public void RootAndHealth_ReturnMetadataAndProviderInformation()
    {
        var rootContext = TestSupport.CreateHttpContext(path: "/api/v1");
        var root = new RootFunction().GetServiceMetadata(rootContext.Request);
        var metadata = Assert.IsType<ServiceMetadataResponse>(Assert.IsType<JsonResult>(root).Value);
        Assert.Equal("Stream Catalog Functions", metadata.Name);
        Assert.Equal("/api/v1/openapi", metadata.Documentation);

        var openApiContext = TestSupport.CreateHttpContext(path: "/api/v1/openapi");
        var openApi = Assert.IsType<ContentResult>(
            new OpenApiFunction().GetOpenApi(openApiContext.Request));
        Assert.Equal(StatusCodes.Status200OK, openApi.StatusCode);
        Assert.Equal("application/yaml; charset=utf-8", openApi.ContentType);
        Assert.Contains("openapi: 3.0.3", openApi.Content, StringComparison.Ordinal);

        var healthContext = TestSupport.CreateHttpContext(path: "/api/v1/health");
        var health = new HealthFunction(
            new ProviderSettings("InMemory", "Blob"),
            new FixedTimeProvider(TestSupport.Now))
            .GetHealth(healthContext.Request);
        var response = Assert.IsType<HealthResponse>(Assert.IsType<JsonResult>(health).Value);
        Assert.Equal("Healthy", response.Status);
        Assert.Equal("InMemory", response.Providers["catalog"]);
        Assert.Equal(TestSupport.Now, response.Timestamp);
    }
}
