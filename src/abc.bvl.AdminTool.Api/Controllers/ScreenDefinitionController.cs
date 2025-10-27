using abc.bvl.AdminTool.Application.ScreenDefinition.Queries;
using abc.bvl.AdminTool.Contracts.Common;
using abc.bvl.AdminTool.Contracts.ScreenDefinition;
using abc.bvl.AdminTool.Api.Validation;
using abc.bvl.AdminTool.Api.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace abc.bvl.AdminTool.Api.Controllers;

/// <summary>
/// Screen Definition management controller with security compliance
/// Implements proper authentication, authorization, and input validation
/// </summary>
[ApiController]
[Route("api/v1/admin/screen-definition")]
[Authorize(Roles = "Admin,ScreenManager")]
public class ScreenDefinitionController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IValidator<ScreenDefnDto> _validator;

    public ScreenDefinitionController(
        IMediator mediator, 
        IValidator<ScreenDefnDto> validator,
        ILogger<ScreenDefinitionController> logger) : base(logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <summary>
    /// Get screen definitions with optional status filtering and pagination
    /// </summary>
    /// <param name="status">Optional status filter (0=Inactive, 1=Active, 2=Pending)</param>
    /// <param name="page">Page number (1-based), default: 1</param>
    /// <param name="pageSize">Items per page (max: 100), default: 20</param>
    /// <param name="searchTerm">Optional search term to filter by name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of screen definitions</returns>
    [HttpGet("screens")]
    [ProducesResponseType(typeof(PagedResult<ScreenDefnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<ScreenDefnDto>>> GetScreens(
        [FromQuery] byte? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        // Validate status parameter
        if (status.HasValue && status > 2)
        {
            LogOperation($"Invalid status parameter: {status}");
            return BadRequest("Status must be 0 (Inactive), 1 (Active), or 2 (Pending)");
        }

        try
        {
            var paginationRequest = new PaginationRequest
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm
            };

            var query = new GetScreenDefinitionsQuery(status, paginationRequest);
            var items = await _mediator.Send(query, cancellationToken);
            
            // Get total count for pagination
            var totalCountQuery = new GetScreenDefinitionsCountQuery(status, searchTerm);
            var totalCount = await _mediator.Send(totalCountQuery, cancellationToken);
            
            // Simple! User/Access/CorrelationId auto-populated by EnrichResponseFilter
            var result = new PagedResult<ScreenDefnDto>(items, page, pageSize, totalCount);

            LogOperation($"Retrieved {result.Items.Count()} of {totalCount} screen definitions (page {page})");
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            LogError(ex, "Error retrieving screen definitions");
            throw;
        }
    }

    /// <summary>
    /// Get a specific screen definition by ID
    /// </summary>
    /// <param name="id">Screen definition ID (must be positive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Screen definition details</returns>
    [HttpGet("screens/{id:long:min(1)}")]
    [ProducesResponseType(typeof(ApiResponse<ScreenDefnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ScreenDefnDto>>> GetScreen(
        [FromRoute] long id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allResults = await _mediator.Send(new GetScreenDefinitionsQuery(), cancellationToken);
            var result = allResults.FirstOrDefault(s => s.Id == id);
            
            if (result == null)
            {
                LogOperation($"Screen definition {id} not found");
                return NotFound($"Screen definition with ID {id} not found");
            }
            
            LogOperation($"Retrieved screen definition {id}");
            return Ok(SingleSuccess(result));
        }
        catch (Exception ex)
        {
            LogError(ex, $"Error retrieving screen definition {id}");
            throw;
        }
    }

    /// <summary>
    /// Create or update a screen definition
    /// </summary>
    /// <param name="request">Screen definition data with validation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created/updated screen definition</returns>
    [HttpPut("screens")]
    [ProducesResponseType(typeof(ApiResponse<ScreenDefnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ScreenDefnDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ScreenDefnDto>>> UpsertScreen(
        [FromBody] ScreenDefnDto request,
        CancellationToken cancellationToken = default)
    {
        // Validate input using FluentValidation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            LogOperation($"Validation failed for screen definition upsert. Errors: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
            
            return BadRequest(validationResult.Errors.Select(e => new 
            { 
                Property = e.PropertyName, 
                Message = e.ErrorMessage 
            }));
        }

        try
        {
            var currentUser = GetCurrentUserId();
            var isUpdate = request.Id.HasValue && request.Id > 0;
            
            // For now, return the same object with generated values
            // In real implementation, you'd use a command handler:
            // var result = isUpdate 
            //     ? await _mediator.Send(new UpdateScreenDefinitionCommand(request), cancellationToken)
            //     : await _mediator.Send(new CreateScreenDefinitionCommand(request), cancellationToken);
            
            var result = request with 
            { 
                Id = request.Id ?? DateTimeOffset.UtcNow.Ticks, // Generate ID if null
                CreatedAt = request.CreatedAt ?? DateTimeOffset.UtcNow,
                CreatedBy = request.CreatedBy ?? currentUser,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = currentUser
            };
            
            var statusCode = isUpdate ? StatusCodes.Status200OK : StatusCodes.Status201Created;
            var action = isUpdate ? "updated" : "created";
            
            LogOperation($"Screen definition {result.Id} {action}", currentUser);
            
            // Use base controller helper for clean response
            return StatusCode(statusCode, SingleSuccess(result));
        }
        catch (Exception ex)
        {
            LogError(ex, $"Error upserting screen definition");
            throw;
        }
    }

    /// <summary>
    /// Delete a screen definition (soft delete)
    /// </summary>
    /// <param name="id">Screen definition ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    [HttpDelete("screens/{id:long:min(1)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteScreen(
        [FromRoute] long id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // In real implementation, you'd use a command handler
            // await _mediator.Send(new DeleteScreenDefinitionCommand(id), cancellationToken);
            await Task.CompletedTask;
            
            LogOperation($"Screen definition {id} deleted");
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            LogOperation($"Attempt to delete non-existent screen {id}");
            return NotFound();
        }
        catch (Exception ex)
        {
            LogError(ex, $"Error deleting screen definition {id}");
            throw;
        }
    }
}