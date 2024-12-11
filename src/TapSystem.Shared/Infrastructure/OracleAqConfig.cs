namespace TapSystem.Shared.Infrastructure;

public class OracleAqConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = "TAP_QUEUE";
    public string QueueTableName { get; set; } = "TAP_QUEUE_TABLE";
    public int BatchSize { get; set; } = 1000;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);
}