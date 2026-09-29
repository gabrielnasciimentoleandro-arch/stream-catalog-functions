using System.Text.RegularExpressions;
using StreamCatalog.Functions.Domain;
using StreamCatalog.Functions.Repositories;

namespace StreamCatalog.Functions.Application;

public sealed partial class CoverService(ICoverStorage storage) : ICoverService
{
    public const long MaximumFileSize = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string[]> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = [".jpg", ".jpeg"],
            ["image/png"] = [".png"],
            ["image/webp"] = [".webp"],
        };

    private static readonly Dictionary<string, string> CanonicalExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
        };

    public async Task<OperationResult<StoredCover>> UploadAsync(
        string? originalFileName,
        string? contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        var normalizedContentType = contentType?.Split(';', 2)[0].Trim();
        if (normalizedContentType is null || !AllowedExtensions.TryGetValue(normalizedContentType, out var extensions))
        {
            return OperationResult.Failure<StoredCover>(ApiErrors.UnsupportedMediaType());
        }

        var fileNameError = ValidateOriginalFileName(originalFileName, extensions);
        if (fileNameError is not null)
        {
            return OperationResult.Failure<StoredCover>(ApiErrors.Validation(fileNameError));
        }

        var bytes = await ReadWithLimitAsync(content, cancellationToken);
        if (bytes is null)
        {
            return OperationResult.Failure<StoredCover>(ApiErrors.PayloadTooLarge(MaximumFileSize));
        }

        if (bytes.Length == 0)
        {
            var errors = new Dictionary<string, string[]>
            {
                ["file"] = ["The uploaded file cannot be empty."],
            };
            return OperationResult.Failure<StoredCover>(ApiErrors.Validation(errors));
        }

        var generatedName = $"{Guid.NewGuid():N}{CanonicalExtensions[normalizedContentType]}";
        var cover = new StoredCover(
            generatedName,
            originalFileName!.Trim(),
            normalizedContentType.ToLowerInvariant(),
            bytes);

        await storage.UploadAsync(cover, cancellationToken);
        return OperationResult.Success<StoredCover>(cover);
    }

    public async Task<OperationResult<StoredCover>> DownloadAsync(
        string? fileName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName) || !StoredFilePattern().IsMatch(fileName))
        {
            var errors = new Dictionary<string, string[]>
            {
                ["fileName"] = ["The cover file name is invalid."],
            };
            return OperationResult.Failure<StoredCover>(ApiErrors.Validation(errors));
        }

        var cover = await storage.DownloadAsync(fileName.ToLowerInvariant(), cancellationToken);
        return cover is null
            ? OperationResult.Failure<StoredCover>(ApiErrors.NotFound("cover"))
            : OperationResult.Success<StoredCover>(cover);
    }

    private static Dictionary<string, string[]>? ValidateOriginalFileName(
        string? originalFileName,
        IReadOnlyCollection<string> allowedExtensions)
    {
        if (string.IsNullOrWhiteSpace(originalFileName) || originalFileName.Trim().Length > 120)
        {
            return new Dictionary<string, string[]>
            {
                ["fileName"] = ["File name must contain between 1 and 120 characters."],
            };
        }

        var trimmed = originalFileName.Trim();
        if (!string.Equals(Path.GetFileName(trimmed), trimmed, StringComparison.Ordinal) ||
            !allowedExtensions.Contains(Path.GetExtension(trimmed), StringComparer.OrdinalIgnoreCase))
        {
            return new Dictionary<string, string[]>
            {
                ["fileName"] = ["File name is unsafe or its extension does not match the content type."],
            };
        }

        return null;
    }

    private static async Task<byte[]?> ReadWithLimitAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        using var destination = new MemoryStream();
        var buffer = new byte[81_920];
        while (true)
        {
            var read = await content.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                return destination.ToArray();
            }

            if (destination.Length + read > MaximumFileSize)
            {
                return null;
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    [GeneratedRegex("^[0-9a-fA-F]{32}\\.(?:jpg|png|webp)$", RegexOptions.CultureInvariant)]
    private static partial Regex StoredFilePattern();
}
