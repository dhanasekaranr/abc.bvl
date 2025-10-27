# 🔄 AdminTool Data Flow - Current vs Enhanced Dual Database

## 📊 **Current Data Flow (As Implemented)**

### **Read Operation Flow**
```
1. HTTP GET /api/v1/admin/screen-pilot/screens
   ↓
2. ScreenDefinitionController.GetScreens()
   ↓  
3. _mediator.Send(new GetScreenDefinitionsQuery(status))
   ↓
4. GetScreenDefinitionsHandler.Handle()
   ↓
5. IScreenDefinitionRepository.GetAllAsync()
   ↓
6. [Current Gap - Repository not fully implemented]
   ↓
7. Mock/Sample Data Returned
   ↓
8. ScreenDefnDto List
   ↓
9. ApiResponse<IEnumerable<ScreenDefnDto>> Wrapper
   ↓
10. HTTP 200 OK Response
```

### **Write Operation Flow (Current)**
```
1. HTTP PUT /api/v1/admin/screen-pilot/screens
   ↓
2. ScreenDefinitionController.UpsertScreen()
   ↓
3. [No MediatR Command - Direct Mock Implementation]
   ↓
4. Generate Mock Response (request with updated fields)
   ↓
5. ApiResponse<ScreenDefnDto> Wrapper  
   ↓
6. HTTP 200 OK Response

❌ ISSUE: No actual database persistence happening!
```

## 🎯 **Current Architecture Issues**

Looking at your `ScreenDefinitionController.cs`:

```csharp
[Route("api/v1/admin/screen-pilot")]  // ← Wrong route (should be screen-definition)
public class ScreenDefinitionController : ControllerBase
{
    [HttpPut("screens")]
    public async Task<ActionResult<ApiResponse<ScreenDefnDto>>> UpsertScreen([FromBody] ScreenDefnDto request)
    {
        // ❌ ISSUE: No actual persistence - just returning mock data!
        await Task.CompletedTask;
        
        var result = request with 
        { 
            Id = request.Id ?? DateTimeOffset.UtcNow.Ticks,
            CreatedAt = request.CreatedAt ?? DateTimeOffset.UtcNow,
            CreatedBy = request.CreatedBy ?? "current-user",
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = "current-user"
        };
        
        return Ok(new ApiResponse<ScreenDefnDto>(result, /*...*/));
    }
}
```

## 🏗️ **Enhanced Implementation: Proper Data Flow**

### **1. Fixed Controller with Real Persistence**

```csharp
[ApiController]
[Route("api/v1/admin/screen-definition")]  // ← Fixed route
public class ScreenDefinitionController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpGet("screens")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ScreenDefnDto>>>> GetScreens(
        [FromQuery] byte? status = null,
        [FromHeader(Name = "X-DB-Route")] string? dbRoute = "primary")
    {
        var query = new GetScreenDefinitionsQuery(status, dbRoute);
        var result = await _mediator.Send(query);
        
        return Ok(CreateApiResponse(result, dbRoute));
    }

    [HttpPost("screens")]
    public async Task<ActionResult<ApiResponse<ScreenDefnDto>>> CreateScreen(
        [FromBody] CreateScreenDefinitionCommand command,
        [FromHeader(Name = "X-DB-Route")] string? dbRoute = "primary")
    {
        command = command with { DbRoute = dbRoute };
        var result = await _mediator.Send(command);
        
        return CreatedAtAction(nameof(GetScreen), 
            new { id = result.Id }, 
            CreateApiResponse(result, dbRoute));
    }

    [HttpPut("screens/{id}")]
    public async Task<ActionResult<ApiResponse<ScreenDefnDto>>> UpdateScreen(
        long id,
        [FromBody] UpdateScreenDefinitionCommand command,
        [FromHeader(Name = "X-DB-Route")] string? dbRoute = "primary")
    {
        command = command with { Id = id, DbRoute = dbRoute };
        var result = await _mediator.Send(command);
        
        return Ok(CreateApiResponse(result, dbRoute));
    }
}
```

### **2. Enhanced Query with DB Route Support**

```csharp
public record GetScreenDefinitionsQuery(
    byte? Status = null, 
    string DbRoute = "primary"
) : IRequest<IEnumerable<ScreenDefnDto>>;

public class GetScreenDefinitionsHandler : IRequestHandler<GetScreenDefinitionsQuery, IEnumerable<ScreenDefnDto>>
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly ILogger<GetScreenDefinitionsHandler> _logger;

    public async Task<IEnumerable<ScreenDefnDto>> Handle(GetScreenDefinitionsQuery request, CancellationToken cancellationToken)
    {
        // Create UnitOfWork for specified database route
        var unitOfWork = _unitOfWorkFactory.Create(request.DbRoute);
        
        return await unitOfWork.ExecuteAsync(async (context, ct) =>
        {
            var query = context.ScreenDefinitions.AsQueryable();
            
            if (request.Status.HasValue)
            {
                query = query.Where(s => s.Status == request.Status.Value);
            }
            
            var entities = await query
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Name)
                .ToListAsync(ct);
                
            return entities.Select(MapToDto);
        }, cancellationToken);
    }
    
    private static ScreenDefnDto MapToDto(ScreenDefinition entity)
    {
        return new ScreenDefnDto(
            Id: entity.Id,
            ScreenCode: entity.Code,
            ScreenName: entity.Name,
            Status: entity.Status,
            Description: entity.Description,
            CreatedAt: entity.CreatedAt,
            CreatedBy: entity.CreatedBy,
            UpdatedAt: entity.UpdatedAt,
            UpdatedBy: entity.UpdatedBy
        );
    }
}
```

### **3. Command with Dual Database Support**

```csharp
public record CreateScreenDefinitionCommand(
    string ScreenCode,
    string ScreenName,
    string? Description = null,
    string DbRoute = "primary",
    bool EnableDualWrite = true
) : IRequest<ScreenDefnDto>;

public class CreateScreenDefinitionHandler : IRequestHandler<CreateScreenDefinitionCommand, ScreenDefnDto>
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly ILogger<CreateScreenDefinitionHandler> _logger;

    public async Task<ScreenDefnDto> Handle(CreateScreenDefinitionCommand request, CancellationToken cancellationToken)
    {
        if (request.EnableDualWrite)
        {
            return await HandleDualWrite(request, cancellationToken);
        }
        else
        {
            return await HandleSingleWrite(request, cancellationToken);
        }
    }

    private async Task<ScreenDefnDto> HandleDualWrite(CreateScreenDefinitionCommand request, CancellationToken cancellationToken)
    {
        // Primary database write with outbox
        var primaryUoW = _unitOfWorkFactory.Create("primary");
        
        return await primaryUoW.ExecuteAsync(async (context, ct) =>
        {
            // 1. Create and save entity to primary database
            var entity = new ScreenDefinition
            {
                Code = request.ScreenCode,
                Name = request.ScreenName,
                Description = request.Description,
                Status = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = GetCurrentUser(),
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = GetCurrentUser()
            };

            context.ScreenDefinitions.Add(entity);
            await context.SaveChangesAsync(ct);

            // 2. Create outbox message for secondary database replication
            var outboxMessage = new OutboxMessage
            {
                EntityType = "ScreenDefinition",
                EntityId = entity.Id,
                Operation = "CREATE",
                Data = JsonSerializer.Serialize(new
                {
                    entity.Id,
                    entity.Code,
                    entity.Name,
                    entity.Description,
                    entity.Status,
                    entity.CreatedAt,
                    entity.CreatedBy,
                    entity.UpdatedAt,
                    entity.UpdatedBy
                }),
                TargetRoute = "secondary",
                CreatedAt = DateTimeOffset.UtcNow,
                Status = OutboxStatus.Pending
            };

            context.OutboxMessages.Add(outboxMessage);
            await context.SaveChangesAsync(ct);

            _logger.LogInformation("Created ScreenDefinition {Id} in primary DB with outbox message {OutboxId}", 
                entity.Id, outboxMessage.Id);

            return MapToDto(entity);
        }, cancellationToken);
    }

    private async Task<ScreenDefnDto> HandleSingleWrite(CreateScreenDefinitionCommand request, CancellationToken cancellationToken)
    {
        var unitOfWork = _unitOfWorkFactory.Create(request.DbRoute);
        
        return await unitOfWork.ExecuteAsync(async (context, ct) =>
        {
            var entity = new ScreenDefinition
            {
                Code = request.ScreenCode,
                Name = request.ScreenName,
                Description = request.Description,
                Status = 1,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = GetCurrentUser(),
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = GetCurrentUser()
            };

            context.ScreenDefinitions.Add(entity);
            await context.SaveChangesAsync(ct);

            _logger.LogInformation("Created ScreenDefinition {Id} in {DbRoute} database", 
                entity.Id, request.DbRoute);

            return MapToDto(entity);
        }, cancellationToken);
    }
}
```

## 🔄 **Complete Dual Database Flow**

### **Write Operation with Dual Database**
```
1. HTTP POST /api/v1/admin/screen-definition/screens
   Headers: { "X-DB-Route": "primary" }
   ↓
2. ScreenDefinitionController.CreateScreen()
   ↓
3. _mediator.Send(CreateScreenDefinitionCommand)
   ↓
4. CreateScreenDefinitionHandler.HandleDualWrite()
   ↓
5. UnitOfWorkFactory.Create("primary")
   ↓
6. PRIMARY DATABASE TRANSACTION:
   ┌─────────────────────────────────────────┐
   │ a. Insert ScreenDefinition              │
   │ b. Insert OutboxMessage                 │
   │ c. Commit Transaction                   │
   └─────────────────────────────────────────┘
   ↓
7. Return ScreenDefnDto (Immediate Response)

BACKGROUND PROCESS:
8. OutboxProcessorService (every 10 seconds)
   ↓
9. Query pending OutboxMessages from Primary DB
   ↓
10. For each message:
    ┌─────────────────────────────────────────┐
    │ a. UnitOfWorkFactory.Create("secondary") │
    │ b. Replicate entity to Secondary DB     │
    │ c. Mark OutboxMessage as Processed      │
    └─────────────────────────────────────────┘
```

### **Read Operation with DB Route Selection**
```
1. HTTP GET /api/v1/admin/screen-definition/screens?status=1
   Headers: { "X-DB-Route": "secondary" }
   ↓
2. ScreenDefinitionController.GetScreens()
   ↓
3. _mediator.Send(GetScreenDefinitionsQuery(status=1, dbRoute="secondary"))
   ↓
4. GetScreenDefinitionsHandler.Handle()
   ↓
5. UnitOfWorkFactory.Create("secondary")  ← Routes to secondary DB
   ↓
6. Query ScreenDefinitions from SECONDARY DATABASE
   ↓
7. Map entities to ScreenDefnDto list
   ↓
8. Return ApiResponse with data from secondary DB
```

## ⚡ **Key Benefits of This Flow**

### **1. Data Consistency**
- ✅ **Atomic writes**: Primary + Outbox in single transaction
- ✅ **No data loss**: Outbox ensures secondary replication
- ✅ **Retry capability**: Failed replications can be retried

### **2. Performance**
- ✅ **Fast writes**: User gets immediate response after primary write
- ✅ **Read scaling**: Can read from secondary for load balancing
- ✅ **Non-blocking**: Background replication doesn't affect user experience

### **3. Reliability**
- ✅ **Graceful degradation**: Works even if secondary DB is down
- ✅ **Monitoring**: Outbox table provides replication status
- ✅ **Recovery**: Can rebuild secondary from primary + outbox

This enhanced flow provides **enterprise-grade dual database support** while maintaining the clean architecture and high performance characteristics of your AdminTool system.