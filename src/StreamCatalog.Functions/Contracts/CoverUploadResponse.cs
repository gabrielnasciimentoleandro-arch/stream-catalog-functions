namespace StreamCatalog.Functions.Contracts;

public sealed record CoverUploadResponse(
    string FileName,
    string OriginalFileName,
    string ContentType,
    long Length,
    string DownloadUrl);
