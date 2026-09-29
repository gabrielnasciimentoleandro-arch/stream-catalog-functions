using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Domain;
using StreamCatalog.Functions.Repositories;

namespace StreamCatalog.Functions.Tests;

public sealed class InMemoryRepositoryTests
{
    [Fact]
    public async Task QueryAsync_AppliesEveryFilter()
    {
        var repository = new InMemoryCatalogRepository();
        await repository.AddAsync(CreateItem("1", "Arrival", CatalogItemType.Movie, "Drama", 2016), CancellationToken.None);
        await repository.AddAsync(CreateItem("2", "Dark", CatalogItemType.Series, "Mystery", 2017), CancellationToken.None);

        var page = await repository.QueryAsync(
            new CatalogQuery("rri", "drama", CatalogItemType.Movie, 2016, 1, 20),
            CancellationToken.None);

        Assert.Single(page.Items);
        Assert.Equal("Arrival", page.Items[0].Title);
        Assert.Equal(1, page.TotalItems);
    }

    [Fact]
    public async Task QueryAsync_ReturnsEmptyPagePastTheEnd()
    {
        var repository = new InMemoryCatalogRepository();
        await repository.AddAsync(CreateItem("1", "Arrival", CatalogItemType.Movie, "Drama", 2016), CancellationToken.None);

        var page = await repository.QueryAsync(CatalogQuery.List(2, 10), CancellationToken.None);

        Assert.Empty(page.Items);
        Assert.Equal(1, page.TotalItems);
        Assert.Equal(1, page.TotalPages);
    }

    [Fact]
    public async Task AddAsync_RejectsDuplicateIdentifier()
    {
        var repository = new InMemoryCatalogRepository();
        var item = CreateItem("same", "Arrival", CatalogItemType.Movie, "Drama", 2016);
        await repository.AddAsync(item, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.AddAsync(item, CancellationToken.None));
    }

    [Fact]
    public async Task CoverStorage_RejectsDuplicateAndCopiesBuffers()
    {
        var storage = new InMemoryCoverStorage();
        var cover = new StoredCover("cover.jpg", "poster.jpg", "image/jpeg", [1, 2]);
        await storage.UploadAsync(cover, CancellationToken.None);
        cover.Content[0] = 9;

        var downloaded = await storage.DownloadAsync("cover.jpg", CancellationToken.None);

        Assert.Equal(new byte[] { 1, 2 }, downloaded!.Content);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.UploadAsync(cover, CancellationToken.None));
        Assert.Null(await storage.DownloadAsync("missing.jpg", CancellationToken.None));
    }

    private static CatalogItem CreateItem(
        string id,
        string title,
        CatalogItemType type,
        string genre,
        int year) =>
        new()
        {
            Id = id,
            Title = title,
            Synopsis = "A sufficiently detailed synopsis.",
            Type = type,
            Genres = [genre],
            ReleaseYear = year,
            AgeRating = "12",
            Seasons = type == CatalogItemType.Series ? 1 : null,
            CreatedAt = TestSupport.Now,
        };
}
