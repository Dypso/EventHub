using Microsoft.AspNetCore.Server.Kestrel.Core;
using TapSystem.Shared.Infrastructure;
using TapSystem.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for maximum performance
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(80); // Écouter explicitement sur toutes les interfaces
    options.Limits.MaxConcurrentConnections = 100_000;
    options.Limits.MaxConcurrentUpgradedConnections = 100_000;
    options.Limits.MaxRequestBodySize = 1024;
    options.Limits.MinRequestBodyDataRate = null;
    options.Limits.MinResponseDataRate = null;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(1);

    options.ConfigureEndpointDefaults(endpoint =>
    {
        endpoint.Protocols = HttpProtocols.Http1;
    });

    // Set advanced options
    options.AllowSynchronousIO = false;
    options.AddServerHeader = false;
});

// Configure thread pool
ThreadPool.SetMinThreads(Environment.ProcessorCount * 4, Environment.ProcessorCount * 4);

// Configure services
builder.Services.Configure<OracleAqConfig>(
    builder.Configuration.GetSection("OracleAq"));

builder.Services.AddSingleton<ITapMessageProcessor, TapMessageProcessor>();
builder.Services.AddSingleton<IOracleAqService, OracleAqService>();

builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// Ajout d'un endpoint de santé simple
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();