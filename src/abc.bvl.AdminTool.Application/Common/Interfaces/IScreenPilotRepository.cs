using abc.bvl.AdminTool.Application.Common.Models;
using ScreenPilotEntity = abc.bvl.AdminTool.Domain.Entities.ScreenPilot;

namespace abc.bvl.AdminTool.Application.Common.Interfaces;

/// <summary>
/// Repository interface for ScreenPilot operations
/// </summary>
public interface IScreenPilotRepository
{
    // Query methods (return DTOs)
    Task<IEnumerable<ScreenPilotDto>> GetByUserIdAsync(long nbUserGk, CancellationToken cancellationToken = default);
    Task<ScreenPilotDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all pilots assigned to a specific screen (for hierarchical display)
    /// </summary>
    Task<IEnumerable<ScreenPilotDto>> GetByScreenGkAsync(long screenGk, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all pilots with optional status filter
    /// </summary>
    Task<IEnumerable<ScreenPilotDto>> GetAllAsync(int? statusId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all pilots as IQueryable for efficient database-level pagination
    /// </summary>
    IQueryable<ScreenPilotDto> GetAllQueryable(int? statusId = null);
    
    // Command methods (work with entities)
    Task<ScreenPilotEntity?> GetEntityByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ScreenPilotEntity> CreateAsync(ScreenPilotEntity screenPilot, CancellationToken cancellationToken = default);
    Task<ScreenPilotEntity> UpdateAsync(ScreenPilotEntity screenPilot, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
