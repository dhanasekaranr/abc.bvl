using abc.bvl.AdminTool.Contracts.ScreenPilot;
using ScreenPilotEntity = abc.bvl.AdminTool.Domain.Entities.ScreenPilot;

namespace abc.bvl.AdminTool.Application.Common.Interfaces;

/// <summary>
/// Repository interface for ScreenPilot operations
/// </summary>
public interface IScreenPilotRepository
{
    // Query methods (return DTOs)
    Task<IEnumerable<ScreenPilotDto>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<ScreenPilotDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    
    // Command methods (work with entities)
    Task<ScreenPilotEntity?> GetEntityByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ScreenPilotEntity> CreateAsync(ScreenPilotEntity screenPilot, CancellationToken cancellationToken = default);
    Task<ScreenPilotEntity> UpdateAsync(ScreenPilotEntity screenPilot, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
