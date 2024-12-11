using TapSystem.Shared.Models;

namespace TapSystem.Worker.Services;

public interface IOracleAqConsumerService
{
    IAsyncEnumerable<IReadOnlyList<TapMessage>> DequeueMessagesAsync(CancellationToken cancellationToken);
}