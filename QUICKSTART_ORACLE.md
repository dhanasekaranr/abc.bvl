# Quick Start - Oracle Database Setup

## TL;DR - Get Running in 5 Minutes

### 1. Prerequisites

- ✅ Oracle 19c or higher installed
- ✅ Two databases created: `primarydb` and `secondarydb`
- ✅ User `APP_USER` with password

### 2. Deploy Database

```powershell
# Run in PowerShell
cd C:\temp\abc.bvl\database

# Primary DB
sqlplus APP_USER/YourPassword@localhost:1521/primarydb @01_Create_Schema_PrimaryDB.sql

# Secondary DB
sqlplus APP_USER/YourPassword@localhost:1521/secondarydb @02_Create_Schema_SecondaryDB.sql

# Outbox Table (on both)
sqlplus APP_USER/YourPassword@localhost:1521/primarydb @03_Create_OutboxTable.sql
sqlplus APP_USER/YourPassword@localhost:1521/secondarydb @03_Create_OutboxTable.sql
```

### 3. Update Connection String

Edit `src/abc.bvl.AdminTool.Api/appsettings.Oracle.json`:

```json
{
  "ConnectionStrings": {
    "AdminDb_Primary": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=primarydb)));User Id=APP_USER;Password=YOUR_PASSWORD;",
    "AdminDb_Secondary": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=secondarydb)));User Id=APP_USER;Password=YOUR_PASSWORD;"
  }
}
```

### 4. Run API

```powershell
dotnet run --project src/abc.bvl.AdminTool.Api --environment Oracle
```

### 5. Test It

```powershell
# Test primary database (default)
curl http://localhost:5092/api/screendefinition

# Test secondary database
curl http://localhost:5092/api/screendefinition -H "X-Database: SECONDARY"
```

### 6. Verify Tests Still Work

```powershell
dotnet test
# Expected: total: 45, failed: 0, succeeded: 45
```

---

## Database Routing

| Request | Routes To | Usage |
|---------|-----------|-------|
| No header | PRIMARY | Default (writes) |
| `X-Database: PRIMARY` | PRIMARY | Explicit primary |
| `X-Database: SECONDARY` | SECONDARY | Read-only queries |

---

## What Was Changed?

### Production API
- ✅ Uses **Oracle** for primary/secondary databases
- ✅ Routes requests based on `X-Database` header
- ✅ Transactional outbox for sync

### Tests
- ✅ Still use **InMemory** database
- ✅ Fast, isolated, no setup needed
- ✅ **Zero test code changes**

---

## Sample Data Included

After running scripts, you get:

- **11 screens** (Dashboard, Admin menu, Reports menu, etc.)
- **9 user-screen assignments** (john.doe, jane.smith, admin.user)
- **3 outbox messages** (sample sync data)

---

## Key Files

| File | Purpose |
|------|---------|
| `appsettings.Oracle.json` | Connection strings |
| `database/01_Create_Schema_PrimaryDB.sql` | Primary DB setup |
| `database/02_Create_Schema_SecondaryDB.sql` | Secondary DB setup |
| `database/03_Create_OutboxTable.sql` | Outbox table |
| `docs/ORACLE_SETUP.md` | Full documentation |
| `docs/ORACLE_MIGRATION_SUMMARY.md` | What changed |

---

## Troubleshooting

### Cannot connect to Oracle?

```sql
-- Check if database is accessible
sqlplus APP_USER/YourPassword@localhost:1521/primarydb
```

### Tables not created?

```sql
-- Verify tables exist
SELECT table_name FROM user_tables WHERE table_name LIKE 'ADMIN_%';
-- Should show: ADMIN_SCREENDEFN, ADMIN_SCREENPILOT
```

### Tests failing?

```powershell
# Clean and rebuild
dotnet clean
dotnet build
dotnet test
```

---

## Next Steps

1. ✅ Deploy to your Oracle instances
2. ✅ Update passwords in config
3. ✅ Test API endpoints
4. 🔄 Implement outbox background worker (optional)
5. 🔄 Set up monitoring and alerts

---

**That's it! Your API now runs on Oracle with dual-database support.** 🚀

For detailed documentation, see:
- `docs/ORACLE_SETUP.md` - Complete setup guide
- `docs/ORACLE_MIGRATION_SUMMARY.md` - All changes made
- `database/README.md` - Database setup reference
