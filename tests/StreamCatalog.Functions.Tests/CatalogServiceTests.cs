using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Contracts;
using StreamCatalog.Functions.Domain;
using StreamCatalog.Functions.Repositories;

namespace StreamCatalog.Functions.Tests;

public sealed class CatalogServiceTests
{
    [Fact]
    public async Task CreateAsync_NormalizesAndStoresValidMovie()
    {
        var repository = new InMemoryCatalogRepository();
        var service = TestSupport.CreateCatalogService(repository);
        var request = TestSupport.ValidMovieRequest("  Arrival  ") with
        {
            Genres = [" Science Fiction ", "science fiction", " Drama "],
            AgeRating = "l",
            CoverFileName = $"{new string('A', 32)}.JPG",
        };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.True(result.Succeeded);
        var item = Assert.IsType<CatalogItem>(result.Value);
        Assert.Matches("^[0-9a-f]{32}$", item.Id);
        Assert.Equal("Arrival", item.Title);
        Assert.Equal(["Science Fiction", "Drama"], item.Genres);
        Assert.Equal("L", item.AgeRating);
        Assert.Equal($"{new string('a', 32)}.jpg", item.CoverFileName);
        Assert.Equal(TestSupport.Now, item.CreatedAt);
        Assert.Same(item, await repository.GetByIdAsync(item.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_AcceptsSeriesWithSeasons()
    {
        var service = TestSupport.CreateCatalogService();
        var request = TestSupport.ValidMovieRequest() with
        {
            Type = CatalogItemType.Series,
            Seasons = 3,
        };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Value!.Seasons);
        Assert.Equal(CatalogItemType.Series, result.Value.Type);
    }

    [Fact]
    public async Task CreateAsync_RejectsMissingBody()
    {
        var result = await TestSupport.CreateCatalogService()
            .CreateAsync(null, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("validation_failed", result.Error!.Code);
        Assert.Contains("body", result.Error.Errors!);
    }

    [Fact]
    public async Task CreateAsync_ReportsAllInvalidFields()
    {
        var request = new CreateCatalogItemRequest(
            "x",
            "short",
            null,
            [""],
            1800,
            "PG",
            0,
            "cover.exe");

        var result = await TestSupport.CreateCatalogService()
            .CreateAsync(request, CancellationToken.None);

        Assert.False(result.Succeeded);
        var errors = result.Error!.Errors!;
        Assert.Equal(
            ["ageRating", "coverFileName", "genres", "releaseYear", "synopsis", "title", "type"],
            errors.Keys.Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task CreateAsync_RejectsMovieWithSeasons()
    {
        var request = TestSupport.ValidMovieRequest() with { Seasons = 1 };

        var result = await TestSupport.CreateCatalogService()
            .CreateAsync(request, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("seasons", result.Error!.Errors!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(101)]
    public async Task CreateAsync_RejectsSeriesWithoutValidSeasonCount(int? seasons)
    {
        var request = TestSupport.ValidMovieRequest() with
        {
            Type = CatalogItemType.Series,
            Seasons = seasons,
        };

        var result = await TestSupport.CreateCatalogService()
            .CreateAsync(request, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("seasons", result.Error!.Errors!);
    }

    [Fact]
    public async Task CreateAsync_RejectsMissingOrExcessiveGenres()
    {
        var service = TestSupport.CreateCatalogService();
        var missing = await service.CreateAsync(
            TestSupport.ValidMovieRequest() with { Genres = null },
            CancellationToken.None);
        var excessive = await service.CreateAsync(
            TestSupport.ValidMovieRequest() with
            {
                Genres = ["One", "Two", "Three", "Four", "Five", "Six"],
            },
            CancellationToken.None);

        Assert.Contains("genres", missing.Error!.Errors!);
        Assert.Contains("genres", excessive.Error!.Errors!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-id")]
    [InlineData("0000000000000000000000000000000z")]
    public async Task GetByIdAsync_RejectsInvalidIdentifier(string? id)
    {
        var result = await TestSupport.CreateCatalogService()
            .GetByIdAsync(id, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCreatedItemAndNotFoundForUnknownId()
    {
        var service = TestSupport.CreateCatalogService();
        var created = await service.CreateAsync(
            TestSupport.ValidMovieRequest(),
            CancellationToken.None);

        var found = await service.GetByIdAsync(created.Value!.Id, CancellationToken.None);
        var missing = await service.GetByIdAsync(new string('f', 32), CancellationToken.None);

        Assert.True(found.Succeeded);
        Assert.Equal(created.Value, found.Value);
        Assert.False(missing.Succeeded);
        Assert.Equal("not_found", missing.Error!.Code);
    }

    [Fact]
    public async Task ListAsync_IgnoresFiltersAndPaginates()
    {
        var service = TestSupport.CreateCatalogService();
        await service.CreateAsync(TestSupport.ValidMovieRequest("Arrival"), CancellationToken.None);
        await service.CreateAsync(TestSupport.ValidMovieRequest("Interstellar"), CancellationToken.None);

        var result = await service.ListAsync(
            new CatalogQuery("No Match", "No Match", CatalogItemType.Series, 1900, 1, 1),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!.Items);
        Assert.Equal(2, result.Value.TotalItems);
        Assert.Equal(2, result.Value.TotalPages);
    }

    [Fact]
    public async Task SearchAsync_NormalizesAndFilters()
    {
        var service = TestSupport.CreateCatalogService();
        await service.CreateAsync(TestSupport.ValidMovieRequest("Arrival"), CancellationToken.None);
        await service.CreateAsync(
            TestSupport.ValidMovieRequest("Dark") with
            {
                Type = CatalogItemType.Series,
                Seasons = 3,
                Genres = ["Mystery"],
                ReleaseYear = 2017,
            },
            CancellationToken.None);

        var result = await service.SearchAsync(
            new CatalogQuery("  arr  ", " science fiction ", CatalogItemType.Movie, 2016, 1, 20),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        var item = Assert.Single(result.Value!.Items);
        Assert.Equal("Arrival", item.Title);
    }

    [Fact]
    public async Task SearchAsync_RequiresAFilter()
    {
        var result = await TestSupport.CreateCatalogService().SearchAsync(
            CatalogQuery.List(1, 20),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("filter", result.Error!.Errors!);
    }

    [Fact]
    public async Task SearchAsync_ValidatesPaginationAndFilterLengths()
    {
        var query = new CatalogQuery(
            new string('a', 121),
            new string('b', 41),
            null,
            1800,
            0,
            101);

        var result = await TestSupport.CreateCatalogService()
            .SearchAsync(query, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(
            ["genre", "page", "pageSize", "releaseYear", "title"],
            result.Error!.Errors!.Keys.Order(StringComparer.Ordinal).ToArray());
    }
}
