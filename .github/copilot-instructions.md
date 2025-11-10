# AdminTool - .NET 8 Web API with Dual-DB Routing and Transactional Outbox

This workspace contains a .NET 8 Web API solution for AdminTool that manages CRUD operations for static master tables with the following key features:

## Architecture
- **Clean Architecture**: Domain, Application, Infrastructure, and API layers
- **Dual-DB Routing**: Support for primary/secondary database routing via headers  
- **Transactional Outbox Pattern**: Ensures data consistency across databases
- **Aggregate Root Pattern**: Single controller manages related domain entities
- **Namespace**: abc.bvl.*
- **CQRS with MediatR**: Command Query Responsibility Segregation pattern

## Key Components
- **PilotEnablement**: Aggregate root managing user screen access (consolidates ScreenDefinition + ScreenPilot)
- **OutboxMessage**: Asynchronous data synchronization between databases (CVLWebTools.AdminToolOutBox table)
- **Generic Pagination**: Reusable PaginatedGroupQuery<T> for grouped data with database-level filtering

## Project Structure
```
AdminTool/
├─ bvlwebtools.sln                               # Solution file
├─ src/
│  ├─ abc.bvl.AdminTool.Api/                     # Host, controllers, middleware, filters
│  ├─ abc.bvl.AdminTool.Application/             # Handlers, UoW, abstractions, generic patterns
│  ├─ abc.bvl.AdminTool.Contracts/               # Public DTOs (API contracts)
│  ├─ abc.bvl.AdminTool.Domain/                  # Entities (no EF deps)
│  ├─ abc.bvl.AdminTool.Infrastructure.Data/     # DbContexts, EF configs, compiled queries  
│  └─ abc.bvl.AdminTool.Infrastructure.Replication/ # Outbox worker
```

## Development Guidelines
- Use Entity Framework Core for data access
- Implement repository pattern with Unit of Work
- Follow SOLID principles and Clean Architecture
- Use async/await throughout
- Use MediatR for CQRS pattern implementation
- **DTOs are Commands**: Use `IRequest<T>` directly, no separate Command classes
- **Database-level pagination**: Use IQueryable for filtering before execution
- Implement proper error handling and logging
- API responses wrapped in standard envelope format

## Current Status
- ✅ Project structure created with correct naming (abc.bvl.AdminTool.*)
- ✅ **Aggregate Root Pattern**: PilotEnablementController manages entire domain
- ✅ **Direct DTO Commands**: No separate Request/Result/Command classes needed
- ✅ **Database-level Pagination**: Two-phase query for grouped data (200K→100 rows)
- ✅ **Generic PaginatedGroupQuery<T>**: Reusable pattern for future tables
- ✅ **Internal DTOs**: Application layer has internal models, public API uses Contracts
- ✅ Domain entities (ScreenDefinition, ScreenPilot, Country, State)
- ✅ EF Core configurations and DbContext setup
- ✅ VS Code tasks for build and run
- ✅ API running successfully on http://localhost:5092
- ✅ **Transactional Outbox Pattern** fully implemented for dual-DB replication

## Architecture Highlights
- **Aggregate Root Pattern**: Single controller per domain (PilotEnablement manages ScreenDefn + ScreenPilot)
- **Direct DTO Commands**: DTOs implement IRequest<T>, no separate Command classes
- **Two-phase Pagination**: Database-level filtering (200K records → only ~100 rows loaded)
- **Generic PaginatedGroupQuery<T>**: Reusable for any grouped data scenario
- **Internal vs Public DTOs**: Application layer uses internal models, Contracts layer exposes public API
- **Clean Architecture** with DDD principles
- **Eventual consistency** via transactional outbox pattern
- **Performance optimized**: IQueryable for database-level operations

## Pagination Pattern
```csharp
// Example: Paginate users with their screen assignments
var results = _repository
    .GetAllQueryable(status)
    .GroupByPaginated(
        groupKeySelector: p => p.UserId,
        resultSelector: g => new PilotEnablementDto { ... })
    .OrderGroupKeysBy(keys => keys.OrderBy(x => x))
    .WhereGroupKey(keys => keys.Where(x => x.Contains(searchTerm)))
    .Paginate(page, pageSize)
    .ExecuteAsync(cancellationToken);
```

## Outbox Pattern Implementation
- ✅ **OutboxMessage Entity** - Domain entity for replication events
- ✅ **IOutboxRepository** - Data access for outbox operations
- ✅ **IOutboxPublisher** - Service for publishing events
- ✅ **OutboxProcessor** - Background service for polling and replication
- ✅ **Configuration** - Configurable polling interval, batch size, retry policy
- ✅ **DI Registration** - Extension method for easy setup
- ✅ **Atomicity** - Events saved in same transaction as domain changes
- ⚠️ **Secondary DB Replication** - Stub implementation (needs actual DB logic)

## Next Development Steps
1. ✅ **Aggregate Root pattern** - PilotEnablementController consolidates domain
2. ✅ **Performance optimization** - Database-level pagination implemented
3. ✅ **Generic patterns** - PaginatedGroupQuery reusable for all tables
4. 🔄 **Complete secondary DB replication** - Implement actual replication logic
5. 🔄 **Memory caching layer** - Static lookup data optimization
6. 🔄 **Generic testing framework** - Parameterized tests for all entities
7. 🔄 **Code generation pipeline** - Auto-create entities from DB schema
8. 🔄 **Increase test coverage** - Target 80% (currently 43.5%)