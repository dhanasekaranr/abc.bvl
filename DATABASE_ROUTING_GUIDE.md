# Database Routing Implementation Guide

## Overview

The AdminTool API now supports **dual-database routing** - you can route requests to either the **Primary** or **Secondary** database by providing a simple parameter in your API requests.

**Key Features:**
- ✅ **Primary Database (Default)** - All requests go to primary database by default
- ✅ **Secondary Database** - Route specific requests to secondary database via header or query parameter
- ✅ **Automatic Resolution** - DbContextResolver automatically selects the correct database
- ✅ **Transactional Safety** - Each request uses a single database context (no cross-database transactions)
- ✅ **Production Ready** - InMemory provider removed, Oracle-only configuration

---

## Architecture

### Components

1. **DbContextResolver** (`IDbContextResolver`)
   - Resolves the appropriate DbContext based on request context
   - Supports X-Database header and ?database query parameter
   - Defaults to Primary if no routing parameter provided

2. **Keyed DbContext Registration**
   - Primary DbContext: `GetRequiredKeyedService<AdminDbContext>("Primary")`
   - Secondary DbContext: `GetRequiredKeyedService<AdminDbContext>("Secondary")`
   - Uses .NET 8 keyed services for clean separation

3. **UnitOfWork with Dynamic Context**
   - Uses factory pattern: `Func<AdminDbContext>`
   - Resolves DbContext at execution time (lazy)
   - Supports different databases per request

4. **UnitOfWorkFactory**
   - Creates UnitOfWork instances with specific database routing
   - Used when you need explicit control over database selection

---

## Configuration

### appsettings.json

```json
{
  "Database": {
    "Provider": "Oracle",
    "EnableMigrations": false
  },
  "ConnectionStrings": {
    "AdminDb_Primary": "${ADMIN_DB_PRIMARY_CONNECTION}",
    "AdminDb_Secondary": "${ADMIN_DB_SECONDARY_CONNECTION}"
  }
}
```

### Environment Variables

Set these environment variables before starting the application:

**Windows PowerShell:**
```powershell
$env:ADMIN_DB_PRIMARY_CONNECTION = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=primary-db)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XEPDB1)));User Id=APP_USER;Password=your_password;"

$env:ADMIN_DB_SECONDARY_CONNECTION = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=secondary-db)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XEPDB1)));User Id=APP_USER;Password=your_password;"
```

**Linux/Mac:**
```bash
export ADMIN_DB_PRIMARY_CONNECTION="Data Source=...;"
export ADMIN_DB_SECONDARY_CONNECTION="Data Source=...;"
```

---

## How to Use

### Method 1: Using HTTP Header (Recommended)

Add the `X-Database` header to your requests:

**Primary Database (Default):**
```http
GET /api/v1/admin/screendefinition HTTP/1.1
Host: localhost:5092
Authorization: Bearer {token}
```

**Secondary Database:**
```http
GET /api/v1/admin/screendefinition HTTP/1.1
Host: localhost:5092
Authorization: Bearer {token}
X-Database: Secondary
```

### Method 2: Using Query Parameter

Add `?database=Secondary` to the URL:

**Primary Database (Default):**
```http
GET /api/v1/admin/screendefinition
```

**Secondary Database:**
```http
GET /api/v1/admin/screendefinition?database=Secondary
```

---

## Examples

### cURL Examples

**Get from Primary Database (Default):**
```bash
curl -X GET "http://localhost:5092/api/v1/admin/screendefinition" \
  -H "Authorization: Bearer {token}"
```

**Get from Secondary Database (Header):**
```bash
curl -X GET "http://localhost:5092/api/v1/admin/screendefinition" \
  -H "Authorization: Bearer {token}" \
  -H "X-Database: Secondary"
```

**Get from Secondary Database (Query Param):**
```bash
curl -X GET "http://localhost:5092/api/v1/admin/screendefinition?database=Secondary" \
  -H "Authorization: Bearer {token}"
```

### JavaScript/Fetch Examples

**Primary Database:**
```javascript
fetch('http://localhost:5092/api/v1/admin/screendefinition', {
  headers: {
    'Authorization': 'Bearer ' + token
  }
});
```

**Secondary Database:**
```javascript
fetch('http://localhost:5092/api/v1/admin/screendefinition', {
  headers: {
    'Authorization': 'Bearer ' + token,
    'X-Database': 'Secondary'
  }
});
```

### C# HttpClient Examples

**Primary Database:**
```csharp
using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);

var response = await client.GetAsync(
    "http://localhost:5092/api/v1/admin/screendefinition");
```

**Secondary Database:**
```csharp
using var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);
client.DefaultRequestHeaders.Add("X-Database", "Secondary");

var response = await client.GetAsync(
    "http://localhost:5092/api/v1/admin/screendefinition");
```

---

## Supported Operations

All CRUD operations support database routing:

| Operation | Method | Route | Database Routing |
|-----------|--------|-------|------------------|
| **Get All** | GET | `/api/v1/admin/{entityType}` | ✅ Header or Query Param |
| **Get By ID** | GET | `/api/v1/admin/{entityType}/{id}` | ✅ Header or Query Param |
| **Create** | POST | `/api/v1/admin/{entityType}` | ✅ Header or Query Param |
| **Update** | PUT | `/api/v1/admin/{entityType}/{id}` | ✅ Header or Query Param |
| **Delete** | DELETE | `/api/v1/admin/{entityType}/{id}` | ✅ Header or Query Param |

---

## How It Works Internally

### Request Flow

```
1. HTTP Request arrives with X-Database header or ?database query param
   ↓
2. DbContextResolver.GetCurrentDatabase() reads the routing parameter
   ↓
3. DbContextResolver.GetDbContext() resolves the appropriate keyed service
   ↓
4. UnitOfWork uses the resolved DbContext for the operation
   ↓
5. Response returns data from the selected database
```

### Code Flow Example

**Request with X-Database: Secondary**

1. **Controller receives request**
   ```csharp
   // No changes needed in controller - routing is automatic!
   public async Task<ActionResult<SingleResult<PagedResult<ScreenDefnDto>>>> GetAll()
   {
       var result = await _service.GetAllAsync();
       return Ok(SingleSuccess(result));
   }
   ```

2. **DbContextResolver detects header**
   ```csharp
   public string GetCurrentDatabase()
   {
       if (httpContext.Request.Headers.TryGetValue("X-Database", out var header))
       {
           return header.ToString(); // "Secondary"
       }
       return "Primary"; // Default
   }
   ```

3. **UnitOfWork resolves DbContext**
   ```csharp
   // In Program.cs DI registration:
   builder.Services.AddScoped<IUnitOfWork>(serviceProvider =>
   {
       var resolver = serviceProvider.GetRequiredService<IDbContextResolver>();
       return new UnitOfWork(() => resolver.GetDbContext()); // Resolves "Secondary"
   });
   ```

4. **Operation executes on Secondary database**
   ```csharp
   await _unitOfWork.ExecuteAsync(async (ctx, ct) =>
   {
       // ctx is now AdminDbContext for Secondary database
       var data = await ctx.ScreenDefinitions.ToListAsync(ct);
       return data;
   });
   ```

---

## Configuration Options

### Fallback Behavior

If **Secondary connection string is not provided**, the system automatically uses **Primary** as fallback:

```csharp
// In DatabaseConfigurationExtensions.cs
if (!string.IsNullOrWhiteSpace(secondaryConnectionString))
{
    services.AddKeyedScoped<AdminDbContext>("Secondary", ...);
}
else
{
    // Fallback: Register Primary as Secondary
    services.AddKeyedScoped<AdminDbContext>("Secondary", (sp, key) => 
        CreateDbContext(primaryConnectionString));
}
```

### Validation

The system validates configuration on startup:

- ✅ **Provider must be Oracle** - InMemory not allowed in production
- ✅ **Primary connection required** - Application won't start without it
- ✅ **Secondary connection optional** - Falls back to Primary if missing

---

## Programmatic Usage (Advanced)

### Using IDbContextResolver Directly

```csharp
public class MyCustomService
{
    private readonly IDbContextResolver _resolver;

    public MyCustomService(IDbContextResolver resolver)
    {
        _resolver = resolver;
    }

    public async Task DoSomething()
    {
        // Get primary database
        var primaryDb = _resolver.GetDbContext("Primary");
        
        // Get secondary database
        var secondaryDb = _resolver.GetDbContext("Secondary");
        
        // Get database based on current request context
        var currentDb = _resolver.GetDbContext();
    }
}
```

### Using UnitOfWorkFactory

```csharp
public class MyHandler
{
    private readonly IUnitOfWorkFactory _factory;

    public MyHandler(IUnitOfWorkFactory factory)
    {
        _factory = factory;
    }

    public async Task Handle()
    {
        // Explicitly route to Secondary database
        var uow = _factory.Create("Secondary");
        
        await uow.ExecuteAsync(async (ctx, ct) =>
        {
            // Always uses Secondary database
            var data = await ctx.ScreenDefinitions.ToListAsync(ct);
            return data;
        });
    }
}
```

---

## Testing

### Test Different Databases

**1. Test Primary Database:**
```bash
curl -X GET "http://localhost:5092/api/v1/admin/country" \
  -H "Authorization: Bearer {token}"
```

**2. Test Secondary Database:**
```bash
curl -X GET "http://localhost:5092/api/v1/admin/country" \
  -H "Authorization: Bearer {token}" \
  -H "X-Database: Secondary"
```

**3. Verify Logs:**
Check application logs for database resolution:
```
[Debug] Resolving DbContext for database: Secondary
[Info] Resolved Secondary DbContext
```

---

## Best Practices

### ✅ DO

- **Use X-Database header** for routing in production
- **Default to Primary** for most operations
- **Route read queries to Secondary** to reduce load on primary
- **Use Primary for writes** to ensure consistency
- **Log database routing** for audit and debugging

### ❌ DON'T

- **Don't use query parameter in production** (use header instead for cleaner URLs)
- **Don't mix databases in a single transaction** (not supported)
- **Don't assume Secondary is available** (could fall back to Primary)
- **Don't use InMemory provider** (removed from production code)

---

## Troubleshooting

### Issue: Always connects to Primary even with X-Database: Secondary

**Solution:** Check connection string environment variable is set:
```powershell
# PowerShell
$env:ADMIN_DB_SECONDARY_CONNECTION

# If empty, set it:
$env:ADMIN_DB_SECONDARY_CONNECTION = "Data Source=...;"
```

### Issue: Build error about AddKeyedScoped

**Solution:** Ensure you're using .NET 8.0 or higher (keyed services introduced in .NET 8):
```xml
<TargetFramework>net8.0</TargetFramework>
```

### Issue: Repository still using old DbContext

**Solution:** Repositories receive DbContext via UnitOfWork operation, not constructor injection. The routing happens automatically.

---

## Migration from Old Code

### Before (Single Database)

```csharp
builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseOracle(connectionString));
```

### After (Dual Database with Routing)

```csharp
// Primary DbContext
builder.Services.AddKeyedScoped<AdminDbContext>("Primary", ...);

// Secondary DbContext
builder.Services.AddKeyedScoped<AdminDbContext>("Secondary", ...);

// DbContextResolver for routing
builder.Services.AddScoped<IDbContextResolver, DbContextResolver>();

// UnitOfWork with dynamic resolution
builder.Services.AddScoped<IUnitOfWork>(sp =>
{
    var resolver = sp.GetRequiredService<IDbContextResolver>();
    return new UnitOfWork(() => resolver.GetDbContext());
});
```

---

## Summary

### What Changed?

1. ✅ **Removed InMemory Database** - Production uses Oracle only
2. ✅ **Added Keyed DbContext Services** - Primary and Secondary registered separately
3. ✅ **Added DbContextResolver** - Automatic database routing from request headers
4. ✅ **Updated UnitOfWork** - Uses factory pattern for dynamic DbContext resolution
5. ✅ **Updated UnitOfWorkFactory** - Supports explicit database routing

### What Stayed the Same?

- ✅ **Controllers unchanged** - No code changes needed
- ✅ **Handlers unchanged** - Continue using IUnitOfWork as before
- ✅ **APIs backward compatible** - Default behavior is Primary database

### What's New?

- ✅ **X-Database header** - Route to Secondary database
- ✅ **?database query parameter** - Alternative routing method
- ✅ **IDbContextResolver** - Programmatic database selection
- ✅ **Automatic fallback** - Uses Primary if Secondary not configured

---

## Next Steps

1. ✅ **Set environment variables** for both connection strings
2. ✅ **Test with Postman/cURL** using X-Database header
3. ✅ **Monitor logs** to verify correct database resolution
4. ✅ **Update client applications** to use routing headers
5. ✅ **Document database routing** in API documentation

---

**Last Updated:** October 27, 2025  
**Version:** 2.0.0  
**Status:** ✅ Production Ready
