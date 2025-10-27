using abc.bvl.AdminTool.Contracts.Admin;
using abc.bvl.AdminTool.Contracts.Common;
using abc.bvl.AdminTool.Contracts.ScreenPilot;
using FluentAssertions;

namespace abc.bvl.AdminTool.MSTests.Contracts;

[TestClass]
public class DtoRecordTests
{
    [TestClass]
    public class CountryDtoTests
    {
        [TestMethod]
        public void CountryDto_Constructor_ShouldSetAllProperties()
        {
            // Arrange & Act
            var dto = new CountryDto(
                Id: 1,
                CountryCode: "US",
                CountryName: "United States",
                Iso3Code: "USA",
                NumericCode: 840,
                PhoneCode: "+1",
                Status: 1,
                UpdatedAt: DateTimeOffset.UtcNow,
                UpdatedBy: "admin"
            );

            // Assert
            dto.Id.Should().Be(1);
            dto.CountryCode.Should().Be("US");
            dto.CountryName.Should().Be("United States");
            dto.Iso3Code.Should().Be("USA");
            dto.Status.Should().Be(1);
        }

        [TestMethod]
        public void CountryDto_WithDefaults_ShouldAllowNullValues()
        {
            // Arrange & Act
            var dto = new CountryDto();

            // Assert
            dto.Id.Should().BeNull();
            dto.CountryCode.Should().BeNull();
            dto.CountryName.Should().BeNull();
        }

        [TestMethod]
        public void CountryDto_WithPartialData_ShouldWork()
        {
            // Arrange & Act
            var dto = new CountryDto(
                CountryCode: "CA",
                CountryName: "Canada"
            );

            // Assert
            dto.CountryCode.Should().Be("CA");
            dto.CountryName.Should().Be("Canada");
            dto.Id.Should().BeNull();
        }
    }

    [TestClass]
    public class StateDtoTests
    {
        [TestMethod]
        public void StateDto_Constructor_ShouldSetAllProperties()
        {
            // Arrange & Act
            var dto = new StateDto(
                Id: 1,
                StateCode: "CA",
                StateName: "California",
                CountryId: 1,
                CountryCode: "US",
                CountryName: "United States",
                Status: 1,
                UpdatedAt: DateTimeOffset.UtcNow,
                UpdatedBy: "admin"
            );

            // Assert
            dto.Id.Should().Be(1);
            dto.StateCode.Should().Be("CA");
            dto.StateName.Should().Be("California");
            dto.CountryId.Should().Be(1);
            dto.Status.Should().Be(1);
        }

        [TestMethod]
        public void StateDto_WithDefaults_ShouldAllowNullValues()
        {
            // Arrange & Act
            var dto = new StateDto();

            // Assert
            dto.Id.Should().BeNull();
            dto.StateCode.Should().BeNull();
            dto.StateName.Should().BeNull();
            dto.CountryId.Should().BeNull();
        }

        [TestMethod]
        public void StateDto_WithCountryInfo_ShouldIncludeCountryData()
        {
            // Arrange & Act
            var dto = new StateDto(
                StateCode: "TX",
                StateName: "Texas",
                CountryCode: "US",
                CountryName: "United States"
            );

            // Assert
            dto.StateCode.Should().Be("TX");
            dto.StateName.Should().Be("Texas");
            dto.CountryCode.Should().Be("US");
            dto.CountryName.Should().Be("United States");
        }
    }

    [TestClass]
    public class ScreenPilotDtoTests
    {
        [TestMethod]
        public void ScreenPilotDto_Constructor_ShouldSetAllProperties()
        {
            // Arrange
            var now = DateTimeOffset.UtcNow;

            // Act
            var dto = new ScreenPilotDto(
                Id: 1,
                ScreenDefnId: 100,
                UserId: "john.doe",
                Status: 1,
                UpdatedAt: now,
                UpdatedBy: "admin",
                RowVersion: "ABC123",
                ScreenName: "Orders Management"
            );

            // Assert
            dto.Id.Should().Be(1);
            dto.ScreenDefnId.Should().Be(100);
            dto.UserId.Should().Be("john.doe");
            dto.Status.Should().Be(1);
            dto.UpdatedAt.Should().Be(now);
            dto.UpdatedBy.Should().Be("admin");
            dto.RowVersion.Should().Be("ABC123");
            dto.ScreenName.Should().Be("Orders Management");
        }

        [TestMethod]
        public void ScreenPilotDto_WithNullId_ShouldSupportCreateScenario()
        {
            // Arrange & Act
            var dto = new ScreenPilotDto(
                Id: null,
                ScreenDefnId: 100,
                UserId: "new.user",
                Status: 1,
                UpdatedAt: DateTimeOffset.UtcNow,
                UpdatedBy: "admin",
                RowVersion: null,
                ScreenName: "New Screen"
            );

            // Assert
            dto.Id.Should().BeNull();
            dto.ScreenDefnId.Should().Be(100);
            dto.UserId.Should().Be("new.user");
        }
    }

    [TestClass]
    public class ApiResponseTests
    {
        [TestMethod]
        public void ApiResponse_Constructor_ShouldSetAllProperties()
        {
            // Arrange
            var testData = "Test Data";
            var user = new UserInfo("user123", "John Doe", "john@example.com");
            var access = new AccessInfo(true, false, new[] { "read" }, "primary");
            var correlationId = Guid.NewGuid().ToString();
            var serverTime = DateTimeOffset.UtcNow;

            // Act
            var response = new ApiResponse<string>(
                Data: testData,
                User: user,
                Access: access,
                CorrelationId: correlationId,
                ServerTime: serverTime
            );

            // Assert
            response.Data.Should().Be(testData);
            response.User.Should().Be(user);
            response.Access.Should().Be(access);
            response.CorrelationId.Should().Be(correlationId);
            response.ServerTime.Should().Be(serverTime);
        }

        [TestMethod]
        public void ApiResponse_WithComplexData_ShouldWork()
        {
            // Arrange
            var data = new List<string> { "item1", "item2", "item3" };
            var user = new UserInfo("user456", "Jane Smith", "jane@example.com");
            var access = new AccessInfo(true, true, new[] { "read", "write" }, "secondary");

            // Act
            var response = new ApiResponse<List<string>>(
                Data: data,
                User: user,
                Access: access,
                CorrelationId: "test-correlation",
                ServerTime: DateTimeOffset.UtcNow
            );

            // Assert
            response.Data.Should().HaveCount(3);
            response.Data.Should().Contain("item1");
        }
    }

    [TestClass]
    public class SingleResultTests
    {
        [TestMethod]
        public void SingleResult_Constructor_ShouldSetAllProperties()
        {
            // Arrange
            var testData = "Test Result";

            // Act
            var result = new SingleResult<string>(
                data: testData,
                success: true,
                message: "Success"
            );

            // Assert
            result.Data.Should().Be(testData);
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Success");
        }

        [TestMethod]
        public void SingleResult_WithNullData_ShouldAllowNull()
        {
            // Arrange & Act
            var result = new SingleResult<string>(
                data: null,
                success: false,
                message: "Not found"
            );

            // Assert
            result.Data.Should().BeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Not found");
        }
    }

    [TestClass]
    public class UserInfoTests
    {
        [TestMethod]
        public void UserInfo_Constructor_ShouldSetAllProperties()
        {
            // Arrange & Act
            var userInfo = new UserInfo(
                UserId: "user123",
                DisplayName: "John Doe",
                Email: "john@example.com"
            );

            // Assert
            userInfo.UserId.Should().Be("user123");
            userInfo.DisplayName.Should().Be("John Doe");
            userInfo.Email.Should().Be("john@example.com");
        }

        [TestMethod]
        public void UserInfo_WithDifferentData_ShouldWork()
        {
            // Arrange & Act
            var userInfo = new UserInfo(
                UserId: "admin",
                DisplayName: "Administrator",
                Email: "admin@system.local"
            );

            // Assert
            userInfo.UserId.Should().Be("admin");
            userInfo.DisplayName.Should().Be("Administrator");
        }
    }

    [TestClass]
    public class AccessInfoTests
    {
        [TestMethod]
        public void AccessInfo_Constructor_ShouldSetAllProperties()
        {
            // Arrange & Act
            var accessInfo = new AccessInfo(
                CanRead: true,
                CanWrite: false,
                Roles: new[] { "viewer", "analyst" },
                DbRoute: "primary"
            );

            // Assert
            accessInfo.CanRead.Should().BeTrue();
            accessInfo.CanWrite.Should().BeFalse();
            accessInfo.Roles.Should().HaveCount(2);
            accessInfo.Roles.Should().Contain("viewer");
            accessInfo.DbRoute.Should().Be("primary");
        }

        [TestMethod]
        public void AccessInfo_WithFullAccess_ShouldWork()
        {
            // Arrange & Act
            var accessInfo = new AccessInfo(
                CanRead: true,
                CanWrite: true,
                Roles: new[] { "admin", "superuser" },
                DbRoute: "secondary"
            );

            // Assert
            accessInfo.CanRead.Should().BeTrue();
            accessInfo.CanWrite.Should().BeTrue();
            accessInfo.Roles.Should().Contain("admin");
            accessInfo.DbRoute.Should().Be("secondary");
        }

        [TestMethod]
        public void AccessInfo_WithNoRoles_ShouldAllowEmptyArray()
        {
            // Arrange & Act
            var accessInfo = new AccessInfo(
                CanRead: false,
                CanWrite: false,
                Roles: Array.Empty<string>(),
                DbRoute: "primary"
            );

            // Assert
            accessInfo.Roles.Should().BeEmpty();
            accessInfo.CanRead.Should().BeFalse();
            accessInfo.CanWrite.Should().BeFalse();
        }
    }
}
