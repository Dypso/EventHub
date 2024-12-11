namespace TapSystem.Worker.Infrastructure;

public class FileOutputConfig
{
    public string OutputPath { get; set; } = "output/taps.dat";
    public int MaxFileSize { get; set; } = 1024 * 1024 * 100; // 100MB
    public bool EnableCompression { get; set; } = true;
}