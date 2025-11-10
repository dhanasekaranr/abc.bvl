using abc.bvl.AdminTool.Application.PilotEnablement.Queries;
using abc.bvl.AdminTool.Application.PilotEnablement.Commands;
using abc.bvl.AdminTool.Contracts.Common;
using abc.bvl.AdminTool.Contracts.PilotEnablement;
using abc.bvl.AdminTool.Api.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace abc.bvl.AdminTool.Api.Controllers;

/// <summary>
/// Pilot Enablement Controller - Single endpoint for managing user screen access
/// This is the PRIMARY controller for UI operations
/// Manages users (pilots) and their screen assignments in one unified interface
/// </summary>
[ApiController]
[Route("api/v1/pilot-enablement")]
[Authorize(Roles = "Admin,ScreenManager")]
public class PilotEnablementController : BaseApiController
{
    private readonly IMediator _mediator;

    public PilotEnablementController(
        IMediator mediator,
        ILogger<PilotEnablementController> logger) : base(logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Get all pilots (users) with their screen assignments
    /// This is the PRIMARY endpoint for displaying data in the UI
    /// </summary>
    /// <param name="userId">Optional: Filter by specific user ID</param>
    /// <param name="status">Optional: Filter by status (0=Inactive, 1=Active)</param>
    /// <param name="page">Page number (1-based), default: 1</param>
    /// <param name="pageSize">Items per page (max: 100), default: 20</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pilots with their screen assignments</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PilotEnablementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<PilotEnablementDto>>> GetPilotEnablements(
        [FromQuery] long? userId = null,
        [FromQuery] byte? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (status.HasValue && status > 1)
        {
            LogOperation($"Invalid status parameter: {status}");
            return BadRequest("Status must be 0 (Inactive) or 1 (Active)");
        }

        try
        {
            var paginationRequest = new PaginationRequest
            {
                Page = page,
                PageSize = pageSize
            };

            var query = new GetPilotEnablementsQuery(userId, status, paginationRequest);
            var result = await _mediator.Send(query, cancellationToken);
            
            LogOperation($"Retrieved {result.Count()} pilot enablements");
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error retrieving pilot enablements");
            throw;
        }
    }

    /// <summary>
    /// Get a specific user's screen assignments
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pilot enablement for the specified user</returns>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(ApiResponse<PilotEnablementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PilotEnablementDto>>> GetPilotEnablement(
        [FromRoute] long userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return BadRequest("User ID must be a positive number");
        }

        try
        {
            var query = new GetPilotEnablementsQuery(NbUserGk: userId);
            var result = await _mediator.Send(query, cancellationToken);
            
            var pilot = result.FirstOrDefault();
            if (pilot == null)
            {
                LogOperation($"Pilot {userId} not found");
                return NotFound($"User '{userId}' not found or has no screen assignments");
            }
            
            LogOperation($"Retrieved pilot enablement for {userId}");
            return Ok(SingleSuccess(pilot));
        }
        catch (Exception ex)
        {
            LogError(ex, $"Error retrieving pilot enablement for {userId}");
            throw;
        }
    }

    /// <summary>
    /// Manage pilot enablement (CRUD operations for user screen assignments)
    /// This is the PRIMARY endpoint for ALL UI CRUD operations
    /// 
    /// Operations supported:
    /// - Add new screen assignments for a user
    /// - Update existing screen assignments
    /// - Remove screen assignments
    /// - All operations are atomic (succeed or fail together)
    /// </summary>
    /// <param name="request">Pilot enablement data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated pilot enablement</returns>
    [HttpPost("manage")]
    [ProducesResponseType(typeof(ApiResponse<PilotEnablementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PilotEnablementDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PilotEnablementDto>>> ManagePilotEnablement(
        [FromBody] PilotEnablementDto request,
        CancellationToken cancellationToken = default)
    {
        // Validate input
        if (request.NbUserGk <= 0)
        {
            LogOperation("Validation failed: NbUserGk is required");
            return BadRequest("NbUserGk must be a positive number");
        }

        if (request.ScreenAssignments == null || !request.ScreenAssignments.Any())
        {
            LogOperation($"Validation failed: No screen assignments for user {request.NbUserGk}");
            return BadRequest("At least one screen assignment must be specified");
        }

        try
        {
            var currentUserString = GetCurrentUserId();
            if (!long.TryParse(currentUserString, out var currentUser))
            {
                currentUser = 1000; // Default system user if parse fails
            }
            
            // Add audit info and send directly to MediatR
            var enrichedRequest = request with { RequestedBy = currentUser };
            var result = await _mediator.Send(enrichedRequest, cancellationToken);
            
            // Return 201 for new assignments, 200 for updates
            var statusCode = request.ScreenAssignments.Any(s => !s.ScreenPilotGk.HasValue)
                ? StatusCodes.Status201Created 
                : StatusCodes.Status200OK;
            
            LogOperation($"Pilot enablement for user {request.NbUserGk} managed successfully", currentUser.ToString());
            
            return StatusCode(statusCode, SingleSuccess(result));
        }
        catch (KeyNotFoundException ex)
        {
            LogError(ex, $"User or screen not found: {ex.Message}");
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            LogError(ex, $"Error managing pilot enablement for user {request.NbUserGk}");
            throw;
        }
    }

    /// <summary>
    /// Remove all screen assignments for a user (deactivate pilot)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeletePilotEnablement(
        [FromRoute] long userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return BadRequest("User ID must be a positive number");
        }

        try
        {
            var currentUserString = GetCurrentUserId();
            if (!long.TryParse(currentUserString, out var currentUser))
            {
                currentUser = 1000; // Default system user if parse fails
            }
            
            // Get all screen assignments for this user
            var query = new GetPilotEnablementsQuery(NbUserGk: userId);
            var pilots = await _mediator.Send(query, cancellationToken);
            var pilot = pilots.FirstOrDefault();
            
            if (pilot == null)
            {
                LogOperation($"Attempt to delete non-existent pilot {userId}");
                return NotFound($"User '{userId}' not found");
            }

            // Remove all assignments by setting status to 0
            var deactivateRequest = new PilotEnablementDto
            {
                NbUserGk = userId,
                UserName = pilot.UserName,
                ScreenAssignments = pilot.ScreenAssignments
                    .Select(a => a with { StatusId = 0 }) // Set all to inactive
                    .ToList(),
                RequestedBy = currentUser
            };
            
            await _mediator.Send(deactivateRequest, cancellationToken);
            
            LogOperation($"All screen assignments removed for user {userId}", currentUser.ToString());
            return NoContent();
        }
        catch (Exception ex)
        {
            LogError(ex, $"Error deleting pilot enablement for user {userId}");
            throw;
        }
    }
}
