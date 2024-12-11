using System.Text.Json.Serialization;
//using TapSystem.Shared.Models;

namespace TapSystem.Shared.Models;

public record TapRequest
{
    [JsonPropertyName("cardId")]
    public required string CardId { get; init; }

    [JsonPropertyName("stationId")]
    public required string StationId { get; init; }

    [JsonPropertyName("tapType")]
    public TapType TapType { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }

    [JsonPropertyName("payload")]
    public ReadOnlyMemory<byte>? Payload { get; init; }
}