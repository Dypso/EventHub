using TapSystem.Shared.Models;

namespace TapSystem.Api.Services;

public interface ITapMessageProcessor
{
    ValueTask ProcessAsync(TapMessage message, CancellationToken cancellationToken);
}