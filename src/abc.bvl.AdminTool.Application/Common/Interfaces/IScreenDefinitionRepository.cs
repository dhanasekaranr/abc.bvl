using abc.bvl.AdminTool.Contracts.Common;
using abc.bvl.AdminTool.Contracts.ScreenDefinition;
using ScreenDefinitionEntity = abc.bvl.AdminTool.Domain.Entities.ScreenDefinition;

namespace abc.bvl.AdminTool.Application.Common.Interfaces;

public interface IScreenDefinitionRepository
{
    // Query methods (return DTOs)
    Task<IEnumerable<ScreenDefnDto>> GetAllAsync(byte? status = null, CancellationToken cancellationToken = default);
    Task<ScreenDefnDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ScreenDefnDto>> GetPagedAsync(byte? status, string? searchTerm, PaginationRequest pagination, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(byte? status, string? searchTerm, CancellationToken cancellationToken = default);
    
    // Command methods (work with entities)
    Task<ScreenDefinitionEntity?> GetEntityByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ScreenDefinitionEntity> CreateAsync(ScreenDefinitionEntity screenDefinition, CancellationToken cancellationToken = default);
    Task<ScreenDefinitionEntity> UpdateAsync(ScreenDefinitionEntity screenDefinition, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}