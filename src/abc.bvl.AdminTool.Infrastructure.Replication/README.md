# Outbox Pattern Implementation

## Overview
This project implements the **Transactional Outbox Pattern** to ensure eventual consistency between primary and secondary databases in a dual-database architecture.

## Architecture

### Components

1. **OutboxMessage Entity** (`Domain/Entities/OutboxMessage.cs`)
   - Stores replication events in the primary database
   - Properties: Type, EntityId, Operation, Payload, Status, RetryCount, etc.

2. **IOutboxRepository** (`Infrastructure.Replication/Interfaces/`)
   - GetPendingMessagesAsync: Queries messages with Status="Pending"
   - GetRetryableMessagesAsync: Queries failed messages eligible for retry
   - MarkAsProcessedAsync: Updates message to Status="Completed"
   - MarkAsFailedAsync: Updates message to Status="Failed", increments RetryCount

3. **IOutboxPublisher** (`Infrastructure.Replication/Interfaces/`)
   - PublishAsync: Adds single event to outbox
   - PublishBatchAsync: Adds multiple events atomically

4. **OutboxProcessor** (`Infrastructure.Replication/Services/`)
   - Background service (IHostedService) that polls the outbox table
   - Processes messages in batches
   - Replicates to secondary database
   - Handles retries with exponential backoff

## Configuration

Add to `appsettings.json`:

```json
{
  "Outbox": {
    "Enabled": true,
    "PollingIntervalSeconds": 10,
    "BatchSize": 100,
    "MaxRetryCount": 3,
    "RetryDelayMinutes": 5,
    "SecondaryConnectionString": "${ADMIN_DB_SECONDARY_CONNECTION}"
  }
}
```

### Settings

| Setting | Description | Default |
|---------|-------------|---------|
| `Enabled` | Enable/disable the outbox processor | `true` |
| `PollingIntervalSeconds` | Interval between polling cycles | `10` |
| `BatchSize` | Max messages to process per batch | `100` |
| `MaxRetryCount` | Maximum retry attempts for failed messages | `3` |
| `RetryDelayMinutes` | Delay before retrying failed messages | `5` |
| `SecondaryConnectionString` | Connection string for secondary DB | - |

## Registration

In `Program.cs`:

```csharp
using abc.bvl.AdminTool.Infrastructure.Replication.Extensions;

// Register outbox services
builder.Services.AddOutboxPattern(builder.Configuration);
```

This registers:
- `IOutboxRepository` → `OutboxRepository`
- `IOutboxPublisher` → `OutboxPublisher`
- `OutboxProcessor` as `IHostedService`

## Usage

### 1. Publishing Events to Outbox

In your command handlers:

```csharp
public class CreateScreenDefinitionHandler : IRequestHandler<CreateScreenDefinitionCommand, Result<ScreenDefnDto>>
{
    private readonly IOutboxPublisher _outboxPublisher;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<ScreenDefnDto>> Handle(CreateScreenDefinitionCommand request, CancellationToken cancellationToken)
    {
        // Save to primary database
        var entity = new ScreenDefinition { /* ... */ };
        await _unitOfWork.ExecuteAsync(async () =>
        {
            // Save entity
            await _repository.AddAsync(entity, cancellationToken);
            
            // Publish to outbox (same transaction)
            await _outboxPublisher.PublishAsync(new OutboxMessage
            {
                Type = "ScreenDefinition",
                EntityId = entity.ScreenDefnId,
                Operation = "INSERT",
                Payload = JsonSerializer.Serialize(entity),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                SourceDatabase = "Primary",
                TargetDatabase = "Secondary"
            }, cancellationToken);
            
            return Result.Success();
        });
    }
}
```

### 2. Background Processing

The `OutboxProcessor` service runs automatically:

1. **Polls** the `OutboxMessages` table every `PollingIntervalSeconds`
2. **Fetches** pending messages (Status="Pending") in batches
3. **Replicates** to secondary database
4. **Marks** as processed or failed
5. **Retries** failed messages up to `MaxRetryCount`

## Database Tables

### OutboxMessages Table

```sql
CREATE TABLE CVLWebTools.AdminToolOutBox (
    Id NUMBER PRIMARY KEY,
    Type NVARCHAR2(255) NOT NULL,
    EntityId NUMBER NOT NULL,
    Operation NVARCHAR2(50) NOT NULL,  -- INSERT, UPDATE, DELETE
    Payload NCLOB NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    ProcessedAt TIMESTAMP,
    Status NVARCHAR2(50) NOT NULL,     -- Pending, Processing, Completed, Failed
    RetryCount NUMBER DEFAULT 0,
    Error NCLOB,
    SourceDatabase NVARCHAR2(100),
    TargetDatabase NVARCHAR2(100),
    CorrelationId NVARCHAR2(100)
);

CREATE INDEX IDX_OutBox_Status_Created ON CVLWebTools.AdminToolOutBox(Status, CreatedAt);
```

## Flow Diagram

```
User Request
    ↓
Command Handler
    ↓
┌─────────────────────────────────────┐
│ UnitOfWork.ExecuteAsync()           │
│                                     │
│  1. Save Entity to Primary DB      │
│  2. Publish OutboxMessage           │  ← Same Transaction
│  3. Commit Transaction              │
└─────────────────────────────────────┘
    ↓
OutboxMessages Table (Primary DB)
    ↓
OutboxProcessor (Background Service)
    ↓
┌─────────────────────────────────────┐
│ Every PollingIntervalSeconds:       │
│                                     │
│  1. Query Pending Messages          │
│  2. Replicate to Secondary DB       │
│  3. Mark as Processed               │
│  4. Handle Retries                  │
└─────────────────────────────────────┘
    ↓
Secondary Database (Eventually Consistent)
```

## Benefits

1. **Atomicity**: Outbox messages saved in same transaction as domain changes
2. **Eventual Consistency**: Secondary database eventually receives all changes
3. **Resilience**: Automatic retries for failed replications
4. **Observability**: Track processing status via Status field
5. **Performance**: Batch processing reduces database load
6. **Reliability**: No data loss even if secondary DB is down

## Monitoring

Monitor the outbox health by:

1. **Pending Count**: `SELECT COUNT(*) FROM OutboxMessages WHERE Status='Pending'`
2. **Failed Count**: `SELECT COUNT(*) FROM OutboxMessages WHERE Status='Failed'`
3. **Retry Count**: `SELECT AVG(RetryCount) FROM OutboxMessages WHERE Status='Failed'`
4. **Processing Time**: `SELECT AVG(EXTRACT(SECOND FROM (ProcessedAt - CreatedAt))) FROM OutboxMessages WHERE Status='Completed'`

## Troubleshooting

### Outbox Processor Not Running

Check:
- `Outbox:Enabled` is `true` in appsettings
- No errors in application logs
- Background service registered correctly

### Messages Stuck in Pending

Check:
- Secondary database connectivity
- Outbox processor logs for errors
- Retry count hasn't exceeded `MaxRetryCount`

### High Retry Count

Check:
- Secondary database performance
- Network connectivity
- Payload serialization issues

## Development Mode

For development, set `Outbox:Enabled` to `false` in `appsettings.Development.json` to disable background replication while testing.

## Future Enhancements

1. **Dead Letter Queue**: Move permanently failed messages to separate table
2. **Metrics**: Export processing metrics to monitoring system
3. **Priority Queue**: Process high-priority entities first
4. **Batched Replication**: Combine multiple operations into single DB call
5. **Partitioning**: Archive old processed messages for performance
