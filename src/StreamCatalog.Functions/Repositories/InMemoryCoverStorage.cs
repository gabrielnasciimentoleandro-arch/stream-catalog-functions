using System.Collections.Concurrent;
using StreamCatalog.Functions.Domain;

namespace StreamCatalog.Functions.Repositories;

public sealed class InMemoryCoverStorage : ICoverStorage
{
    private readonly ConcurrentDictionary<string, StoredCover> _covers =
        new(StringComparer.OrdinalIgnoreCase);

    public Task UploadAsync(StoredCover cover, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cover);
        cancellationToken.ThrowIfCancellationRequested();

        var copy = cover with { Content = [.. cover.Content] };
        if (!_covers.TryAdd(copy.FileName, copy))
        {
            throw new InvalidOperationException($"A cover named '{copy.FileName}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<StoredCover?> DownloadAsync(string fileName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        cancellationToken.ThrowIfCancellationRequested();

        _covers.TryGetValue(fileName, out var cover);
        return Task.FromResult(cover is null ? null : cover with { Content = [.. cover.Content] });
    }
}
