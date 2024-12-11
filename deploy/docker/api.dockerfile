# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src

# Optimize for build performance
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_TC_QuickJitForLoops=1 \
    DOTNET_ReadyToRun=1

# Copy project files
COPY ["src/TapSystem.Api/TapSystem.Api.csproj", "src/TapSystem.Api/"]
COPY ["src/TapSystem.Shared/TapSystem.Shared.csproj", "src/TapSystem.Shared/"]
RUN dotnet restore "src/TapSystem.Api/TapSystem.Api.csproj" /p:UseSharedCompilation=false

# Copy source
COPY . .

# Build with optimizations
RUN dotnet publish "src/TapSystem.Api/TapSystem.Api.csproj" -c Release -o /app \
    /p:UseSharedCompilation=false \
    /p:PublishReadyToRun=true \
    /p:PublishSingleFile=false \
    /p:PublishTrimmed=false \
    /p:EnableDynamicLoading=false \
    /p:EnableCompressionInSingleFile=false \
    /p:DebugType=None \
    /p:DebugSymbols=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy
WORKDIR /app

# Runtime optimization
ENV ASPNETCORE_URLS=http://+:80 \
    ASPNETCORE_ENVIRONMENT=Production \
    COMPlus_TieredCompilation=1 \
    COMPlus_TC_QuickJitForLoops=1 \
    COMPlus_ReadyToRun=1 \
    COMPlus_ThreadPool_ForceMinWorkerThreads=200 \
    COMPlus_ThreadPool_ForceMaxWorkerThreads=2000 \
    DOTNET_GCHeapCount=8 \
    DOTNET_GCHighMemPercent=90 \
    DOTNET_gcServer=1

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy build artifacts
COPY --from=build /app .

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=5s --timeout=3s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "TapSystem.Api.dll"]