using System.Text.Json;
using Connect.Application.Results;

namespace Connect.Application.Common;

internal static class CommandOutcomeSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Serialize(ConnectCommandOutcome outcome) =>
        JsonSerializer.Serialize(outcome, Options);

    public static ConnectCommandOutcome? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ConnectCommandOutcome>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
