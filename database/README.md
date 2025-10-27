# Oracle Database Setup Guide - AdminTool

## Overview

This guide helps you set up Oracle Database XE for the AdminTool. The current configuration uses **Oracle XE 21c** with the **XEPDB1** pluggable database. For production dual-database support, see the advanced setup section.

---

## Prerequisites

- **Oracle Database XE 21c** (or higher)
- **SQL*Plus** or Oracle SQL Developer
- **Service Name:** XEPDB1
- **User:** APP_USER
- **Password:** App_User_Pass@ss1 (change for production)

---

## Quick Start (Local Development)

### 1. Create APP_USER (Run as SYSDBA)

```powershell
# Connect to XEPDB1 as SYSDBA
sqlplus sys/oracle@localhost:1521/XEPDB1 as sysdba

# Run the setup script
@C:\temp\abc.bvl\database\00_Setup_Oracle_XEPDB1.sql
```

Or manually execute:

```sql
-- Create user
CREATE USER APP_USER IDENTIFIED BY App_User_Pass@ss1;

-- Grant privileges
GRANT CONNECT, RESOURCE TO APP_USER;
GRANT CREATE SESSION, CREATE TABLE, CREATE SEQUENCE, UNLIMITED TABLESPACE TO APP_USER;

-- Verify
SELECT username, account_status, created FROM dba_users WHERE username = 'APP_USER';
EXIT;
```

### 2. Create Tables and Sample Data

```powershell
# Connect as APP_USER
sqlplus APP_USER/App_User_Pass@ss1@localhost:1521/XEPDB1

# Create ScreenDefinition and ScreenPilot tables
@C:\temp\abc.bvl\database\01_Create_Schema_PrimaryDB.sql

# Create Outbox table
@C:\temp\abc.bvl\database\03_Create_OutboxTable.sql
```

### 3. Verify Installation

```sql
-- Check tables created
SELECT table_name FROM user_tables ORDER BY table_name;

-- Should see:
-- ADMIN_SCREENDEFN
-- ADMIN_SCREENPILOT
-- CVLWEBTOOLS_ADMINTOOLOUTBOX

-- Check row counts
SELECT 'ADMIN_SCREENDEFN' AS table_name, COUNT(*) AS row_count FROM ADMIN_SCREENDEFN
UNION ALL
SELECT 'ADMIN_SCREENPILOT', COUNT(*) FROM ADMIN_SCREENPILOT
UNION ALL
SELECT 'CVLWEBTOOLS_ADMINTOOLOUTBOX', COUNT(*) FROM CVLWEBTOOLS_ADMINTOOLOUTBOX;

-- Expected:
-- ADMIN_SCREENDEFN: 11 rows
-- ADMIN_SCREENPILOT: 9 rows
-- CVLWEBTOOLS_ADMINTOOLOUTBOX: 6 rows
```

---

## Database Schema

### Tables

1. **ADMIN_SCREENDEFN** - Screen/menu definitions with hierarchy
2. **ADMIN_SCREENPILOT** - User-to-screen access assignments
3. **CVLWEBTOOLS_ADMINTOOLOUTBOX** - Transactional outbox for eventual consistency

### Sequences

1. **SEQ_ADMIN_SCREENDEFN** - Auto-increment for SCREENDEFNID (starts at 1000)
2. **SEQ_ADMIN_SCREENPILOT** - Auto-increment for SCREENPILOTID (starts at 100)
3. **SEQ_CVLWEBTOOLS_ADMINTOOLOUTBOX** - Auto-increment for OUTBOXID (starts at 1)

---

## Connection String

**Development (appsettings.Development.json):**

```json
{
  "ConnectionStrings": {
    "AdminDb_Primary": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XEPDB1)));User Id=APP_USER;Password=App_User_Pass@ss1;",
    "AdminDb_Secondary": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XEPDB1)));User Id=APP_USER;Password=App_User_Pass@ss1;"
  }
}
```

**Production (use environment variables):**

```powershell
$env:ADMIN_DB_PRIMARY_CONNECTION="Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=prod-oracle-primary)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=PRODDB1)));User Id=APP_USER;Password=SecurePassword;"
$env:ADMIN_DB_SECONDARY_CONNECTION="Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=prod-oracle-secondary)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=PRODDB2)));User Id=APP_USER;Password=SecurePassword;"
```

---

## Table Details

### ADMIN_SCREENDEFN

**Purpose:** Stores screen/menu definitions with hierarchical structure

| Column | Type | Description |
|--------|------|-------------|
| SCREENDEFNID | NUMBER(19) | Primary key |
| SCREENCODE | VARCHAR2(50) | Unique code (e.g., 'DASH001') |
| SCREENNAME | VARCHAR2(200) | Display name |
| SCREENDESC | VARCHAR2(500) | Description |
| STATUS | NUMBER(3) | 0=Inactive, 1=Active, 2=Pending |
| SCREENURL | VARCHAR2(500) | Route/URL |
| PARENTSCREENID | NUMBER(19) | Parent menu FK (nullable) |
| DISPLAYORDER | NUMBER(10) | Sort order |
| ICONCLASS | VARCHAR2(100) | CSS icon class |
| CREATEDAT | TIMESTAMP(6) | Creation timestamp |
| CREATEDBY | VARCHAR2(100) | Created by user |
| UPDATEDAT | TIMESTAMP(6) | Update timestamp |
| UPDATEDBY | VARCHAR2(100) | Updated by user |
| ROWVERSION | VARCHAR2(50) | Concurrency control |

**Sample Data:**
- Dashboard (DASH001)
- Administration (ADM001) → User Mgmt, Screen Mgmt, Role Mgmt, Settings
- Reports (REP001) → Usage Report, Audit Report

### ADMIN_SCREENPILOT

**Purpose:** User-to-screen access assignments

| Column | Type | Description |
|--------|------|-------------|
| SCREENPILOTID | NUMBER(19) | Primary key |
| SCREENDEFNID | NUMBER(19) | FK to ADMIN_SCREENDEFN |
| USERID | VARCHAR2(100) | User identifier |
| STATUS | NUMBER(3) | 0=Inactive, 1=Active |
| CREATEDAT | TIMESTAMP(6) | Creation timestamp |
| CREATEDBY | VARCHAR2(100) | Created by user |
| UPDATEDAT | TIMESTAMP(6) | Update timestamp |
| UPDATEDBY | VARCHAR2(100) | Updated by user |
| ROWVERSION | VARCHAR2(50) | Concurrency control |

### CVLWEBTOOLS_ADMINTOOLOUTBOX

**Purpose:** Transactional outbox pattern for cross-database synchronization

| Column | Type | Description |
|--------|------|-------------|
| OUTBOXID | NUMBER(19) | Primary key |
| ENTITYTYPE | VARCHAR2(100) | Entity type name |
| ENTITYID | NUMBER(19) | Entity ID |
| OPERATION | VARCHAR2(20) | INSERT/UPDATE/DELETE |
| PAYLOAD | CLOB | JSON data |
| CREATEDAT | TIMESTAMP(6) | Creation timestamp |
| PROCESSEDAT | TIMESTAMP(6) | Processing timestamp |
| STATUS | VARCHAR2(20) | Pending/Processing/Completed/Failed |
| RETRYCOUNT | NUMBER(10) | Retry attempts |
| ERRORMESSAGE | VARCHAR2(4000) | Error details |
| SOURCEDATABASE | VARCHAR2(50) | Source DB |
| TARGETDATABASE | VARCHAR2(50) | Target DB |
| CORRELATIONID | VARCHAR2(100) | Request correlation |

---

## Useful Queries

### Get Menu Hierarchy

```sql
SELECT 
    LEVEL AS tree_level,
    LPAD(' ', (LEVEL-1)*2, ' ') || SCREENNAME AS screen_hierarchy,
    SCREENCODE,
    SCREENURL,
    STATUS
FROM ADMIN_SCREENDEFN
WHERE STATUS = 1
START WITH PARENTSCREENID IS NULL
CONNECT BY PRIOR SCREENDEFNID = PARENTSCREENID
ORDER SIBLINGS BY DISPLAYORDER;
```

### Get User Access Rights

```sql
SELECT 
    sp.USERID,
    sd.SCREENCODE,
    sd.SCREENNAME,
    sd.SCREENURL,
    sp.STATUS AS assignment_status
FROM ADMIN_SCREENPILOT sp
JOIN ADMIN_SCREENDEFN sd ON sp.SCREENDEFNID = sd.SCREENDEFNID
WHERE sp.USERID = 'john.doe'
  AND sp.STATUS = 1
  AND sd.STATUS = 1
ORDER BY sd.DISPLAYORDER;
```

### Check Outbox Status

```sql
SELECT 
    STATUS,
    COUNT(*) AS message_count,
    MIN(CREATEDAT) AS oldest_message,
    MAX(CREATEDAT) AS newest_message
FROM CVLWEBTOOLS_ADMINTOOLOUTBOX
GROUP BY STATUS
ORDER BY STATUS;
```

---

## Troubleshooting

### ORA-12514: TNS:listener does not currently know of service

**Solution:** Verify service name is XEPDB1:

```sql
-- Check available services
SELECT name, con_id FROM v$active_services ORDER BY name;
```

### ORA-00942: table or view does not exist

**Solution:** Oracle stores identifiers in UPPERCASE unless quoted. Use:

```sql
SELECT * FROM ADMIN_SCREENDEFN;  -- Correct
-- Not: SELECT * FROM Admin_ScreenDefn;
```

### Tables exist but EF Core can't find them

**Solution:** Ensure all column mappings use UPPERCASE in C# configurations:

```csharp
builder.ToTable("ADMIN_SCREENDEFN", "APP_USER");
builder.Property(x => x.Id).HasColumnName("SCREENDEFNID");
```

---

## Files Reference

- **00_Setup_Oracle_XEPDB1.sql** - User creation (run as SYSDBA)
- **01_Create_Schema_PrimaryDB.sql** - Main tables (ADMIN_SCREENDEFN, ADMIN_SCREENPILOT)
- **03_Create_OutboxTable.sql** - Outbox table (CVLWEBTOOLS_ADMINTOOLOUTBOX)
- **Setup-Oracle.ps1** - PowerShell automation script

---

## Next Steps

1. ✅ Create APP_USER
2. ✅ Run schema scripts
3. ⏭️ Start .NET API and verify connection
4. ⏭️ Test CRUD operations via Swagger
5. ⏭️ Implement outbox worker for dual-DB sync (if needed)

---

**Your Oracle XE database is ready!** 🎉

---

## Prerequisites

- Oracle Database 19c or higher
- SQL*Plus or Oracle SQL Developer
- User: **APP_USER**
- Databases: **primarydb** and **secondarydb**

---

## Database Schema

### Tables Created

1. **Admin_ScreenDefn** - Screen/menu definitions
2. **Admin_ScreenPilot** - User-to-screen assignments
3. **CVLWebTools_AdminToolOutBox** - Transactional outbox for dual-DB sync

### Sequences Created

1. **SEQ_Admin_ScreenDefn** - Auto-increment for ScreenDefnId
2. **SEQ_Admin_ScreenPilot** - Auto-increment for ScreenPilotId
3. **SEQ_CVLWebTools_AdminToolOutBox** - Auto-increment for OutBoxId

---

## Installation Steps

### Step 1: Create APP_USER (Run as SYSTEM/DBA)

```sql
-- Connect as SYSTEM
sqlplus sys/<password>@localhost:1521/XE as sysdba

-- Switch to primary database
ALTER SESSION SET CONTAINER = primarydb;

-- Create user
CREATE USER APP_USER IDENTIFIED BY YourSecurePassword123;

-- Grant privileges
GRANT CONNECT, RESOURCE TO APP_USER;
GRANT CREATE SESSION, CREATE TABLE, CREATE SEQUENCE, CREATE VIEW TO APP_USER;
GRANT UNLIMITED TABLESPACE TO APP_USER;

-- Repeat for secondary database
ALTER SESSION SET CONTAINER = secondarydb;
CREATE USER APP_USER IDENTIFIED BY YourSecurePassword123;
GRANT CONNECT, RESOURCE TO APP_USER;
GRANT CREATE SESSION, CREATE TABLE, CREATE SEQUENCE, CREATE VIEW TO APP_USER;
GRANT UNLIMITED TABLESPACE TO APP_USER;
```

---

### Step 2: Run DDL/DML Scripts

#### Option A: Using SQL*Plus

```bash
# Connect to primary database
sqlplus APP_USER/YourSecurePassword123@localhost:1521/primarydb

# Run primary database script
@C:\temp\abc.bvl\database\01_Create_Schema_PrimaryDB.sql

# Connect to secondary database
sqlplus APP_USER/YourSecurePassword123@localhost:1521/secondarydb

# Run secondary database script
@C:\temp\abc.bvl\database\02_Create_Schema_SecondaryDB.sql

# Run outbox table on both databases
sqlplus APP_USER/YourSecurePassword123@localhost:1521/primarydb
@C:\temp\abc.bvl\database\03_Create_OutboxTable.sql

sqlplus APP_USER/YourSecurePassword123@localhost:1521/secondarydb
@C:\temp\abc.bvl\database\03_Create_OutboxTable.sql
```

#### Option B: Using Oracle SQL Developer

1. Open SQL Developer
2. Create connection to `primarydb` as `APP_USER`
3. Open `01_Create_Schema_PrimaryDB.sql`
4. Execute script (F5)
5. Repeat for `secondarydb` with `02_Create_Schema_SecondaryDB.sql`
6. Run `03_Create_OutboxTable.sql` on both databases

---

### Step 3: Verify Installation

```sql
-- Check table counts
SELECT 'Admin_ScreenDefn' AS TableName, COUNT(*) AS RowCount 
FROM APP_USER.Admin_ScreenDefn
UNION ALL
SELECT 'Admin_ScreenPilot', COUNT(*) 
FROM APP_USER.Admin_ScreenPilot
UNION ALL
SELECT 'CVLWebTools_AdminToolOutBox', COUNT(*) 
FROM APP_USER.CVLWebTools_AdminToolOutBox;

-- Expected results:
-- Admin_ScreenDefn: 11 rows
-- Admin_ScreenPilot: 9 rows
-- CVLWebTools_AdminToolOutBox: 3 rows (sample messages)
```

---

### Step 4: Update Connection Strings in .NET API

Update your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "AdminDb_Primary": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=primarydb)));User Id=APP_USER;Password=YourSecurePassword123;",
    "AdminDb_Secondary": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=secondarydb)));User Id=APP_USER;Password=YourSecurePassword123;"
  }
}
```

Or using environment variables (recommended for production):

```bash
# PowerShell
$env:ADMIN_DB_PRIMARY_CONNECTION="Data Source=...;User Id=APP_USER;Password=YourSecurePassword123;"
$env:ADMIN_DB_SECONDARY_CONNECTION="Data Source=...;User Id=APP_USER;Password=YourSecurePassword123;"
```

---

## Database Schema Details

### Admin_ScreenDefn Table

| Column | Type | Description |
|--------|------|-------------|
| ScreenDefnId | NUMBER(19) | Primary key (auto-increment) |
| ScreenCode | VARCHAR2(50) | Unique screen code (e.g., 'DASH001') |
| ScreenName | VARCHAR2(200) | Display name |
| ScreenDesc | VARCHAR2(500) | Description |
| Status | NUMBER(3) | 0=Inactive, 1=Active, 2=Pending |
| ScreenUrl | VARCHAR2(500) | Route/URL path |
| ParentScreenId | NUMBER(19) | Parent menu item (nullable) |
| DisplayOrder | NUMBER(10) | Sort order |
| IconClass | VARCHAR2(100) | CSS icon class |
| CreatedAt | TIMESTAMP(6) | Creation timestamp |
| CreatedBy | VARCHAR2(100) | Created by user |
| UpdatedAt | TIMESTAMP(6) | Last update timestamp |
| UpdatedBy | VARCHAR2(100) | Last updated by user |
| RowVersion | VARCHAR2(50) | Concurrency control |

**Sample Data:**
- Dashboard (DASH001)
- Administration Menu (ADM001)
  - User Management (ADM_USR001)
  - Screen Management (ADM_SCR001)
  - Role Management (ADM_ROL001)
  - System Settings (ADM_SET001)
- Reports Menu (REP001)
  - Usage Report (REP_USG001)
  - Audit Report (REP_AUD001)
- Test Screen (TEST001) - Inactive
- Beta Feature (BETA001) - Pending

---

### Admin_ScreenPilot Table

| Column | Type | Description |
|--------|------|-------------|
| ScreenPilotId | NUMBER(19) | Primary key (auto-increment) |
| ScreenDefnId | NUMBER(19) | FK to Admin_ScreenDefn |
| UserId | VARCHAR2(100) | User identifier |
| Status | NUMBER(3) | 0=Inactive, 1=Active |
| UpdatedAt | TIMESTAMP(6) | Last update timestamp |
| UpdatedBy | VARCHAR2(100) | Last updated by user |
| RowVersion | VARCHAR2(50) | Concurrency control |
| ScreenCode | VARCHAR2(50) | Denormalized screen code |
| ScreenName | VARCHAR2(200) | Denormalized screen name |

**Sample Data:**
- john.doe → Dashboard, Usage Report
- jane.smith → Dashboard, Screen Management
- admin.user → Dashboard, User Management, Screen Management, Role Management, Audit Report

---

### CVLWebTools_AdminToolOutBox Table

| Column | Type | Description |
|--------|------|-------------|
| OutBoxId | NUMBER(19) | Primary key (auto-increment) |
| EntityType | VARCHAR2(100) | Entity type (ScreenDefinition, ScreenPilot) |
| EntityId | NUMBER(19) | ID of the entity |
| Operation | VARCHAR2(20) | INSERT, UPDATE, DELETE |
| Payload | CLOB | JSON payload |
| CreatedAt | TIMESTAMP(6) | Creation timestamp |
| ProcessedAt | TIMESTAMP(6) | Processing timestamp |
| Status | VARCHAR2(20) | Pending, Processing, Completed, Failed |
| RetryCount | NUMBER(10) | Retry attempt count |
| ErrorMessage | VARCHAR2(4000) | Error details |
| SourceDatabase | VARCHAR2(50) | Origin database |
| TargetDatabase | VARCHAR2(50) | Destination database |
| CorrelationId | VARCHAR2(100) | Request correlation ID |

---

## Sample Queries

### Get All Active Screens with Hierarchy

```sql
SELECT 
    LEVEL AS TreeLevel,
    LPAD(' ', (LEVEL-1)*2) || s.ScreenName AS ScreenHierarchy,
    s.ScreenCode,
    s.ScreenUrl,
    s.DisplayOrder
FROM APP_USER.Admin_ScreenDefn s
WHERE s.Status = 1
START WITH s.ParentScreenId IS NULL
CONNECT BY PRIOR s.ScreenDefnId = s.ParentScreenId
ORDER SIBLINGS BY s.DisplayOrder;
```

### Get User's Assigned Screens

```sql
SELECT 
    sp.UserId,
    sd.ScreenCode,
    sd.ScreenName,
    sd.ScreenUrl,
    sp.Status AS AssignmentStatus
FROM APP_USER.Admin_ScreenPilot sp
JOIN APP_USER.Admin_ScreenDefn sd ON sp.ScreenDefnId = sd.ScreenDefnId
WHERE sp.UserId = 'john.doe'
  AND sp.Status = 1
  AND sd.Status = 1
ORDER BY sd.DisplayOrder;
```

### Check Pending Outbox Messages

```sql
SELECT 
    EntityType,
    EntityId,
    Operation,
    CreatedAt,
    RetryCount,
    SUBSTR(Payload, 1, 100) AS PayloadPreview
FROM APP_USER.CVLWebTools_AdminToolOutBox
WHERE Status = 'Pending'
ORDER BY CreatedAt;
```

---

## Testing Dual-Database Setup

### Test 1: Insert in Primary, Check Outbox

```sql
-- Connect to primarydb
INSERT INTO APP_USER.Admin_ScreenDefn 
(ScreenDefnId, ScreenCode, ScreenName, ScreenDesc, Status, CreatedBy, UpdatedBy)
VALUES 
(SEQ_Admin_ScreenDefn.NEXTVAL, 'TEST_NEW', 'Test New Screen', 'Testing dual-write', 1, 'test.user', 'test.user');

-- This should create an outbox message
SELECT * FROM APP_USER.CVLWebTools_AdminToolOutBox 
WHERE EntityType = 'ScreenDefinition' 
ORDER BY CreatedAt DESC 
FETCH FIRST 1 ROWS ONLY;
```

### Test 2: Verify Data Consistency

```sql
-- Run on primarydb
SELECT COUNT(*) AS PrimaryCount FROM APP_USER.Admin_ScreenDefn;

-- Run on secondarydb
SELECT COUNT(*) AS SecondaryCount FROM APP_USER.Admin_ScreenDefn;

-- Counts should match after outbox processing
```

---

## Maintenance Scripts

### Cleanup Old Outbox Messages

```sql
-- Delete completed messages older than 30 days
DELETE FROM APP_USER.CVLWebTools_AdminToolOutBox
WHERE Status = 'Completed'
  AND ProcessedAt < SYSTIMESTAMP - INTERVAL '30' DAY;

COMMIT;
```

### Reset Failed Outbox Messages

```sql
-- Reset failed messages for retry (max 5 retries)
UPDATE APP_USER.CVLWebTools_AdminToolOutBox
SET Status = 'Pending',
    RetryCount = 0,
    ErrorMessage = NULL
WHERE Status = 'Failed'
  AND RetryCount < 5;

COMMIT;
```

### Check Sequence Values

```sql
-- Check current sequence values
SELECT 'SEQ_Admin_ScreenDefn' AS SequenceName, SEQ_Admin_ScreenDefn.CURRVAL AS CurrentValue FROM DUAL
UNION ALL
SELECT 'SEQ_Admin_ScreenPilot', SEQ_Admin_ScreenPilot.CURRVAL FROM DUAL
UNION ALL
SELECT 'SEQ_CVLWebTools_AdminToolOutBox', SEQ_CVLWebTools_AdminToolOutBox.CURRVAL FROM DUAL;
```

---

## Troubleshooting

### Issue: ORA-01843: not a valid month

**Solution:** Check timestamp format in DML statements.

### Issue: ORA-00001: unique constraint violated

**Solution:** Check for duplicate ScreenCode or duplicate ScreenDefnId/UserId in ScreenPilot.

### Issue: Tables not found

**Solution:** Ensure you're connected to the correct database and schema:
```sql
SELECT SYS_CONTEXT('USERENV', 'CON_NAME') AS CurrentDatabase FROM DUAL;
SELECT USER AS CurrentSchema FROM DUAL;
```

### Issue: Insufficient privileges

**Solution:** Grant required permissions as SYSTEM:
```sql
GRANT SELECT, INSERT, UPDATE, DELETE ON APP_USER.Admin_ScreenDefn TO <your_user>;
```

---

## Next Steps

1. ✅ Install database schema (this guide)
2. ⏭️ Configure connection strings in .NET API
3. ⏭️ Test API endpoints
4. ⏭️ Implement outbox worker for dual-write synchronization
5. ⏭️ Set up monitoring and alerts

---

## Summary

**Files Created:**
- `01_Create_Schema_PrimaryDB.sql` - Primary database setup
- `02_Create_Schema_SecondaryDB.sql` - Secondary database setup
- `03_Create_OutboxTable.sql` - Outbox table for both databases

**Sample Data:**
- 11 Screen Definitions (menu hierarchy)
- 9 Screen Pilot assignments (user access)
- 3 Outbox messages (for testing)

**Your databases are ready for the AdminTool API!** 🚀
