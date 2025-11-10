# AdminTool - .NET 8 Enterprise API

A high-performance Web API demonstrating Clean Architecture, CQRS, Dual-DB Routing, and Transactional Outbox patterns.

## Architecture Patterns

- Clean Architecture - Domain-centric design
- CQRS with MediatR - Separate read/write operations  
- Aggregate Root Pattern - Single controller per domain
- Dual-DB Routing - Primary/secondary via headers
- Transactional Outbox - Eventual consistency
- Database-Level Pagination - Two-phase queries

## Project Structure

```
AdminTool/
├── bvlwebtools.sln
├── src/
│   ├── abc.bvl.AdminTool.Api/
│   ├── abc.bvl.AdminTool.Application/
│   ├── abc.bvl.AdminTool.Contracts/
│   ├── abc.bvl.AdminTool.Domain/
│   ├── abc.bvl.AdminTool.Infrastructure.Data/
│   └── abc.bvl.AdminTool.Infrastructure.Replication/
├── benchmarks/
│   └── abc.bvl.AdminTool.Benchmarks/
└── tests/
    └── abc.bvl.AdminTool.MSTests/
```

## Core Domain

**Entities:**
- ScreenDefinition - Screen metadata
- ScreenPilot - User access assignments  
- OutboxMessage - Replication events

**Aggregate Root:**
- PilotEnablement - Complete screen access workflow


## Performance Benchmarks

### Pagination (10,000 records)

| Strategy    | Time    | Memory  | vs Baseline |
|-------------|---------|---------|-------------|
| Traditional | 36.9 ms | 23.8 MB | Baseline    |
| Optimized   | 22.3 ms | 12.5 MB | 40% faster  |
| Hybrid      | 10.0 ms | 4.0 MB  | 73% faster  |

See benchmarks/README.md for details.

## Configuration

### Database Routing

```http
X-Database-Route: primary
X-Database-Route: secondary
```

### Outbox Replication

```csharp
services.AddOutboxReplication(options => {
    options.PollingIntervalSeconds = 5;
    options.BatchSize = 100;
});
```


## Testing

```bash
# Unit tests
dotnet test

# Benchmarks
cd benchmarks/abc.bvl.AdminTool.Benchmarks
dotnet run -c Release
```

## Documentation

- Benchmark Guide: benchmarks/README.md