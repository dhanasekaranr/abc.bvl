# Integration Tests - abc.bvl.AdminTool.Tests

This project contains **integration tests** that connect to an actual Oracle database to test repository operations without mocks.

## Test Naming Convention

All integration test files follow the `*ITests.cs` pattern to distinguish them from unit tests:
- `ScreenDefinitionRepositoryITests.cs`
- `ScreenPilotRepositoryITests.cs`

This allows easy filtering in CI/CD pipelines.

## Prerequisites

1. **Oracle Database Access**
   - Accessible Oracle database instance
   - Schema: `CVLWebTools`
   - Tables: `ScreenDefinition`, `ScreenPilot`, `AdminToolOutBox`

2. **Connection String**
   - Set environment variable: `ADMIN_DB_INTEGRATION_CONNECTION`
   - Format: `User Id=username;Password=password;Data Source=hostname:port/servicename`

3. **Test Data Range**
   - Integration tests use IDs >= 900000
   - Automatically cleaned up before/after tests
   - Safe to run against shared dev/test databases

## Running Integration Tests

### Run All Tests
```bash
dotnet test abc.bvl.AdminTool.Tests
```

### Run Only Integration Tests (by pattern)
```bash
dotnet test abc.bvl.AdminTool.Tests --filter "FullyQualifiedName~ITests"
```

### Exclude Integration Tests (for CI/CD)
```bash
# Run only unit tests (exclude integration tests)
dotnet test --filter "FullyQualifiedName!~ITests"
```

## Configuration

### appsettings.Integration.json
```json
{
  "ConnectionStrings": {
    "AdminDb_Integration": "${ADMIN_DB_INTEGRATION_CONNECTION}"
  },
  "IntegrationTestSettings": {
    "TestDataKeyPrefix": "900000",
    "CleanupAfterTests": true,
    "EnableDatabaseLogging": true,
    "TimeoutSeconds": 300
  }
}
```

### Environment Variables
```bash
# Windows
set ADMIN_DB_INTEGRATION_CONNECTION="User Id=testuser;Password=testpass;Data Source=localhost:1521/XEPDB1"

# Linux/Mac
export ADMIN_DB_INTEGRATION_CONNECTION="User Id=testuser;Password=testpass;Data Source=localhost:1521/XEPDB1"
```

## Test Structure

### DatabaseFixture
- Shared xUnit collection fixture
- Manages database context lifecycle
- Provides cleanup utilities

### Test Organization
```
Integration/
├── DatabaseFixture.cs                          # Shared database setup
├── ScreenDefinitionRepositoryITests.cs         # 12 integration tests
└── ScreenPilotRepositoryITests.cs             # 16 integration tests
```

## Test Coverage

### ScreenDefinitionRepository (12 tests)
- ✅ Create operations
- ✅ Read operations (by ID, all, filtered)
- ✅ Update operations
- ✅ Delete operations
- ✅ Pagination
- ✅ Count queries
- ✅ Concurrent updates
- ✅ Bulk inserts

### ScreenPilotRepository (16 tests)
- ✅ Create operations
- ✅ Read operations (by ID, user, screen)
- ✅ Update operations
- ✅ Delete operations
- ✅ Status filtering
- ✅ DualMode flag handling
- ✅ Foreign key constraints
- ✅ Bulk operations
- ✅ IQueryable support

## CI/CD Integration

### GitHub Actions Example
```yaml
name: CI

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
      - name: Run Unit Tests
        run: dotnet test --filter "FullyQualifiedName!~ITests"

  integration-tests:
    runs-on: ubuntu-latest
    needs: unit-tests
    steps:
      - uses: actions/checkout@v3
      - name: Setup Oracle
        # Setup Oracle database container
      - name: Run Integration Tests
        env:
          ADMIN_DB_INTEGRATION_CONNECTION: ${{ secrets.INTEGRATION_DB_CONNECTION }}
        run: dotnet test --filter "FullyQualifiedName~ITests"
```

### Azure DevOps Example
```yaml
stages:
- stage: UnitTests
  jobs:
  - job: RunUnitTests
    steps:
    - task: DotNetCoreCLI@2
      inputs:
        command: 'test'
        arguments: '--filter "FullyQualifiedName!~ITests"'

- stage: IntegrationTests
  dependsOn: UnitTests
  jobs:
  - job: RunIntegrationTests
    steps:
    - task: DotNetCoreCLI@2
      inputs:
        command: 'test'
        arguments: '--filter "FullyQualifiedName~ITests"'
      env:
        ADMIN_DB_INTEGRATION_CONNECTION: $(IntegrationDbConnection)
```

## Best Practices

1. **Data Isolation**: Tests use ID range >= 900000
2. **Cleanup**: Automatic cleanup before and after each test
3. **Independence**: Tests don't rely on execution order
4. **Realistic Scenarios**: Tests real database operations
5. **FK Constraints**: Tests verify referential integrity
6. **Concurrency**: Tests handle concurrent updates

## Troubleshooting

### Connection Issues
```
Error: Cannot connect to integration test database
Solution: Verify ADMIN_DB_INTEGRATION_CONNECTION is set correctly
```

### FK Violations
```
Error: ORA-02291: integrity constraint violated
Solution: Ensure test screens (900001, 900002) are created in InitializeAsync
```

### Cleanup Failures
```
Warning: Could not clear test data
Solution: Check database permissions for DELETE operations
```

## Performance

- **Unit Tests**: ~2-3 seconds (92 tests)
- **Integration Tests**: ~30-60 seconds (28 tests)
- Run integration tests separately in CI/CD for faster feedback loops
