using System.Text.Json;
using Connect.Contracts.States;

namespace Connect.ValidationTests;

public sealed class SerializationSizeTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(100, 20_000)]
    [InlineData(1_000, 200_000)]
    public void QueueSerializationSize_IsMeasuredAndBounded(int itemCount, int maximumBytes)
    {
        QueueStateDto queue = Queue(itemCount);

        int bytes = JsonSerializer.SerializeToUtf8Bytes(queue).Length;

        bytes.ShouldBeLessThan(maximumBytes);
    }

    [Fact]
    public void FullSnapshotAtQueueLimit_HasDocumentedSizeBoundary()
    {
        var snapshot = new ConnectSnapshot(
            Guid.NewGuid(),
            new PlayerStateDto(false, 0, Now, 50, 1),
            Queue(1_000),
            new PresenceStateDto(
                [
                    new DevicePresenceDto(
                        Guid.NewGuid(),
                        "Browser",
                        [
                            new ConnectionPresenceDto(
                                "connection-1",
                                Now)
                        ],
                        true)
                ],
                null,
                null,
                1),
            Now);

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(snapshot);

        bytes.Length.ShouldBeLessThan(250_000);
        ConnectStateInvariantValidator.Validate(snapshot).ShouldBeEmpty();
    }

    private static QueueStateDto Queue(int itemCount) =>
        new(
            Enumerable.Range(0, itemCount)
                .Select(index => new QueueItemDto(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    index))
                .ToArray(),
            null,
            RepeatModeDto.None,
            false,
            1);
}
