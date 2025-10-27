using abc.bvl.AdminTool.Contracts.Common;
using abc.bvl.AdminTool.Domain.Entities.Base;
using abc.bvl.AdminTool.Api.Controllers.Base;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace abc.bvl.AdminTool.Api.Controllers.Generic;

/// <summary>
/// Generic CRUD controller that handles basic operations for ANY admin entity
/// This single controller can manage 100+ lookup tables automatically
/// 
/// Usage Examples:
/// GET  /api/v1/admin/country     -> Get all countries
/// GET  /api/v1/admin/state/123   -> Get state by ID  
/// POST /api/v1/admin/category    -> Create new category
/// PUT  /api/v1/admin/product/456 -> Update product
/// DELETE /api/v1/admin/region/789 -> Delete region
/// </summary>
/// <typeparam name="TEntity">Any entity that inherits from BaseAdminEntity</typeparam>
/// <typeparam name="TDto">DTO for the entity</typeparam>
[ApiController]
[Route("api/v1/admin/{entityType}")]
public class GenericAdminController<TEntity, TDto> : BaseApiController 
    where TEntity : BaseAdminEntity, new()
    where TDto : class
{
    private readonly IGenericAdminService<TEntity, TDto> _service;

    public GenericAdminController(
        IGenericAdminService<TEntity, TDto> service,
        ILogger<GenericAdminController<TEntity, TDto>> logger) : base(logger)
    {
        _service = service;
    }

    /// <summary>
    /// Get all entities with optional filtering
    /// </summary>
    /// <param name="status">Filter by status (optional)</param>
    /// <param name="pageSize">Page size for pagination (default: 50, max: 1000)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="search">Search term for name/code fields (optional)</param>
    /// <returns>Paginated list of entities</returns>
    [HttpGet]
    public async Task<ActionResult<SingleResult<PagedResult<TDto>>>> GetAll(
        [FromQuery] byte? status = null,
        [FromQuery] int pageSize = 50,
        [FromQuery] int pageNumber = 1,
        [FromQuery] string? search = null)
    {
        try
        {
            // Validate pagination parameters
            pageSize = Math.Min(Math.Max(1, pageSize), 1000); // Limit to 1000 records max
            pageNumber = Math.Max(1, pageNumber);

            var result = await _service.GetAllAsync(status, pageSize, pageNumber, search);
            
            // Simple! User/Access/CorrelationId auto-populated by EnrichResponseFilter
            return Ok(SingleSuccess(result));
        }
        catch (Exception ex)
        {
            LogError(ex, $"Error getting all {typeof(TEntity).Name}");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get entity by ID
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <returns>Single entity or NotFound</returns>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<SingleResult<TDto>>> GetById(long id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            
            if (result == null)
                return NotFound($"{typeof(TEntity).Name} with ID {id} not found");

            // Simple! User/Access/CorrelationId auto-populated by EnrichResponseFilter
            return Ok(SingleSuccess(result));
        }
        catch (Exception ex)
        {
            LogError(ex, $"Error getting {typeof(TEntity).Name} with ID {id}");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create new entity
    /// </summary>
    /// <param name="dto">Entity data</param>
    /// <returns>Created entity with assigned ID</returns>
    [HttpPost]
    public async Task<ActionResult<SingleResult<TDto>>> Create([FromBody] TDto dto)
    {
        try
        {
            // Validate the DTO
            if (!TryValidateModel(dto))
            {
                return BadRequest(ModelState);
            }

            var result = await _service.CreateAsync(dto, GetCurrentUserId());
            
            return CreatedAtAction(
                nameof(GetById), 
                new { id = GetEntityId(result) }, 
                SingleSuccess(result));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            LogError(ex, $"Error creating {typeof(TEntity).Name}");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update existing entity
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <param name="dto">Updated entity data</param>
    /// <returns>Updated entity</returns>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<SingleResult<TDto>>> Update(long id, [FromBody] TDto dto)
    {
        try
        {
            // Validate the DTO
            if (!TryValidateModel(dto))
            {
                return BadRequest(ModelState);
            }

            var result = await _service.UpdateAsync(id, dto, GetCurrentUserId());
            
            if (result == null)
                return NotFound($"{typeof(TEntity).Name} with ID {id} not found");

            // Simple! User/Access/CorrelationId auto-populated by EnrichResponseFilter
            return Ok(SingleSuccess(result));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            LogError(ex, $"Error updating {typeof(TEntity).Name} with ID {id}");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Soft delete entity (sets Status = 0)
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <returns>Success or NotFound</returns>
    [HttpDelete("{id:long}")]
    public async Task<ActionResult> Delete(long id)
    {
        try
        {
            var success = await _service.DeleteAsync(id, GetCurrentUserId());
            
            if (!success)
                return NotFound($"{typeof(TEntity).Name} with ID {id} not found");

            LogOperation($"{typeof(TEntity).Name} with ID {id} deleted");
            return NoContent();
        }
        catch (Exception ex)
        {
            LogError(ex, $"Error deleting {typeof(TEntity).Name} with ID {id}");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Bulk operations for efficiency
    /// </summary>
    /// <param name="dtos">List of entities to create/update</param>
    /// <returns>Results of bulk operation</returns>
    [HttpPost("bulk")]
    public async Task<ActionResult<SingleResult<BulkOperationResult<TDto>>>> BulkUpsert([FromBody] IEnumerable<TDto> dtos)
    {
        try
        {
            var result = await _service.BulkUpsertAsync(dtos, GetCurrentUserId());
            
            // Simple! User/Access/CorrelationId auto-populated by EnrichResponseFilter
            return Ok(SingleSuccess(result));
        }
        catch (Exception ex)
        {
            LogError(ex, $"Error in bulk upsert for {typeof(TEntity).Name}");
            return StatusCode(500, "Internal server error");
        }
    }

    #region Private Helper Methods

    private object GetEntityId(TDto dto)
    {
        // Use reflection to get Id property value
        var idProperty = typeof(TDto).GetProperty("Id");
        return idProperty?.GetValue(dto) ?? 0;
    }

    #endregion
}

/// <summary>
/// Service interface for generic admin operations
/// </summary>
public interface IGenericAdminService<TEntity, TDto>
    where TEntity : BaseAdminEntity
    where TDto : class
{
    Task<PagedResult<TDto>> GetAllAsync(byte? status, int pageSize, int pageNumber, string? search);
    Task<TDto?> GetByIdAsync(long id);
    Task<TDto> CreateAsync(TDto dto, string createdBy);
    Task<TDto?> UpdateAsync(long id, TDto dto, string updatedBy);
    Task<bool> DeleteAsync(long id, string deletedBy);
    Task<BulkOperationResult<TDto>> BulkUpsertAsync(IEnumerable<TDto> dtos, string operatedBy);
}

/// <summary>
/// Generic pagination result
/// </summary>
public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

/// <summary>
/// Bulk operation result
/// </summary>
public record BulkOperationResult<T>(
    int Created,
    int Updated,
    int Errors,
    IEnumerable<string> ErrorMessages,
    IEnumerable<T> Results);