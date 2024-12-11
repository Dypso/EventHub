using TapSystem.Shared.Models;

namespace TapSystem.Worker.Services;

public interface IFileOutputService
{
    ValueTask WriteMessagesAsync(IReadOnlyList<TapMessage> messages, CancellationToken cancellationToken);
}