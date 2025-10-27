# Oracle Database Configuration Guide

## Overview

The AdminTool API is configured to use **Oracle Database** for production with dual-database support (primary and secondary) for high availability and data synchronization.

**Testing** uses **InMemory Database** for fast, isolated provider/repository integration tests.

---

## Architecture

### Dual-Database Pattern

```
                    ┌──────────────┐
                    │  API Request │
                    └──────┬───────┘
                           │
                   ┌───────▼────────┐
                   │ X-Database     │
                   │ Header Routing │
                   └───────┬────────┘
                           │
              ┌────────────┴────────────┐
              │                         │
       ┌──────▼──────┐          ┌──────▼──────┐
       │  PRIMARY DB │          │ SECONDARY DB│
       │  (Write)    │          │  (Failover) │
       └─────────────┘          └─────────────┘
              │                         │
              └────────────┬────────────┘
                           │
                   ┌───────▼────────┐
                   │  Outbox Table  │
                   │  (Sync Queue)  │
                   └────────────────┘
```

### Database Providers

| Environment | Provider  | Purpose |
|------------|-----------|---------|
| **Production** | Oracle | Dual-database with failover |
| **Development** | Oracle | Local testing with real DB |
| **Testing** | InMemory | Fast, isolated unit/integration tests |

---

## Configuration

### Connection Strings

Update `appsettings.Oracle.json`:

```json
{
  "Database": {
    "Provider": "Oracle",
    "EnableSeeding": false,
    "EnableMigrations": true
  },
  "ConnectionStrings": {
    "AdminDb_Primary": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=primarydb)));User Id=APP_USER;Password=your_password_here;",
    "AdminDb_Secondary": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=secondarydb)));User Id=APP_USER;Password=your_password_here;"
  }
}
```

### Environment Variables (Recommended for Production)

```bash
# PowerShell
$env:ADMIN_DB_PRIMARY_CONNECTION="Data Source=...;User Id=APP_USER;Password=SecurePassword123;"
$env:ADMIN_DB_SECONDARY_CONNECTION="Data Source=...;User Id=APP_USER;Password=SecurePassword123;"

# Bash/Linux
export ADMIN_DB_PRIMARY_CONNECTION="Data Source=...;User Id=APP_USER;Password=SecurePassword123;"
export ADMIN_DB_SECONDARY_CONNECTION="Data Source=...;User Id=APP_USER;Password=SecurePassword123;"
```

---

## Database Routing

### X-Database Header

The API supports database routing via the `X-Database` HTTP header:

| Header Value | Routes To | Use Case |
|-------------|-----------|----------|
| `PRIMARY` or missing | Primary DB | Default - all write operations |
| `SECONDARY` | Secondary DB | Read operations, load balancing |

### Example Requests

```bash
# Route to Primary (default)
curl -X GET http://localhost:5092/api/screendefinition

# Route to Secondary (explicit)
curl -X GET http://localhost:5092/api/screendefinition \
  -H "X-Database: SECONDARY"
```

### CORS Configuration

The `X-Database` header is allowed in CORS:

```csharp
// Program.cs
policy.WithHeaders("Authorization", "Content-Type", "X-DB-Route", "X-Enable-Dual-Write", "X-Database")
```

---

## Entity Framework Configurations

### Table Mappings

| Entity | Oracle Table | Primary Key | Special Notes |
|--------|-------------|-------------|---------------|
| ScreenDefinition | `Admin_ScreenDefn` | `ScreenDefnId` | Hierarchical (self-FK) |
| ScreenPilot | `Admin_ScreenPilot` | `ScreenPilotId` | FK to ScreenDefn |
| OutboxMessage | `CVLWebTools_AdminToolOutBox` | `OutBoxId` | Sync queue |

### Oracle-Specific Configurations

#### ScreenDefinitionConfiguration.cs
```csharp
builder.ToTable("Admin_ScreenDefn");
builder.Property(x => x.Id).HasColumnName("ScreenDefnId").ValueGeneratedOnAdd();
builder.Property(x => x.Code).HasColumnName("ScreenCode").HasMaxLength(50);
builder.Property(x => x.Name).HasColumnName("ScreenName").HasMaxLength(200);
builder.Property(x => x.Status).HasColumnType("NUMBER(3)");
builder.Property(x => x.CreatedAt).HasColumnType("TIMESTAMP(6)");
builder.Property(x => x.RowVersion).IsConcurrencyToken(); // Oracle uses SYS_GUID()
```

#### ScreenPilotConfiguration.cs
```csharp
builder.ToTable("Admin_ScreenPilot");
builder.Property(x => x.Id).HasColumnName("ScreenPilotId").ValueGeneratedOnAdd();
builder.Property(x => x.ScreenDefnId).HasColumnName("ScreenDefnId");
builder.Property(x => x.UserId).HasColumnName("UserId").HasMaxLength(100);
builder.Property(x => x.Status).HasColumnType("NUMBER(3)");
```

#### OutboxMessageConfiguration.cs
```csharp
builder.ToTable("CVLWebTools_AdminToolOutBox");
builder.Property(x => x.Id).HasColumnName("OutBoxId").ValueGeneratedOnAdd();
builder.Property(x => x.Type).HasColumnName("EntityType").HasMaxLength(100);
builder.Property(x => x.Payload).HasColumnType("CLOB"); // Large JSON storage
builder.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("Pending");
```

---

## Transactional Outbox Pattern

### Purpose

Ensures **eventual consistency** between primary and secondary databases when direct writes fail.

### Workflow

```
1. API receives write request
2. Transaction begins on PRIMARY
3. Write to Admin_ScreenDefn
4. Create OutboxMessage (Pending)
5. Commit transaction
6. Background worker picks up Pending messages
7. Replicate to SECONDARY
8. Mark OutboxMessage as Completed
```

### OutboxMessage Properties

| Property | Type | Description |
|----------|------|-------------|
| OutBoxId | NUMBER(19) | Primary key |
| EntityType | VARCHAR2(100) | "ScreenDefinition", "ScreenPilot" |
| EntityId | NUMBER(19) | ID of entity being synced |
| Operation | VARCHAR2(20) | INSERT, UPDATE, DELETE |
| Payload | CLOB | JSON representation of entity |
| Status | VARCHAR2(20) | Pending, Processing, Completed, Failed |
| RetryCount | NUMBER(10) | Number of retry attempts |
| ErrorMessage | VARCHAR2(4000) | Error details if failed |
| SourceDatabase | VARCHAR2(50) | e.g., "primarydb" |
| TargetDatabase | VARCHAR2(50) | e.g., "secondarydb" |
| CorrelationId | VARCHAR2(100) | Request tracking |
| CreatedAt | TIMESTAMP(6) | When message was created |
| ProcessedAt | TIMESTAMP(6) | When successfully processed |

### Monitoring Outbox Messages

```sql
-- Check pending messages
SELECT COUNT(*) FROM CVLWebTools_AdminToolOutBox WHERE Status = 'Pending';

-- Check failed messages
SELECT EntityType, EntityId, ErrorMessage, RetryCount
FROM CVLWebTools_AdminToolOutBox 
WHERE Status = 'Failed'
ORDER BY CreatedAt DESC;

-- Retry failed messages (reset to Pending)
UPDATE CVLWebTools_AdminToolOutBox
SET Status = 'Pending', RetryCount = 0, ErrorMessage = NULL
WHERE Status = 'Failed' AND RetryCount < 5;
COMMIT;
```

---

## Testing Strategy

### Provider/Repository Tests (InMemory)

```csharp
// ScreenDefinitionRepositoryTests.cs
[TestInitialize]
public void Setup()
{
    var options = new DbContextOptionsBuilder<AdminDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Isolated DB per test
        .Options;

    _context = new AdminDbContext(options);
    _repository = new ScreenDefinitionRepository(_context);
}
```

**Why InMemory for Tests?**
- ✅ **Fast**: No network latency, instant execution
- ✅ **Isolated**: Each test gets fresh database
- ✅ **No Setup**: No Oracle installation required
- ✅ **CI/CD Friendly**: Runs on any build agent

### Service/Controller Tests (Mock)

```csharp
// Use Moq for mocking repositories and DbContext
var mockRepo = new Mock<IScreenDefinitionRepository>();
mockRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<ScreenDefinition>());
```

### Integration Tests (Oracle - Optional)

For end-to-end testing with real Oracle database:

```csharp
[TestClass]
public class OracleIntegrationTests
{
    [TestMethod]
    public async Task DatabaseConnectivity_ShouldSucceed()
    {
        var options = new DbContextOptionsBuilder<AdminDbContext>()
            .UseOracle("YOUR_CONNECTION_STRING")
            .Options;

        using var context = new AdminDbContext(options);
        var canConnect = await context.Database.CanConnectAsync();
        Assert.IsTrue(canConnect);
    }
}
```

---

## Database Setup Scripts

Located in `/database/` folder:

| Script | Purpose |
|--------|---------|
| `01_Create_Schema_PrimaryDB.sql` | Create tables, indexes, sequences, sample data for PRIMARY |
| `02_Create_Schema_SecondaryDB.sql` | Mirror schema for SECONDARY |
| `03_Create_OutboxTable.sql` | Outbox table for both databases |

### Running Scripts

```bash
# Connect to primary database
sqlplus APP_USER/YourPassword@localhost:1521/primarydb
@C:\temp\abc.bvl\database\01_Create_Schema_PrimaryDB.sql

# Connect to secondary database
sqlplus APP_USER/YourPassword@localhost:1521/secondarydb
@C:\temp\abc.bvl\database\02_Create_Schema_SecondaryDB.sql

# Create outbox on both
sqlplus APP_USER/YourPassword@localhost:1521/primarydb
@C:\temp\abc.bvl\database\03_Create_OutboxTable.sql

sqlplus APP_USER/YourPassword@localhost:1521/secondarydb
@C:\temp\abc.bvl\database\03_Create_OutboxTable.sql
```

---

## Verification

### 1. Check Table Creation

```sql
SELECT 'Admin_ScreenDefn' AS TableName, COUNT(*) AS RowCount 
FROM APP_USER.Admin_ScreenDefn
UNION ALL
SELECT 'Admin_ScreenPilot', COUNT(*) 
FROM APP_USER.Admin_ScreenPilot
UNION ALL
SELECT 'CVLWebTools_AdminToolOutBox', COUNT(*) 
FROM APP_USER.CVLWebTools_AdminToolOutBox;

-- Expected:
-- Admin_ScreenDefn: 11 rows
-- Admin_ScreenPilot: 9 rows
-- CVLWebTools_AdminToolOutBox: 3 rows (sample messages)
```

### 2. Test API Connectivity

```bash
# Start API
dotnet run --project src/abc.bvl.AdminTool.Api

# Test endpoint (should return sample data)
curl -X GET http://localhost:5092/api/screendefinition
```

### 3. Test Database Routing

```bash
# Primary database (default)
curl -X GET http://localhost:5092/api/screendefinition

# Secondary database (explicit)
curl -X GET http://localhost:5092/api/screendefinition \
  -H "X-Database: SECONDARY"
```

### 4. Run Unit Tests

```bash
# All tests should pass (using InMemory database)
dotnet test tests/abc.bvl.AdminTool.MSTests

# Expected:
# Test summary: total: 45, failed: 0, succeeded: 45, skipped: 0
```

---

## Troubleshooting

### Issue: ORA-12154: TNS:could not resolve the connect identifier

**Solution**: Check `tnsnames.ora` or use full connection string with DESCRIPTION.

```
Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=primarydb)));
```

### Issue: ORA-01017: invalid username/password

**Solution**: Verify APP_USER exists and password is correct.

```sql
-- As SYSTEM
ALTER USER APP_USER IDENTIFIED BY NewPassword123;
```

### Issue: ORA-00942: table or view does not exist

**Solution**: Ensure you're connected to the correct database (primarydb/secondarydb).

```sql
SELECT SYS_CONTEXT('USERENV', 'CON_NAME') AS CurrentDatabase FROM DUAL;
SELECT USER AS CurrentSchema FROM DUAL;
```

### Issue: Tests failing with "property 'X' cannot be added"

**Solution**: Ensure entity classes match EF Core configurations. Remove shadow properties.

### Issue: API cannot connect to Oracle

**Solution**: Check `appsettings.Oracle.json` is being used:

```bash
dotnet run --project src/abc.bvl.AdminTool.Api --environment Oracle
```

---

## Performance Tuning

### Indexes

All critical indexes are created by the DDL scripts:

```sql
-- ScreenDefinition indexes
CREATE UNIQUE INDEX UX_ScreenDefn_Code ON Admin_ScreenDefn(ScreenCode);
CREATE INDEX IX_ScreenDefn_Name ON Admin_ScreenDefn(ScreenName);
CREATE INDEX IX_ScreenDefn_Status ON Admin_ScreenDefn(Status);

-- ScreenPilot indexes
CREATE UNIQUE INDEX UX_ScreenPilot_Screen_User ON Admin_ScreenPilot(ScreenDefnId, UserId);
CREATE INDEX IX_ScreenPilot_UserId ON Admin_ScreenPilot(UserId);

-- Outbox indexes
CREATE INDEX IX_OutBox_Status_Created ON CVLWebTools_AdminToolOutBox(Status, CreatedAt);
CREATE INDEX IX_OutBox_Entity ON CVLWebTools_AdminToolOutBox(EntityType, EntityId);
```

### Query Optimization

EF Core is configured to use compiled queries for frequent operations:

```csharp
// Infrastructure.Data/Repositories/ScreenDefinitionRepository.cs
private static readonly Func<AdminDbContext, IAsyncEnumerable<ScreenDefinition>> _getActiveQuery =
    EF.CompileAsyncQuery((AdminDbContext context) =>
        context.ScreenDefinitions.Where(x => x.Status == 1));
```

### Connection Pooling

Oracle EF Core automatically uses connection pooling. Configure in connection string:

```
Min Pool Size=5;Max Pool Size=100;Connection Timeout=30;
```

---

## Security Best Practices

### 1. Use Environment Variables for Passwords

❌ **DON'T** store passwords in `appsettings.json`:
```json
"Password=MyPassword123"  // ❌ Exposed in source control
```

✅ **DO** use environment variables or Azure Key Vault:
```bash
$env:ADMIN_DB_PRIMARY_CONNECTION="...Password=SecurePassword;"
```

### 2. Restrict Database User Permissions

```sql
-- Grant minimal permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON Admin_ScreenDefn TO APP_USER;
GRANT SELECT, INSERT, UPDATE, DELETE ON Admin_ScreenPilot TO APP_USER;
GRANT SELECT, INSERT, UPDATE, DELETE ON CVLWebTools_AdminToolOutBox TO APP_USER;

-- Revoke dangerous permissions
REVOKE DROP ANY TABLE FROM APP_USER;
REVOKE CREATE USER FROM APP_USER;
```

### 3. Enable SSL/TLS for Oracle Connections

```
Data Source=(...);SSL Wallet Path=/path/to/wallet;
```

### 4. Audit Logging

All tables have audit fields:
- `CreatedAt` / `CreatedBy`
- `UpdatedAt` / `UpdatedBy`

These are automatically populated by `BaseAdminEntity.UpdateAuditFields()`.

---

## Summary

| Component | Configuration | Purpose |
|-----------|--------------|---------|
| **Production API** | Oracle (Primary + Secondary) | High availability |
| **Unit Tests** | InMemory | Fast, isolated testing |
| **Database Routing** | X-Database header | Load balancing |
| **Sync Mechanism** | Outbox Pattern | Eventual consistency |
| **Entity Mappings** | EF Core Configurations | Oracle-specific types |
| **Connection Strings** | appsettings.Oracle.json | Environment-based config |

**Your AdminTool is now ready for Oracle dual-database deployment!** 🚀
