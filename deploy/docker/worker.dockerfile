# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src

# Install native AOT dependencies
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang \
    zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

# Optimize for build performance
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_TC_QuickJitForLoops=1 \
    DOTNET_ReadyToRun=1

# Copy project files
COPY ["src/TapSystem.Worker/TapSystem.Worker.csproj", "src/TapSystem.Worker/"]
COPY ["src/TapSystem.Shared/TapSystem.Shared.csproj", "src/TapSystem.Shared/"]
RUN dotnet restore "src/TapSystem.Worker/TapSystem.Worker.csproj" /p:UseSharedCompilation=false

# Copy source
COPY . .

# Build with AOT optimizations
RUN dotnet publish "src/TapSystem.Worker/TapSystem.Worker.csproj" -c Release -o /app \
    /p:UseSharedCompilation=false \
    /p:PublishAot=true \
    /p:PublishTrimmed=true \
    /p:EnableDynamicLoading=false \
    /p:EnableCompressionInSingleFile=false \
    /p:DebugType=None \
    /p:DebugSymbols=false \
    /p:InvariantGlobalization=true \
    /p:IlcGenerateStackTraceData=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-jammy-chiseled AS final
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

USER app

ENTRYPOINT ["./TapSystem.Worker"]