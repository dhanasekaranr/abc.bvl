using abc.bvl.AdminTool.Application.ScreenPilot.Queries;
using abc.bvl.AdminTool.Application.ScreenPilot.Commands;
using abc.bvl.AdminTool.Contracts.Common;
using abc.bvl.AdminTool.Contracts.ScreenPilot;
using abc.bvl.AdminTool.Api.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace abc.bvl.AdminTool.Api.Controllers;

/// <summary>
/// Screen Pilot (user-screen assignment) management controller
/// </summary>
[ApiController]
[Route("api/v1/admin/screen-pilot")]
[Authorize(Roles = "Admin,ScreenManager")]
public class ScreenPilotController : BaseApiController
{
    private readonly IMediator _mediator;

    public ScreenPilotController(
        IMediator mediator,
        ILogger<ScreenPilotController> logger) : base(logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Get all screen pilot assignments for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of screen assignments for the user</returns>
    [HttpGet("users/{userId}/pilot")]
    [ProducesResponseType(typeof(SingleResult<IEnumerable<ScreenPilotDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SingleResult<IEnumerable<ScreenPilotDto>>>> GetUserScreenPilots(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetUserScreenPilotsQuery(userId);
            var userPilots = await _mediator.Send(query, cancellationToken);
            
            LogOperation($"Retrieved {userPilots.Count()} screen pilots for user {userId}");
            return Ok(SingleSuccess(userPilots));
        }
        catch (Exception ex)
        {
            LogError(ex, $"Error retrieving screen pilots for user {userId}");
            throw;
        }
    }

    /// <summary>
    /// Create or update screen pilot assignment
    /// Handler determines create vs update based on data existence
    /// </summary>
    /// <param name="request">Screen pilot data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created/updated screen pilot</returns>
    [HttpPut("pilots")]
    [ProducesResponseType(typeof(SingleResult<ScreenPilotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SingleResult<ScreenPilotDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SingleResult<ScreenPilotDto>>> UpsertScreenPilot(
        [FromBody] ScreenPilotDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUserId();
            
            var command = new UpsertScreenPilotCommand
            {
                Id = request.Id,
                ScreenDefnId = request.ScreenDefnId,
                UserId = request.UserId,
                Status = request.Status,
                RequestedBy = currentUser
            };
            
            var result = await _mediator.Send(command, cancellationToken);
            
            var statusCode = request.Id.HasValue ? StatusCodes.Status200OK : StatusCodes.Status201Created;
            LogOperation($"Screen pilot {result.Id} processed successfully", currentUser);
            
            return StatusCode(statusCode, SingleSuccess(result));
        }
        catch (Exception ex)
        {
            LogError(ex, "Error upserting screen pilot");
            throw;
        }
    }
}