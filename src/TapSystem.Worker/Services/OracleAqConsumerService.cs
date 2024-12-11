using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using TapSystem.Shared.Infrastructure;
using TapSystem.Shared.Models;

namespace TapSystem.Worker.Services;

public sealed class OracleAqConsumerService : IOracleAqConsumerService, IDisposable
{
    private readonly OracleConnection _connection;
    private readonly string _queueName;
    private readonly int _batchSize;
    private readonly ILogger<OracleAqConsumerService> _logger;

    public OracleAqConsumerService(
        IOptions<OracleAqConfig> config,
        ILogger<OracleAqConsumerService> logger)
    {
        _connection = new OracleConnection(config.Value.ConnectionString);
        _queueName = config.Value.QueueName;
        _batchSize = config.Value.BatchSize;
        _logger = logger;

        _connection.Open();
    }

public async IAsyncEnumerable<IReadOnlyList<TapMessage>> DequeueMessagesAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    var messages = new List<TapMessage>(_batchSize);
    
    while (!cancellationToken.IsCancellationRequested)
    {
        var batch = await SafeDequeueMessagesAsync(messages, cancellationToken);
        if (batch != null)
        {
            yield return batch;
        }
    }
}

private async Task<IReadOnlyList<TapMessage>?> SafeDequeueMessagesAsync(
    List<TapMessage> messages, 
    CancellationToken cancellationToken)
{
    try
    {
        messages.Clear();
        using var transaction = _connection.BeginTransaction();
        using var command = _connection.CreateCommand();
        command.BindByName = true;
        command.CommandText = $@"
            BEGIN
                DBMS_AQ.DEQUEUE(
                    queue_name => '{_queueName}',
                    dequeue_options => DBMS_AQ.DEQUEUE_OPTIONS_T,
                    message_properties => DBMS_AQ.MESSAGE_PROPERTIES_T,
                    payload => :payload,
                    msgid => :msgid
                );
            END;";
        
        var payloadParam = command.Parameters.Add(":payload", OracleDbType.Raw);
        var msgIdParam = command.Parameters.Add(":msgid", OracleDbType.Raw);
        
        for (int i = 0; i < _batchSize; i++)
        {
            if (await TryDequeueMessageAsync(command, payloadParam, msgIdParam, messages, cancellationToken))
            {
                continue;
            }
            break;
        }
        
        if (messages.Count > 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return messages.ToArray();
        }
        
        await transaction.RollbackAsync(cancellationToken);
        await Task.Delay(100, cancellationToken);
        return null;
    }
    catch (OperationCanceledException)
    {
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error dequeuing messages from Oracle AQ");
        await Task.Delay(1000, cancellationToken);
        return null;
    }
}
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task<bool> TryDequeueMessageAsync(
        OracleCommand command,
        OracleParameter payloadParam,
        OracleParameter msgIdParam,
        List<TapMessage> messages,
        CancellationToken cancellationToken)
    {
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            
            var payload = (byte[])payloadParam.Value;
            var msgId = new Guid((byte[])msgIdParam.Value);

            // In a real implementation, deserialize the payload to get the full message
            // This is a simplified example
            messages.Add(new TapMessage(msgId, "card123", "station456", TapType.Entry, DateTime.UtcNow, payload));
            
            return true;
        }
        catch (OracleException ex) when (ex.Number == 25228) // No message found
        {
            return false;
        }
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}