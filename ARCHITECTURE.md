# AdminTool - Architecture & Design Documentation

## Table of Contents
1. [Overview](#overview)
2. [Architecture Pattern](#architecture-pattern)
3. [Project Structure](#project-structure)
4. [Request Flow](#request-flow)
5. [Design Patterns](#design-patterns)
6. [Technology Stack](#technology-stack)
7. [Key Components](#key-components)
8. [Security](#security)
9. [Testing Strategy](#testing-strategy)

---

## Overview

**AdminTool** is an enterprise-grade .NET 8 Web API for managing administrative master data with support for dual-database routing and transactional consistency. Built using Clean Architecture principles with selective CQRS implementation for optimal balance between simplicity and scalability.

### Key Features
- ✅ Clean Architecture with clear separation of concerns
- ✅ CQRS pattern for complex read operations
- ✅ Dual-database routing with transactional outbox pattern
- ✅ JWT-based authentication and role-based authorization
- ✅ Comprehensive security middleware pipeline
- ✅ Oracle XE database with Entity Framework Core
- ✅ FluentValidation for input validation
- ✅ Structured logging with Serilog
- ✅ Generic repository pattern for 100+ table scalability
- ✅ 43.5% test coverage with 169 passing tests

---

## Architecture Pattern

### **Hybrid Clean Architecture + CQRS**

The application follows **Clean Architecture** (Onion Architecture) with **selective CQRS** implementation:

```
┌─────────────────────────────────────────────────────────────┐
│                      Presentation Layer                      │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  abc.bvl.AdminTool.Api (ASP.NET Core Web API)         │ │
│  │  - Controllers (ScreenDefinition, ScreenPilot)         │ │
│  │  - Middleware (Security, Exception, RateLimit)         │ │
│  │  - Filters (EnrichResponse)                            │ │
│  │  - Validation (FluentValidation)                       │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                            ↓↑
┌─────────────────────────────────────────────────────────────┐
│                     Application Layer                        │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  abc.bvl.AdminTool.Application                         │ │
│  │  - CQRS Handlers (GetScreenDefinitions, etc.)          │ │
│  │  - MediatR pipeline                                    │ │
│  │  - Interfaces (IUnitOfWork, IRequestContext)           │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                            ↓↑
┌─────────────────────────────────────────────────────────────┐
│                       Domain Layer                           │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  abc.bvl.AdminTool.Domain (Pure Business Logic)        │ │
│  │  - Entities (ScreenDefinition, ScreenPilot, etc.)      │ │
│  │  - Base Classes (BaseAdminEntity, BaseLookupEntity)    │ │
│  │  - Business Rules & Validation                         │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                            ↓↑
┌─────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                       │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  abc.bvl.AdminTool.Infrastructure.Data                 │ │
│  │  - DbContext (EF Core for Oracle)                      │ │
│  │  - Repositories (Generic + Specific)                   │ │
│  │  - UnitOfWork Pattern                                  │ │
│  │  - Database Seeder                                     │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  abc.bvl.AdminTool.Infrastructure.Replication          │ │
│  │  - Outbox Pattern (Dual-DB sync)                       │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                            ↓↑
┌─────────────────────────────────────────────────────────────┐
│                      Contracts Layer                         │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  abc.bvl.AdminTool.Contracts                           │ │
│  │  - DTOs (ScreenDefnDto, CountryDto, etc.)              │ │
│  │  - Response Wrappers (ApiResponse, PagedResult)        │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                            ↓↑
                    ┌───────────────┐
                    │ Oracle DB     │
                    │ (XEPDB1)      │
                    └───────────────┘
```

### **Dependency Rule**
- **Domain Layer**: No dependencies (pure business logic)
- **Application Layer**: Depends only on Domain
- **Infrastructure Layer**: Implements Application interfaces, depends on Domain
- **API Layer**: Depends on Application (not Infrastructure directly)
- **Contracts Layer**: Shared across all layers (DTOs only)

---

## Project Structure

```
AdminTool/
├── src/
│   ├── abc.bvl.AdminTool.Api/                      # 🌐 Presentation Layer
│   │   ├── Controllers/
│   │   │   ├── Base/
│   │   │   │   └── BaseApiController.cs            # Base controller with common functionality
│   │   │   ├── ScreenDefinitionController.cs       # CQRS pattern with MediatR
│   │   │   ├── ScreenPilotController.cs            # Direct controller pattern
│   │   │   ├── GenericAdminController.cs           # Generic CRUD for all entities
│   │   │   └── DevTokenController.cs               # JWT token generation (dev only)
│   │   ├── Middleware/
│   │   │   ├── GlobalExceptionMiddleware.cs        # Centralized exception handling
│   │   │   └── SecurityHeadersMiddleware.cs        # OWASP security headers
│   │   ├── Filters/
│   │   │   └── EnrichResponseFilter.cs             # Auto-enriches responses with user/access info
│   │   ├── Validation/
│   │   │   ├── ScreenDefnDtoValidator.cs           # FluentValidation validators
│   │   │   └── SecurityValidationAttributes.cs     # Custom validation attributes
│   │   ├── Configuration/
│   │   │   ├── JwtSettings.cs                      # JWT configuration
│   │   │   └── SecuritySettings.cs                 # Security configuration
│   │   └── Program.cs                              # Application entry point & DI setup
│   │
│   ├── abc.bvl.AdminTool.Application/              # 📋 Application Layer (Use Cases)
│   │   ├── ScreenDefinition/
│   │   │   └── Queries/
│   │   │       ├── GetScreenDefinitionsQuery.cs    # CQRS Query
│   │   │       └── GetScreenDefinitionsHandler.cs  # Query Handler
│   │   └── Common/
│   │       └── Interfaces/
│   │           ├── IUnitOfWork.cs                  # Transaction management
│   │           ├── IRequestContext.cs              # Request context abstraction
│   │           └── IAdminDbContext.cs              # Database context interface
│   │
│   ├── abc.bvl.AdminTool.Domain/                   # 🎯 Domain Layer (Business Logic)
│   │   └── Entities/
│   │       ├── Base/
│   │       │   ├── BaseAdminEntity.cs              # Base entity with audit fields
│   │       │   └── BaseLookupEntity.cs             # Base lookup entity with Code/Name
│   │       ├── ScreenDefinition.cs                 # Screen definition entity
│   │       ├── ScreenPilot.cs                      # User-screen assignment entity
│   │       ├── OutboxMessage.cs                    # Outbox pattern for dual-DB sync
│   │       ├── Country.cs                          # Country lookup entity
│   │       └── State.cs                            # State lookup entity
│   │
│   ├── abc.bvl.AdminTool.Infrastructure.Data/      # 🗄️ Infrastructure Layer (Data Access)
│   │   ├── Context/
│   │   │   └── AdminDbContext.cs                   # EF Core DbContext for Oracle
│   │   ├── Configurations/
│   │   │   ├── ScreenDefinitionConfiguration.cs    # Entity Type Configuration
│   │   │   ├── ScreenPilotConfiguration.cs
│   │   │   └── OutboxMessageConfiguration.cs
│   │   ├── Repositories/
│   │   │   ├── GenericRepository.cs                # Generic CRUD operations
│   │   │   ├── ScreenDefinitionRepository.cs       # Specific repository with custom queries
│   │   │   ├── CompiledQueries.cs                  # EF Core compiled queries for performance
│   │   │   └── PagedResult.cs                      # Pagination result model
│   │   └── Services/
│   │       ├── UnitOfWork.cs                       # Transaction management implementation
│   │       ├── UnitOfWorkFactory.cs                # Factory for creating UnitOfWork
│   │       ├── RequestContextAccessor.cs           # Request context implementation
│   │       └── DatabaseSeeder.cs                   # Initial data seeding
│   │
│   ├── abc.bvl.AdminTool.Infrastructure.Replication/ # 🔄 Replication Layer
│   │   └── OutboxWorker.cs                         # Background worker for outbox processing
│   │
│   └── abc.bvl.AdminTool.Contracts/                # 📦 Contracts Layer (DTOs)
│       ├── Admin/
│       │   ├── AdminLookupDtos.cs                  # Country, State DTOs
│       │   └── BaseAdminDto.cs                     # Base DTO
│       ├── ScreenDefinition/
│       │   └── ScreenDefnDto.cs                    # Screen definition DTO
│       ├── ScreenPilot/
│       │   └── ScreenPilotDto.cs                   # Screen pilot DTO
│       └── Common/
│           ├── ApiResponse.cs                      # Standard API response wrapper
│           ├── PagedResult.cs                      # Paginated response
│           ├── SingleResult.cs                     # Single item response
│           └── PaginationRequest.cs                # Pagination parameters
│
└── tests/
    └── abc.bvl.AdminTool.MSTests/                  # 🧪 Test Project
        ├── Domain/                                 # Domain entity tests (60 tests)
        ├── Application/                            # Query handler tests (4 tests)
        ├── Api/
        │   ├── Controllers/                        # Controller tests
        │   ├── Middleware/                         # Middleware tests (19 tests)
        │   ├── Filters/                            # Filter tests (9 tests)
        │   └── ValidationAttributeTests.cs         # Validation tests (39 tests)
        ├── Infrastructure/                         # Repository & service tests (10 tests)
        └── Contracts/                              # DTO tests (28 tests)
```

---

## Request Flow

### **Complete Request Flow: Get Screen Definitions**

```
┌─────────────────────────────────────────────────────────────┐
│  1. HTTP Request                                             │
│  GET /api/v1/admin/screen-definition/screens?status=1&page=1│
│  Authorization: Bearer <JWT_TOKEN>                           │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  2. Middleware Pipeline (Order Matters!)                     │
│                                                              │
│  SecurityHeadersMiddleware                                   │
│  └→ Adds: X-Frame-Options, X-Content-Type-Options,          │
│           Content-Security-Policy, etc.                      │
│                                                              │
│  GlobalExceptionMiddleware                                   │
│  └→ Wraps request in try-catch                              │
│  └→ Sanitizes errors before returning                       │
│                                                              │
│  Authentication Middleware (JWT)                             │
│  └→ Validates JWT token                                     │
│  └→ Extracts claims (UserId, Email, Roles)                  │
│  └→ Sets HttpContext.User                                   │
│                                                              │
│  Authorization Middleware                                    │
│  └→ Checks [Authorize(Roles = "Admin,ScreenManager")]       │
│  └→ Returns 403 if unauthorized                             │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  3. Controller: ScreenDefinitionController                   │
│                                                              │
│  public async Task<ActionResult<PagedResult<ScreenDefnDto>>> │
│      GetScreens(byte? status, int page, int pageSize, ...)  │
│  {                                                           │
│      // Create CQRS Query                                   │
│      var query = new GetScreenDefinitionsQuery {            │
│          Status = status,                                   │
│          Page = page,                                       │
│          PageSize = pageSize                                │
│      };                                                      │
│                                                              │
│      // Send to MediatR                                     │
│      var result = await _mediator.Send(query, ct);          │
│                                                              │
│      // Return wrapped response                             │
│      return Ok(PagedSuccess(result.Items, ...));            │
│  }                                                           │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  4. MediatR Pipeline                                         │
│  └→ Dispatches to registered handler                        │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  5. Application Layer: Query Handler                         │
│                                                              │
│  public class GetScreenDefinitionsHandler                    │
│      : IRequestHandler<GetScreenDefinitionsQuery, ...>       │
│  {                                                           │
│      public async Task<...> Handle(...)                     │
│      {                                                       │
│          // Get data from repository                        │
│          var result = await _repository                     │
│              .GetPagedScreenDefinitionsAsync(...);          │
│                                                              │
│          // Map entities to DTOs                            │
│          var dtos = result.Items.Select(entity => new       │
│              ScreenDefnDto(...));                            │
│                                                              │
│          return new PagedResult<ScreenDefnDto>(...);        │
│      }                                                       │
│  }                                                           │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  6. Infrastructure Layer: Repository                         │
│                                                              │
│  public class ScreenDefinitionRepository                     │
│  {                                                           │
│      public async Task<PagedResult<ScreenDefinition>>       │
│          GetPagedScreenDefinitionsAsync(...)                │
│      {                                                       │
│          // Build EF Core query                             │
│          var query = _context.ScreenDefinitions             │
│              .Where(s => s.Status == status)                │
│              .OrderBy(s => s.ScreenName);                   │
│                                                              │
│          // Get total count                                 │
│          var total = await query.CountAsync();              │
│                                                              │
│          // Get paginated items                             │
│          var items = await query                            │
│              .Skip((page - 1) * pageSize)                   │
│              .Take(pageSize)                                │
│              .ToListAsync();                                │
│                                                              │
│          return new PagedResult<ScreenDefinition>(          │
│              items, total, page, pageSize);                 │
│      }                                                       │
│  }                                                           │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  7. Database: Oracle XE (XEPDB1)                            │
│                                                              │
│  SELECT SCREENDEFNID, SCREENNAME, DESCRIPTION,              │
│         STATUS, CREATEDAT, CREATEDBY, ...                   │
│  FROM ADMIN.SCREENDEFN                                      │
│  WHERE STATUS = 1                                           │
│  ORDER BY SCREENNAME                                        │
│  OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY                     │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  8. Response Filter: EnrichResponseFilter                   │
│                                                              │
│  └→ Automatically enriches PagedResult with:                │
│     - UserInfo (from JWT claims)                            │
│     - AccessInfo (roles, permissions, db route)             │
│     - CorrelationId (trace identifier)                      │
│     - ServerTime (UTC timestamp)                            │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  9. HTTP Response                                            │
│  200 OK                                                      │
│  Content-Type: application/json                             │
│  X-Frame-Options: DENY                                      │
│  X-Content-Type-Options: nosniff                            │
│  ...                                                         │
│                                                              │
│  {                                                           │
│    "items": [                                               │
│      {                                                       │
│        "id": 1,                                             │
│        "screenName": "Orders Management",                   │
│        "description": "Manage customer orders",             │
│        "status": 1,                                         │
│        ...                                                   │
│      }                                                       │
│    ],                                                        │
│    "totalCount": 50,                                        │
│    "page": 1,                                               │
│    "pageSize": 20,                                          │
│    "totalPages": 3,                                         │
│    "hasNextPage": true,                                     │
│    "hasPreviousPage": false,                                │
│    "user": {                                                │
│      "userId": "john.doe",                                  │
│      "displayName": "John Doe",                             │
│      "email": "john@example.com"                            │
│    },                                                        │
│    "access": {                                              │
│      "canRead": true,                                       │
│      "canWrite": true,                                      │
│      "roles": ["Admin"],                                    │
│      "dbRoute": "primary"                                   │
│    },                                                        │
│    "correlationId": "0HMVEK3...",                           │
│    "serverTime": "2025-10-26T19:30:00Z"                     │
│  }                                                           │
└─────────────────────────────────────────────────────────────┘
```

---

## Design Patterns

### **1. Clean Architecture (Onion Architecture)**
**Purpose**: Separation of concerns with dependency inversion

**Implementation**:
- **Domain** layer has zero dependencies
- **Application** depends only on Domain
- **Infrastructure** implements Application interfaces
- **API** depends on Application abstractions

**Benefits**:
- Testable in isolation
- Framework independent
- UI/Database agnostic
- Easy to swap implementations

---

### **2. CQRS (Command Query Responsibility Segregation)**
**Purpose**: Separate read and write operations

**Implementation**:
```csharp
// Query (Read)
public record GetScreenDefinitionsQuery : IRequest<PagedResult<ScreenDefnDto>>
{
    public byte? Status { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public class GetScreenDefinitionsHandler 
    : IRequestHandler<GetScreenDefinitionsQuery, PagedResult<ScreenDefnDto>>
{
    // Query logic - optimized for reads
}

// Command (Write) - Future implementation
public record UpdateScreenDefinitionCommand : IRequest<ScreenDefnDto>
{
    public long Id { get; init; }
    public string ScreenName { get; init; }
    // ...
}
```

**Benefits**:
- Optimized queries for reads
- Clear separation of responsibilities
- Easier to scale read and write independently
- Better testability

---

### **3. Repository Pattern**
**Purpose**: Abstract data access logic

**Implementation**:
```csharp
// Generic Repository
public interface IGenericRepository<T> where T : BaseAdminEntity
{
    Task<T?> GetByIdAsync(long id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<PagedResult<T>> GetPagedAsync(int page, int pageSize);
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(long id, string deletedBy);
}

// Specific Repository
public interface IScreenDefinitionRepository : IGenericRepository<ScreenDefinition>
{
    Task<PagedResult<ScreenDefinition>> GetPagedScreenDefinitionsAsync(...);
    Task<ScreenDefinition?> GetByNameAsync(string screenName);
}
```

**Benefits**:
- Centralized data access logic
- Easy to mock for testing
- Consistent API across entities
- Can optimize queries per entity

---

### **4. Unit of Work Pattern**
**Purpose**: Manage transactions and coordinate multiple repositories

**Implementation**:
```csharp
public interface IUnitOfWork
{
    Task<TResult> ExecuteAsync<TResult>(
        Func<IAdminDbContext, CancellationToken, Task<TResult>> operation, 
        CancellationToken ct = default);
        
    Task ExecuteAsync(
        Func<IAdminDbContext, CancellationToken, Task> operation, 
        CancellationToken ct = default);
}

// Usage
await _unitOfWork.ExecuteAsync(async (ctx, ct) =>
{
    // Multiple operations in single transaction
    await repository1.AddAsync(entity1);
    await repository2.UpdateAsync(entity2);
    await ctx.SaveChangesAsync(ct);
});
```

**Benefits**:
- Atomic operations
- Automatic transaction management
- Consistent error handling
- Rollback on failure

---

### **5. DTO Pattern (Data Transfer Objects)**
**Purpose**: Separate internal entities from API contracts

**Implementation**:
```csharp
// Domain Entity (Internal)
public class ScreenDefinition : BaseAdminEntity
{
    public string ScreenName { get; set; }
    public string? Description { get; set; }
    // Complex business logic
}

// DTO (External API Contract)
public record ScreenDefnDto(
    long? Id,
    string? ScreenName,
    string? Description,
    byte? Status,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy
);
```

**Benefits**:
- API stability (internal changes don't break contracts)
- Security (hide internal implementation)
- Flexibility (different views of same data)
- Validation at boundary

---

### **6. Middleware Pipeline Pattern**
**Purpose**: Process requests in a chain of responsibility

**Implementation**:
```csharp
// Program.cs - Order matters!
app.UseMiddleware<SecurityHeadersMiddleware>();      // 1. Security headers
app.UseMiddleware<GlobalExceptionMiddleware>();      // 2. Exception handling
app.UseAuthentication();                             // 3. JWT validation
app.UseAuthorization();                              // 4. Role checking
app.MapControllers();                                // 5. Route to controller
```

**Benefits**:
- Cross-cutting concerns
- Reusable components
- Easy to add/remove features
- Clear execution order

---

### **7. Factory Pattern**
**Purpose**: Create objects without specifying exact class

**Implementation**:
```csharp
public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();
}

public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly AdminDbContext _context;
    
    public IUnitOfWork Create() => new UnitOfWork(_context);
}
```

**Benefits**:
- Encapsulates object creation
- Easy to change implementation
- Supports dependency injection
- Testable

---

### **8. Outbox Pattern**
**Purpose**: Ensure eventual consistency in distributed systems

**Implementation**:
```csharp
// 1. Write to primary database + outbox in single transaction
await _unitOfWork.ExecuteAsync(async (ctx, ct) =>
{
    await ctx.ScreenDefinitions.AddAsync(screenDefn);
    
    var outboxMessage = new OutboxMessage
    {
        EntityType = "ScreenDefinition",
        Operation = "INSERT",
        Payload = JsonSerializer.Serialize(screenDefn),
        SourceDatabase = "Primary",
        TargetDatabase = "Secondary"
    };
    
    await ctx.OutboxMessages.AddAsync(outboxMessage);
    await ctx.SaveChangesAsync(ct);
});

// 2. Background worker processes outbox asynchronously
public class OutboxWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var pendingMessages = await GetPendingMessages();
            foreach (var message in pendingMessages)
            {
                await ProcessMessage(message);
            }
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }
}
```

**Benefits**:
- Guaranteed delivery
- Transactional consistency
- Retry logic
- Decoupled systems

---

### **9. Strategy Pattern**
**Purpose**: Select algorithm at runtime

**Implementation**:
```csharp
// Database routing strategy based on HTTP headers
public interface IRequestContext
{
    string DbRoute { get; } // "primary" or "secondary"
}

public class RequestContextAccessor : IRequestContext
{
    public string DbRoute => 
        _httpContextAccessor.HttpContext?
            .Request.Headers["X-Database"].FirstOrDefault() 
        ?? "primary";
}
```

**Benefits**:
- Runtime selection
- Easy to add strategies
- Testable independently
- Flexible configuration

---

### **10. Generic Programming**
**Purpose**: Write type-safe, reusable code

**Implementation**:
```csharp
// Generic repository for all entities
public class GenericRepository<T> : IGenericRepository<T> 
    where T : BaseAdminEntity, new()
{
    private readonly AdminDbContext _context;
    private readonly DbSet<T> _dbSet;
    
    public async Task<T?> GetByIdAsync(long id)
    {
        return await CompiledQueries<T>.GetById(_context, id);
    }
    
    // Same code works for Country, State, ScreenDefinition, etc.
}

// Compiled queries for performance
public static class CompiledQueries<T> where T : BaseAdminEntity
{
    public static readonly Func<AdminDbContext, long, Task<T?>> GetById =
        EF.CompileAsyncQuery((AdminDbContext ctx, long id) =>
            ctx.Set<T>().FirstOrDefault(e => e.Id == id && e.Status == 1));
}
```

**Benefits**:
- Code reuse (95% reduction)
- Type safety
- Performance (compiled queries)
- Scalable to 100+ tables

---

## Technology Stack

### **Backend**
| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET** | 8.0 | Application framework |
| **ASP.NET Core** | 8.0 | Web API framework |
| **Entity Framework Core** | 8.0 | ORM for database access |
| **Oracle.EntityFrameworkCore** | 8.0 | Oracle database provider |
| **MediatR** | 12.x | CQRS implementation |
| **FluentValidation** | 11.x | Input validation |
| **Serilog** | 3.x | Structured logging |

### **Database**
| Technology | Version | Purpose |
|------------|---------|---------|
| **Oracle XE** | 21c | Database server |
| **Service** | XEPDB1 | Pluggable database |
| **Schema** | ADMIN | Application schema |

### **Authentication & Security**
| Technology | Version | Purpose |
|------------|---------|---------|
| **JWT Bearer** | - | Stateless authentication |
| **Microsoft.IdentityModel.Tokens** | 7.x | JWT validation |
| **HTTPS** | TLS 1.2+ | Transport security |

### **Testing**
| Technology | Version | Purpose |
|------------|---------|---------|
| **MSTest** | 3.x | Test framework |
| **FluentAssertions** | 6.x | Assertion library |
| **Moq** | 4.x | Mocking framework |
| **Coverlet** | 6.x | Code coverage |
| **ReportGenerator** | 5.x | Coverage reports |

### **Development Tools**
| Technology | Purpose |
|------------|---------|
| **Visual Studio Code** | IDE |
| **Swagger/OpenAPI** | API documentation |
| **Postman** | API testing |
| **SQL Developer** | Database management |

---

## Key Components

### **1. BaseAdminEntity**
Base class for all admin entities with audit tracking:

```csharp
public abstract class BaseAdminEntity
{
    public long Id { get; set; }
    public byte Status { get; set; } = 1;  // 0=Inactive, 1=Active, 2=Pending
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    
    public void MarkDeleted(string deletedBy)
    {
        Status = 0;
        UpdateAuditFields(deletedBy);
    }
    
    public void UpdateAuditFields(string updatedBy)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
```

---

### **2. AdminDbContext**
EF Core context for Oracle database:

```csharp
public class AdminDbContext : DbContext, IAdminDbContext
{
    public DbSet<ScreenDefinition> ScreenDefinitions => Set<ScreenDefinition>();
    public DbSet<ScreenPilot> ScreenPilots => Set<ScreenPilot>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdminDbContext).Assembly);
        
        // Oracle uppercase naming convention
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.GetTableName()?.ToUpperInvariant());
            // ... column name conversion
        }
    }
}
```

---

### **3. EnrichResponseFilter**
Automatically adds metadata to all API responses:

```csharp
public class EnrichResponseFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is OkObjectResult okResult && 
            okResult.Value is BasePageDto basePageDto)
        {
            // Extract user info from JWT claims
            var user = new UserInfo(
                userId: GetClaim(ClaimTypes.NameIdentifier),
                displayName: GetClaim(ClaimTypes.Name),
                email: GetClaim(ClaimTypes.Email)
            );
            
            // Extract access info
            var access = new AccessInfo(
                canRead: true,
                canWrite: HasRole("Admin"),
                roles: GetRoles(),
                dbRoute: GetDbRoute()
            );
            
            // Enrich the response
            basePageDto.User = user;
            basePageDto.Access = access;
            basePageDto.CorrelationId = context.HttpContext.TraceIdentifier;
            basePageDto.ServerTime = DateTimeOffset.UtcNow;
        }
    }
}
```

---

### **4. GlobalExceptionMiddleware**
Centralized exception handling with security:

```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            
            var response = new ErrorResponse
            {
                CorrelationId = context.TraceIdentifier,
                Message = _environment.IsDevelopment() 
                    ? ex.Message 
                    : "An error occurred",
                StatusCode = GetStatusCode(ex)
            };
            
            context.Response.StatusCode = response.StatusCode;
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
```

---

### **5. GenericRepository**
Type-safe CRUD operations for all entities:

```csharp
public class GenericRepository<T> : IGenericRepository<T> 
    where T : BaseAdminEntity, new()
{
    public async Task<PagedResult<T>> GetPagedAsync(
        int pageNumber, 
        int pageSize, 
        Expression<Func<T, bool>>? filter = null)
    {
        var query = _dbSet.AsQueryable();
        
        if (filter != null)
            query = query.Where(filter);
            
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
        return new PagedResult<T>(
            Items: items,
            TotalCount: totalCount,
            PageNumber: pageNumber,
            PageSize: pageSize,
            TotalPages: (int)Math.Ceiling(totalCount / (double)pageSize)
        );
    }
}
```

---

## Security

### **1. Authentication**
- **JWT Bearer Tokens**: Stateless authentication
- **Token Validation**: ValidateIssuerSigningKey, ValidateIssuer, ValidateAudience, ValidateLifetime
- **Claims-Based**: User identity, roles, and permissions in JWT claims

### **2. Authorization**
- **Role-Based Access Control (RBAC)**: `[Authorize(Roles = "Admin,ScreenManager")]`
- **Policy-Based**: Custom policies for fine-grained control
- **Resource-Based**: Check permissions at runtime

### **3. Input Validation**
- **FluentValidation**: Declarative validation rules
- **Model Validation**: Automatic model state validation
- **Custom Attributes**: `[SafeString]`, `[ValidEntityId]`, `[EntityCode]`
- **SQL Injection Prevention**: Parameterized queries via EF Core

### **4. Security Headers**
```
X-Frame-Options: DENY
X-Content-Type-Options: nosniff
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
Content-Security-Policy: default-src 'self'
Permissions-Policy: geolocation=(), camera=(), microphone=()
```

### **5. Error Handling**
- **No Information Disclosure**: Generic error messages in production
- **Sanitized Stack Traces**: Remove sensitive data from logs
- **Correlation IDs**: Track errors across systems
- **Structured Logging**: Separate security events

### **6. Rate Limiting**
- **Request Throttling**: Configurable limits per endpoint
- **IP-Based**: Track requests by client IP
- **User-Based**: Track requests by authenticated user

### **7. CORS**
- **Whitelist Origins**: Only allowed domains
- **Controlled Headers**: Specific headers allowed
- **HTTP Methods**: Only required methods enabled

---

## Testing Strategy

### **Current Test Coverage: 43.5%**

| Layer | Coverage | Tests | Status |
|-------|----------|-------|--------|
| **Domain** | 86.5% | 60 | ✅ Excellent |
| **Application** | 100% | 4 | ✅ Perfect |
| **Contracts** | 65.4% | 28 | ⚠️ Good |
| **API** | 36.8% | 67 | ⚠️ Needs Work |
| **Infrastructure** | 44.8% | 10 | ❌ Needs Work |

### **Test Categories**

#### **1. Unit Tests** (169 tests)
- **Domain Entities**: BaseAdminEntity, BaseLookupEntity, all entities
- **Validation**: FluentValidation validators
- **DTOs**: Record initialization and properties
- **Services**: RequestContextAccessor

#### **2. Integration Tests** (Planned)
- **API Endpoints**: Full request/response cycle
- **Database**: Repository operations with real DB
- **Authentication**: JWT token validation
- **Middleware**: Pipeline execution

#### **3. Test Patterns**
```csharp
// Arrange-Act-Assert (AAA) Pattern
[TestMethod]
public void MarkDeleted_ShouldSetStatusToZero()
{
    // Arrange
    var entity = new ScreenDefinition { Status = 1 };
    
    // Act
    entity.MarkDeleted("test-user");
    
    // Assert
    entity.Status.Should().Be(0);
    entity.UpdatedBy.Should().Be("test-user");
    entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
}
```

### **Test Tools**
- **MSTest**: Test framework
- **FluentAssertions**: Readable assertions
- **Moq**: Mocking dependencies
- **Coverlet**: Code coverage collection
- **ReportGenerator**: HTML coverage reports

### **Running Tests**
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" \
                -targetdir:"TestResults/CoverageReport" \
                -reporttypes:"Html"

# Open report
open TestResults/CoverageReport/index.html
```

---

## Best Practices

### **1. Naming Conventions**
- **Pascal Case**: Classes, methods, properties
- **Camel Case**: Private fields, parameters
- **UPPERCASE**: Database tables and columns (Oracle standard)
- **Descriptive Names**: Clear intent over brevity

### **2. Error Handling**
- **Never swallow exceptions**: Always log
- **Use specific exceptions**: Create custom exceptions
- **Provide context**: Include correlation IDs
- **Sanitize errors**: Remove sensitive data in production

### **3. Async/Await**
- **All I/O operations**: Database, HTTP, file access
- **ConfigureAwait(false)**: Not needed in ASP.NET Core
- **Cancellation tokens**: Support request cancellation
- **Avoid async void**: Use async Task

### **4. Dependency Injection**
- **Constructor injection**: Preferred method
- **Interface-based**: Depend on abstractions
- **Scoped lifetime**: For DbContext and repositories
- **Singleton lifetime**: For stateless services

### **5. Database**
- **Compiled queries**: For frequently used queries
- **Pagination**: Always use for large datasets
- **Indexes**: On foreign keys and search columns
- **Soft deletes**: Status = 0 instead of physical delete

### **6. API Design**
- **RESTful**: Standard HTTP methods and status codes
- **Versioning**: URL path versioning (api/v1/)
- **Pagination**: Query parameters (page, pageSize)
- **Filtering**: Query parameters (status, search)
- **Consistent responses**: Standard envelope format

---

## Configuration

### **appsettings.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=localhost:1521/XEPDB1;User Id=ADMIN;Password=***;"
  },
  "Jwt": {
    "SecretKey": "your-super-secret-key-must-be-at-least-64-characters-long-for-security",
    "Issuer": "https://localhost:5092",
    "Audience": "https://localhost:5092",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "Security": {
    "RequireHttps": true,
    "EnableCors": true,
    "AllowedOrigins": ["https://localhost:3000"],
    "MaxRequestBodySize": 10485760,
    "RateLimitRequests": 100,
    "RateLimitWindowMinutes": 1,
    "EnableHsts": true
  },
  "Database": {
    "EnableSeeding": false,
    "SeedDataPath": "Data/SeedData",
    "CommandTimeout": 30
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/admintool-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

---

## Future Enhancements

### **Planned Features**
- [ ] Command pattern implementation (write operations)
- [ ] Event sourcing for audit trail
- [ ] Redis caching for lookups
- [ ] SignalR for real-time updates
- [ ] GraphQL endpoint
- [ ] Multi-tenancy support
- [ ] Advanced search with full-text indexing
- [ ] Bulk operations API
- [ ] Data import/export functionality
- [ ] Audit log visualization

### **Infrastructure Improvements**
- [ ] Kubernetes deployment
- [ ] Health check endpoints
- [ ] Metrics and monitoring (Prometheus/Grafana)
- [ ] Distributed tracing (OpenTelemetry)
- [ ] Circuit breaker pattern
- [ ] API gateway integration
- [ ] Message queue integration (RabbitMQ/Kafka)

### **Testing Improvements**
- [ ] Increase coverage to 80%+
- [ ] Performance tests
- [ ] Load tests
- [ ] Security penetration tests
- [ ] Mutation testing
- [ ] Integration test suite

---

## License

**Internal Project** - © 2025 abc.bvl

---

## Contact & Support

For questions or support, contact:
- **Team**: AdminTool Development Team
- **Email**: support@abc.bvl
- **Repository**: https://github.com/dhanasekaranr/abc.bvl

---

**Last Updated**: October 26, 2025  
**Version**: 1.0.0  
**Status**: Active Development
