-- ============================================================================
-- Complete Oracle XEPDB1 Setup Script
-- Run this as SYSDBA first to create the user
-- Then run the table creation scripts as APP_USER
-- ============================================================================

-- STEP 1: Run this block as SYSDBA
-- Command: sqlplus sys/oracle@localhost:1521/XEPDB1 as sysdba
-- ============================================================================

-- Create APP_USER
CREATE USER APP_USER IDENTIFIED BY App_User_Pass@ss1;

-- Grant privileges
GRANT CONNECT, RESOURCE TO APP_USER;
GRANT CREATE SESSION, CREATE TABLE, CREATE SEQUENCE, UNLIMITED TABLESPACE TO APP_USER;

-- Verify user created
SELECT username, account_status, created 
FROM dba_users 
WHERE username = 'APP_USER';

EXIT;

-- ============================================================================
-- STEP 2: After creating user, run table creation scripts as APP_USER
-- Command: sqlplus APP_USER/App_User_Pass@ss1@localhost:1521/XEPDB1
-- ============================================================================

-- Run from PowerShell:
-- cd c:\temp\abc.bvl\Database
-- sqlplus APP_USER/App_User_Pass@ss1@localhost:1521/XEPDB1 @01_Create_Schema_PrimaryDB.sql
-- sqlplus APP_USER/App_User_Pass@ss1@localhost:1521/XEPDB1 @03_Create_OutboxTable.sql
