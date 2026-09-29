using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace StreamCatalog.Functions.Infrastructure;

public sealed class CosmosSystemTextJsonSerializer(JsonSerializerOptions options) : CosmosSerializer
{
    private readonly JsonSerializerOptions _options =
        options ?? throw new ArgumentNullException(nameof(options));

    public override T FromStream<T>(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (typeof(Stream).IsAssignableFrom(typeof(T)))
        {
            return (T)(object)stream;
        }

        using (stream)
        {
            return JsonSerializer.Deserialize<T>(stream, _options)
                ?? throw new JsonException($"Unable to deserialize {typeof(T).Name}.");
        }
    }

    public override Stream ToStream<T>(T input)
    {
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, input, _options);
        stream.Position = 0;
        return stream;
    }
}
