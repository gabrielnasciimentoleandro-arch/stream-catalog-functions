namespace StreamCatalog.Functions.Domain;

public sealed record StoredCover(
    string FileName,
    string OriginalFileName,
    string ContentType,
    byte[] Content);
