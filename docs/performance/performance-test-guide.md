# Performance Testing Guide

This guide explains how to run performance tests for the TapSystem using Bombardier.

## Prerequisites

- Docker Desktop
- Node.js 18+
- PowerShell 7+
- Go (optional, for building Bombardier from source)

## Running Tests

1. Start the system:
   ```powershell
   .\startup.ps1
   ```

2. Run performance tests:
   ```powershell
   .\scripts\run-performance-test.ps1
   ```

   Optional parameters:
   - `-Duration`: Test duration in seconds (default: 30)
   - `-Connections`: Number of concurrent connections (default: 1000)
   - `-Rate`: Target requests per second (default: 30000)

## Test Configuration

The performance test simulates high-throughput tap events using the following configuration:

- HTTP POST requests to `/api/tap`
- JSON payload with tap event data
- Concurrent connections: 1000
- Target rate: 30,000 requests per second
- Test duration: 30 seconds

## Monitoring

During the test, monitor:

1. API response times
2. Oracle AQ queue depth
3. Worker processing rate
4. System resource usage (CPU, memory, network)

## Results Analysis

The test output includes:

- Request rate (requests/sec)
- Latency statistics (min, mean, max, p99)
- HTTP response codes
- Throughput (bytes/sec)

## Troubleshooting

If you encounter issues:

1. Check Docker container logs
2. Verify Oracle AQ connectivity
3. Monitor system resource usage
4. Review API and Worker logs

## Performance Targets

The system should achieve:

- Latency: < 1ms p99
- Throughput: 30,000+ requests/second
- Error rate: < 0.1%