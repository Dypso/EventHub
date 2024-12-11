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

# Build with optimizations for API
RUN dotnet publish "src/TapSystem.Api/TapSystem.Api.csproj" -c Release -o /app \
    /p:UseSharedCompilation=false \
    /p:PublishReadyToRun=true \
    /p:PublishSingleFile=true \
    /p:PublishTrimmed=true \
    /p:EnableDynamicLoading=false \
    /p:EnableCompressionInSingleFile=false \
    /p:DebugType=None \
    /p:DebugSymbols=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled
WORKDIR /app

# Runtime optimization
ENV COMPlus_EnableDiagnostics=0 \
    COMPlus_TieredCompilation=1 \
    COMPlus_TC_QuickJitForLoops=1 \
    COMPlus_ReadyToRun=1 \
    COMPlus_ThreadPool_ForceMinWorkerThreads=100 \
    COMPlus_ThreadPool_ForceMaxWorkerThreads=1000 \
    DOTNET_GCHeapCount=8 \
    DOTNET_GCHighMemPercent=90 \
    DOTNET_GCConserveMemory=0

COPY --from=build /app .

# Network configuration 
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

USER app

HEALTHCHECK --interval=5s --timeout=3s --retries=3 \
    CMD wget -q --spider http://localhost/health || exit 1

ENTRYPOINT ["./TapSystem.Api"]