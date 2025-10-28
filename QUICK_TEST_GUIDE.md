# Quick Test Guide - Database Routing

## Prerequisites

1. Set environment variables (PowerShell):
```powershell
$env:ADMIN_DB_PRIMARY_CONNECTION = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XEPDB1)));User Id=APP_USER;Password=your_password;"

$env:ADMIN_DB_SECONDARY_CONNECTION = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XEPDB1)));User Id=APP_USER;Password=your_password;"
```

2. Start the API:
```powershell
cd c:\temp\abc.bvl
dotnet run --project src/abc.bvl.AdminTool.Api
```

---

## Test 1: Default Primary Database

### Request
```bash
curl -X GET "http://localhost:5092/api/v1/admin/country"
```

### Expected Log Output
```
[Debug] Resolving DbContext for database: Primary
[Info] Resolved Primary DbContext
```

---

## Test 2: Route to Secondary via Header

### Request
```bash
curl -X GET "http://localhost:5092/api/v1/admin/country" \
  -H "X-Database: Secondary"
```

### Expected Log Output
```
[Debug] Database routing header found: Secondary
[Debug] Resolving DbContext for database: Secondary
[Info] Resolved Secondary DbContext
```

---

## Test 3: Route to Secondary via Query Parameter

### Request
```bash
curl -X GET "http://localhost:5092/api/v1/admin/country?database=Secondary"
```

### Expected Log Output
```
[Debug] Database routing query parameter found: Secondary
[Debug] Resolving DbContext for database: Secondary
[Info] Resolved Secondary DbContext
```

---

## Test 4: PowerShell Example

```powershell
# Test Primary (default)
Invoke-RestMethod -Uri "http://localhost:5092/api/v1/admin/country" -Method GET

# Test Secondary (header)
$headers = @{ "X-Database" = "Secondary" }
Invoke-RestMethod -Uri "http://localhost:5092/api/v1/admin/country" -Headers $headers -Method GET

# Test Secondary (query param)
Invoke-RestMethod -Uri "http://localhost:5092/api/v1/admin/country?database=Secondary" -Method GET
```

---

## Verify in Logs

Check `logs/` folder or console output for:

```
[INF] Request starting HTTP/1.1 GET http://localhost:5092/api/v1/admin/country
[DBG] Database routing header found: Secondary
[DBG] Resolving DbContext for database: Secondary
[INF] Resolved Secondary DbContext
[INF] Request finished HTTP/1.1 GET http://localhost:5092/api/v1/admin/country - 200
```

---

## Database Connection Verification

To verify different databases are being used, you can temporarily modify the connection strings to point to different databases and confirm data differences.

**Example:**
1. Insert different data in Primary vs Secondary databases
2. Query with `X-Database: Primary` → See Primary data
3. Query with `X-Database: Secondary` → See Secondary data

---

## Troubleshooting

### Issue: Always returns Primary data
**Solution:** Check environment variable is set:
```powershell
$env:ADMIN_DB_SECONDARY_CONNECTION
# Should output the connection string, not blank
```

### Issue: No debug logs visible
**Solution:** Update `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "abc.bvl.AdminTool.Api.Services": "Debug"
    }
  }
}
```

---

**Status:** Ready to test!
