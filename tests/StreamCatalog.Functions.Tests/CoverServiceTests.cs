using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Domain;
using StreamCatalog.Functions.Repositories;

namespace StreamCatalog.Functions.Tests;

public sealed class CoverServiceTests
{
    [Theory]
    [InlineData("poster.jpg", "image/jpeg", ".jpg")]
    [InlineData("poster.jpeg", "image/jpeg; charset=binary", ".jpg")]
    [InlineData("poster.png", "image/png", ".png")]
    [InlineData("poster.webp", "image/webp", ".webp")]
    public async Task UploadAsync_StoresAllowedImage(
        string originalName,
        string contentType,
        string expectedExtension)
    {
        var storage = new InMemoryCoverStorage();
        var service = new CoverService(storage);
        var bytes = new byte[] { 1, 2, 3, 4 };

        var result = await service.UploadAsync(
            originalName,
            contentType,
            new MemoryStream(bytes),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        var cover = Assert.IsType<StoredCover>(result.Value);
        Assert.Matches($"^[0-9a-f]{{32}}\\{expectedExtension}$", cover.FileName);
        Assert.Equal(originalName, cover.OriginalFileName);
        Assert.Equal(contentType.Split(';')[0], cover.ContentType);
        Assert.Equal(bytes, cover.Content);

        var downloaded = await service.DownloadAsync(cover.FileName, CancellationToken.None);
        Assert.True(downloaded.Succeeded);
        Assert.Equal(bytes, downloaded.Value!.Content);
        Assert.NotSame(cover.Content, downloaded.Value.Content);
    }

    [Theory]
    [InlineData("poster.gif", "image/gif")]
    [InlineData("poster.jpg", null)]
    [InlineData("poster.jpg", "application/octet-stream")]
    public async Task UploadAsync_RejectsUnsupportedContentType(
        string fileName,
        string? contentType)
    {
        var result = await new CoverService(new InMemoryCoverStorage()).UploadAsync(
            fileName,
            contentType,
            new MemoryStream([1]),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("unsupported_media_type", result.Error!.Code);
    }

    [Theory]
    [InlineData(null, "image/jpeg")]
    [InlineData("", "image/jpeg")]
    [InlineData("../poster.jpg", "image/jpeg")]
    [InlineData("poster.png", "image/jpeg")]
    public async Task UploadAsync_RejectsInvalidFileName(string? fileName, string contentType)
    {
        var result = await new CoverService(new InMemoryCoverStorage()).UploadAsync(
            fileName,
            contentType,
            new MemoryStream([1]),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("validation_failed", result.Error!.Code);
        Assert.Contains("fileName", result.Error.Errors!);
    }

    [Fact]
    public async Task UploadAsync_RejectsEmptyFile()
    {
        var result = await new CoverService(new InMemoryCoverStorage()).UploadAsync(
            "poster.png",
            "image/png",
            new MemoryStream(),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("file", result.Error!.Errors!);
    }

    [Fact]
    public async Task UploadAsync_RejectsFileLargerThanLimit()
    {
        var content = new byte[CoverService.MaximumFileSize + 1];

        var result = await new CoverService(new InMemoryCoverStorage()).UploadAsync(
            "poster.webp",
            "image/webp",
            new MemoryStream(content),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("payload_too_large", result.Error!.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("poster.jpg")]
    [InlineData("00000000000000000000000000000000.gif")]
    public async Task DownloadAsync_RejectsInvalidStoredName(string? fileName)
    {
        var result = await new CoverService(new InMemoryCoverStorage())
            .DownloadAsync(fileName, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task DownloadAsync_ReturnsNotFoundForMissingCover()
    {
        var result = await new CoverService(new InMemoryCoverStorage()).DownloadAsync(
            $"{new string('a', 32)}.jpg",
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("not_found", result.Error!.Code);
    }
}
