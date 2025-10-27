# Oracle XEPDB1 Setup Instructions

## Step 1: Create APP_USER

Connect to Oracle as SYSDBA:
```powershell
sqlplus sys/oracle@localhost:1521/XEPDB1 as sysdba
```

Run these commands:
```sql
CREATE USER APP_USER IDENTIFIED BY "App_User_Pass@ss1";
GRANT CONNECT, RESOURCE TO APP_USER;
GRANT CREATE SESSION, CREATE TABLE, CREATE SEQUENCE, UNLIMITED TABLESPACE TO APP_USER;
EXIT;
```

## Step 2: Create Tables

Connect as APP_USER:
```powershell
cd c:\temp\abc.bvl\Database
sqlplus APP_USER/"App_User_Pass@ss1"@localhost:1521/XEPDB1
```

Run the scripts:
```sql
@01_Create_Schema_PrimaryDB.sql
@03_Create_OutboxTable.sql
EXIT;
```

## Step 3: Verify Tables

Connect as APP_USER:
```powershell
sqlplus APP_USER/"App_User_Pass@ss1"@localhost:1521/XEPDB1
```

Check tables:
```sql
SELECT table_name FROM user_tables;
SELECT COUNT(*) FROM Admin_ScreenDefn;
SELECT COUNT(*) FROM Admin_ScreenPilot;
SELECT COUNT(*) FROM CVLWebTools_AdminToolOutBox;
EXIT;
```

## Step 4: Restart API and Test

```powershell
cd c:\temp\abc.bvl
dotnet run --project src\abc.bvl.AdminTool.Api
```

Then open http://localhost:5092/swagger
1. GET /api/DevToken/generate
2. Click "Authorize" and paste token
3. GET /api/screendefinition
