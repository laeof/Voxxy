using System.Text.Json;
using System.Text.Json.Serialization;

namespace Connect.Infrastructure.Redis.Serialization;

internal sealed class ConnectRedisSerializer
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        NumberHandling = JsonNumberHandling.Strict,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, _options);

    public bool TryDeserialize<T>(string json, out T? value, out string? error)
    {
        try
        {
            value = JsonSerializer.Deserialize<T>(json, _options);
            if (value is null)
            {
                error = "Redis state contains JSON null.";
                return false;
            }

            error = null;
            return true;
        }
        catch (JsonException exception)
        {
            value = default;
            error = exception.Message;
            return false;
        }
    }
}
