using System.Threading.Channels;
using TapSystem.Shared.Infrastructure;
using TapSystem.Shared.Models;
using System.Runtime.CompilerServices;

namespace TapSystem.Api.Services;

public sealed class TapMessageProcessor : ITapMessageProcessor, IDisposable
{
    private readonly Channel<TapMessage> _channel;
    private readonly IOracleAqService _oracleAqService;
    private readonly ILogger<TapMessageProcessor> _logger;
    private readonly CancellationTokenSource _cts;
    private readonly Task _processingTask;
    private readonly TapMessage[] _messageBuffer;
    
    private const int BatchSize = 1000;
    private const int ChannelCapacity = 100_000;

    public TapMessageProcessor(
        IOracleAqService oracleAqService,
        ILogger<TapMessageProcessor> logger)
    {
        _oracleAqService = oracleAqService;
        _logger = logger;
        _cts = new CancellationTokenSource();
        _messageBuffer = new TapMessage[BatchSize];

        var options = new BoundedChannelOptions(ChannelCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = true,
            FullMode = BoundedChannelFullMode.Wait
        };

        _channel = Channel.CreateBounded<TapMessage>(options);
        _processingTask = ProcessMessagesAsync(_cts.Token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask ProcessAsync(TapMessage message, CancellationToken cancellationToken)
    {
        // TryWrite est plus rapide que WriteAsync quand possible
        if (_channel.Writer.TryWrite(message))
        {
            return default;
        }

        // Si TryWrite échoue, on bascule sur WriteAsync
        return new ValueTask(_channel.Writer.WriteAsync(message, cancellationToken).AsTask());
    }

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        int count = 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    while (await _channel.Reader.WaitToReadAsync(cancellationToken))
                    {
                        // Lecture par lots des messages
                        while (count < BatchSize && _channel.Reader.TryRead(out var message))
                        {
                            _messageBuffer[count++] = message;
                        }

                        if (count > 0)
                        {
                            try
                            {
                                await _oracleAqService.EnqueueBatchAsync(
                                    new ReadOnlySpan<TapMessage>(_messageBuffer, 0, count), 
                                    cancellationToken);

                                count = 0;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Erreur lors de l'envoi du batch de {Count} messages", count);
                                // En cas d'erreur, on réinitialise le compteur pour éviter de réessayer les mêmes messages
                                count = 0;
                                // Petite pause avant de réessayer pour éviter de surcharger le système
                                await Task.Delay(100, cancellationToken);
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Erreur dans la boucle de traitement des messages");
                    // Réinitialisation du compteur en cas d'erreur
                    count = 0;
                    // Petite pause avant de réessayer
                    await Task.Delay(100, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Arrêt normal du traitement
            _logger.LogInformation("Arrêt du traitement des messages");
        }
        finally
        {
            // Traitement des derniers messages en attente si possible
            if (count > 0 && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _oracleAqService.EnqueueBatchAsync(
                        new ReadOnlySpan<TapMessage>(_messageBuffer, 0, count),
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors du traitement final de {Count} messages", count);
                }
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            // Attente de la fin du traitement avec timeout
            if (!_processingTask.Wait(TimeSpan.FromSeconds(5)))
            {
                _logger.LogWarning("Le traitement des messages n'a pas pu se terminer proprement dans le délai imparti");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'arrêt du processor");
        }
        finally
        {
            _cts.Dispose();
        }
    }
}
