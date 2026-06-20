using System.Text.Json;

namespace Domain.OutboxMessages;

public class OutboxMessage
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;

    public DateTime OccurredAtUtc { get; init; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? Error { get; private set; }

    public static OutboxMessage Create<T>(T message)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(T).FullName!,
            Content = JsonSerializer.Serialize(message),
            OccurredAtUtc = DateTime.UtcNow
        };
    }

    public void MarkProcessed()
    {
        ProcessedAtUtc = DateTime.UtcNow;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        Error = error;
    }
}