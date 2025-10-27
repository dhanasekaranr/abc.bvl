# ============================================================================
# Oracle XEPDB1 Setup Script for AdminTool
# Prerequisites: Oracle XE installed and running
# ============================================================================

Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "Oracle XEPDB1 Setup for AdminTool" -ForegroundColor Cyan
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$OracleHost = "localhost"
$OraclePort = "1521"
$ServiceName = "XEPDB1"
$SysPassword = Read-Host "Enter SYS password for Oracle XE" -AsSecureString
$SysPasswordText = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($SysPassword))

Write-Host ""
Write-Host "STEP 1: Creating APP_USER..." -ForegroundColor Yellow

# Create user creation script
$CreateUserScript = @"
CREATE USER APP_USER IDENTIFIED BY App_User_Pass@ss1;
GRANT CONNECT, RESOURCE TO APP_USER;
GRANT CREATE SESSION, CREATE TABLE, CREATE SEQUENCE, UNLIMITED TABLESPACE TO APP_USER;
SELECT username, account_status FROM dba_users WHERE username = 'APP_USER';
EXIT;
"@

$CreateUserScript | Out-File -FilePath ".\temp_create_user.sql" -Encoding ASCII

# Run as SYSDBA
Write-Host "Connecting as SYSDBA to create user..." -ForegroundColor Gray
echo "$SysPasswordText" | sqlplus -S sys@${OracleHost}:${OraclePort}/${ServiceName} as sysdba @temp_create_user.sql

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ APP_USER created successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to create APP_USER" -ForegroundColor Red
    Remove-Item ".\temp_create_user.sql" -ErrorAction SilentlyContinue
    exit 1
}

Remove-Item ".\temp_create_user.sql" -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "STEP 2: Creating tables..." -ForegroundColor Yellow

# Run table creation scripts as APP_USER
Write-Host "Creating Admin_ScreenDefn and Admin_ScreenPilot tables..." -ForegroundColor Gray
sqlplus -S APP_USER/App_User_Pass@ss1@${OracleHost}:${OraclePort}/${ServiceName} @01_Create_Schema_PrimaryDB.sql

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Admin tables created successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to create admin tables" -ForegroundColor Red
    exit 1
}

Write-Host "Creating CVLWebTools_AdminToolOutBox table..." -ForegroundColor Gray
sqlplus -S APP_USER/App_User_Pass@ss1@${OracleHost}:${OraclePort}/${ServiceName} @03_Create_OutboxTable.sql

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Outbox table created successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to create outbox table" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host "Oracle XEPDB1 setup completed successfully!" -ForegroundColor Green
Write-Host "============================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Database Information:" -ForegroundColor Yellow
Write-Host "  Host: $OracleHost" -ForegroundColor Gray
Write-Host "  Port: $OraclePort" -ForegroundColor Gray
Write-Host "  Service: $ServiceName" -ForegroundColor Gray
Write-Host "  User: APP_USER" -ForegroundColor Gray
Write-Host "  Password: App_User_Pass@ss1" -ForegroundColor Gray
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Restart the API: dotnet run --project src\abc.bvl.AdminTool.Api" -ForegroundColor Gray
Write-Host "  2. Open Swagger: http://localhost:5092/swagger" -ForegroundColor Gray
Write-Host "  3. Get token: GET /api/DevToken/generate" -ForegroundColor Gray
Write-Host "  4. Test endpoints: GET /api/screendefinition" -ForegroundColor Gray
Write-Host ""
