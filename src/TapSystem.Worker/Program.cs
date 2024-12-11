using TapSystem.Shared.Infrastructure;
using TapSystem.Worker.Infrastructure;
using TapSystem.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure services
builder.Services.Configure<OracleAqConfig>(
    builder.Configuration.GetSection("OracleAq"));

builder.Services.Configure<FileOutputConfig>(
    builder.Configuration.GetSection("FileOutput"));

builder.Services.AddSingleton<IOracleAqConsumerService, OracleAqConsumerService>();
builder.Services.AddSingleton<IFileOutputService, FileOutputService>();
builder.Services.AddHostedService<TapMessageConsumerService>();

// Configure thread pool for optimal performance
ThreadPool.SetMinThreads(Environment.ProcessorCount * 4, Environment.ProcessorCount * 4);

var host = builder.Build();
host.Run();