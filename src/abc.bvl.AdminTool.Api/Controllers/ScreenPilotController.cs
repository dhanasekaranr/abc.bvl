using abc.bvl.AdminTool.Contracts.Common;
using abc.bvl.AdminTool.Contracts.ScreenPilot;
using abc.bvl.AdminTool.Api.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace abc.bvl.AdminTool.Api.Controllers;

[ApiController]
[Route("api/v1/admin/screen-pilot")]
public class ScreenPilotController : BaseApiController
{
    // For now, using mock data - you'd inject your services here
    private static readonly List<ScreenPilotDto> _mockData = new()
    {
        new ScreenPilotDto(1, 1, "john.doe", 1, DateTimeOffset.UtcNow, "admin", "ABC123", "Orders Management"),
        new ScreenPilotDto(2, 1, "jane.smith", 1, DateTimeOffset.UtcNow, "admin", "DEF456", "Orders Management"),
        new ScreenPilotDto(3, 2, "john.doe", 1, DateTimeOffset.UtcNow, "admin", "GHI789", "Customer Portal"),
    };

    public ScreenPilotController(ILogger<ScreenPilotController> logger) : base(logger)
    {
    }

    /// <summary>
    /// Get all screen pilot assignments for a specific user
    /// </summary>
    [HttpGet("users/{userId}/pilot")]
    public async Task<ActionResult<SingleResult<IEnumerable<ScreenPilotDto>>>> GetUserScreenPilots(string userId)
    {
        await Task.CompletedTask; // Simulate async operation
        
        var userPilots = _mockData.Where(p => p.UserId == userId);
        
        // Simple! User/Access/CorrelationId auto-populated by EnrichResponseFilter
        return Ok(SingleSuccess(userPilots));
    }

    /// <summary>
    /// Get all user assignments for a specific screen
    /// </summary>
    [HttpGet("screens/{screenId}/pilot")]
    public async Task<ActionResult<SingleResult<IEnumerable<ScreenPilotDto>>>> GetScreenPilots(long screenId)
    {
        await Task.CompletedTask; // Simulate async operation
        
        var screenPilots = _mockData.Where(p => p.ScreenDefnId == screenId);
        
        // Simple! User/Access/CorrelationId auto-populated by EnrichResponseFilter
        return Ok(SingleSuccess(screenPilots));
    }

    /// <summary>
    /// Get a specific screen pilot assignment
    /// </summary>
    [HttpGet("pilot/{id}")]
    public async Task<ActionResult<SingleResult<ScreenPilotDto>>> GetScreenPilot(long id)
    {
        await Task.CompletedTask; // Simulate async operation
        
        var pilot = _mockData.FirstOrDefault(p => p.Id == id);
        if (pilot == null)
        {
            return NotFound();
        }
        
        // Simple! User/Access/CorrelationId auto-populated by EnrichResponseFilter
        return Ok(SingleSuccess(pilot));
    }

    /// <summary>
    /// Create or update screen pilot assignment
    /// Same DTO for both create and update operations
    /// </summary>
    [HttpPut("pilot")]
    public async Task<ActionResult<SingleResult<ScreenPilotDto>>> UpsertScreenPilot(
        [FromBody] ScreenPilotDto request)
    {
        await Task.CompletedTask; // Simulate async operation
        
        // Create operation (Id is null)
        if (request.Id == null)
        {
            var newPilot = request with 
            { 
                Id = _mockData.Max(p => p.Id ?? 0) + 1,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = GetCurrentUserId(),
                RowVersion = Guid.NewGuid().ToString()[..8]
            };
            
            _mockData.Add(newPilot);
            
            // Simple! User/Access/CorrelationId auto-populated by EnrichResponseFilter
            return CreatedAtAction(nameof(GetScreenPilot), new { id = newPilot.Id }, SingleSuccess(newPilot));
        }
        
        // Update operation (Id is provided)
        var existingIndex = _mockData.FindIndex(p => p.Id == request.Id);
        if (existingIndex == -1)
        {
            return NotFound();
        }
        
        var updatedPilot = request with 
        { 
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = GetCurrentUserId(),
            RowVersion = Guid.NewGuid().ToString()[..8]
        };
        
        _mockData[existingIndex] = updatedPilot;
        
        // Simple! User/Access/CorrelationId auto-populated by EnrichResponseFilter
        return Ok(SingleSuccess(updatedPilot));
    }

    /// <summary>
    /// Delete screen pilot assignment
    /// Simple ID-based deletion
    /// </summary>
    [HttpDelete("pilot/{id}")]
    public async Task<ActionResult> DeleteScreenPilot(long id)
    {
        await Task.CompletedTask; // Simulate async operation
        
        var pilot = _mockData.FirstOrDefault(p => p.Id == id);
        if (pilot == null)
        {
            return NotFound();
        }
        
        _mockData.Remove(pilot);
        return NoContent();
    }

    /// <summary>
    /// Alternative delete with DTO (includes concurrency check)
    /// </summary>
    [HttpDelete("pilot")]
    public async Task<ActionResult> DeleteScreenPilotWithConcurrency([FromBody] ScreenPilotDto request)
    {
        await Task.CompletedTask; // Simulate async operation
        
        var pilot = _mockData.FirstOrDefault(p => p.Id == request.Id);
        if (pilot == null)
        {
            return NotFound();
        }
        
        // Check concurrency (RowVersion)
        if (pilot.RowVersion != request.RowVersion)
        {
            return Conflict("The record has been modified by another user.");
        }
        
        _mockData.Remove(pilot);
        return NoContent();
    }
}