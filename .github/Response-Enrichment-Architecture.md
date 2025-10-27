# Response Enrichment Architecture

## Overview

All API responses automatically include `UserInfo` and `AccessInfo` metadata via the `EnrichResponseFilter`. This eliminates code duplication and ensures consistent response structure across all endpoints.

## Architecture Components

### 1. BasePageDto (Contracts Layer)
```csharp
public abstract record BasePageDto
{
    public string CorrelationId { get; init; }
    public DateTimeOffset ServerTime { get; init; }
    public UserInfo? User { get; init; }        // ← Automatically populated
    public AccessInfo? Access { get; init; }    // ← Automatically populated
    public PaginationInfo? Pagination { get; init; }
}
```

### 2. Response Wrappers
- **PagedResult<T>**: For list responses with pagination
- **SingleResult<T>**: For single item responses

Both inherit from `BasePageDto` and automatically get enriched.

### 3. EnrichResponseFilter (API Layer)
Automatically enriches all `BasePageDto` responses with:
- **UserInfo**: Extracted from JWT claims (UserId, DisplayName, Email)
- **AccessInfo**: Extracted from JWT roles and headers (CanRead, CanWrite, Roles, DbRoute)
- **CorrelationId**: From HttpContext.TraceIdentifier

### 4. BaseApiController (API Layer)
Provides helper methods available to all controllers:
- `GetUserInfo()` - Manual access if needed
- `GetAccessInfo()` - Manual access if needed
- `SingleSuccess<T>(data)` - Standardized single item response
- `PagedSuccess<T>(items, page, size, total)` - Standardized paged response

## How It Works

### Automatic Enrichment Flow

```
1. Controller returns OkObjectResult with BasePageDto derivative
                    ↓
2. EnrichResponseFilter.OnResultExecuting() intercepts
                    ↓
3. Filter extracts claims from HttpContext.User
                    ↓
4. Filter populates User and Access properties
                    ↓
5. Response sent to Angular with full metadata
```

### Request/Response Example

**Request:**
```http
GET /api/v1/admin/screen-definition/screens?page=1&pageSize=20
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...
X-DB-Route: primary
```

**Response (Automatically Enriched):**
```json
{
  "items": [
    { "id": 1, "name": "Dashboard", "status": 1 },
    { "id": 2, "name": "Reports", "status": 1 }
  ],
  "correlationId": "0HN7VQKJ3M1KL:00000001",
  "serverTime": "2025-10-25T10:30:00Z",
  "user": {
    "userId": "user123",
    "displayName": "John Doe",
    "email": "john.doe@company.com"
  },
  "access": {
    "canRead": true,
    "canWrite": true,
    "roles": ["Admin", "ScreenManager"],
    "dbRoute": "primary"
  },
  "pagination": {
    "currentPage": 1,
    "pageSize": 20,
    "totalItems": 42,
    "totalPages": 3,
    "hasPrevious": false,
    "hasNext": true
  }
}
```

## Controller Implementation

### Simple Approach (Recommended)
Controllers just return data - enrichment happens automatically:

```csharp
public class ScreenDefinitionController : BaseApiController
{
    [HttpGet("screens")]
    public async Task<ActionResult<PagedResult<ScreenDefnDto>>> GetScreens(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var items = await _mediator.Send(new GetScreenDefinitionsQuery());
        var totalCount = await _mediator.Send(new GetScreenDefinitionsCountQuery());
        
        // User/Access will be auto-populated by filter
        return PagedSuccess(items, page, pageSize, totalCount);
    }
}
```

### Manual Control (If Needed)
If you need to customize User/Access info:

```csharp
[HttpGet("screens")]
public async Task<ActionResult<PagedResult<ScreenDefnDto>>> GetScreens()
{
    var items = await _mediator.Send(new GetScreenDefinitionsQuery());
    var totalCount = await _mediator.Send(new GetScreenDefinitionsCountQuery());
    
    // Manually specify User/Access (will NOT be overwritten by filter)
    var result = new PagedResult<ScreenDefnDto>(
        items,
        currentPage: 1,
        pageSize: 20,
        totalItems: totalCount,
        User: GetUserInfo(),           // Custom user info
        Access: GetAccessInfo(),       // Custom access info
        CorrelationId: HttpContext.TraceIdentifier
    );
    
    return Ok(result);
}
```

## Angular Integration

### TypeScript Interfaces

```typescript
export interface BasePageDto {
  correlationId: string;
  serverTime: string;
  user?: UserInfo;
  access?: AccessInfo;
  pagination?: PaginationInfo;
}

export interface UserInfo {
  userId: string;
  displayName: string;
  email: string;
}

export interface AccessInfo {
  canRead: boolean;
  canWrite: boolean;
  roles: string[];
  dbRoute: string;
}

export interface PagedResult<T> extends BasePageDto {
  items: T[];
}

export interface SingleResult<T> extends BasePageDto {
  data: T;
}
```

### Angular Service

```typescript
@Injectable()
export class ScreenDefinitionService {
  constructor(private http: HttpClient) {}
  
  getScreens(page: number, pageSize: number): Observable<PagedResult<ScreenDefnDto>> {
    return this.http.get<PagedResult<ScreenDefnDto>>(
      `/api/v1/admin/screen-definition/screens?page=${page}&pageSize=${pageSize}`
    );
  }
}
```

### Angular Component

```typescript
export class ScreenListComponent implements OnInit {
  screens: ScreenDefnDto[] = [];
  currentUser?: UserInfo;
  canEdit: boolean = false;
  canDelete: boolean = false;
  
  ngOnInit() {
    this.screenService.getScreens(1, 20).subscribe(response => {
      this.screens = response.items;
      
      // Use metadata for UI control
      this.currentUser = response.user;
      this.canEdit = response.access?.canWrite ?? false;
      this.canDelete = response.access?.roles?.includes('Admin') ?? false;
      
      // Use pagination info
      if (response.pagination) {
        this.totalPages = response.pagination.totalPages;
        this.hasNext = response.pagination.hasNext;
      }
      
      // Use correlation ID for error tracking
      console.log('Request ID:', response.correlationId);
    });
  }
}
```

### Angular Template (Disable Controls Based on Access)

```html
<table>
  <tr *ngFor="let screen of screens">
    <td>{{ screen.name }}</td>
    <td>
      <button 
        [disabled]="!canEdit" 
        (click)="editScreen(screen)">
        Edit
      </button>
      <button 
        [disabled]="!canDelete" 
        (click)="deleteScreen(screen)">
        Delete
      </button>
    </td>
  </tr>
</table>

<div class="user-info">
  Logged in as: {{ currentUser?.displayName }} ({{ currentUser?.email }})
</div>
```

## Configuration

### Global Registration (Recommended)
In `Program.cs`:
```csharp
builder.Services.AddControllers(options =>
{
    options.Filters.Add<EnrichResponseFilter>(); // ← All responses enriched
});
```

### Selective Registration (Per Controller)
```csharp
[ApiController]
[EnrichResponse] // ← Only this controller's responses enriched
public class ScreenDefinitionController : ControllerBase
{
}
```

### Selective Registration (Per Action)
```csharp
[HttpGet]
[EnrichResponse] // ← Only this action's responses enriched
public async Task<ActionResult<PagedResult<ScreenDefnDto>>> GetScreens()
{
}
```

## Benefits

### 1. **Zero Code Duplication**
- No need to call `GetUserInfo()` and `GetAccessInfo()` in every controller action
- Metadata automatically added to all responses

### 2. **Consistent Response Structure**
- Every response has the same metadata structure
- Frontend knows exactly what to expect

### 3. **Centralized Logic**
- User/Access extraction logic in one place
- Easy to update claim mappings globally

### 4. **UI-Friendly**
- Angular components can enable/disable controls based on roles
- No separate API calls for user permissions

### 5. **Traceable Requests**
- Every response includes CorrelationId
- Easy to track requests across distributed systems

### 6. **Future-Proof**
- Easy to add new metadata fields (e.g., tenant ID, organization)
- Change claim mappings without touching controllers

## Advanced Scenarios

### Custom Access Logic
Override `GetAccessInfo()` in specific controllers:

```csharp
public class ReportController : BaseApiController
{
    protected override AccessInfo GetAccessInfo()
    {
        var baseAccess = base.GetAccessInfo();
        
        // Custom logic: Can only write if user is in "ReportAdmin" role
        return baseAccess with
        {
            CanWrite = User.IsInRole("ReportAdmin")
        };
    }
}
```

### Conditional Enrichment
Skip enrichment for specific endpoints:

```csharp
[HttpGet("public/health")]
[SkipEnrichment] // Custom attribute
public IActionResult HealthCheck()
{
    return Ok(new { status = "healthy" });
}
```

### Multi-Tenant Support
Add tenant ID to AccessInfo:

```csharp
var tenantId = User.FindFirst("tenant_id")?.Value ?? "default";
var accessInfo = new AccessInfo(...) with { TenantId = tenantId };
```

## Testing

The filter is automatically tested via integration tests:

```csharp
[TestMethod]
public async Task GetScreens_ShouldIncludeUserInfo()
{
    var result = await _controller.GetScreens();
    var pagedResult = (result.Result as OkObjectResult)?.Value as PagedResult<ScreenDefnDto>;
    
    pagedResult.User.Should().NotBeNull();
    pagedResult.User.UserId.Should().Be("test-user-id");
}
```

## Performance Considerations

- **Minimal Overhead**: Filter only runs on successful (200 OK) responses
- **No Database Calls**: All data extracted from in-memory JWT claims
- **Async-Safe**: Works with async controller actions
- **Request Scoped**: Claims are already parsed by ASP.NET Core authentication

## Migration Path

### Existing Controllers
No changes needed! Controllers that already populate User/Access will continue to work. The filter only populates null properties.

### New Controllers
Just inherit from `BaseApiController` and use helper methods:
```csharp
return PagedSuccess(items, page, pageSize, total); // Auto-enriched
```

## Summary

✅ **Zero code duplication** - Metadata added automatically  
✅ **Consistent responses** - All responses have same structure  
✅ **UI-friendly** - Angular can disable/enable controls  
✅ **Traceable** - Every response has CorrelationId  
✅ **Future-proof** - Easy to extend  
✅ **Performance** - Minimal overhead  
✅ **Testable** - Integration tests verify enrichment  
✅ **Clean Architecture** - Separation of concerns maintained  
