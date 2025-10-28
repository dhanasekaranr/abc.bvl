using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Contracts.ScreenPilot;
using MediatR;

namespace abc.bvl.AdminTool.Application.ScreenPilot.Commands;

/// <summary>
/// Handler for upserting screen pilot assignments
/// </summary>
public class UpsertScreenPilotHandler : IRequestHandler<UpsertScreenPilotCommand, ScreenPilotDto>
{
    private readonly IScreenPilotRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpsertScreenPilotHandler(
        IScreenPilotRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ScreenPilotDto> Handle(UpsertScreenPilotCommand request, CancellationToken cancellationToken)
    {
        var result = await _unitOfWork.ExecuteAsync(async (ctx, ct) =>
        {
            Domain.Entities.ScreenPilot entity;

            // Determine create vs update based on data existence
            if (request.Id.HasValue && request.Id.Value > 0)
            {
                var existingEntity = await _repository.GetEntityByIdAsync(request.Id.Value, ct);

                if (existingEntity != null)
                {
                    // UPDATE
                    entity = existingEntity;
                    entity.ScreenDefnId = request.ScreenDefnId;
                    entity.UserId = request.UserId;
                    entity.Status = request.Status;
                    entity.UpdateAuditFields(request.RequestedBy);
                    
                    await _repository.UpdateAsync(entity, ct);
                }
                else
                {
                    // CREATE (ID provided but doesn't exist)
                    entity = CreateNewEntity(request);
                    await _repository.CreateAsync(entity, ct);
                }
            }
            else
            {
                // CREATE (No ID)
                entity = CreateNewEntity(request);
                await _repository.CreateAsync(entity, ct);
            }

            await ctx.SaveChangesAsync(ct);

            return entity;
        }, cancellationToken);

        // Load screen definition for response
        var dto = await _repository.GetByIdAsync(result.Id, cancellationToken);
        return dto!;
    }

    private static Domain.Entities.ScreenPilot CreateNewEntity(UpsertScreenPilotCommand request)
    {
        return new Domain.Entities.ScreenPilot
        {
            ScreenDefnId = request.ScreenDefnId,
            UserId = request.UserId,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.RequestedBy,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = request.RequestedBy
        };
    }
}
