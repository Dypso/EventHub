using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using TapSystem.Shared.Infrastructure;
using TapSystem.Shared.Models;

namespace TapSystem.Api.Services;





public sealed class OracleAqService : IOracleAqService, IDisposable
{
    private readonly OracleConnection _connection;
    private readonly string _queueName;
    private readonly ILogger<OracleAqService> _logger;
    private readonly SemaphoreSlim _semaphore;

    public OracleAqService(
        IOptions<OracleAqConfig> config,
        ILogger<OracleAqService> logger)
    {
        _connection = new OracleConnection(config.Value.ConnectionString);
        _queueName = config.Value.QueueName;
        _logger = logger;
        _semaphore = new SemaphoreSlim(1, 1);

        _connection.Open();
    }

    public async Task EnqueueBatchAsync(ReadOnlySpan<TapMessage> messages, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            using var transaction = _connection.BeginTransaction();
            using var command = _connection.CreateCommand();
            
            command.BindByName = true;
            command.CommandText = $"BEGIN DBMS_AQ.ENQUEUE(queue_name => '{_queueName}', enqueue_options => DBMS_AQ.ENQUEUE_OPTIONS_T, message_properties => DBMS_AQ.MESSAGE_PROPERTIES_T, payload => :payload, msgid => :msgid); END;";
            
            var payloadParam = command.Parameters.Add(":payload", OracleDbType.Raw);
            var msgIdParam = command.Parameters.Add(":msgid", OracleDbType.Raw);

            foreach (var message in messages)
            {
                await EnqueueMessageAsync(command, payloadParam, msgIdParam, message, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueueing messages to Oracle AQ");
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async ValueTask EnqueueMessageAsync(
        OracleCommand command,
        OracleParameter payloadParam,
        OracleParameter msgIdParam,
        TapMessage message,
        CancellationToken cancellationToken)
    {
        payloadParam.Value = message.Payload.ToArray();
        msgIdParam.Value = message.MessageId.ToByteArray();
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public void Dispose()
    {
        _connection.Dispose();
        _semaphore.Dispose();
    }
}