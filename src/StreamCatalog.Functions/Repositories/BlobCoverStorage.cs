using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using StreamCatalog.Functions.Domain;

namespace StreamCatalog.Functions.Repositories;

public sealed class BlobCoverStorage(BlobContainerClient container) : ICoverStorage
{
    private const string OriginalNameMetadataKey = "originalName";

    public async Task UploadAsync(StoredCover cover, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cover);

        await container.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(cover.FileName);
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = cover.ContentType },
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [OriginalNameMetadataKey] = Uri.EscapeDataString(cover.OriginalFileName),
            },
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All },
        };

        await using var content = new MemoryStream(cover.Content, writable: false);
        await blob.UploadAsync(content, options, cancellationToken);
    }

    public async Task<StoredCover?> DownloadAsync(
        string fileName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        try
        {
            var result = await container
                .GetBlobClient(fileName)
                .DownloadContentAsync(cancellationToken);
            var details = result.Value.Details;
            var originalName = details.Metadata.TryGetValue(OriginalNameMetadataKey, out var encodedName)
                ? Uri.UnescapeDataString(encodedName)
                : fileName;

            return new StoredCover(
                fileName,
                originalName,
                details.ContentType ?? "application/octet-stream",
                result.Value.Content.ToArray());
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }
}
