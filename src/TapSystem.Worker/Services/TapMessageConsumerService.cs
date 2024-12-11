using TapSystem.Shared.Models;

namespace TapSystem.Worker.Services;

public sealed class TapMessageConsumerService : BackgroundService
{
    private readonly IOracleAqConsumerService _oracleAqConsumer;
    private readonly IFileOutputService _fileOutput;
    private readonly ILogger<TapMessageConsumerService> _logger;

    public TapMessageConsumerService(
        IOracleAqConsumerService oracleAqConsumer,
        IFileOutputService fileOutput,
        ILogger<TapMessageConsumerService> logger)
    {
        _oracleAqConsumer = oracleAqConsumer;
        _fileOutput = fileOutput;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var messages in _oracleAqConsumer.DequeueMessagesAsync(stoppingToken))
            {
                try
                {
                    await _fileOutput.WriteMessagesAsync(messages, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing dequeued messages");
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Fatal error in message consumer service");
            throw;
        }
    }
}