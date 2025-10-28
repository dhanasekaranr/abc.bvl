# Database Routing - Visual Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           CLIENT APPLICATION                                 │
│                                                                              │
│  Option 1: Header              Option 2: Query Param      Option 3: Default │
│  X-Database: Secondary         ?database=Secondary        (no parameter)    │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          ASP.NET Core Pipeline                               │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ 1. Request arrives at Controller                                       │ │
│  │    GET /api/v1/admin/country                                          │ │
│  │    Headers: { "X-Database": "Secondary" }                             │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        DbContextResolver Service                             │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ 2. GetCurrentDatabase()                                                │ │
│  │    - Read X-Database header → "Secondary"                             │ │
│  │    - Or read ?database query param                                     │ │
│  │    - Default to "Primary" if not found                                │ │
│  │                                                                         │ │
│  │ 3. GetDbContext("Secondary")                                           │ │
│  │    - Resolve keyed service from DI                                     │ │
│  │    - serviceProvider.GetRequiredKeyedService<AdminDbContext>("Secondary") │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                       Dependency Injection Container                         │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ Keyed Services (Registered in Program.cs)                              │ │
│  │                                                                         │ │
│  │ Key: "Primary"                    Key: "Secondary"                     │ │
│  │ ┌──────────────────────┐         ┌──────────────────────┐            │ │
│  │ │ AdminDbContext       │         │ AdminDbContext       │            │ │
│  │ │ ─────────────────    │         │ ─────────────────    │            │ │
│  │ │ Connection:          │         │ Connection:          │            │ │
│  │ │ AdminDb_Primary      │         │ AdminDb_Secondary    │            │ │
│  │ │                      │         │                      │            │ │
│  │ │ Oracle Provider      │         │ Oracle Provider      │            │ │
│  │ │ localhost:1521       │         │ localhost:1521       │            │ │
│  │ │ XEPDB1 (Primary)     │         │ XEPDB1 (Secondary)   │            │ │
│  │ └──────────────────────┘         └──────────────────────┘            │ │
│  │           ▲                                 ▲                          │ │
│  │           │                                 │                          │ │
│  │           └──────────┬──────────────────────┘                          │ │
│  │                      │ Selected based on request                       │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            UnitOfWork Pattern                                │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ 4. UnitOfWork.ExecuteAsync()                                           │ │
│  │    - Call _contextFactory() → Returns Secondary DbContext             │ │
│  │    - BeginTransaction()                                                │ │
│  │    - Execute operation (query/insert/update/delete)                    │ │
│  │    - CommitTransaction() or RollbackTransaction()                      │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Oracle Database Tier                                 │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                                                                         │ │
│  │  PRIMARY DATABASE              SECONDARY DATABASE                      │ │
│  │  ┌──────────────────┐          ┌──────────────────┐                   │ │
│  │  │ Oracle XE        │          │ Oracle XE        │                   │ │
│  │  │ localhost:1521   │          │ localhost:1521   │                   │ │
│  │  │ XEPDB1           │          │ XEPDB1           │                   │ │
│  │  │                  │          │                  │                   │ │
│  │  │ Tables:          │          │ Tables:          │                   │ │
│  │  │ - Admin.ScreenDefn│         │ - Admin.ScreenDefn│                  │ │
│  │  │ - Admin.ScreenPilot│        │ - Admin.ScreenPilot│                 │ │
│  │  │ - Lookup.Country │          │ - Lookup.Country │                   │ │
│  │  │ - Lookup.State   │          │ - Lookup.State   │                   │ │
│  │  └──────────────────┘          └──────────────────┘                   │ │
│  │          ▲                              ▲                               │ │
│  │          │                              │                               │ │
│  │          │ Request uses Primary         │ Request uses Secondary       │ │
│  │          │ (Default behavior)           │ (X-Database: Secondary)      │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Request Flow Example

### Scenario: Get Country Data from Secondary Database

```
Step 1: Client Request
──────────────────────────────────────────────────────────────
HTTP GET /api/v1/admin/country
Headers:
  Authorization: Bearer eyJhbGc...
  X-Database: Secondary
──────────────────────────────────────────────────────────────

Step 2: DbContextResolver Detection
──────────────────────────────────────────────────────────────
DbContextResolver.GetCurrentDatabase()
  → Reads X-Database header
  → Returns "Secondary"
──────────────────────────────────────────────────────────────

Step 3: DbContext Resolution
──────────────────────────────────────────────────────────────
DbContextResolver.GetDbContext("Secondary")
  → serviceProvider.GetRequiredKeyedService<AdminDbContext>("Secondary")
  → Returns AdminDbContext configured with AdminDb_Secondary connection
──────────────────────────────────────────────────────────────

Step 4: UnitOfWork Execution
──────────────────────────────────────────────────────────────
UnitOfWork.ExecuteAsync(async (ctx, ct) => {
  var countries = await ctx.Countries
    .Where(c => c.Status == 1)
    .ToListAsync(ct);
  return countries;
});
  → ctx = Secondary DbContext
  → Query executes on Secondary database
──────────────────────────────────────────────────────────────

Step 5: Database Query
──────────────────────────────────────────────────────────────
SELECT * FROM Lookup.COUNTRY WHERE STATUS = 1
  → Executed on SECONDARY Oracle database
──────────────────────────────────────────────────────────────

Step 6: Response
──────────────────────────────────────────────────────────────
HTTP 200 OK
{
  "Success": true,
  "Data": [
    { "Id": 1, "Name": "United States", "Code": "US" },
    { "Id": 2, "Name": "Canada", "Code": "CA" }
  ],
  "UserInfo": { ... },
  "AccessInfo": { ... }
}
──────────────────────────────────────────────────────────────
```

---

## Configuration in Program.cs

```csharp
// ────────────────────────────────────────────────────────────
// Step 1: Register Both DbContexts with Keyed Services
// ────────────────────────────────────────────────────────────

services.AddKeyedScoped<AdminDbContext>("Primary", (sp, key) =>
{
    var optionsBuilder = new DbContextOptionsBuilder<AdminDbContext>();
    optionsBuilder.UseOracle(primaryConnectionString);
    return new AdminDbContext(optionsBuilder.Options);
});

services.AddKeyedScoped<AdminDbContext>("Secondary", (sp, key) =>
{
    var optionsBuilder = new DbContextOptionsBuilder<AdminDbContext>();
    optionsBuilder.UseOracle(secondaryConnectionString);
    return new AdminDbContext(optionsBuilder.Options);
});

// ────────────────────────────────────────────────────────────
// Step 2: Register DbContextResolver
// ────────────────────────────────────────────────────────────

services.AddScoped<IDbContextResolver, DbContextResolver>();

// ────────────────────────────────────────────────────────────
// Step 3: Register UnitOfWork with Factory Pattern
// ────────────────────────────────────────────────────────────

services.AddScoped<IUnitOfWork>(serviceProvider =>
{
    var resolver = serviceProvider.GetRequiredService<IDbContextResolver>();
    return new UnitOfWork(() => resolver.GetDbContext());
});
```

---

## Key Design Decisions

### 1. **Keyed Services (.NET 8)**
- Multiple instances of same type registered with different keys
- Clean separation: `"Primary"` vs `"Secondary"`
- Type-safe resolution

### 2. **Factory Pattern in UnitOfWork**
- `Func<AdminDbContext>` instead of direct injection
- Lazy resolution at execution time
- Supports different database per request

### 3. **Request Context Detection**
- Header: `X-Database: Secondary` (recommended)
- Query: `?database=Secondary` (alternative)
- Default: `"Primary"` (if not specified)

### 4. **Automatic Fallback**
- If Secondary connection not configured → Uses Primary
- Prevents application startup failures
- Graceful degradation

---

## Testing Checklist

- [ ] Set `ADMIN_DB_PRIMARY_CONNECTION` environment variable
- [ ] Set `ADMIN_DB_SECONDARY_CONNECTION` environment variable
- [ ] Start API: `dotnet run --project src/abc.bvl.AdminTool.Api`
- [ ] Test Primary: `GET /api/v1/admin/country` (no header)
- [ ] Test Secondary: `GET /api/v1/admin/country` (with `X-Database: Secondary`)
- [ ] Check logs for: `"Resolved Primary DbContext"` or `"Resolved Secondary DbContext"`
- [ ] Verify different data from each database (if databases differ)

---

**Architecture:** Clean, Flexible, Production-Ready ✅
