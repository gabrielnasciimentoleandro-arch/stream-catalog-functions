using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StreamCatalog.Functions.Application;
using StreamCatalog.Functions.Repositories;
using StreamCatalog.Functions.Serialization;

namespace StreamCatalog.Functions.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStreamCatalog(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ICatalogService, CatalogService>();
        services.AddSingleton<ICoverService, CoverService>();
        services.AddSingleton<TokenCredential>(_ => new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                ExcludeInteractiveBrowserCredential = true,
            }));

        var catalogProvider = configuration["Catalog:Provider"] ?? "InMemory";
        var coverProvider = configuration["CoverStorage:Provider"] ?? "InMemory";
        ConfigureCatalogRepository(services, configuration, catalogProvider);
        ConfigureCoverStorage(services, configuration, coverProvider);
        services.AddSingleton(new ProviderSettings(catalogProvider, coverProvider));
        return services;
    }

    private static void ConfigureCatalogRepository(
        IServiceCollection services,
        IConfiguration configuration,
        string provider)
    {
        if (provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ICatalogRepository, InMemoryCatalogRepository>();
            return;
        }

        if (!provider.Equals("Cosmos", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Catalog:Provider must be either InMemory or Cosmos.");
        }

        var databaseName = Require(configuration, "Cosmos:DatabaseName");
        var containerName = Require(configuration, "Cosmos:ContainerName");
        var clientOptions = new CosmosClientOptions
        {
            ApplicationName = "StreamCatalog.Functions",
            ConnectionMode = ConnectionMode.Direct,
            Serializer = new CosmosSystemTextJsonSerializer(ApiJson.Options),
        };

        var connectionString = configuration["Cosmos:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton(new CosmosClient(connectionString, clientOptions));
        }
        else
        {
            var endpoint = new Uri(Require(configuration, "Cosmos:Endpoint"), UriKind.Absolute);
            services.AddSingleton(serviceProvider => new CosmosClient(
                endpoint.ToString(),
                serviceProvider.GetRequiredService<TokenCredential>(),
                clientOptions));
        }

        services.AddSingleton(serviceProvider =>
            serviceProvider
                .GetRequiredService<CosmosClient>()
                .GetContainer(databaseName, containerName));
        services.AddSingleton<ICatalogRepository, CosmosCatalogRepository>();
    }

    private static void ConfigureCoverStorage(
        IServiceCollection services,
        IConfiguration configuration,
        string provider)
    {
        if (provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ICoverStorage, InMemoryCoverStorage>();
            return;
        }

        if (!provider.Equals("Blob", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "CoverStorage:Provider must be either InMemory or Blob.");
        }

        var containerName = Require(configuration, "BlobStorage:ContainerName");
        var connectionString = configuration["BlobStorage:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton(new BlobServiceClient(connectionString));
        }
        else
        {
            var serviceUri = new Uri(
                Require(configuration, "BlobStorage:ServiceUri"),
                UriKind.Absolute);
            services.AddSingleton(serviceProvider => new BlobServiceClient(
                serviceUri,
                serviceProvider.GetRequiredService<TokenCredential>()));
        }

        services.AddSingleton(serviceProvider =>
            serviceProvider
                .GetRequiredService<BlobServiceClient>()
                .GetBlobContainerClient(containerName));
        services.AddSingleton<ICoverStorage, BlobCoverStorage>();
    }

    private static string Require(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Configuration '{key}' is required.")
            : value;
    }
}
