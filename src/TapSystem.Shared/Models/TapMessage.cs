using System.Runtime.InteropServices;

namespace TapSystem.Shared.Models;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct TapMessage
{
    public readonly Guid MessageId { get; init; }
    public readonly string CardId { get; init; }
    public readonly string StationId { get; init; }
    public readonly TapType TapType { get; init; }
    public readonly DateTime Timestamp { get; init; }
    public readonly ReadOnlyMemory<byte> Payload { get; init; }

    public TapMessage(Guid messageId, string cardId, string stationId, TapType tapType, DateTime timestamp, ReadOnlyMemory<byte> payload)
    {
        MessageId = messageId;
        CardId = cardId;
        StationId = stationId;
        TapType = tapType;
        Timestamp = timestamp;
        Payload = payload;
    }
}

public enum TapType
{
    Entry = 1,
    Exit = 2
}