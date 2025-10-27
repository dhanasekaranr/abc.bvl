# Database Configuration Guide

The AdminTool API supports multiple database providers through configuration files. No code changes are required to switch between databases.

## 🎛️ **Configuration Options**

### **Database Settings**
```json
{
  "Database": {
    "Provider": "InMemory|Oracle",
    "EnableSeeding": true|false,
    "EnableMigrations": true|false
  }
}
```

## 📋 **Available Configurations**

### **1. In-Memory Database (Default)**
```bash
# Uses appsettings.json
dotnet run --project src/abc.bvl.AdminTool.Api
```

**Configuration (`appsettings.json`):**
```json
{
  "Database": {
    "Provider": "InMemory",
    "EnableSeeding": true,
    "EnableMigrations": false
  }
}
```

**Features:**
- ✅ No database setup required
- ✅ Automatic data seeding
- ✅ Perfect for development and testing
- ✅ Fast startup

### **2. Oracle Database (Production)**
```bash
# Uses appsettings.Oracle.json
dotnet run --project src/abc.bvl.AdminTool.Api --environment Oracle
```

**Configuration (`appsettings.Oracle.json`):**
```json
{
  "Database": {
    "Provider": "Oracle",
    "EnableSeeding": false,
    "EnableMigrations": true
  },
  "ConnectionStrings": {
    "AdminDb_Primary": "Data Source=localhost:1521/XE;User Id=ADMINTOOL;Password=your_password;"
  }
}
```

**Features:**
- ✅ Production-ready Oracle database
- ✅ Real persistence
- ✅ Migration support
- ✅ No automatic seeding (uses existing data)

### **3. Testing Environment**
```bash
# Uses appsettings.Testing.json
dotnet run --project src/abc.bvl.AdminTool.Api --environment Testing
```

**Configuration (`appsettings.Testing.json`):**
```json
{
  "Database": {
    "Provider": "InMemory",
    "EnableSeeding": true,
    "EnableMigrations": false
  }
}
```

## 🚀 **How to Switch Configurations**

### **Method 1: Environment Variable**
```bash
# Set environment
$env:ASPNETCORE_ENVIRONMENT="Oracle"
dotnet run --project src/abc.bvl.AdminTool.Api

# Or for specific session
dotnet run --project src/abc.bvl.AdminTool.Api --environment Oracle
```

### **Method 2: Direct Configuration Override**
Create a custom `appsettings.MyEnvironment.json` file and run:
```bash
dotnet run --project src/abc.bvl.AdminTool.Api --environment MyEnvironment
```

### **Method 3: Runtime Configuration**
Update `appsettings.json` directly:
```json
{
  "Database": {
    "Provider": "Oracle"  // Change this value
  },
  "ConnectionStrings": {
    "AdminDb_Primary": "your_oracle_connection_string"
  }
}
```

## 🔧 **Oracle Database Setup**

### **1. Create Oracle Schema**
Run the DDL script provided in `/database/oracle-schema.sql`:
```sql
-- Creates tables: Admin_ScreenDefn, Admin_ScreenPilot, CVLWebTools_AdminToolOutBox
-- Creates sequences and indexes
-- Inserts sample data
```

### **2. Update Connection String**
```json
{
  "ConnectionStrings": {
    "AdminDb_Primary": "Data Source=your-server:1521/YOUR_SID;User Id=ADMINTOOL;Password=your_password;"
  }
}
```

### **3. Test Connection**
```bash
# Test with Oracle environment
dotnet run --project src/abc.bvl.AdminTool.Api --environment Oracle
```

## 🧪 **Unit Testing**

Tests automatically use in-memory database:
```bash
# All tests use in-memory database regardless of app configuration
dotnet test tests/abc.bvl.AdminTool.Tests/abc.bvl.AdminTool.Tests.csproj
```

**Test Features:**
- ✅ Isolated in-memory database per test
- ✅ Fast execution
- ✅ No external dependencies
- ✅ Automatic test data seeding

## 📊 **Configuration Summary**

| Environment | Provider | Seeding | Migrations | Use Case |
|-------------|----------|---------|------------|----------|
| **Development** | InMemory | ✅ | ❌ | Local development |
| **Oracle** | Oracle | ❌ | ✅ | Production with existing DB |
| **Testing** | InMemory | ✅ | ❌ | Automated testing |
| **Unit Tests** | InMemory | Manual | ❌ | Unit/Integration tests |

## 🎯 **Benefits**

- ✅ **No Code Changes**: Switch databases via configuration only
- ✅ **Environment Flexibility**: Different configs for different environments
- ✅ **Fast Development**: In-memory database with instant startup
- ✅ **Production Ready**: Oracle support for real applications
- ✅ **Testing Friendly**: Isolated in-memory tests
- ✅ **Easy Migration**: From development to production seamlessly