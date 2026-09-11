using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Connect.Application.Common;

internal static class DeterministicShuffle
{
    public static Guid[] CreateOrder(IEnumerable<Guid> queueItemIds, int seed) =>
    [
        .. queueItemIds
            .Select(id => (Id: id, Key: CreateKey(id, seed)))
            .OrderBy(item => item.Key, ByteArrayComparer.Instance)
            .ThenBy(item => item.Id)
            .Select(item => item.Id)
    ];

    private static byte[] CreateKey(Guid id, int seed)
    {
        Span<byte> payload = stackalloc byte[20];
        id.TryWriteBytes(payload);
        BinaryPrimitives.WriteInt32BigEndian(payload[16..], seed);
        return SHA256.HashData(payload);
    }

    private sealed class ByteArrayComparer : IComparer<byte[]>
    {
        public static readonly ByteArrayComparer Instance = new();

        public int Compare(byte[]? left, byte[]? right) =>
            left.AsSpan().SequenceCompareTo(right);
    }
}
