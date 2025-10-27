using abc.bvl.AdminTool.Domain.Entities;
using FluentAssertions;

namespace abc.bvl.AdminTool.MSTests.Domain;

[TestClass]
public class OutboxMessageTests
{
    [TestMethod]
    public void Constructor_ShouldCreateOutboxMessageWithDefaultValues()
    {
        // Arrange & Act
        var message = new OutboxMessage
        {
            Type = "ScreenDefinition",
            EntityId = 123,
            Operation = "INSERT"
        };

        // Assert
        message.Type.Should().Be("ScreenDefinition");
        message.EntityId.Should().Be(123);
        message.Operation.Should().Be("INSERT");
        message.Status.Should().Be("Pending");
        message.RetryCount.Should().Be(0);
        message.ProcessedAt.Should().BeNull();
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void OutboxMessage_WithAllProperties_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var createdAt = DateTime.UtcNow.AddMinutes(-10);
        var processedAt = DateTime.UtcNow;
        
        var message = new OutboxMessage
        {
            Id = 1,
            Type = "ScreenPilot",
            EntityId = 456,
            Operation = "UPDATE",
            Payload = "{\"id\":456,\"name\":\"Test\"}",
            CreatedAt = createdAt,
            ProcessedAt = processedAt,
            Status = "Completed",
            RetryCount = 2,
            Error = null,
            SourceDatabase = "primarydb",
            TargetDatabase = "secondarydb",
            CorrelationId = "abc-123-def"
        };

        // Assert
        message.Id.Should().Be(1);
        message.Type.Should().Be("ScreenPilot");
        message.EntityId.Should().Be(456);
        message.Operation.Should().Be("UPDATE");
        message.Payload.Should().Contain("Test");
        message.CreatedAt.Should().Be(createdAt);
        message.ProcessedAt.Should().Be(processedAt);
        message.Status.Should().Be("Completed");
        message.RetryCount.Should().Be(2);
        message.SourceDatabase.Should().Be("primarydb");
        message.TargetDatabase.Should().Be("secondarydb");
        message.CorrelationId.Should().Be("abc-123-def");
    }

    [TestMethod]
    public void OutboxMessage_Operations_ShouldSupportInsertUpdateDelete()
    {
        // Arrange & Act
        var insertMessage = new OutboxMessage { Operation = "INSERT" };
        var updateMessage = new OutboxMessage { Operation = "UPDATE" };
        var deleteMessage = new OutboxMessage { Operation = "DELETE" };

        // Assert
        insertMessage.Operation.Should().Be("INSERT");
        updateMessage.Operation.Should().Be("UPDATE");
        deleteMessage.Operation.Should().Be("DELETE");
    }

    [TestMethod]
    public void OutboxMessage_Status_ShouldSupportAllStates()
    {
        // Arrange & Act
        var pending = new OutboxMessage { Status = "Pending" };
        var processing = new OutboxMessage { Status = "Processing" };
        var completed = new OutboxMessage { Status = "Completed" };
        var failed = new OutboxMessage { Status = "Failed" };

        // Assert
        pending.Status.Should().Be("Pending");
        processing.Status.Should().Be("Processing");
        completed.Status.Should().Be("Completed");
        failed.Status.Should().Be("Failed");
    }

    [TestMethod]
    public void OutboxMessage_ProcessedAt_ShouldBeNullForPending()
    {
        // Arrange & Act
        var message = new OutboxMessage
        {
            Type = "ScreenDefinition",
            EntityId = 1,
            Operation = "INSERT",
            Status = "Pending"
        };

        // Assert
        message.ProcessedAt.Should().BeNull();
    }

    [TestMethod]
    public void OutboxMessage_ProcessedAt_ShouldBeSetWhenCompleted()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Type = "ScreenDefinition",
            EntityId = 1,
            Operation = "INSERT",
            Status = "Pending"
        };

        // Act - Simulate processing
        message.Status = "Completed";
        message.ProcessedAt = DateTime.UtcNow;

        // Assert
        message.Status.Should().Be("Completed");
        message.ProcessedAt.Should().NotBeNull();
        message.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public void OutboxMessage_RetryCount_ShouldIncrementOnRetry()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Type = "ScreenDefinition",
            EntityId = 1,
            Operation = "INSERT",
            RetryCount = 0
        };

        // Act - Simulate retries
        message.RetryCount++;
        message.RetryCount++;

        // Assert
        message.RetryCount.Should().Be(2);
    }

    [TestMethod]
    public void OutboxMessage_Error_ShouldBeNullWhenSuccessful()
    {
        // Arrange & Act
        var message = new OutboxMessage
        {
            Type = "ScreenDefinition",
            EntityId = 1,
            Operation = "INSERT",
            Status = "Completed",
            Error = null
        };

        // Assert
        message.Error.Should().BeNull();
    }

    [TestMethod]
    public void OutboxMessage_Error_ShouldStoreErrorMessageWhenFailed()
    {
        // Arrange & Act
        var message = new OutboxMessage
        {
            Type = "ScreenDefinition",
            EntityId = 1,
            Operation = "INSERT",
            Status = "Failed",
            Error = "Connection timeout to secondary database"
        };

        // Assert
        message.Status.Should().Be("Failed");
        message.Error.Should().Be("Connection timeout to secondary database");
    }

    [TestMethod]
    public void OutboxMessage_SourceAndTargetDatabase_ShouldIdentifyDatabases()
    {
        // Arrange & Act
        var message = new OutboxMessage
        {
            Type = "ScreenDefinition",
            EntityId = 1,
            Operation = "INSERT",
            SourceDatabase = "XEPDB1",
            TargetDatabase = "XEPDB2"
        };

        // Assert
        message.SourceDatabase.Should().Be("XEPDB1");
        message.TargetDatabase.Should().Be("XEPDB2");
    }

    [TestMethod]
    public void OutboxMessage_CorrelationId_ShouldEnableRequestTracking()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act
        var message = new OutboxMessage
        {
            Type = "ScreenDefinition",
            EntityId = 1,
            Operation = "INSERT",
            CorrelationId = correlationId
        };

        // Assert
        message.CorrelationId.Should().Be(correlationId);
    }

    [TestMethod]
    public void OutboxMessage_Payload_ShouldStoreJsonData()
    {
        // Arrange
        var jsonPayload = "{\"id\":100,\"name\":\"Test Screen\",\"status\":1}";

        // Act
        var message = new OutboxMessage
        {
            Type = "ScreenDefinition",
            EntityId = 100,
            Operation = "INSERT",
            Payload = jsonPayload
        };

        // Assert
        message.Payload.Should().Be(jsonPayload);
        message.Payload.Should().Contain("Test Screen");
    }

    [TestMethod]
    public void OutboxMessage_Type_ShouldIdentifyEntityType()
    {
        // Arrange & Act
        var screenDefMessage = new OutboxMessage { Type = "ScreenDefinition" };
        var pilotMessage = new OutboxMessage { Type = "ScreenPilot" };
        var countryMessage = new OutboxMessage { Type = "Country" };

        // Assert
        screenDefMessage.Type.Should().Be("ScreenDefinition");
        pilotMessage.Type.Should().Be("ScreenPilot");
        countryMessage.Type.Should().Be("Country");
    }
}
