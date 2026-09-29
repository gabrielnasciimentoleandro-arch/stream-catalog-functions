using StreamCatalog.Functions.Domain;

namespace StreamCatalog.Functions.Repositories;

public interface ICoverStorage
{
    Task UploadAsync(StoredCover cover, CancellationToken cancellationToken);

    Task<StoredCover?> DownloadAsync(string fileName, CancellationToken cancellationToken);
}
