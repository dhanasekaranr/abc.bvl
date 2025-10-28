using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Contracts.ScreenDefinition;
using MediatR;

namespace abc.bvl.AdminTool.Application.ScreenDefinition.Commands;

/// <summary>
/// Handler for upserting (create or update) a screen definition
/// Determines operation type based on data existence in repository, not just ID presence
/// This prevents data integrity issues and keeps controller thin
/// </summary>
public class UpsertScreenDefinitionHandler : IRequestHandler<UpsertScreenDefinitionCommand, ScreenDefnDto>
{
    private readonly IScreenDefinitionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpsertScreenDefinitionHandler(
        IScreenDefinitionRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ScreenDefnDto> Handle(UpsertScreenDefinitionCommand request, CancellationToken cancellationToken)
    {
        // Execute within transaction
        var result = await _unitOfWork.ExecuteAsync(async (ctx, ct) =>
        {
            Domain.Entities.ScreenDefinition entity;

            // Determine if this is create or update based on actual data existence
            if (request.Id.HasValue && request.Id.Value > 0)
            {
                var existingEntity = await _repository.GetEntityByIdAsync(request.Id.Value, ct);
                
                if (existingEntity != null)
                {
                    // UPDATE: Entity exists in database
                    entity = existingEntity;
                    
                    // Update properties
                    entity.Name = request.ScreenName;
                    entity.Status = request.Status;
                    entity.UpdateAuditFields(request.RequestedBy);
                    
                    await _repository.UpdateAsync(entity, ct);
                }
                else
                {
                    // CREATE: ID provided but doesn't exist (data integrity issue caught!)
                    entity = CreateNewEntity(request);
                    await _repository.CreateAsync(entity, ct);
                }
            }
            else
            {
                // CREATE: No ID provided
                entity = CreateNewEntity(request);
                await _repository.CreateAsync(entity, ct);
            }

            await ctx.SaveChangesAsync(ct);
            
            return entity;
        }, cancellationToken);

        // Map to DTO
        return new ScreenDefnDto(
            Id: result.Id,
            Name: result.Name,
            Status: result.Status,
            CreatedAt: new DateTimeOffset(result.CreatedAt, TimeSpan.Zero),
            CreatedBy: result.CreatedBy,
            UpdatedAt: new DateTimeOffset(result.UpdatedAt, TimeSpan.Zero),
            UpdatedBy: result.UpdatedBy
        );
    }

    /// <summary>
    /// Helper method to create a new entity with proper defaults
    /// </summary>
    private Domain.Entities.ScreenDefinition CreateNewEntity(UpsertScreenDefinitionCommand request)
    {
        return new Domain.Entities.ScreenDefinition
        {
            Name = request.ScreenName,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.RequestedBy,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = request.RequestedBy
        };
    }
}
