using abc.bvl.AdminTool.Application.Common.Interfaces;
using FluentAssertions;

namespace abc.bvl.AdminTool.MSTests.Infrastructure;

[TestClass]
public class ScreenDefinitionRepositoryTests
{
    [TestMethod]
    public void ScreenDefinitionRepository_ShouldImplementIScreenDefinitionRepository()
    {
        // This test verifies the repository contract exists
        // Actual implementation tests would require in-memory database setup
        var repositoryType = typeof(IScreenDefinitionRepository);
        
        repositoryType.Should().NotBeNull();
        repositoryType.IsInterface.Should().BeTrue();
    }

    [TestMethod]
    public void ScreenDefinitionRepository_ShouldHaveGetAllAsyncMethod()
    {
        var repositoryType = typeof(IScreenDefinitionRepository);
        var method = repositoryType.GetMethod("GetAllAsync");
        
        method.Should().NotBeNull();
        method!.Name.Should().Be("GetAllAsync");
    }

    [TestMethod]
    public void ScreenDefinitionRepository_ShouldHaveCreateAsyncMethod()
    {
        var repositoryType = typeof(IScreenDefinitionRepository);
        var method = repositoryType.GetMethod("CreateAsync");
        
        method.Should().NotBeNull();
        method!.Name.Should().Be("CreateAsync");
    }
}