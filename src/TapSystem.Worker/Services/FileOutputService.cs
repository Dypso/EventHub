using System.Buffers;
using System.IO.Pipelines;
using Microsoft.Extensions.Options;
using TapSystem.Shared.Models;
using TapSystem.Worker.Infrastructure;

namespace TapSystem.Worker.Services;

public sealed class FileOutputService : IFileOutputService
{
    private readonly string _outputPath;
    private readonly ILogger<FileOutputService> _logger;
    private readonly SemaphoreSlim _writeLock;

    public FileOutputService(
        IOptions<FileOutputConfig> config,
        ILogger<FileOutputService> logger)
    {
        _outputPath = config.Value.OutputPath;
        _logger = logger;
        _writeLock = new SemaphoreSlim(1, 1);

        Directory.CreateDirectory(Path.GetDirectoryName(_outputPath)!);
    }

    public async ValueTask WriteMessagesAsync(IReadOnlyList<TapMessage> messages, CancellationToken cancellationToken)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await using var fileStream = new FileStream(
                _outputPath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            var pipe = new Pipe(new PipeOptions(
                pool: MemoryPool<byte>.Shared,
                minimumSegmentSize: 4096,
                pauseWriterThreshold: 1024 * 1024));

            await WriteMessagesToPipeAsync(messages, pipe.Writer, cancellationToken);
            await pipe.Reader.CopyToAsync(fileStream, cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static async ValueTask WriteMessagesToPipeAsync(
        IReadOnlyList<TapMessage> messages,
        PipeWriter writer,
        CancellationToken cancellationToken)
    {
        foreach (var message in messages)
        {
            var memory = writer.GetMemory(4096);
            var position = 0;

            position += WriteString(memory[position..], message.MessageId.ToString());
            position += WriteString(memory[position..], message.CardId);
            position += WriteString(memory[position..], message.StationId);
            position += WriteInt32(memory[position..], (int)message.TapType);
            position += WriteDateTime(memory[position..], message.Timestamp);
            position += WriteBytes(memory[position..], message.Payload.Span);

            writer.Advance(position);
        }

        await writer.FlushAsync(cancellationToken);
        await writer.CompleteAsync();
    }

    private static int WriteString(Memory<byte> buffer, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        WriteInt32(buffer, bytes.Length);
        bytes.CopyTo(buffer[4..]);
        return 4 + bytes.Length;
    }

    private static int WriteInt32(Memory<byte> buffer, int value)
    {
        BitConverter.TryWriteBytes(buffer.Span, value);
        return 4;
    }

    private static int WriteDateTime(Memory<byte> buffer, DateTime value)
    {
        BitConverter.TryWriteBytes(buffer.Span, value.ToBinary());
        return 8;
    }

    private static int WriteBytes(Memory<byte> buffer, ReadOnlySpan<byte> value)
    {
        WriteInt32(buffer, value.Length);
        value.CopyTo(buffer[4..].Span);
        return 4 + value.Length;
    }
}