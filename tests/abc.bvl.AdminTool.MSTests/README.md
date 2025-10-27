# AdminTool MSTest Suite

Comprehensive test suite for the AdminTool .NET 8 Web API application with 80%+ code coverage target.

## Test Structure

```
abc.bvl.AdminTool.MSTests/
├── Domain/                         # Domain entity tests
│   └── ScreenDefinitionTests.cs    # 7 tests covering entity behavior
├── Application/                    # Application layer tests
│   └── GetScreenDefinitionsHandlerTests.cs  # 7 tests for CQRS handlers
├── Infrastructure/                 # Infrastructure tests
│   └── ScreenDefinitionRepositoryTests.cs   # 14 tests with in-memory DB
└── Api/                           # API layer tests
    ├── ScreenDefinitionControllerTests.cs   # 11 tests with mocked dependencies
    └── ScreenDefnDtoValidatorTests.cs       # 6 validation tests
```

## Test Categories

### Domain Tests (7 tests)
- Entity creation and property validation
- Business rule validation
- Audit field behavior
- Status management

### Application Tests (7 tests)
- Query handler behavior
- Pagination logic
- Filter application
- Count queries

### Infrastructure Tests (14 tests)
- Repository CRUD operations
- In-memory database integration
- Paged queries and search
- Count aggregations

### API Tests (17 tests)
- Controller endpoint behavior
- Authorization and claims
- Pagination and filtering
- Response wrapping with BasePageDto
- FluentValidation rules

## Running Tests

### Run All Tests
```powershell
dotnet test tests\abc.bvl.AdminTool.MSTests\abc.bvl.AdminTool.MSTests.csproj
```

### Run with Code Coverage
```powershell
dotnet test tests\abc.bvl.AdminTool.MSTests\abc.bvl.AdminTool.MSTests.csproj `
  /p:CollectCoverage=true `
  /p:CoverletOutputFormat=cobertura `
  /p:CoverletOutput=./TestResults/coverage.cobertura.xml
```

### Run with Detailed Output
```powershell
dotnet test tests\abc.bvl.AdminTool.MSTests\abc.bvl.AdminTool.MSTests.csproj `
  --logger "console;verbosity=detailed"
```

### Run Specific Test Category
```powershell
# Run only domain tests
dotnet test --filter "FullyQualifiedName~Domain"

# Run only API tests
dotnet test --filter "FullyQualifiedName~Api"

# Run only infrastructure tests
dotnet test --filter "FullyQualifiedName~Infrastructure"
```

## Code Coverage

### Generate HTML Coverage Report
```powershell
dotnet test tests\abc.bvl.AdminTool.MSTests\abc.bvl.AdminTool.MSTests.csproj `
  /p:CollectCoverage=true `
  /p:CoverletOutputFormat="json,cobertura,lcov" `
  /p:CoverletOutput=./TestResults/

# Install ReportGenerator (one time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator `
  -reports:"tests\abc.bvl.AdminTool.MSTests\TestResults\coverage.cobertura.xml" `
  -targetdir:"tests\abc.bvl.AdminTool.MSTests\TestResults\CoverageReport" `
  -reporttypes:Html

# Open report
start tests\abc.bvl.AdminTool.MSTests\TestResults\CoverageReport\index.html
```

### Coverage Thresholds
The project targets **80% minimum code coverage** across all layers:

- **Domain**: >90% (simple entities with clear business rules)
- **Application**: >85% (MediatR handlers with focused logic)
- **Infrastructure**: >80% (repositories with EF Core)
- **API**: >75% (controllers with dependency injection)

## Test Technologies

### Core Frameworks
- **MSTest**: Microsoft's test framework (3.7.0+)
- **FluentAssertions**: Readable assertions (8.8.0+)
- **Moq**: Mocking framework (4.20+)

### Code Coverage
- **coverlet.collector**: Test coverage data collection (6.0.4+)
- **coverlet.msbuild**: MSBuild integration (6.0.4+)

### Integration Testing
- **Microsoft.AspNetCore.Mvc.Testing**: WebApplicationFactory for API tests (9.0.10+)
- **Microsoft.EntityFrameworkCore.InMemory**: In-memory database for repository tests (9.0.10+)

## Test Patterns

### Arrange-Act-Assert (AAA)
All tests follow the AAA pattern for clarity:

```csharp
[TestMethod]
public async Task GetScreens_ShouldReturnPagedResult()
{
    // Arrange - Set up dependencies and test data
    var mockMediator = new Mock<IMediator>();
    mockMediator.Setup(m => m.Send(...)).ReturnsAsync(...);
    
    // Act - Execute the method under test
    var result = await controller.GetScreens();
    
    // Assert - Verify the expected outcome
    result.Should().NotBeNull();
    result.Value.Should().BeOfType<PagedResult<ScreenDefnDto>>();
}
```

### Test Naming Convention
`MethodName_Scenario_ExpectedBehavior`

Examples:
- `GetAllAsync_WithStatusFilter_ShouldReturnFilteredResults`
- `Validate_WithEmptyName_ShouldHaveError`
- `Handle_WithoutPagination_ShouldReturnAllScreenDefinitions`

### Setup and Cleanup
```csharp
[TestClass]
public class RepositoryTests
{
    private DbContext _context;
    
    [TestInitialize]
    public void Setup()
    {
        // Initialize test dependencies
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        // Clean up resources
        _context?.Dispose();
    }
}
```

## Test Data Management

### In-Memory Database
Infrastructure tests use EF Core in-memory database:

```csharp
var options = new DbContextOptionsBuilder<AdminDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;
    
_context = new AdminDbContext(options);
```

Each test gets a unique database instance to avoid interference.

### Mocked Dependencies
Unit tests mock external dependencies:

```csharp
var mockRepository = new Mock<IScreenDefinitionRepository>();
mockRepository
    .Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()))
    .ReturnsAsync(expectedData);
```

## Security Testing

Validation tests verify security compliance:

- XSS prevention (script tag detection)
- SQL injection prevention (parameterized queries)
- Input sanitization (safe string validation)
- Length constraints (buffer overflow prevention)

## Continuous Integration

### Azure DevOps Pipeline
```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Tests with Coverage'
  inputs:
    command: 'test'
    projects: '**/*MSTests.csproj'
    arguments: '/p:CollectCoverage=true /p:CoverletOutputFormat=cobertura'
    
- task: PublishCodeCoverageResults@2
  inputs:
    codeCoverageTool: 'Cobertura'
    summaryFileLocation: '**/coverage.cobertura.xml'
```

### GitHub Actions
```yaml
- name: Run Tests
  run: |
    dotnet test tests/abc.bvl.AdminTool.MSTests/abc.bvl.AdminTool.MSTests.csproj \
      /p:CollectCoverage=true \
      /p:CoverletOutputFormat=cobertura
      
- name: Upload Coverage
  uses: codecov/codecov-action@v3
  with:
    files: ./tests/abc.bvl.AdminTool.MSTests/TestResults/coverage.cobertura.xml
```

## Current Test Results

```
Total Tests: 45
✅ Passed: 45
❌ Failed: 0
⏭️ Skipped: 0
```

### Test Distribution
- Domain: 7 tests
- Application: 7 tests (queries + handlers)
- Infrastructure: 14 tests (repositories)
- API: 17 tests (controllers + validation)

## Best Practices

1. **One Assertion Per Test**: Each test verifies a single behavior
2. **Test Independence**: Tests don't depend on execution order
3. **Meaningful Names**: Test names describe the scenario and expected outcome
4. **Fast Execution**: All tests run in <5 seconds
5. **Clean Code**: Tests are as maintainable as production code
6. **Mock External Dependencies**: Only test the unit under test
7. **Use FluentAssertions**: More readable than Assert.AreEqual
8. **Cleanup Resources**: Dispose of DbContext and other resources

## Troubleshooting

### Tests Fail Randomly
- Ensure tests are independent (don't share state)
- Use unique database names for in-memory databases
- Check for DateTime.Now issues (use DateTimeOffset.UtcNow)

### Low Code Coverage
- Check that tests actually exercise the code paths
- Add tests for error handling and edge cases
- Test both happy path and failure scenarios

### Slow Test Execution
- Avoid unnecessary database operations
- Use in-memory databases instead of real databases
- Mock external services and APIs

## Contributing

When adding new tests:

1. Follow the existing folder structure
2. Use the AAA pattern consistently
3. Name tests descriptively
4. Add XML documentation comments
5. Maintain >80% code coverage
6. Run all tests before committing

## License

MIT License - Copyright (c) 2025 abc.bvl
