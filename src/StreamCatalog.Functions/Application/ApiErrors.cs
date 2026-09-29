namespace StreamCatalog.Functions.Application;

public static class ApiErrors
{
    public static ApiError Validation(IReadOnlyDictionary<string, string[]> errors) =>
        new(
            "validation_failed",
            "Validation failed",
            "One or more fields are invalid.",
            errors);

    public static ApiError InvalidJson(string detail) =>
        new("invalid_json", "Invalid JSON", detail);

    public static ApiError NotFound(string resource) =>
        new("not_found", "Resource not found", $"The requested {resource} was not found.");

    public static ApiError UnsupportedMediaType() =>
        new(
            "unsupported_media_type",
            "Unsupported media type",
            "Only image/jpeg, image/png and image/webp are accepted.");

    public static ApiError PayloadTooLarge(long maximumBytes) =>
        new(
            "payload_too_large",
            "Payload too large",
            $"The file cannot exceed {maximumBytes} bytes.");
}
