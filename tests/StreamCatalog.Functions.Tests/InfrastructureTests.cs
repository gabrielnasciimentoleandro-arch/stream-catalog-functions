using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Contracts;
using StreamCatalog.Functions.Domain;
using StreamCatalog.Functions.Infrastructure;
using StreamCatalog.Functions.Repositories;
using StreamCatalog.Functions.Serialization;

namespace StreamCatalog.Functions.Tests;

public sealed class InfrastructureTests
{
    [Fact]
    public void CosmosSerializer_RoundTripsCatalogItem()
    {
        var serializer = new CosmosSystemTextJsonSerializer(ApiJson.Options);
        var item = new CatalogItem
        {
            Id = new string('a', 32),
            Title = "Arrival",
            Synopsis = "A sufficiently detailed synopsis.",
            Type = CatalogItemType.Movie,
            Genres = ["Drama"],
            ReleaseYear = 2016,
            AgeRating = "12",
            CreatedAt = TestSupport.Now,
        };

        using var stream = serializer.ToStream(item);
        var json = new StreamReader(stream, leaveOpen: true).ReadToEnd();
        Assert.Contains("\"type\":\"movie\"", json);
        stream.Position = 0;

        var deserialized = serializer.FromStream<CatalogItem>(stream);
        Assert.Equal(item.Id, deserialized.Id);
        Assert.Equal(item.Title, deserialized.Title);
        Assert.Equal(item.Synopsis, deserialized.Synopsis);
        Assert.Equal(item.Type, deserialized.Type);
        Assert.Equal(item.Genres, deserialized.Genres);
        Assert.Equal(item.ReleaseYear, deserialized.ReleaseYear);
        Assert.Equal(item.AgeRating, deserialized.AgeRating);
        Assert.Equal(item.CreatedAt, deserialized.CreatedAt);
    }

    [Fact]
    public void CosmosSerializer_ReturnsStreamWithoutDisposingIt()
    {
        var serializer = new CosmosSystemTextJsonSerializer(ApiJson.Options);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("{}"));

        var returned = serializer.FromStream<Stream>(stream);

        Assert.Same(stream, returned);
        Assert.True(returned.CanRead);
        returned.Dispose();
    }

    [Fact]
    public void CosmosSerializer_RejectsJsonNullForNonNullableObject()
    {
        var serializer = new CosmosSystemTextJsonSerializer(ApiJson.Options);
        var stream = new MemoryStream("null"u8.ToArray());

        Assert.Throws<JsonException>(() => serializer.FromStream<CatalogItem>(stream));
    }

    [Fact]
    public void CosmosSerializer_RequiresOptions()
    {
        Assert.Throws<ArgumentNullException>(() => new CosmosSystemTextJsonSerializer(null!));
    }

    [Fact]
    public void AddStreamCatalog_RegistersInMemoryProvidersByDefault()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        services.AddStreamCatalog(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<InMemoryCatalogRepository>(provider.GetRequiredService<ICatalogRepository>());
        Assert.IsType<InMemoryCoverStorage>(provider.GetRequiredService<ICoverStorage>());
        Assert.IsType<CatalogService>(provider.GetRequiredService<ICatalogService>());
        Assert.IsType<CoverService>(provider.GetRequiredService<ICoverService>());
    }

    [Theory]
    [InlineData("Unknown", "InMemory")]
    [InlineData("InMemory", "Unknown")]
    public void AddStreamCatalog_RejectsUnknownProvider(string catalog, string cover)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Catalog:Provider"] = catalog,
            ["CoverStorage:Provider"] = cover,
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddStreamCatalog(configuration));

        Assert.Contains("Provider", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddStreamCatalog_RequiresCosmosConfiguration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Catalog:Provider"] = "Cosmos",
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddStreamCatalog(configuration));

        Assert.Contains("Cosmos:DatabaseName", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddStreamCatalog_RequiresBlobConfiguration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Catalog:Provider"] = "InMemory",
            ["CoverStorage:Provider"] = "Blob",
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddStreamCatalog(configuration));

        Assert.Contains("BlobStorage:ContainerName", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddStreamCatalog_RegistersCosmosWithManagedIdentityConfiguration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Catalog:Provider"] = "Cosmos",
            ["Cosmos:Endpoint"] = "https://localhost:8081/",
            ["Cosmos:DatabaseName"] = "stream-catalog",
            ["Cosmos:ContainerName"] = "items",
            ["CoverStorage:Provider"] = "InMemory",
        });
        var services = new ServiceCollection();

        services.AddStreamCatalog(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<CosmosCatalogRepository>(provider.GetRequiredService<ICatalogRepository>());
        Assert.Equal("Cosmos", provider.GetRequiredService<ProviderSettings>().Catalog);
    }

    [Fact]
    public void AddStreamCatalog_RegistersCosmosWithConnectionString()
    {
        var key = Convert.ToBase64String(new byte[64]);
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Catalog:Provider"] = "Cosmos",
            ["Cosmos:ConnectionString"] = $"AccountEndpoint=https://localhost:8081/;AccountKey={key};",
            ["Cosmos:DatabaseName"] = "stream-catalog",
            ["Cosmos:ContainerName"] = "items",
            ["CoverStorage:Provider"] = "InMemory",
        });
        var services = new ServiceCollection();

        services.AddStreamCatalog(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<CosmosCatalogRepository>(provider.GetRequiredService<ICatalogRepository>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddStreamCatalog_RegistersBlobProvider(bool useConnectionString)
    {
        var values = new Dictionary<string, string?>
        {
            ["Catalog:Provider"] = "InMemory",
            ["CoverStorage:Provider"] = "Blob",
            ["BlobStorage:ContainerName"] = "covers",
        };
        values[useConnectionString
            ? "BlobStorage:ConnectionString"
            : "BlobStorage:ServiceUri"] = useConnectionString
                ? "UseDevelopmentStorage=true"
                : "https://streamcatalog.blob.core.windows.net/";
        var configuration = BuildConfiguration(values);
        var services = new ServiceCollection();

        services.AddStreamCatalog(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.IsType<BlobCoverStorage>(provider.GetRequiredService<ICoverStorage>());
        Assert.Equal("Blob", provider.GetRequiredService<ProviderSettings>().CoverStorage);
    }

    [Fact]
    public void ApiJson_DisallowsUnknownPropertiesAndNumericEnums()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CreateCatalogItemRequest>(
            "{\"unknown\":true}",
            ApiJson.Options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CreateCatalogItemRequest>(
            "{\"type\":0}",
            ApiJson.Options));
    }

    private static IConfiguration BuildConfiguration(
        IDictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
