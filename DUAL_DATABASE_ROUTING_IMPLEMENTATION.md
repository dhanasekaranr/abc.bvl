# Dual-Database Routing Implementation - Summary

## Overview
Implemented Option 2 (Middleware + Provider pattern) for dual-database routing with proper separation of concerns. The solution allows routing requests to either primary (APP_USER) or secondary (CVLWEBTOOLS) database schemas based on HTTP headers.

## Architecture Changes

### 1. Abstract Base DbContext
**File**: `src/abc.bvl.AdminTool.Infrastructure.Data/Context/AdminDbContext.cs`
- Changed from concrete class to **abstract base class**
- Added abstract property: `public abstract string SchemaName { get; }`
- Constructor now accepts `DbContextOptions` (not generic)
- Entity configurations now receive schema name via constructor

### 2. Derived Context Classes

**Primary Context**: `src/abc.bvl.AdminTool.Infrastructure.Data/Context/AdminDbPrimaryContext.cs`
```csharp
public class AdminDbPrimaryContext : AdminDbContext
{
    public override string SchemaName => "APP_USER";
}
```

**Secondary Context**: `src/abc.bvl.AdminTool.Infrastructure.Data/Context/AdminDbSecondaryContext.cs`
```csharp
public class AdminDbSecondaryContext : AdminDbContext
{
    public override string SchemaName => "CVLWEBTOOLS";
}
```

### 3. Entity Configurations Updated
All entity configurations now accept schema parameter:
- `ScreenDefinitionConfiguration(string schemaName)`
- `ScreenPilotConfiguration(string schemaName)`
- `OutboxMessageConfiguration(string schemaName)`

Example:
```csharp
public class ScreenDefinitionConfiguration : IEntityTypeConfiguration<ScreenDefinition>
{
    private readonly string _schemaName;
    
    public ScreenDefinitionConfiguration(string schemaName)
    {
        _schemaName = schemaName;
    }
    
    public void Configure(EntityTypeBuilder<ScreenDefinition> builder)
    {
        builder.ToTable("ADMIN_SCREENDEFN", _schemaName);
        // ... rest of configuration
    }
}
```

### 4. Context Provider Pattern

**Interface**: `src/abc.bvl.AdminTool.Infrastructure.Data/Interfaces/ICurrentDbContextProvider.cs`
```csharp
public interface ICurrentDbContextProvider
{
    AdminDbContext GetContext();
    void SetContextType(DatabaseContextType contextType);
}

public enum DatabaseContextType
{
    Primary,
    Secondary
}
```

**Implementation**: `src/abc.bvl.AdminTool.Infrastructure.Data/Providers/CurrentDbContextProvider.cs`
- Request-scoped service
- Holds references to both Primary and Secondary contexts
- Returns appropriate context based on middleware configuration

### 5. Database Routing Middleware

**File**: `src/abc.bvl.AdminTool.Api/Middleware/DatabaseRoutingMiddleware.cs`
- Reads `X-Database-Route` HTTP header
- Supported values:
  - `primary` or `app_user` → Routes to APP_USER schema
  - `secondary` or `cvlwebtools` → Routes to CVLWEBTOOLS schema
- Sets context type in `ICurrentDbContextProvider` for current request
- Must be registered **before authentication** middleware

### 6. Updated Repositories
All repositories now use `ICurrentDbContextProvider` instead of direct context injection:

**Pattern**:
```csharp
public class ScreenDefinitionRepository : IScreenDefinitionRepository
{
    private readonly ICurrentDbContextProvider _contextProvider;
    private AdminDbContext Context => _contextProvider.GetContext();

    public ScreenDefinitionRepository(ICurrentDbContextProvider contextProvider)
    {
        _contextProvider = contextProvider;
    }
    
    // Methods use Context property instead of _context field
}
```

**Updated Repositories**:
- `ScreenDefinitionRepository`
- `ScreenPilotRepository`
- `GenericRepository<T>`

### 7. DI Registration Updates

**File**: `src/abc.bvl.AdminTool.Api/Services/DatabaseConfigurationExtensions.cs`
- Registers `AdminDbPrimaryContext` with primary connection string
- Registers `AdminDbSecondaryContext` with secondary connection string (or primary as fallback)
- Registers `ICurrentDbContextProvider` as scoped service
- Removed old keyed service registration and `IDbContextResolver`

**File**: `src/abc.bvl.AdminTool.Api/Program.cs`
- Updated `IUnitOfWork` registration to use `ICurrentDbContextProvider`
- Added `app.UseDatabaseRouting()` middleware before authentication

## Request Flow

1. **HTTP Request arrives** with optional `X-Database-Route` header
2. **DatabaseRoutingMiddleware** reads header
3. **Middleware** calls `ICurrentDbContextProvider.SetContextType()`
4. **Controller** calls handler via MediatR
5. **Handler** uses repository
6. **Repository** calls `_contextProvider.GetContext()` to get correct context
7. **Query/Command** executes against correct database schema
8. **Response** returned to client

## Connection String Configuration

**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "AdminDb_Primary": "User Id=app_user;Password=***;Data Source=***",
    "AdminDb_Secondary": "User Id=cvlwebtools;Password=***;Data Source=***"
  }
}
```

## HTTP Header Usage

**Primary Database Request** (default):
```
GET /api/pilotenablement/users?page=1&pageSize=10
```

**Secondary Database Request**:
```
GET /api/pilotenablement/users?page=1&pageSize=10
X-Database-Route: secondary
```

## Testing

To test the dual-database routing:

1. **Test Primary Database**:
```bash
curl -X GET "http://localhost:5092/api/pilotenablement/users?page=1&pageSize=10" \
  -H "Authorization: Bearer <token>"
```

2. **Test Secondary Database**:
```bash
curl -X GET "http://localhost:5092/api/pilotenablement/users?page=1&pageSize=10" \
  -H "Authorization: Bearer <token>" \
  -H "X-Database-Route: secondary"
```

3. **Check Logs** for routing confirmation:
```
[Debug] Routing request to PRIMARY database (APP_USER schema)
[Debug] Routing request to SECONDARY database (CVLWEBTOOLS schema)
```

## Benefits of This Approach

1. **Clean Separation of Concerns**
   - Controllers don't know about database routing
   - Repositories don't know about routing logic
   - Middleware handles all routing decisions

2. **Type Safety**
   - Compile-time checking with strongly-typed contexts
   - No string-based routing in business logic

3. **Testability**
   - Easy to mock `ICurrentDbContextProvider`
   - Can test with different contexts without HTTP headers

4. **Maintainability**
   - Single place to add new database targets
   - Easy to understand request flow
   - Follows SOLID principles

5. **Performance**
   - Request-scoped contexts (efficient memory usage)
   - No reflection or dynamic context creation
   - Lazy context retrieval via property

## Files Created/Modified

### Created:
1. `src/abc.bvl.AdminTool.Infrastructure.Data/Context/AdminDbPrimaryContext.cs`
2. `src/abc.bvl.AdminTool.Infrastructure.Data/Context/AdminDbSecondaryContext.cs`
3. `src/abc.bvl.AdminTool.Infrastructure.Data/Interfaces/ICurrentDbContextProvider.cs`
4. `src/abc.bvl.AdminTool.Infrastructure.Data/Providers/CurrentDbContextProvider.cs`
5. `src/abc.bvl.AdminTool.Api/Middleware/DatabaseRoutingMiddleware.cs`

### Modified:
1. `src/abc.bvl.AdminTool.Infrastructure.Data/Context/AdminDbContext.cs`
2. `src/abc.bvl.AdminTool.Infrastructure.Data/Configurations/ScreenDefinitionConfiguration.cs`
3. `src/abc.bvl.AdminTool.Infrastructure.Data/Configurations/ScreenPilotConfiguration.cs`
4. `src/abc.bvl.AdminTool.Infrastructure.Data/Configurations/OutboxMessageConfiguration.cs`
5. `src/abc.bvl.AdminTool.Infrastructure.Data/Repositories/ScreenDefinitionRepository.cs`
6. `src/abc.bvl.AdminTool.Infrastructure.Data/Repositories/ScreenPilotRepository.cs`
7. `src/abc.bvl.AdminTool.Infrastructure.Data/Repositories/GenericRepository.cs`
8. `src/abc.bvl.AdminTool.Api/Services/DatabaseConfigurationExtensions.cs`
9. `src/abc.bvl.AdminTool.Api/Program.cs`

## Build Status
✅ Solution builds successfully with no errors
✅ All repositories updated to use context provider pattern
✅ Middleware registered in correct position in pipeline
✅ Both contexts registered with proper lifetimes

## Next Steps
1. Test routing with actual HTTP requests
2. Verify both schemas work correctly
3. Add integration tests for dual-database scenarios
4. Document X-Database-Route header in Swagger/API docs
5. Consider adding request logging for audit trail
