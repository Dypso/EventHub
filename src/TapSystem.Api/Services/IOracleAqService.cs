using TapSystem.Shared.Models;

namespace TapSystem.Api.Services;

public interface IOracleAqService
{
    
     Task EnqueueBatchAsync(ReadOnlySpan<TapMessage> messages, CancellationToken cancellationToken);

}