using StreamCatalog.Functions.Domain;

namespace StreamCatalog.Functions.Application;

public interface ICoverService
{
    Task<OperationResult<StoredCover>> UploadAsync(
        string? originalFileName,
        string? contentType,
        Stream content,
        CancellationToken cancellationToken);

    Task<OperationResult<StoredCover>> DownloadAsync(
        string? fileName,
        CancellationToken cancellationToken);
}
